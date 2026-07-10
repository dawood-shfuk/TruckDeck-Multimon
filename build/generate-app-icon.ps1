# Generates TruckDeck Multimon app.ico — 2x2 quad monitor logo.
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$outPath = Join-Path $root "src\TruckDeck.Multimon\Resources\app.ico"
$outDir = Split-Path -Parent $outPath
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

Add-Type -AssemblyName System.Drawing

$iconCode = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class MultimonIconGenerator
{
    static readonly Color Bg = Color.FromArgb(7, 8, 10);
    static readonly Color Panel = Color.FromArgb(28, 38, 18);
    static readonly Color Accent = Color.FromArgb(182, 255, 31);
    static readonly Color Line = Color.FromArgb(29, 35, 22);
    static readonly Color Side = Color.FromArgb(80, 160, 255);
    static readonly Color SideAlt = Color.FromArgb(255, 160, 80);

    public static void Generate(string path)
    {
        var sizes = new[] { 16, 32, 48, 64, 128, 256 };
        var images = sizes.Select(Draw).ToList();
        SaveIco(images, path);
        foreach (var img in images) img.Dispose();
    }

    static Bitmap Draw(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Bg);

            var pad = Math.Max(1, (int)(size * 0.10f));
            var gap = Math.Max(1, (int)(size * 0.05f));
            var grid = size - pad * 2;
            var cellW = (grid - gap) / 2;
            var cellH = (grid - gap) / 2;

            // 2x2 monitors — bottom row = game row (center + sides), top = aux/unused (quad preset).
            var rects = new[]
            {
                new Rectangle(pad, pad, cellW, cellH),                                     // TL unused
                new Rectangle(pad + cellW + gap, pad, cellW, cellH),                         // TR unused
                new Rectangle(pad, pad + cellH + gap, cellW, cellH),                       // BL center (main)
                new Rectangle(pad + cellW + gap, pad + cellH + gap, cellW, cellH)          // BR split side
            };

            var borderW = Math.Max(1f, size / 32f);

            for (var i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                if (r.Width < 2 || r.Height < 2) continue;

                var inset = Math.Max(0, (int)(borderW / 2));
                using (var fill = new SolidBrush(i == 3 ? SideAlt : (i == 2 ? Color.FromArgb(140, Accent) : Panel)))
                using (var pen = new Pen(i == 2 ? Accent : Line, borderW))
                {
                    var inner = Rectangle.Inflate(r, -inset, -inset);
                    if (inner.Width > 0 && inner.Height > 0)
                    {
                        g.FillRectangle(fill, inner);
                        g.DrawRectangle(pen, inner);
                    }
                }

                // Split line on bottom-right (L/R side windows).
                if (i == 3 && r.Width > 4)
                {
                    var midX = r.Left + r.Width / 2;
                    using (var splitPen = new Pen(Line, Math.Max(1f, borderW * 0.8f)))
                        g.DrawLine(splitPen, midX, r.Top + inset, midX, r.Bottom - inset);
                }
            }

            // Small "4" badge — quad layout hint.
            if (size >= 32)
            {
                var badgeSize = Math.Max(8, size / 5);
                var badge = new Rectangle(size - pad - badgeSize, pad, badgeSize, badgeSize);
                using (var badgeBg = new SolidBrush(Color.FromArgb(220, Bg)))
                using (var badgePen = new Pen(Accent, Math.Max(1f, borderW)))
                using (var font = new Font("Segoe UI", Math.Max(6f, badgeSize * 0.55f), FontStyle.Bold, GraphicsUnit.Pixel))
                using (var textBrush = new SolidBrush(Accent))
                {
                    g.FillEllipse(badgeBg, badge);
                    g.DrawEllipse(badgePen, badge);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("4", font, textBrush, badge, sf);
                }
            }
        }
        return bmp;
    }

    static void SaveIco(IList<Bitmap> images, string path)
    {
        var ordered = images.OrderByDescending(b => b.Width).ToList();
        var pngData = new List<byte[]>();
        foreach (var img in ordered)
        {
            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                pngData.Add(ms.ToArray());
            }
        }

        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)ordered.Count);

            var offset = 6 + 16 * ordered.Count;
            for (var i = 0; i < ordered.Count; i++)
            {
                var w = ordered[i].Width;
                var h = ordered[i].Height;
                bw.Write((byte)(w >= 256 ? 0 : w));
                bw.Write((byte)(h >= 256 ? 0 : h));
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write(pngData[i].Length);
                bw.Write(offset);
                offset += pngData[i].Length;
            }

            foreach (var data in pngData)
                bw.Write(data);
        }
    }
}
'@

if (-not ([System.Management.Automation.PSTypeName]'MultimonIconGenerator').Type) {
    Add-Type -TypeDefinition $iconCode -ReferencedAssemblies System.Drawing
}

[MultimonIconGenerator]::Generate($outPath)
Write-Host "Generated: $outPath"
