using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public sealed class ParsedMonitorConfig
    {
        public string Name { get; set; }
        public float NormalizedX { get; set; }
        public float NormalizedY { get; set; }
        public float NormalizedWidth { get; set; }
        public float NormalizedHeight { get; set; }
        public float HeadingOffset { get; set; }
        public float PitchOffset { get; set; }
        public float HorizontalFovOverride { get; set; }
    }

    /// <summary>
    /// Reads monitor_config blocks from multimon_config.sii (for importing saved camera tweaks).
    /// </summary>
    public static class MultimonConfigReader
    {
        static readonly Regex FloatField = new Regex(
            @"^\s*(normalized_x|normalized_y|normalized_width|normalized_height|heading_offset|pitch_offset|horizontal_fov_override):\s*([-\d.]+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex NameField = new Regex(
            @"^\s*name:\s*(\S+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IList<ParsedMonitorConfig> ParseFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return new List<ParsedMonitorConfig>();

            return Parse(File.ReadAllText(path));
        }

        public static IList<ParsedMonitorConfig> Parse(string content)
        {
            var result = new List<ParsedMonitorConfig>();
            if (string.IsNullOrWhiteSpace(content))
                return result;

            ParsedMonitorConfig current = null;
            var depth = 0;
            var inMonitor = false;

            foreach (var rawLine in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("monitor_config", StringComparison.OrdinalIgnoreCase))
                {
                    current = new ParsedMonitorConfig();
                    inMonitor = true;
                    depth = 0;
                    continue;
                }

                if (!inMonitor || current == null)
                    continue;

                if (line == "{")
                {
                    depth++;
                    continue;
                }

                if (line == "}")
                {
                    depth--;
                    if (depth <= 0)
                    {
                        if (!string.IsNullOrWhiteSpace(current.Name))
                            result.Add(current);
                        current = null;
                        inMonitor = false;
                    }
                    continue;
                }

                var nameMatch = NameField.Match(line);
                if (nameMatch.Success)
                {
                    current.Name = nameMatch.Groups[1].Value;
                    continue;
                }

                var floatMatch = FloatField.Match(line);
                if (!floatMatch.Success)
                    continue;

                if (!float.TryParse(floatMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    continue;

                switch (floatMatch.Groups[1].Value.ToLowerInvariant())
                {
                    case "normalized_x": current.NormalizedX = value; break;
                    case "normalized_y": current.NormalizedY = value; break;
                    case "normalized_width": current.NormalizedWidth = value; break;
                    case "normalized_height": current.NormalizedHeight = value; break;
                    case "heading_offset": current.HeadingOffset = value; break;
                    case "pitch_offset": current.PitchOffset = value; break;
                    case "horizontal_fov_override": current.HorizontalFovOverride = value; break;
                }
            }

            return result;
        }

        public static ViewportRole? RoleFromMonitorName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            switch (name.ToLowerInvariant())
            {
                case "center":
                case "middle":
                    return ViewportRole.Center;
                case "left":
                    return ViewportRole.Left;
                case "right":
                    return ViewportRole.Right;
                case "left_mirror":
                    return ViewportRole.MirrorLeft;
                case "right_mirror":
                    return ViewportRole.MirrorRight;
                case "aux":
                    return ViewportRole.Aux;
                default:
                    return null;
            }
        }

        public static int ImportIntoProfile(LayoutProfile profile, string configPath)
        {
            if (profile == null)
                return 0;

            var monitors = ParseFile(configPath);
            if (monitors.Count == 0)
                return 0;

            if (profile.MainPipPanels == null)
                profile.MainPipPanels = new List<MainPipPanel>();

            var imported = 0;
            foreach (var monitor in monitors)
            {
                var role = RoleFromMonitorName(monitor.Name);
                if (role == null || role == ViewportRole.Center || role == ViewportRole.Unused)
                    continue;

                var panel = profile.MainPipPanels.FirstOrDefault(p => p.Role == role.Value);
                if (panel == null)
                {
                    panel = new MainPipPanel { Role = role.Value };
                    profile.MainPipPanels.Add(panel);
                }

                panel.X = monitor.NormalizedX;
                panel.Y = monitor.NormalizedY;
                panel.Width = monitor.NormalizedWidth;
                panel.Height = monitor.NormalizedHeight;
                panel.HeadingOffset = monitor.HeadingOffset;
                panel.PitchOffset = monitor.PitchOffset;
                panel.HorizontalFovOverride = monitor.HorizontalFovOverride;
                panel.Clamp();
                imported++;
            }

            if (imported > 0)
                profile.UseMainPipMode = true;

            return imported;
        }
    }
}
