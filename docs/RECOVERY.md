# Making the Decompiled VBA6.DLL Compile — Full Recovery Report

This document records everything done to take the Ghidra-decompiled `VBA6.DLL`
source (8 files, ~368k lines) from **a stuck C++ build to a clean compile**, the
challenges encountered, and how each was solved.

**Result: 1,352 → 0 compile errors. All 8 `vba6_part*.c` compile to `.o`** with
`gcc -w -std=gnu89`. This is compile-to-object for analysis (not a linked/runnable
binary — addresses, globals and some bodies are decompiler artifacts).

Guiding rule throughout: **no shortcuts that fabricate or hide information.** Every
correction is evidence-based, reproducible, and recorded in an auditable tracking
file. Where there was genuinely no correct way without more information, the work
paused for a decision rather than guessing.

---

## 1. The first, decisive problem: wrong language

A previous attempt compiled the output as **C++** (`g++ -x c++ -fpermissive`) on top
of a ~2,500-line C++ compatibility layer (overloaded `undefined4` structs, etc.).
Part 1 alone had **964 errors** and was stuck.

**Challenge.** Ghidra output is C *pseudocode* with two properties C++ cannot
tolerate: (a) the same function is *defined* with one parameter count but *called*
with another (Ghidra infers signatures inconsistently), and (b) loose pointer
conversions everywhere. In C++, `f()`/`f(void)` means *exactly zero args*, so every
mismatched call becomes a hard `too many arguments` error that `-fpermissive`/`-w`
**cannot** suppress.

**Fix.** Compile as **C (`gcc -w -std=gnu89`)**. A quick experiment proved the point:
the same arity mismatch and pointer mismatch that are *hard errors* in C++ compile to
**zero errors** in C with `-w`. C is also the correct language — the decompiled code
uses no C++ features. The C++ layer was retired (kept in `_cpp_attempt_backup/`), and
a small C type shim (`vba_compat.h`) replaced it.

---

## 2. Architecture: a reproducible pipeline from an immutable reference

Rather than hand-edit 368k lines, the build is **regenerated** from the pristine
Ghidra output and only *faithful*, *recorded* transforms are applied.

| File | Role |
|---|---|
| `prepare_build.py` | Regenerates `vba6_part*.c` from the reference; applies all transforms; generates headers; splices recovered functions; applies signature/patch/localtype corrections. Idempotent. |
| `vba_compat.h` | C type shim (Ghidra types, Windows/OLE types, x87 macros). |
| `vba_protos.h` | **Generated** — prototypes derived *from the definitions* (so calls match by construction). |
| `vba_symbols.h` | **Generated** — externs for Ghidra global symbols, typed by usage. |
| `compile.py` / `build_all.py` | Compile one file / all files + categorize errors. |

Source of truth = the read-only reference decompilation in
`C:\projects\VB6_lsp\re_lab\ghidra_out\vba6_src` (identical to the repo's initial
commit). It is never modified.

---

## 3. Type-system problems (Ghidra types vs C/Windows headers)

**Ghidra primitive types undefined.** `undefined1/2/4/8`, `byte`, `uint`, `ushort`,
`code`, odd widths (`undefined3`, `uint3`, `sbyte`…). → Typedefs in `vba_compat.h`.
`code` is modelled as an **int-returning function type** so `(**(code**)x)(…)` calls
compile *and* their results can be used (a `void` return would be "void value used").

**OLE/COM types missing** (`BSTR`, `VARIANT`, `SAFEARRAY`, `OLECHAR`, `ITypeLib`…).
→ Caused by `WIN32_LEAN_AND_MEAN`; removed it and included `<oleauto.h>`/`<oaidl.h>`.

**`VARIANT` member layout** — code used `V.n1.n2.n3.*`, but mingw's default `tagVARIANT`
has no `n1`. → Define `NONAMELESSUNION`/`NONAMELESSSTRUCT` before the headers, which
selects the named-union SDK layout (432 "request for member" errors cleared at once).

**Windows structs referenced by bare tag** (`tagRECT`, `tagPOINT`, `_SYSTEMTIME`,
`_STARTUPINFOA`…). → `typedef struct tag tag;` aliases. (These also fixed a large
cascade of "request for member in non-struct" errors, since vars of unknown type had
defaulted to `int`.)

