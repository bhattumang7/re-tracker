#!/usr/bin/env python3
"""
clangd_callgraph.py — Harvest the function call graph for the decompiled VBA6
sources using clangd's semantic index (LSP call hierarchy), then POST the
caller->callee edges to re-tracker's /callgraph import endpoint.

Why LSP and not regex: clangd resolves real call targets (cross-file, through
the already-built background index), so the graph that drives re-tracker's
leaf-first "next" ordering is accurate rather than a textual approximation.

Pipeline:
  initialize clangd over stdio  ->  didOpen all 8 part files  ->  wait for the
  background index  ->  per file: documentSymbol (list functions + name
  positions)  ->  per function: prepareCallHierarchy + outgoingCalls  ->
  collect (caller, callee) name pairs  ->  POST to re-tracker.

Usage:
  python clangd_callgraph.py                  # full harvest + import
  python clangd_callgraph.py --files vba6_part0008.c   # one file (smoke test)
  python clangd_callgraph.py --max-funcs 50   # cap functions (smoke test)
  python clangd_callgraph.py --no-import      # write edges.json only

Env:
  RETRACKER_API   default http://localhost:5000/api
  CLANGD_EXE      default C:\\Users\\Umang\\tools\\clangd_22.1.0\\bin\\clangd.exe
"""
import argparse, json, os, subprocess, sys, threading, time, urllib.request

ROOT      = r"C:\projects\vba6_src"
CLANGD    = os.environ.get("CLANGD_EXE", r"C:\Users\Umang\tools\clangd_22.1.0\bin\clangd.exe")
API       = os.environ.get("RETRACKER_API", "http://localhost:5000/api")
PROJECT   = 1
ALL_FILES = [f"vba6_part{n:04d}.c" for n in range(1, 9)]
QUERY_DRIVER = "C:/Users/Umang/AppData/Local/Microsoft/WinGet/Packages/*/mingw64/bin/*"

SYMBOL_FUNCTION = 12   # LSP SymbolKind.Function


def log(*a):
    print(*a, file=sys.stderr, flush=True)


def path_to_uri(path):
    p = os.path.abspath(path).replace("\\", "/")
    return "file:///" + p


# ── Minimal LSP client over stdio ───────────────────────────────────────────

