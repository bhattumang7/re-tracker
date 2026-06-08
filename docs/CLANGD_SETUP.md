# clangd-rename MCP: Setup Notes

## Background Index Wait (Critical)

The original `WaitForServerReady` slept for only 1 second. For large C projects
(e.g. 8 files × ~1MB each), clangd takes 30–120s to build its background index.
Any rename before the index is ready only updates the single opened file — all
cross-file references are missed silently.

**Fix (applied):** A `$/progress` handler tracks `token=backgroundIndexProgress, kind=end`.
`WaitForServerReady` stays at 2s so Claude Code connects immediately. A new
`WaitForIndexReady` method blocks on the channel and is called inside `RenameSymbol`,
so the first rename simply waits rather than firing too early.

Files changed:
- `internal/lsp/client.go` — `bgIndexDone` channel + `bgIndexOnce`; `$/progress` handler; `WaitForIndexReady`; `WaitForServerReady` = 2s
- `internal/lsp/server-request-handlers.go` — `HandleProgress`
- `internal/tools/rename-symbol.go` — calls `client.WaitForIndexReady` before rename

After any source change, rebuild with:
```
go build -o C:\Users\Umang\go\bin\mcp-ls-rename.exe .
```
Then restart Claude Code so the MCP server picks up the new binary.

## Project .clangd Config

Add a `.clangd` file to any C project to speed up indexing:

```yaml
CompileFlags:
  Add: [-w, -std=gnu89]

Index:
  Background: Build
  StandardLibrary: No   # skip system headers — big speedup

Diagnostics:
  Suppress: '*'
```

## MCP Server Command

```
mcp-ls-rename.exe
  -workspace <project-dir>
  -lsp C:\Users\Umang\tools\clangd_22.1.0\bin\clangd.exe
  -- --compile-commands-dir=<project-dir>
     --query-driver=<mingw64/bin/*>
     --background-index
```

The `--background-index` flag on the clangd side and the `WaitForIndexReady`
call inside `RenameSymbol` must both be present for reliable cross-file renames.
