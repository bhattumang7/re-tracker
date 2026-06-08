# Rescan & Position Reconciliation

When a function is renamed or refactored in Ghidra, the decompiled source file changes. Every function below the edit shifts to a new line. This document explains how re-tracker keeps line/column numbers accurate across those changes.

---

## How positions are stored

Each `Method` record stores two spans:

- **Declaration span** — `StartLine / StartColumn` → `EndLine / EndColumn` (the signature)
- **Body span** — `BodyStartLine / BodyStartColumn` → `BodyEndLine / BodyEndColumn` (opening to closing brace)

`MethodParameter` and `TrackedClass` each store their own span. `MethodCall` stores `CallLine / CallColumn` for every call site.

---

## Triggering a rescan

```
re-tracker scan --path <file-or-directory>
```

Or via the API:

```
POST /api/projects/{id}/scan
```

The scan re-parses every source file and runs `ReconcileFileAsync` to sync the database.

---

## How methods are matched during reconciliation

When a file is rescanned, the reconciler must decide which existing database record corresponds to each parsed symbol. It uses a **three-tier match**, tried in order:

### Tier 1 — Original name
Matches `existing.OriginalName == parsed.Name`.

Covers the common case: the method has never been renamed in Ghidra, so the name in the file is still the original decompiler name (`sub_1234`, etc.). Positions are updated if they shifted.

### Tier 2 — Current name
Matches `existing.CurrentName == parsed.Name`.

Covers the renamed case: the developer has already logged a rename in the tracker (`CurrentName = "processPacket"`), and the Ghidra source now reflects that name. The tracker record — its status, comments, and rename history — is preserved. Only positions are updated.

### Tier 3 — Position proximity (±5 lines)
Falls back to `|existing.StartLine - parsed.StartLine| <= 5`.

Catches methods that shifted in the file due to edits above them but whose name did not appear in either tier (e.g., a method whose name is still ambiguous). Only fires when **exactly one** candidate falls within the tolerance, to avoid false matches.

---

## What happens after matching

| Outcome | Action |
|---|---|
| Match found, positions unchanged | No-op |
| Match found, positions changed | All 8 position fields updated; a `RenameHistory` entry is written recording the old and new line numbers |
| No match found | Treated as a new method; a fresh record is inserted with `OriginalName = CurrentName = parsed.Name` |
| Existing record not matched by anything | Soft-deleted (`RemovedAt` set); history is preserved |

---

## Recommended workflow after a Ghidra rename

1. Rename the function in Ghidra (the source file changes).
2. Mark it in the tracker:
   ```
   re-tracker done <original-name> --comment "renamed to processPacket"
   ```
3. Rescan the file:
   ```
   re-tracker scan --path <file>
   ```

On rescan, the renamed function is matched via **Tier 2** (its `CurrentName` now matches what the parser sees). Every other function in the file is matched via **Tier 1** or **Tier 3** and has its positions updated automatically.

---

## Position change history

Every time a method's line numbers change during a rescan, a `RenameHistory` row is written:

| Field | Value |
|---|---|
| `OldName` / `NewName` | The method's current name (unchanged — this is a position event, not a rename event) |
| `OldStartLine` / `OldStartColumn` | Position before the rescan |
| `NewStartLine` / `NewStartColumn` | Position after the rescan |
| `Comment` | `"Line position updated by rescan"` |