**Ghidra anonymous unions `_union_2683` / `_union_2685`.** The code accessed a VARIANT
value union both nested (`.n2.n3.vt`) and directly (`->iVal`, `->bstrVal`), and
*assigned* one (`*p = v.n1.n2.n3`). A look-alike struct won't type-check (C forbids
assignment between distinct union types).
→ `typedef __typeof__(((VARIANT*)0)->n1) _union_2683;` and
`__typeof__(((VARIANT*)0)->n1.n2.n3) _union_2685;` — alias the *exact* SDK types.

**`bool`, `FILE`, `tm`, `_func_5023`, `BADSPACEBASE`, `math.h` constants** — small
typedefs/includes. `BADSPACEBASE` is Ghidra's "unresolved address space" marker (the
access was already broken); giving the marker a type lets the surrounding code compile.

---

## 4. Global-symbol typing (`DAT_`, `PTR_`, `LAB_`, …)

**Challenge.** Thousands of `DAT_`/`PTR_`/`UNK_`/`LAB_` globals are used
*inconsistently* — the same kind of symbol is dereferenced (`*DAT_x`), called
(`(*DAT_x)(…)`), indexed (`DAT_x[i]`), AND used in integer arithmetic (`DAT_x & 1`).
No single C type satisfies all of those (this is exactly what the C++ overloads had
papered over).

**Fix.** `prepare_build.py` scans every symbol's *usage* and generates `vba_symbols.h`
with a type chosen per symbol:
- `(*S)(…)` → `extern code *S;` (function pointer)
- `*S` or `S[i]` → `extern uintptr_t *S;` (data pointer)
- otherwise → `extern GhidraGlobal S;` (`uintptr_t` scalar)

`LAB_` symbols used as values are safe to extern because C labels live in a separate
namespace from ordinary identifiers, so `goto LAB_x;` and a value `&LAB_x` coexist.

---

## 5. Syntactic artifacts in the decompiled text

Each is a faithful, mechanical transform in `prepare_build.py`:

| Artifact | Example | Fix |
|---|---|---|
| Sub-field access | `X._4_4_` | `*(undefined4*)((char*)&(X) + 4)` (general; a careful backward "lvalue capture" handles `arr[i]._N_N_`, `(expr)._N_N_`) |
| `(void)` params | `f(void)` | `f()` |
| value-returning `void` | `void f(){…}` used as a value | return type → `undefined4` (honest "unknown 4-byte return") |
| Malformed cast | `X.((uint*)&Field)[0]` | `((uint*)&X.Field)[0]` |
| Split return type | `undefined4\nFUN_x(…)` | joined to one line so def-detection/proto-gen see it (recovered 61 defs) |
| Pointer `case` labels | `case (undefined4*)0x1:` | `case 0x1:` (case values are integer constants) |
| Wrapped "decompile failed" marker | `// <… timeout` then a bare `>` | fold the stray `>` into the comment |
| `<>` in string-label symbol names | `PTR_s_..._<>_...` | `<>` → `__` (valid identifier) |
| `_exref` import markers | `CallWindowProcA_exref` | strip suffix → the real API name |
| Stray-underscore locals | `_param_1`, `_local_8` | de-underscore (the real declared local; never a legit standalone name in Ghidra) |

---

## 6. The big one: function arity (and a shortcut that was rejected)

**Challenge.** Hundreds of functions are *defined* with one parameter count but
*called* with a different one — the dominant error class (~1,000+ errors). The
fundamental constraint: for a function defined in the file being compiled, its
definition is authoritative, so **no prototype trick can hide a same-file call/def
arity mismatch** — only correcting the signature or the call can.

**The wrong turn (and why).** An early `fix_arity.py` auto-edited call sites to match
the definition: trimming "extra" args and *padding missing args with `0`*. It compiled
(it cleared the arity errors) but it was a **fabrication** — inventing argument values
and deleting real argument expressions. This was explicitly reverted: the source was
restored to pristine and `fix_arity.py` deleted.

