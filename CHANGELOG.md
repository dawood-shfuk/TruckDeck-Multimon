# Changelog

All notable changes to TruckDeck Multimon are documented in this file.

## [1.2.0.6] — 2026-07-15

### Fixed
- **PiP Left/Right camera headings** were inverted: panels set to the right side looked left in game (and vice versa). Default headings for Left / Right / Mirror Left / Mirror Right are corrected.
- Existing layouts may still store the old heading values — on the **PiP on MAIN** tab select each panel → **Reset camera**, then **Apply** (game closed).

### Packaging
- Portable ZIP: `TruckDeck-Multimon-1.2.0.6.zip`
- SHA-256: `d423ae7865fd3d9cb8c8e4b6d8391bb509a762a2fa4f3828afd13a311c5315b5`

## [1.2.0.5] — 2026-07-10

### Added
- Version bump and release packaging for site / GitHub distribution
- Window title branding for the Multimon wizard

### Notes
- PiP on MAIN free-place side cameras, stacked dual presets, and camera adjust controls from the prior release line

## [1.2.0.0] — 2026-07

### Added
- Initial public TruckDeck Multimon wizard for ETS2 / ATS
- PiP on MAIN (native primary resolution + free-place panels)
- Full-span stacked dual-monitor presets
- Built-in presets for 2–6 screens
- Apply / Apply & Launch workflow (game must be closed for Apply)
