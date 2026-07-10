using System;
using System.Drawing;
using System.Windows.Forms;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public sealed class MultimonCfgPatchOptions
    {
        public int ModeWidth { get; set; }
        public int ModeHeight { get; set; }
        public bool Fullscreen { get; set; }
        public int OutputIndex { get; set; } = -1;
        public int WindowX { get; set; }
        public int WindowY { get; set; }
        public bool EnableDeveloperConsole { get; set; }
    }

    public static class ConfigCfgPatcher
    {
        public static void Patch(
            string configPath,
            MultimonCfgPatchOptions options = null,
            MultimonApplyPhase phase = MultimonApplyPhase.Driving)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                throw new ArgumentException("Config path is required.", nameof(configPath));

            var lines = System.IO.File.Exists(configPath)
                ? new System.Collections.Generic.List<string>(System.IO.File.ReadAllLines(configPath))
                : new System.Collections.Generic.List<string>();

            if (phase == MultimonApplyPhase.Menu)
            {
                UpsertSetting(lines, "r_multimon_mode", "0");
            }
            else
            {
                UpsertSetting(lines, "r_multimon_mode", "4");
                UpsertSetting(lines, "g_interior_camera_zero_pitch", "1");
                UpsertSetting(lines, "r_multimon_fov_horizontal", "0");
                UpsertSetting(lines, "r_multimon_fov_vertical", "0");
                UpsertSetting(lines, "r_multimon_interior_in_main", "1");
            }

            if (options != null && options.ModeWidth > 0 && options.ModeHeight > 0)
            {
                UpsertSetting(lines, "r_mode_width", options.ModeWidth.ToString());
                UpsertSetting(lines, "r_mode_height", options.ModeHeight.ToString());
                UpsertSetting(lines, "r_fullscreen", "0");
                UpsertSetting(lines, "r_output", options.OutputIndex.ToString());
                UpsertSetting(lines, "r_minimize", "0");
                UpsertSetting(lines, "r_windowed_borderless", "1");
                UpsertSetting(lines, "r_fullscreen_borderless", "1");

                if (options.EnableDeveloperConsole)
                {
                    UpsertSetting(lines, "g_developer", "1");
                    UpsertSetting(lines, "g_console", "1");
                }

                ToggleVsync(lines);
            }

            var directory = System.IO.Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            System.IO.File.WriteAllLines(configPath, lines, System.Text.Encoding.UTF8);
        }

        public static MultimonCfgPatchOptions CreateOptions(LayoutProfile profile)
        {
            if (profile == null)
                return null;

            var virtualBounds = profile.GameDesktopBounds.Width > 0 && profile.GameDesktopBounds.Height > 0
                ? profile.GameDesktopBounds
                : profile.VirtualDesktopBounds;
            if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0)
                return null;

            return new MultimonCfgPatchOptions
            {
                ModeWidth = virtualBounds.Width,
                ModeHeight = virtualBounds.Height,
                Fullscreen = false,
                OutputIndex = -1,
                WindowX = virtualBounds.Left,
                WindowY = virtualBounds.Top,
                EnableDeveloperConsole = DisplayLayoutHelper.CountActivePhysicalScreens(profile) > 1
            };
        }

        public static MultimonCfgPatchOptions CreateMenuOptions(LayoutProfile profile)
        {
            if (profile == null)
                return null;

            var primary = MenuLayoutHelper.GetPrimaryBounds(profile);
            if (primary.Width <= 0 || primary.Height <= 0)
                return null;

            return new MultimonCfgPatchOptions
            {
                ModeWidth = primary.Width,
                ModeHeight = primary.Height,
                Fullscreen = false,
                OutputIndex = -1,
                WindowX = primary.Left,
                WindowY = primary.Top,
                EnableDeveloperConsole = true
            };
        }

        static void ToggleVsync(System.Collections.Generic.IList<string> lines)
        {
            var current = ReadSetting(lines, "r_vsync");
            UpsertSetting(lines, "r_vsync", current == "1" ? "0" : "1");
        }

        static string ReadSetting(System.Collections.Generic.IList<string> lines, string key)
        {
            var pattern = new System.Text.RegularExpressions.Regex(
                @"^\s*uset\s+" + System.Text.RegularExpressions.Regex.Escape(key) + @"\s+""([^""]*)""\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var line in lines)
            {
                var match = pattern.Match(line);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        static void UpsertSetting(System.Collections.Generic.IList<string> lines, string key, string value)
        {
            var pattern = new System.Text.RegularExpressions.Regex(
                @"^\s*uset\s+" + System.Text.RegularExpressions.Regex.Escape(key) + @"\s+""[^""]*""\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var replacement = $"uset {key} \"{value}\"";

            for (var i = 0; i < lines.Count; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    lines[i] = replacement;
                    return;
                }
            }

            lines.Add(replacement);
        }
    }
}
