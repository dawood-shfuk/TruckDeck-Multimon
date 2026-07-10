# TruckDeck Multimon 1.2.0.5 — Agent Handoff (truckdeck.site download)

**For:** Agent deploying **TruckDeck Multimon** on **https://truckdeck.site**  
**App version:** **1.2.0.5**  
**GitHub repo:** https://github.com/dawood-shfuk/TruckDeck-Multimon  
**GitHub release:** https://github.com/dawood-shfuk/TruckDeck-Multimon/releases/tag/v1.2.0.5  
**Companion to:** TruckDeck telemetry (separate product — Multimon does **not** need TruckDeck.exe)

---

## Status (local)

| Step | Status |
|------|--------|
| App built / version **1.2.0.5** | Done |
| GitHub `main` pushed | Done |
| GitHub Release **v1.2.0.5** + zip asset | Done |
| Landing `DOWNLOADS` + PiP page copy (local tree) | Done — see `TruckDeck/build/landing/` |
| Upload zip to VPS `downloads/` | **Pending** (needs SSH/SCP) |
| Sync landing templates/CSS/app.py to VPS + restart | **Pending** |

---

## 1. What you are shipping

| Item | Value |
|------|--------|
| Product | TruckDeck Multimon |
| Version | 1.2.0.5 |
| Platform | Windows 10/11 · .NET Framework 4.8 |
| Download file | `TruckDeck-Multimon-1.2.0.5.zip` |
| Download id | `multimon` |
| Purpose | ETS2 / ATS multi-monitor layout wizard (`multimon_config.sii` + `config.cfg`) |

**User install:** unzip → run `TruckDeckMultimon.exe` (portable — no installer required).

### What PiP means (site copy)

**PiP = Picture-in-Picture** (not Python `pip`). Extra camera views as panels on the **MAIN** monitor at that screen’s **native resolution**, instead of one giant mismatched canvas across stacked monitors. Full-span dual remains available for real second-screen layouts.

Site anchors after deploy:

- https://truckdeck.site/downloads#multimon  
- https://truckdeck.site/downloads#pip  

---

## 2. Production paths (do not change)

```
Web root:  /var/www/veggrowing_g_usr/data/www/truckdeck.site/
Flask app: .../truckdeck.site/landing/
Downloads: .../truckdeck.site/downloads/
Service:   truckdeck-landing.service → gunicorn :25855
```

Static files under `downloads/` are served by nginx (`try_files`); Flask only lists them via `DOWNLOADS` in `landing/app.py`.

---

## 3. Upload the zip

Local package:

```
L:\FUNBIT TS4 src\TruckDeck.Multimon_release_1.2.0.5\TruckDeck-Multimon-1.2.0.5.zip
```

Also attached on GitHub Release **v1.2.0.5**.

Copy to VPS:

```
/var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip
```

Example:

```bash
scp TruckDeck-Multimon-1.2.0.5.zip \
  user@87.106.99.188:/var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/
```

Confirm:

```bash
ls -lh /var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip
sha256sum /var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip
```

Expected SHA-256 is in `SHA256.txt` in the release package (regenerate after any re-zip).

---

## 4. Sync landing (already prepared in repo)

From workspace **`TruckDeck/build/landing/`** → VPS **`.../truckdeck.site/landing/`**, at least:

| File | Change |
|------|--------|
| `app.py` | `DOWNLOADS` entry `id: multimon`, file `TruckDeck-Multimon-1.2.0.5.zip`, `github_url` to release |
| `templates/downloads.html` | Multimon card `#multimon`, PiP guide `#pip`, install notes |
| `templates/index.html` | Multimon feature card + links |
| `static/style.css` | `.version-tag`, `.multimon-guide`, `.multimon-steps` |

**Do not** bump TruckDeck `APP_VERSION` for this — Multimon is a separate download.

Example:

```bash
rsync -avz --exclude '.venv' --exclude '__pycache__' --exclude 'data/' \
  TruckDeck/build/landing/ \
  user@87.106.99.188:/var/www/veggrowing_g_usr/data/www/truckdeck.site/landing/
```

---

## 5. Restart landing

```bash
sudo systemctl restart truckdeck-landing.service
sudo systemctl status truckdeck-landing.service --no-pager
```

---

## 6. Verify

```bash
curl -sI https://truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip | head -n 5
curl -s https://truckdeck.site/downloads | grep -i Multimon
curl -s https://truckdeck.site/downloads | grep -i 'Picture-in-Picture'
curl -sI https://truckdeck.site/dl/multimon | head -n 5
```

In a browser:

1. Open https://truckdeck.site/downloads#multimon  
2. Confirm **TruckDeck Multimon** card shows size + SHA-256 + GitHub release button  
3. Open https://truckdeck.site/downloads#pip — PiP explanation visible  
4. Download → unzip → run `TruckDeckMultimon.exe`  
5. Title bar should show **TruckDeck Multimon 1.2.0.5**

Until the zip is on the VPS, the card still shows the **GitHub release** link so users can download.

---

## 7. Package contents (what users get)

```
TruckDeck-Multimon-1.2.0.5/
├── TruckDeckMultimon.exe
├── TruckDeckMultimon.exe.config   (if present)
├── Presets/                       (layout JSON presets)
├── Resources/app.ico              (if present)
├── VERSION.txt
├── README.txt
└── INSTALL.txt
```

---

## 8. Done when

- [x] GitHub Release **v1.2.0.5** with zip
- [x] Landing Multimon + PiP copy ready in `TruckDeck/build/landing/`
- [ ] Zip under `.../truckdeck.site/downloads/`
- [ ] Landing synced + service restarted
- [ ] `/downloads` shows Multimon with size + checksum
- [ ] Direct URL returns 200
- [ ] Unzipped exe runs and shows version **1.2.0.5**

---

## 9. Notes for future Multimon releases

1. Bump `AssemblyVersion` / `AssemblyFileVersion` in Multimon `Properties/AssemblyInfo.cs`
2. `.\build\build.ps1` → pack new `TruckDeck-Multimon-x.y.z.w.zip`
3. `gh release create vX.Y.Z.W …zip`
4. Upload new zip; update `file` / `version` / `github_url` in `DOWNLOADS` (keep `id: "multimon"` so counters stay continuous)
5. Restart landing

Source: https://github.com/dawood-shfuk/TruckDeck-Multimon
