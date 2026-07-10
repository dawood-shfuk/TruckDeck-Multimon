# TruckDeck Multimon

Portable Windows wizard for **Euro Truck Simulator 2** and **American Truck Simulator** multi-monitor layouts. Writes `multimon_config.sii` and patches `config.cfg` so the game renders all camera views.

Companion to [TruckDeck](https://truckdeck.site) telemetry — **does not require TruckDeck.exe**.

**Current version: 1.2.0.5**

## Download

| Source | Link |
|--------|------|
| **TruckDeck site** (recommended) | [truckdeck.site/downloads](https://truckdeck.site/downloads#multimon) |
| **Direct ZIP** | [TruckDeck-Multimon-1.2.0.5.zip](https://truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip) |
| **GitHub release** | [v1.2.0.5](https://github.com/dawood-shfuk/TruckDeck-Multimon/releases/tag/v1.2.0.5) |
| **PiP guide** | [truckdeck.site/multimon](https://truckdeck.site/multimon) |

**Install:** unzip anywhere → run `TruckDeckMultimon.exe` → close the game → **Apply & Launch**.

**SHA-256:** `ad05791892c16c15d36b3d228fad602d0ebe9bb99757450b0488fd140d6b80cb`

## What is PiP?

**PiP** means **Picture-in-Picture** (not Python’s `pip` package tool).

In ETS2/ATS there is still only **one** game window. Multimon lays out camera regions inside that window — it does not create separate full-screen apps per monitor.

### PiP on MAIN (recommended for mixed-size monitors)

- The game runs at your **primary (MAIN) monitor’s native resolution** (for example **3440×1440**).
- **Center** fills the windshield view.
- **Left**, **Right**, and **Mirror** are free-placed, resizable inset panels on that same screen.
- Use the camera arrows to tweak **heading**, **pitch**, and **zoom (FOV)**, then Apply (game closed) and relaunch.
- Best when you want side/mirror cameras **without** stretching one giant mismatched canvas across stacked monitors of different sizes.

SCS allows up to **four** `monitor_config` regions: Center + up to three PiP panels.

### Full-span dual (classic stacked)

- Bottom monitor = center road view; top monitor = split side windows.
- The game window spans the whole Windows virtual desktop (for example **3440×2520**).
- Best when you want real second-screen views on a physical top monitor.
- **NVIDIA Surround** or **AMD Eyefinity** is recommended so Windows presents one virtual desktop to the game.

## Quick start

1. Download and unzip **TruckDeck-Multimon-1.2.0.5.zip**.
2. Run `TruckDeckMultimon.exe`.
3. **Close ETS2/ATS** if running (Apply is blocked while the game is open).
4. **PiP on MAIN:** open the **PiP on MAIN** tab → enable → place panels → adjust camera → **Apply & Launch**.
5. **Stacked dual:** Layout / Screens → Stacked preset → Split 2 on the top monitor → **Apply & Launch**.
6. Do **not** change in-game **Display** options afterward — that resets multimon.
7. Optional in-game fine-tune (developer console): `multimon set …` then `multimon save`.

## Features (1.2.0.5)

- **PiP on MAIN** — native MAIN resolution + free-place side cameras on the same screen
- Camera controls — heading / pitch / FOV zoom; load saved cameras from existing `multimon_config.sii`
- Full-span stacked dual-monitor presets (bottom center, top Split 2)
- Built-in presets for 2–6 screens (dual, triple, quad, five, six-surround, stacked)
- Modern UI with **Apply** / **Apply & Launch**
- Portable — no installer; does not require TruckDeck.exe

## Prerequisites

- Windows 10/11
- .NET Framework 4.8 (included on current Windows)
- Euro Truck Simulator 2 and/or American Truck Simulator
- Surround/Eyefinity recommended for full-span dual layouts

See [docs/DUAL_MONITOR_GUIDE.md](docs/DUAL_MONITOR_GUIDE.md) for architecture and troubleshooting.

## Presets

| Preset | Screens | Layout |
|--------|---------|--------|
| Dual — screen 1 center / screen 2 side windows | 2 | Windshield + L/R window views |
| Dual — screen 1 side windows / screen 2 center | 2 | Side windows left, center right |
| Stacked — bottom center / top split | 2 | Full-span dual stacked |
| Triple Front | 3 | Left / center / right |
| Quad — L/C/R + Aux | 4 | Triple + dashboard/aux |
| 4 screens — bottom center + side window screen | 4 | 2×2 layout |
| Five — L/C/R + 2 Aux | 5 | Experimental |
| Six Surround | 6 | Experimental surround |

Edit JSON files under [`Presets/`](Presets/) to add custom templates.

## What gets written

| File | Action |
|------|--------|
| `Documents\Euro Truck Simulator 2\multimon_config.sii` | Generated viewport layout |
| `Documents\Euro Truck Simulator 2\config.cfg` | `r_multimon_mode`, resolution, interior camera flags |
| Same paths under `American Truck Simulator\` | When ATS or Both is selected |

Timestamped `.bak.*` copies are created before overwrite.

## Build from source

```powershell
cd TruckDeck.Multimon
.\build\build.ps1
```

Output: `dist\Release\TruckDeckMultimon.exe` and `dist\Release\Presets\`.

## Troubleshooting

- **Distorted side views** — adjust heading in-game or use `multimon` console commands; run `multimon save` before exit.
- **Small viewports with black borders** — re-run Apply (sets `r_mode_width` / `r_mode_height` to virtual desktop size). Fully quit and restart the game.
- **More than 4 viewports** — SCS documents up to 4; 5–6 presets are experimental.

## Links

- [TruckDeck downloads](https://truckdeck.site/downloads)
- [TruckDeck Multimon page](https://truckdeck.site/multimon)
- [SCS multi-monitor documentation](https://modding.scssoft.com/wiki/Documentation/Engine/Multi_monitor_configuration)
- [TruckDeck](https://truckdeck.site)
