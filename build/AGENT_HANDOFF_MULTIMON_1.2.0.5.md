# TruckDeck Multimon 1.2.0.5 — Agent Handoff (truckdeck.site download)

**For:** Agent adding **TruckDeck Multimon** to **https://truckdeck.site** downloads  
**App version:** **1.2.0.5**  
**GitHub:** https://github.com/dawood-shfuk/TruckDeck-Multimon  
**Companion to:** TruckDeck telemetry (separate product — Multimon does **not** need TruckDeck.exe)

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

From this handoff package, copy:

```
TruckDeck-Multimon-1.2.0.5.zip
```

to VPS:

```
/var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip
```

Example:

```bash
scp TruckDeck-Multimon-1.2.0.5.zip \
  user@<vps>:/var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/
```

Confirm:

```bash
ls -lh /var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip
sha256sum /var/www/veggrowing_g_usr/data/www/truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip
```

Expected SHA-256 is in `SHA256.txt` in this package (regenerate after any re-zip).

---

## 4. Register the download in Flask (`landing/app.py`)

In `DOWNLOADS = [ ... ]`, **add** (after the existing setup/apk/mod entries):

```python
    {
        "id": "multimon",
        "title": "TruckDeck Multimon",
        "file": "TruckDeck-Multimon-1.2.0.5.zip",
        "description": "ETS2/ATS multi-monitor layout wizard — PiP on MAIN, stacked dual, presets. Portable ZIP (unzip and run).",
    },
```

Optional icon in `templates/downloads.html` (download-type-icon block):

```jinja
{% if d.id == 'setup' %}🪟{% elif d.id == 'apk' %}📱{% elif d.id == 'multimon' %}🖥️{% else %}📦{% endif %}
```

**Do not** bump TruckDeck `APP_VERSION` for this — Multimon is a separate download; main app version stays as-is.

---

## 5. Restart landing

```bash
sudo systemctl restart truckdeck-landing.service
sudo systemctl status truckdeck-landing.service --no-pager
```

---

## 6. Verify

```bash
# File reachable
curl -sI https://truckdeck.site/downloads/TruckDeck-Multimon-1.2.0.5.zip | head -n 5

# Downloads page lists Multimon
curl -s https://truckdeck.site/downloads | grep -i Multimon

# Waiting page / counter id
curl -sI https://truckdeck.site/dl/multimon | head -n 5
```

In a browser:

1. Open https://truckdeck.site/downloads  
2. Confirm **TruckDeck Multimon** card shows size + SHA-256  
3. Download → unzip → run `TruckDeckMultimon.exe`  
4. Title bar should show **TruckDeck Multimon 1.2.0.5**

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

- [ ] Zip is under `.../truckdeck.site/downloads/`
- [ ] `DOWNLOADS` entry `id: multimon` points at that filename
- [ ] Landing service restarted
- [ ] `/downloads` shows Multimon with size + checksum
- [ ] Direct URL returns 200
- [ ] Unzipped exe runs and shows version **1.2.0.5**

---

## 9. Notes for future Multimon releases

1. Bump `AssemblyVersion` / `AssemblyFileVersion` in Multimon `Properties/AssemblyInfo.cs`
2. `.\build\build.ps1` → pack new `TruckDeck-Multimon-x.y.z.w.zip`
3. Upload new zip; update `file` string in `DOWNLOADS` (keep `id: "multimon"` so download counters stay continuous)
4. Restart landing

Source: https://github.com/dawood-shfuk/TruckDeck-Multimon
