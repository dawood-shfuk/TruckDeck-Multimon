# TruckDeck Multimon

Windows configuration wizard for **ETS2** and **ATS** native multi-monitor setups. Generates `multimon_config.sii` and patches `config.cfg` — the game renders all camera views.

Companion to [TruckDeck](https://truckdeck.site) telemetry dashboards (separate tool, no TruckDeck server required).

## Prerequisites

- Windows 10/11
- .NET Framework 4.8 (included on current Windows)
- Euro Truck Simulator 2 and/or American Truck Simulator
- **NVIDIA Surround** or **AMD Eyefinity** (recommended) so Windows presents one virtual desktop to the game

See [docs/DUAL_MONITOR_GUIDE.md](docs/DUAL_MONITOR_GUIDE.md) for architecture and troubleshooting.

## Quick start

1. Enable Surround/Eyefinity for your monitor group (same refresh rate on all panels).
2. Build or run `TruckDeckMultimon.exe` (see Build below).
3. Pick a preset matching your screen count (2–6). For dual monitor: **screen 1 = Center**, **screen 2 = Split** with **Left window** + **Right window** (junction traffic — not mirrors).
4. Choose **ETS2**, **ATS**, or **Both**.
5. Click **Apply & Launch** (recommended) so config is written right before the game starts.
6. Do **not** change display settings in-game Options — that resets multimon.

## Presets

| Preset | Screens | Layout |
|--------|---------|--------|
| Dual — screen 1 center / screen 2 side windows | 2 | **Recommended** — windshield + L/R window views for junctions |
| Dual — screen 1 side windows / screen 2 center | 2 | Side windows left, center right |
| Dual — Main + Split Side | 2 | Same as screen1-center / screen2-side-windows |
| Triple Front | 3 | Left / center / right |
| Quad — L/C/R + Aux | 4 | Triple + dashboard/aux |
| **4 screens — bottom center + side window screen** | 4 | 2×2: top unused, bottom center + split side windows |
| Five — L/C/R + 2 Aux | 5 | Experimental (5 viewports) |
| Six Surround | 6 | Experimental surround |

Edit JSON files under [`Presets/`](Presets/) to add custom templates.

## What gets written

| File | Action |
|------|--------|
| `Documents\Euro Truck Simulator 2\multimon_config.sii` | Generated viewport layout |
| `Documents\Euro Truck Simulator 2\config.cfg` | `r_multimon_mode "4"`, `g_interior_camera_zero_pitch "1"`, `r_mode_width` / `r_mode_height` = virtual desktop size |
| Same paths under `American Truck Simulator\` | When ATS or Both is selected |

Timestamped `.bak.*` copies are created before overwrite.

## Build

```powershell
cd "L:\FUNBIT TS4 src\TruckDeck.Multimon"
.\build\build.ps1
```

Output: `dist\Release\TruckDeckMultimon.exe` and `dist\Release\Presets\`.

## Manual test checklist

- [ ] App launches and lists connected displays
- [ ] Preset dropdown filters by screen count
- [ ] Split toggle produces two viewports on one monitor in preview
- [ ] Apply creates backup files when configs already exist
- [ ] Apply blocked while `eurotrucks2.exe` or `amtrucks.exe` is running
- [ ] `config.cfg` patch is idempotent (run Apply twice)
- [ ] Triple preset `multimon_config.sii` has 3 `monitor_config` blocks with normalized widths summing to layout

## Troubleshooting

- **Distorted side views** — adjust heading in-game or enable `g_developer` / `g_console` and use `multimon` commands; run `multimon save` before exit.
- **Small viewports on one monitor with black borders** — re-run Apply (sets `r_mode_width` / `r_mode_height` to your full virtual desktop). Fully quit and restart the game. If still wrong, enable NVIDIA Surround / AMD Eyefinity.
- **More than 4 viewports** — SCS documents up to 4; 5–6 are community/experimental.

## Links

- [SCS multi-monitor documentation](https://modding.scssoft.com/wiki/Documentation/Engine/Multi_monitor_configuration)
- [TruckDeck](https://truckdeck.site)

## Future

- TruckDeck tray menu shortcut
- Inno Setup bundling with TruckDeck installer
