# re-tracker Design System
> Derived from GitHub's Primer CSS (https://primer.style)

---

## Spacing Scale

Base unit: **8px**

| Token | px | Usage |
|---|---|---|
| `$s1` | 4px | Icon gap, tight inline spacing |
| `$s2` | 8px | Small gaps, compact padding |
| `$s3` | 16px | Standard padding (box rows, form inputs) |
| `$s4` | 24px | Section spacing, spacious padding |
| `$s5` | 32px | Page section gaps |
| `$s6` | 40px | Large section gaps |

---

## Typography

Font family: `-apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", Helvetica, Arial, sans-serif`
Mono family: `ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, "Liberation Mono", monospace`

| Level | Size | Line-height | Weight | Usage |
|---|---|---|---|---|
| Display | 40px / 2.5rem | 1.375 | 500 | — |
| Title L | 32px / 2rem | 1.5 | 600 | H1 |
| Title M | 20px / 1.25rem | 1.625 | 600 | H2, page titles |
| Title S | 16px / 1rem | 1.5 | 600 | H3, card headers |
| Body L | 16px / 1rem | 1.5 | 400 | — |
| **Body M** | **14px / 0.875rem** | **1.5714 (22px)** | **400** | **Default body text** |
| Body S | 12px / 0.75rem | 1.6667 (20px) | 400 | Secondary text, metadata |
| Caption | 12px / 0.75rem | 1.25 | 400 | Labels, counters |
| Code | 13px / 0.8125rem | 1.5 | 400 | Monospace identifiers |

---

## Colors (Light Theme)

### Canvas
| Token | Value | Usage |
|---|---|---|
| `--color-canvas-default` | `#ffffff` | Page background |
| `--color-canvas-subtle` | `#f6f8fa` | Sidebar, box headers, table headers |
| `--color-canvas-inset` | `#f0f6fc` | Nested inset areas |

### Borders
| Token | Value |
|---|---|
| `--color-border-default` | `#d0d7de` |
| `--color-border-muted` | `#d8dee4` |

### Foreground
| Token | Value | Usage |
|---|---|---|
| `--color-fg-default` | `#1f2328` | Primary text |
| `--color-fg-muted` | `#656d76` | Secondary text, placeholders |
| `--color-fg-subtle` | `#6e7781` | Tertiary text |

### Accent / Interactive
| Token | Value |
|---|---|
| `--color-accent-fg` | `#0969da` |
| `--color-accent-emphasis` | `#0969da` |
| `--color-success-fg` | `#1a7f37` |
| `--color-success-emphasis` | `#1f883d` |
| `--color-danger-fg` | `#d1242f` |

### Status
| Status | Color | Meaning |
|---|---|---|
| Pending | `#6e7781` | Not started |
| InProgress | `#0969da` | Being worked on |
| NeedsReview | `#9a6700` | Awaiting review |
| Done | `#1a7f37` | Complete |
| Skipped | `#6e7781` | Intentionally skipped |
| Deferred | `#8250df` | Postponed |

---

## Component Specs

### Box
```
border: 1px solid #d0d7de
border-radius: 6px
background: #ffffff

.Box-header
  padding: 12px 16px
  background: #f6f8fa
  border-bottom: 1px solid #d0d7de
  font-size: 14px
  font-weight: 600
  border-radius: 6px 6px 0 0

.Box-row
  padding: 14px 16px           ← 14px vertical gives comfortable 42px+ rows
  border-bottom: 1px solid #d8dee4
  display: flex; align-items: center; gap: 12px
  min-height: 48px
  :hover → background: #f6f8fa
  :last-child → no border-bottom
```

### Table (gh-table)
```
th
  padding: 8px 16px
  font-size: 12px; font-weight: 600
  background: #f6f8fa
  color: #656d76

td
  padding: 12px 16px           ← was 8px, now 12px — more breathing room
  font-size: 14px
  border-bottom: 1px solid #d8dee4
  line-height: 1.5714          ← 22px effective

tr:hover td → background: #f6f8fa
```

### Button
```
.btn (default)
  padding: 5px 16px
  font-size: 14px; font-weight: 500
  border-radius: 6px
  border: 1px solid #d0d7de
  background: #f6f8fa
  color: #1f2328
  :hover → background: #f3f4f6, border-color: #1f2328

.btn-primary
  background: #1f883d
  border-color: #1f883d
  color: #ffffff
  :hover → background: #1a7f37

.btn-sm
  padding: 3px 12px
  font-size: 12px
```

### Form Control
```
padding: 5px 12px
font-size: 14px
line-height: 1.5714
border: 1px solid #d0d7de
border-radius: 6px
background: #ffffff
:focus → border-color: #0969da, box-shadow: 0 0 0 3px rgba(9,105,218,0.3)
```

### Status Label
```
padding: 0 7px
font-size: 12px; font-weight: 500
line-height: 20px
border: 1px solid <status-color>
border-radius: 2em
color: <status-color>
```

### Counter
```
padding: 0 6px
font-size: 12px; font-weight: 500
line-height: 18px
border-radius: 2em
background: rgba(175,184,193,0.2)
border: 1px solid #d0d7de
```

### Stat Card
```
border: 1px solid #d0d7de
border-radius: 6px
padding: 16px
background: #ffffff
.stat-num: 32px, font-weight: 600
.stat-lbl: 12px, color: #656d76, margin-top: 4px
```

### Progress Bar
```
height: 8px
border-radius: 6px
background: #d0d7de (track)
.inner: background: #1f883d
```

### Sidebar
```
width: 240px
background: #f6f8fa
border-right: 1px solid #d0d7de
padding: 16px 8px

.nav-link
  padding: 7px 8px             ← was 6px, now 7px for touch target
  font-size: 14px; font-weight: 500
  color: #1f2328
  border-radius: 6px
  gap: 8px
  :hover → background: rgba(208,215,222,0.32)
  .active → background: rgba(208,215,222,0.48); font-weight: 600

.sidebar-brand
  padding: 4px 8px 16px
  font-size: 15px; font-weight: 600
  border-bottom: 1px solid #d0d7de
  margin-bottom: 8px
```

### Flash / Toast
```
.flash-success
  background: #dafbe1
  border: 1px solid #2da44e
  color: #1a7f37
  padding: 8px 16px

.flash-error
  background: #ffebe9
  border: 1px solid #ff8182
  color: #d1242f
```

---

## Spacing Utilities

```
.mt-1  margin-top: 4px
.mt-2  margin-top: 8px
.mt-3  margin-top: 16px
.mt-4  margin-top: 24px
.mt-5  margin-top: 32px
.mb-1  margin-bottom: 4px
.mb-2  margin-bottom: 8px
.mb-3  margin-bottom: 16px
.mb-4  margin-bottom: 24px
.mb-5  margin-bottom: 32px
.gap-1 gap: 4px
.gap-2 gap: 8px
.gap-3 gap: 12px
.gap-4 gap: 16px
```

---

## Page Layout

```
body: background #ffffff

.layout
  display: flex
  height: 100vh
  overflow: hidden

.sidebar
  width: 240px; flex-shrink: 0

.main
  flex: 1
  overflow-y: auto
  padding: 24px 32px
  max-width: 1280px             ← cap so lines don't get too long on wide screens

.page-header
  padding-bottom: 16px
  margin-bottom: 24px           ← was 16px, more breathing room before content
  border-bottom: 1px solid #d0d7de
  display: flex; align-items: center; gap: 12px
```