**The right way — recover real signatures from evidence.** `analyze_arity.py` reads the
pristine reference and reports, per function, its definition arity vs the histogram of
argument counts at all its call sites. The *call-site consensus is the truth* (Ghidra
usually gets the per-call stack/register pushes right; it's the inferred definition
that's wrong). For each function the true signature was determined and recorded in
`vba6_signatures.tsv` (with confidence + rationale), applied by `prepare_build.py`:
- **Under-count** (callers pass more): widen the definition (add the real trailing params).
- **Variadic** (call counts vary widely, e.g. a `printf`-style function called with 2–27 args): declare `(named…, …)`.
- **Mis-modelled register/output params**: e.g. a function reading an uninitialized
  `in_ECX` is `__thiscall` — bind `in_ECX` as the leading parameter; a function that
  reads an uninitialized stack local and writes through it has a missed *output*
  parameter — promote that local to a parameter (keeping its name so the body is unchanged).

`triage.py` classifies each function's missing parameter by body evidence
(`&stack0xNN`, `in_stack_NN`, `in_ECX`, `unaff_retaddr`) to pick the right recovery.

Identified, real functions along the way (recorded in `vba6_names.txt`):
`_setjmp3` (the "VC20" jmp_buf cookie), `_snprintf`-style formatters, an assert reporter.

---

## 7. Conflicts, const-correctness, and mis-typed parameters

**Prototype/definition conflicts.** A generated `()` prototype is *incompatible* with a
definition taking *promotable* params (`short`/`char`/`float`). → `fix_conflicts.py`
read the compiler's reported signature and restored the matching prototype. (Later
superseded by generating prototypes directly from definitions.)

**API re-implementations clashing with the SDK.** `VBA6.DLL` defines its *own*
`SysFreeString`, `SysAllocStringLen`, `SysAllocStringByteLen`, `RtlUnwind`, which
conflict with the Windows headers. → Renamed the DLL's versions (definitions **and**
internal calls) with a `vba_` prefix; the SDK declarations are left untouched. These
are the only **breaking renames** (documented in `README.md`).

**Spurious `const` on written buffers.** Ghidra over-applies `const` to string pointers
the code writes through (`LPCWSTR`/`LPCSTR`/`LPCOLESTR`…). → Aliased those types to their
non-const forms (non-const is a strict superset; reads and API calls are unaffected).

**Mis-typed params/locals** (per-case, via `vba6_signatures.tsv` / `vba6_localtypes.tsv` /
`vba6_patches.tsv`): a `float` parameter actually used as an object pointer; a `short`
parameter reused as a dereferenced pointer; an `undefined4` local holding a `BSTR`; a
parameter reused to hold a `float` result (`*(float*)&p = …` stores the bits faithfully);
a pointer/integer DAT subtracted as an address (`(uintptr_t)`-cast at the site).

---

## 8. x87 floating-point (the gnarliest type work)

**Ghidra integer concat/extract macros undefined.** `CONCAT22`, `CONCAT46`, `SUB104`…
were implicit-int (silently wrong for >4-byte results, and caused array-assign errors).
→ Defined all used variants precisely in `vba_compat.h` (`CONCATab(hi,lo)` = high `a`
bytes + low `b` bytes; `SUBab(v,o)` = extract `b` bytes at offset `o`).

