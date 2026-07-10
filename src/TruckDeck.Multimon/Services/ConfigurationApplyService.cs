using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public sealed class ApplyConfigurationResult
    {
        public IList<string> Messages { get; } = new List<string>();
        public IList<string> Warnings { get; } = new List<string>();
        public bool Success { get; set; }
    }

    public static class ConfigurationApplyService
    {
        public static ApplyConfigurationResult Apply(
            LayoutProfile profile,
            GameTarget target,
            MultimonApplyPhase phase = MultimonApplyPhase.Driving,
            bool allowWhileGameRunning = false)
        {
            var result = new ApplyConfigurationResult();
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (!allowWhileGameRunning && GameProcessGuard.IsGameRunning(out var runningGame))
            {
                result.Warnings.Add($"Close {runningGame} before applying configuration.");
                result.Success = false;
                return result;
            }

            DisplayLayoutHelper.FinalizeProfile(profile);

            IList<ViewportDefinition> viewports;
            MultimonCfgPatchOptions cfgOptions;

            if (phase == MultimonApplyPhase.Menu)
            {
                viewports = MenuLayoutHelper.CreateMenuViewports(profile);
                cfgOptions = ConfigCfgPatcher.CreateMenuOptions(profile);
            }
            else if (phase == MultimonApplyPhase.FloatingNativeMain)
            {
                viewports = NativeMainFloatingLayout.CreateViewports(profile);
                cfgOptions = NativeMainFloatingLayout.CreateCfgOptions(profile);
                // Game resolution = MAIN native only for this mode.
                var main = MenuLayoutHelper.GetPrimaryBounds(profile);
                if (main.Width > 0)
                    profile.GameDesktopBounds = main;
            }
            else if (phase == MultimonApplyPhase.MainPip)
            {
                viewports = MainPipLayoutService.CreateViewports(profile);
                cfgOptions = MainPipLayoutService.CreateCfgOptions(profile);
                var main = MenuLayoutHelper.GetPrimaryBounds(profile);
                if (main.Width > 0)
                    profile.GameDesktopBounds = main;
                profile.UseMainPipMode = true;
            }
            else
            {
                viewports = NormalizedCoordCalculator.ExpandLayout(profile);
                cfgOptions = ConfigCfgPatcher.CreateOptions(profile);
            }

            if (viewports.Count == 0)
            {
                result.Warnings.Add("No active viewports configured. Assign at least one screen role.");
                result.Success = false;
                return result;
            }

            if (phase == MultimonApplyPhase.Driving ||
                phase == MultimonApplyPhase.FloatingNativeMain ||
                phase == MultimonApplyPhase.MainPip)
            {
                if (viewports.Count > 4)
                    result.Warnings.Add(
                        $"This layout uses {viewports.Count} viewports. SCS officially documents up to 4 — treat 5+ as experimental.");

                if (phase == MultimonApplyPhase.Driving)
                {
                    foreach (var warning in DisplayLayoutHelper.ValidateLayout(profile, viewports))
                        result.Warnings.Add(warning);
                }
                else if (phase == MultimonApplyPhase.MainPip)
                {
                    result.Messages.Add(
                        "PiP on MAIN: game runs at MAIN native resolution; extra cameras are free-placed panels on that screen.");
                    result.Messages.Add(
                        "Panel pixels show the side camera (not cabin). Drag/resize panels on the PiP tab before Apply.");
                }
                else
                {
                    result.Messages.Add(
                        "Floating mode: game runs at MAIN native resolution; side cameras are thin strips on MAIN for capture.");
                    result.Messages.Add(
                        "Start overlays from Tools — drag/resize floating windows anywhere on any monitor.");
                }
            }

            string siiContent = null;
            if (phase == MultimonApplyPhase.Driving ||
                phase == MultimonApplyPhase.FloatingNativeMain ||
                phase == MultimonApplyPhase.MainPip)
                siiContent = MultimonConfigWriter.Generate(viewports);

            var gameFolders = GameDocumentsService.ResolveTargetFolders(target);

            foreach (var gameFolder in gameFolders)
            {
                GameDocumentsService.EnsureGameFolderExists(gameFolder);

                var siiPath = GameDocumentsService.GetMultimonConfigPath(gameFolder);
                var cfgPath = GameDocumentsService.GetConfigCfgPath(gameFolder);

                var cfgBackup = ConfigBackupService.BackupIfExists(cfgPath);
                ConfigCfgPatcher.Patch(cfgPath, cfgOptions, phase);

                if (phase == MultimonApplyPhase.Menu)
                {
                    var siiBackup = ConfigBackupService.BackupIfExists(siiPath);
                    if (File.Exists(siiPath))
                        File.Delete(siiPath);

                    result.Messages.Add($"Removed {siiPath} [menu — single monitor, multimon disabled]");
                    if (siiBackup != null)
                        result.Messages.Add($"Backup: {siiBackup}");
                }
                else
                {
                    var siiBackup = ConfigBackupService.BackupIfExists(siiPath);
                    File.WriteAllText(siiPath, siiContent, Encoding.UTF8);
                    string label;
                    if (phase == MultimonApplyPhase.FloatingNativeMain)
                        label = "floating — MAIN native + overlay capture strips";
                    else if (phase == MultimonApplyPhase.MainPip)
                        label = "PiP on MAIN — native res + free-placed cameras";
                    else
                        label = "driving — full multimon canvas";
                    result.Messages.Add($"Wrote {siiPath} [{label}]");
                    if (siiBackup != null)
                        result.Messages.Add($"Backup: {siiBackup}");
                }

                result.Messages.Add($"Patched {cfgPath}");
                if (cfgOptions != null)
                {
                    result.Messages.Add(
                        $"Game resolution set to {cfgOptions.ModeWidth}×{cfgOptions.ModeHeight} at ({cfgOptions.WindowX}, {cfgOptions.WindowY})");
                    if (phase == MultimonApplyPhase.Menu)
                        result.Messages.Add("r_multimon_mode set to 0 (primary monitor only until driving).");
                    else if (phase == MultimonApplyPhase.FloatingNativeMain)
                        result.Messages.Add("r_multimon_mode set to 4 on MAIN native resolution (overlay capture sources).");
                    else if (phase == MultimonApplyPhase.MainPip)
                        result.Messages.Add("r_multimon_mode set to 4 on MAIN native resolution (PiP panels).");
                    else
                        result.Messages.Add("r_multimon_mode set to 4 (all configured viewports).");
                }
                if (cfgBackup != null)
                    result.Messages.Add($"Backup: {cfgBackup}");
            }

            if (phase == MultimonApplyPhase.Driving &&
                profile.PhysicalScreens.Count > 1 && profile.HasDesktopGaps)
            {
                result.Warnings.Add(
                    "Active game displays have gaps — side views may not align until the game window spans all active monitors.");
            }

            if (phase == MultimonApplyPhase.Driving)
            {
                result.Messages.Add(
                    $"Game desktop area: {profile.GameDesktopBounds.Width}×{profile.GameDesktopBounds.Height} at ({profile.GameDesktopBounds.Left}, {profile.GameDesktopBounds.Top}).");
                result.Messages.Add(
                    "Center FOV uses your in-game camera/seat settings (same as single-screen). Adjust in the truck seat menu if needed.");
            }
            else if (phase == MultimonApplyPhase.FloatingNativeMain)
            {
                var primary = MenuLayoutHelper.GetPrimaryBounds(profile);
                result.Messages.Add(
                    $"Floating mode: game window = MAIN native {primary.Width}×{primary.Height}. Side cams → drag overlays wherever you want.");
            }
            else if (phase == MultimonApplyPhase.MainPip)
            {
                var primary = MenuLayoutHelper.GetPrimaryBounds(profile);
                var pipCount = profile.MainPipPanels?.Count ?? 0;
                result.Messages.Add(
                    $"PiP mode: game window = MAIN native {primary.Width}×{primary.Height} with {pipCount} extra panel(s). No second-screen span.");
            }
            else
            {
                var primary = MenuLayoutHelper.GetPrimaryBounds(profile);
                result.Messages.Add(
                    $"Menu mode: title screen uses primary monitor only ({primary.Width}×{primary.Height}). Multimon activates when you enter the world.");
            }

            result.Messages.Add("Do not change display settings in-game Options — that overwrites multimon config.");
            result.Success = true;
            return result;
        }
    }
}
