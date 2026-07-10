using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public sealed class GraphicsResetResult
    {
        public IList<string> Messages { get; } = new List<string>();
        public IList<string> Warnings { get; } = new List<string>();
        public bool Success { get; set; }
    }

    /// <summary>
    /// Restores ETS2/ATS display/graphics settings to single-monitor defaults
    /// and removes TruckDeck multimon overrides.
    /// </summary>
    public static class GraphicsResetService
    {
        static readonly (string Key, string Value)[] DefaultDisplaySettings =
        {
            ("r_multimon_mode", "0"),
            ("r_multimon_fov_horizontal", "50"),
            ("r_multimon_fov_vertical", "0"),
            ("r_fullscreen", "1"),
            ("r_fullscreen_borderless", "0"),
            ("r_windowed_borderless", "0"),
            ("r_minimize", "0"),
            ("r_output", "-1"),
            ("g_interior_camera_zero_pitch", "0")
        };

        public static GraphicsResetResult ResetToDefaults(GameTarget target, bool allowWhileGameRunning = false)
        {
            var result = new GraphicsResetResult();

            if (!allowWhileGameRunning && GameProcessGuard.IsGameRunning(out var runningGame))
            {
                result.Warnings.Add($"Close {runningGame} before resetting graphics settings.");
                result.Success = false;
                return result;
            }

            foreach (var gameFolder in GameDocumentsService.ResolveTargetFolders(target))
            {
                GameDocumentsService.EnsureGameFolderExists(gameFolder);
                ResetFolder(gameFolder, result);
            }

            if (result.Messages.Count == 0 && result.Warnings.Count == 0)
                result.Messages.Add("Nothing to reset — no game config folder found.");

            result.Success = true;
            foreach (var warning in result.Warnings)
            {
                if (warning.StartsWith("Close ", StringComparison.Ordinal))
                {
                    result.Success = false;
                    break;
                }
            }

            return result;
        }

        static void ResetFolder(string gameFolder, GraphicsResetResult result)
        {
            var siiPath = GameDocumentsService.GetMultimonConfigPath(gameFolder);
            var cfgPath = GameDocumentsService.GetConfigCfgPath(gameFolder);

            if (File.Exists(siiPath))
            {
                var backup = ConfigBackupService.BackupIfExists(siiPath);
                File.Delete(siiPath);
                result.Messages.Add($"Removed {siiPath}");
                if (backup != null)
                    result.Messages.Add($"Backup: {backup}");
            }
            else
            {
                result.Messages.Add($"No multimon_config.sii in {gameFolder} (already clear).");
            }

            if (!File.Exists(cfgPath))
            {
                result.Warnings.Add($"config.cfg not found for {gameFolder} — skipped.");
                return;
            }

            var cfgBackup = ConfigBackupService.BackupIfExists(cfgPath);
            var lines = new List<string>(File.ReadAllLines(cfgPath));

            foreach (var setting in DefaultDisplaySettings)
                UpsertSetting(lines, setting.Key, setting.Value);

            // Leave resolution alone if present — user may prefer their native size.
            // Only clear forced virtual-desktop sizes by not rewriting width/height.

            File.WriteAllLines(cfgPath, lines, Encoding.UTF8);
            result.Messages.Add($"Reset display settings in {cfgPath} (single-monitor defaults).");
            if (cfgBackup != null)
                result.Messages.Add($"Backup: {cfgBackup}");
        }

        static void UpsertSetting(IList<string> lines, string key, string value)
        {
            var pattern = new Regex(
                @"^\s*uset\s+" + Regex.Escape(key) + @"\s+""[^""]*""\s*$",
                RegexOptions.IgnoreCase);
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