**`unkuint10`/`unkint10`/`unkbyte10`.** These are 10-byte values *bit-sliced* with `>>`,
so they must be integers, not floats. → `typedef unsigned __int128` (had been `long double`,
which can't be shifted).

**`NAN(x)`.** Ghidra uses `NAN(x)` as an *is-NaN predicate*, but `<math.h>` defines `NAN`
as a constant. → `#undef NAN` then `#define NAN(x) isnan(x)`.

**x87 value built in a byte array.** An x87 float-format routine built a 10-byte value via
`auStack_e = CONCAT46(...)` into a `byte[]`. → Retype that specific local to a scalar
`unkbyte10` (the localtype mechanism drops the array brackets).

---

## 9. The hardest class: arguments Ghidra *lost*, recovered via re-decompilation

**Challenge.** ~265 call sites passed fewer/more arguments than the (now-correct)
definitions — not because the definitions were wrong, but because Ghidra **lost the
real arguments** at those call sites (often register-passed values). Raw `objdump`
disassembly only recovered the ~10 cases with all-constant arguments; the rest carry
register/computed values whose source expression requires data-flow analysis — exactly
what Ghidra's decompiler does, and what produced the lossy output. Hand-guessing them
would have been the fabrication shortcut.

**Fix — let Ghidra recover them with the correct signatures.** The losses came from
Ghidra using *stale database signatures* for the callees. So:
1. `gen_sig_overrides.py` emits each lossy callee's correct arity (`sig_overrides.txt`)
   and the list of caller functions to re-decompile (`recover_callers.txt`).
2. `ghidra/recover_calls.java` (run **headless, read-only** against the analyzed project
   in `re_lab` — the project on disk is *not* modified) forces those callee arities and
   re-decompiles the callers; their corrected functions are written to
   `recovered/recovered.c`.
3. `prepare_build.py` splices each recovered function over the reference version (before
   transforms, skipping any Ghidra `<fail>` stub so a working reference version is kept).

Two rounds (accumulating overrides) restored Ghidra's data-flow-correct arguments —
constant *and* register-derived — at ~262 of the sites, taking the project to 14, then 3.

---

## 10. The one function Ghidra could not decompile — recovered, not stubbed

`FUN_0fafddd9` produced pure garbage (`auStack_e = CONCAT46(0xfafdded, …)` — concatenating
*code addresses*; `auStack_e = in_ST7`). It is an **x87 transcendental (`log`) intrinsic**
that takes its argument on the x87 register stack — and Ghidra had `conv=unknown` for it.

The recovery (the user insisted, correctly, that garbage was unacceptable):
1. `ghidra/dump_listing.java` produced the **exact disassembly** (the listing is reliable
   even when the decompiler fails), confirming the x87 nature and `conv=unknown`.
2. Setting the **calling convention to `__cdecl`** made Ghidra decompile it into real C —
   the `conv=unknown` was the entire cause of the garbage.
3. But forcing the *full* override set re-triggered Ghidra's "overlapping input varnodes"
   error on this function. So `ghidra/recover_fafddd9.java` forces **only** `FUN_0fbfa726`'s
   arity (the math error-handler it calls) + `__cdecl`, yielding clean C **with the full
   6-argument** `FUN_0fbfa726(uVar11, unaff_retaddr, param_1, param_2, param_3, param_4)`
   calls. That clean function was spliced in. No stub, no garbage.

`conv_overrides.txt` records the convention override so the whole recovery is reproducible.

---

## 11. Tooling built (all in the repo)

- **Build**: `compile.py`, `build_all.py`
- **Pipeline**: `prepare_build.py` (transforms, header generation, splicing, corrections)
- **Headers/shim**: `vba_compat.h` (+ generated `vba_protos.h`, `vba_symbols.h`)
- **Analysis**: `analyze_arity.py` (arity worklist), `triage.py` (classify missing params),
  `recover_args.py` (constant-arg recovery from the binary)
- **Ghidra re-decompilation**: `gen_sig_overrides.py`, `ghidra/recover_calls.java`,
  `ghidra/recover_fafddd9.java`, `ghidra/dump_listing.java`; inputs `sig_overrides.txt`,
  `conv_overrides.txt`, `recover_callers.txt`; output `recovered/recovered.c`
- **Auditable corrections**: `vba6_signatures.tsv` (signature corrections),
  `vba6_patches.tsv` (per-site source fixes), `vba6_localtypes.tsv` (local retypes),
  `vba6_names.txt` (identifications)

---

## 12. The journey (project-wide error count)

```
C++ approach .................. stuck (part1 alone: 964)
switch to C, type shim ........ ~1352
generated symbol externs ......  846
arity signatures (manual) .....  566
__thiscall / trailing params ..  455 → 407
const + conflicts + renames ...  312
undeclared / _exref / typos ...  296
x87 macros, NAN, unk*10 .......   25
local retypes, one-offs .......   16 → 3
Ghidra arg recovery ...........   55 → 14 → 3
FUN_0fafddd9 (__cdecl) ........    2 → 0   ✅
```

## 13. Limitations / scope

- **Compile-to-object only**, not linked. Many globals/addresses are decompiler
  placeholders; a working binary is out of scope.
- A handful of fixes are *interface-faithful* (correct arity/types) but cannot perfectly
  reconnect a value to the body where Ghidra lost the data flow; these are recorded with
  `MED` confidence and a rationale.
- The recovery depends on the read-only Ghidra project and the original `VBA6.DLL`
  binary; both are referenced, neither is modified.