class Clangd:
    def __init__(self, exe):
        args = [
            exe,
            f"--compile-commands-dir={ROOT}",
            "--background-index",
            f"--query-driver={QUERY_DRIVER}",
            "--log=error",
        ]
        self.p = subprocess.Popen(
            args, cwd=ROOT,
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
        self._id = 0
        self._pending = {}                 # id -> result/error holder
        self._cv = threading.Condition()
        self._index_end = threading.Event()
        self._diags = set()                # uris that have published diagnostics
        self._alive = True
        threading.Thread(target=self._reader, daemon=True).start()

    # -- raw IO --
    def _send(self, msg):
        body = json.dumps(msg).encode("utf-8")
        header = f"Content-Length: {len(body)}\r\n\r\n".encode("ascii")
        self.p.stdin.write(header + body)
        self.p.stdin.flush()

    def _reader(self):
        f = self.p.stdout
        try:
            while True:
                # read headers
                headers = {}
                while True:
                    line = f.readline()
                    if not line:
                        self._alive = False
                        return
                    line = line.decode("ascii", "replace").strip()
                    if line == "":
                        break
                    if ":" in line:
                        k, v = line.split(":", 1)
                        headers[k.strip().lower()] = v.strip()
                n = int(headers.get("content-length", 0))
                body = b""
                while len(body) < n:
                    chunk = f.read(n - len(body))
                    if not chunk:
                        self._alive = False
                        return
                    body += chunk
                self._dispatch(json.loads(body.decode("utf-8")))
        except Exception as e:
            log(f"[reader] stopped: {e}")
            self._alive = False

    def _dispatch(self, msg):
        if "id" in msg and "method" not in msg:
            # response to one of our requests
            with self._cv:
                self._pending[msg["id"]] = msg
                self._cv.notify_all()
            return
        if "id" in msg and "method" in msg:
            # server -> client request: must reply or clangd may stall
            m = msg["method"]
            if m == "workspace/configuration":
                items = msg.get("params", {}).get("items", [])
                self._send({"jsonrpc": "2.0", "id": msg["id"], "result": [None] * len(items)})
            else:
                self._send({"jsonrpc": "2.0", "id": msg["id"], "result": None})
            return
        # notification
        method = msg.get("method")
        if method == "$/progress":
            val = msg.get("params", {}).get("value", {})
            tok = str(msg.get("params", {}).get("token", ""))
            if val.get("kind") == "end" and "index" in tok.lower():
                self._index_end.set()
        elif method == "textDocument/publishDiagnostics":
            self._diags.add(msg.get("params", {}).get("uri"))

    # -- requests / notifications --
    def request(self, method, params, timeout=120):
        with self._cv:
            self._id += 1
            rid = self._id
        self._send({"jsonrpc": "2.0", "id": rid, "method": method, "params": params})
        deadline = time.time() + timeout
        with self._cv:
            while rid not in self._pending:
                remaining = deadline - time.time()
                if remaining <= 0 or not self._alive:
                    raise TimeoutError(f"{method} timed out")
                self._cv.wait(remaining)
            msg = self._pending.pop(rid)
        if "error" in msg:
            raise RuntimeError(f"{method} error: {msg['error']}")
        return msg.get("result")

    def notify(self, method, params):
        self._send({"jsonrpc": "2.0", "method": method, "params": params})

    # -- lifecycle --
    def initialize(self):
        self.request("initialize", {
            "processId": os.getpid(),
            "rootUri": path_to_uri(ROOT),
            "capabilities": {
                "textDocument": {
                    "documentSymbol": {"hierarchicalDocumentSymbolSupport": True},
                    "callHierarchy": {"dynamicRegistration": False},
                },
                "window": {"workDoneProgress": True},
            },
        })
        self.notify("initialized", {})

    def did_open(self, path):
        uri = path_to_uri(path)
        with open(path, encoding="utf-8", errors="replace") as fh:
            text = fh.read()
        self.notify("textDocument/didOpen", {
            "textDocument": {"uri": uri, "languageId": "c", "version": 1, "text": text}})
        return uri

    def shutdown(self):
        try:
            self.request("shutdown", None, timeout=10)
            self.notify("exit", None)
        except Exception:
            pass
        try:
            self.p.terminate()
        except Exception:
            pass


# ── Harvest ─────────────────────────────────────────────────────────────────

def collect_functions(symbols, out):
    """Flatten a documentSymbol tree into (name, selectionRange.start) for functions."""
    for s in symbols or []:
        if s.get("kind") == SYMBOL_FUNCTION:
            sel = s.get("selectionRange", s.get("range", {}))
            start = sel.get("start")
            if start:
                out.append((s["name"], start))
        collect_functions(s.get("children"), out)


def harvest(files, max_funcs):
    cl = Clangd(CLANGD)
    log("initializing clangd ...")
    cl.initialize()

    uris = [cl.did_open(os.path.join(ROOT, f)) for f in files]
    log(f"opened {len(uris)} file(s); waiting for background index ...")
    # Index shards are already warm, so this is usually quick; cap the wait.
    if cl._index_end.wait(timeout=240):
        log("background index: ready")
    else:
        log("background index: wait timed out — proceeding (queries fall back to AST)")
    time.sleep(2)

    edges = set()
    total_funcs = 0
    for f, uri in zip(files, uris):
        funcs = []
        try:
            syms = cl.request("textDocument/documentSymbol", {"textDocument": {"uri": uri}})
            collect_functions(syms, funcs)
        except Exception as e:
            log(f"[{f}] documentSymbol failed: {e}")
            continue
        log(f"[{f}] {len(funcs)} functions")

        for name, pos in funcs:
            if max_funcs and total_funcs >= max_funcs:
                break
            total_funcs += 1
            try:
                items = cl.request("textDocument/prepareCallHierarchy",
                                   {"textDocument": {"uri": uri}, "position": pos}, timeout=60)
                if not items:
                    continue
                outs = cl.request("callHierarchy/outgoingCalls", {"item": items[0]}, timeout=60)
                for call in outs or []:
                    callee = call.get("to", {}).get("name")
                    if callee and callee != name:
                        edges.add((name, callee))
            except Exception as e:
                log(f"  ! {name}: {e}")
            if total_funcs % 250 == 0:
                log(f"  ... {total_funcs} functions, {len(edges)} edges so far")
        if max_funcs and total_funcs >= max_funcs:
            break

    cl.shutdown()
    log(f"harvest done: {total_funcs} functions, {len(edges)} distinct edges")
    return edges


def import_edges(edges):
    payload = json.dumps({"edges": [{"caller": c, "callee": e} for c, e in sorted(edges)]}).encode()
    req = urllib.request.Request(f"{API}/projects/{PROJECT}/callgraph",
                                 data=payload, method="POST",
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--files", nargs="*", default=ALL_FILES, help="part files to harvest")
    ap.add_argument("--max-funcs", type=int, default=0, help="cap functions (0 = all)")
    ap.add_argument("--no-import", action="store_true", help="write edges.json, skip POST")
    args = ap.parse_args()

    edges = harvest(args.files, args.max_funcs)

    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "edges.json")
    with open(out, "w") as fh:
        json.dump([{"caller": c, "callee": e} for c, e in sorted(edges)], fh)
    log(f"wrote {out}")

    if args.no_import:
        return
    result = import_edges(edges)
    log(f"import result: {result}")


if __name__ == "__main__":
    main()
