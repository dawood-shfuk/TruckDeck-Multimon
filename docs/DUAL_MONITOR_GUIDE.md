# TruckDeck Multimon — dual monitor guide

## How ETS2 multi-monitor actually works

ETS2 does **not** drive separate Windows displays independently. Your GPU must present **one virtual desktop** (extended desktop or NVIDIA Surround / AMD Eyefinity). The game opens **one window** at the combined resolution (e.g. `3840×1080` for two 1080p panels).

`multimon_config.sii` places **viewports** on that virtual canvas using normalized coordinates:

| Field | Meaning |
|-------|---------|
| `normalized_x`, `normalized_y` | Bottom-left corner of the viewport (0–1) |
| `normalized_width`, `normalized_height` | Size of the viewport (0–1) |
| `heading_offset` | Camera yaw — `0` = forward, `±65` = side **window** (junction traffic) |

Each **physical monitor** is a region of that canvas. Each **viewport** is a camera rendered into a sub-region.

## Stacked monitors (top + bottom)

When one monitor sits **above** another (not side-by-side), ETS2 windowed mode maps viewports from the **top** of the canvas downward. TruckDeck uses a corrected Y axis for stacked layouts:

```
┌─────────────────────────┐
│  TOP monitor            │
│  Left window │ R window │  ← split side views (junction traffic)
├─────────────────────────┤
│  BOTTOM monitor         │
│       Center cam        │  ← main road view + UI
└─────────────────────────┘
```

Use preset **Stacked — bottom center / top side windows** or drag **Center** onto the bottom monitor and **L+R split** onto the top monitor.

## Correct dual-monitor layout (center + side windows) — side by side

For junction traffic — looking **through the side windows**, not mirror reflections:

```
┌─────────────────┬─────────────────┐
│   SCREEN 1      │   SCREEN 2      │
│   Center cam    │ L window│R window│
│   (windshield)  │ (split) │        │
└─────────────────┴─────────────────┘
```

**Screen 1** = one viewport, role **Center**  
**Screen 2** = split: **Left window** + **Right window** (roles `Left` / `Right`, not `MirrorLeft` / `MirrorRight`)

| Role | What you see |
|------|----------------|
| **Center** | Forward windshield |
| **Left window** | Look out driver-side window (~65° left) — junction traffic |
| **Right window** | Look out passenger-side window (~65° right) |
| **Left/Right mirror** | Mirror reflection (optional — still in UI if you want mirrors) |

## Presets

| Preset | Use when |
|--------|----------|
| **Dual — screen 1 center / screen 2 side windows** | 2 monitors — recommended |
| **Dual — screen 1 side windows / screen 2 center** | Side windows on left monitor |
| **4 screens — bottom center + side window screen** | 2×2 grid — IDE on top, game on bottom row |

## 2×2 grid (4 monitors)

```
┌──────────┬──────────┐
│ Unused   │ Unused   │  ← top row (IDE)
├──────────┼──────────┤
│ Center   │ L win│R win│  ← bottom row (game)
└──────────┴──────────┘
```

## Manual setup (2 monitors)

1. Screen 1: **Center**, Split off  
2. Screen 2: **Split L/R** → L half = **Left window**, R half = **Right window**  
3. **Apply & Launch**  
4. Fine-tune `heading_offset` in-game if junction view angle needs tweaking (`multimon save`)

## References

- [SCS multimon wiki](https://modding.scssoft.com/wiki/Documentation/Engine/Multi_monitor_configuration)
- [Roextended FOV / multimon calculator](https://roextended.ro/forum/viewtopic.php?t=2125)
