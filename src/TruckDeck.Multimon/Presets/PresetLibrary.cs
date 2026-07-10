using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using TruckDeck.Multimon.Models;
using TruckDeck.Multimon.Services;

namespace TruckDeck.Multimon.Presets
{
    public sealed class PresetDefinition
    {
        public string Name { get; set; }
        public int MinScreens { get; set; }
        public int MaxScreens { get; set; }
        /// <summary>primaryCenter_secondarySplit | primarySplit_secondaryCenter | quad2x2_bottomCenter | byIndex</summary>
        public string Layout { get; set; }
        public IList<PresetScreenDefinition> Screens { get; set; } = new List<PresetScreenDefinition>();

        public override string ToString() => Name ?? "Preset";
    }

    public sealed class PresetScreenDefinition
    {
        public string Role { get; set; }
        public bool Split { get; set; }
        public string SplitLeftRole { get; set; }
        public string SplitRightRole { get; set; }
        public bool SpanNext { get; set; }
    }

    public static class PresetLibrary
    {
        static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static IList<PresetDefinition> LoadAll()
        {
            var presets = new List<PresetDefinition>();
            var presetDir = ResolvePresetDirectory();
            if (!Directory.Exists(presetDir))
                return presets;

            foreach (var file in Directory.GetFiles(presetDir, "*.json").OrderBy(Path.GetFileName))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var preset = Serializer.Deserialize<PresetDefinition>(json);
                    if (preset != null && !string.IsNullOrWhiteSpace(preset.Name))
                        presets.Add(preset);
                }
                catch
                {
                    // skip invalid preset files
                }
            }

            return presets;
        }

        public static IList<PresetDefinition> GetPresetsForScreenCount(int screenCount)
        {
            return LoadAll()
                .Where(p => screenCount >= p.MinScreens && screenCount <= p.MaxScreens)
                .ToList();
        }

        public static LayoutProfile ApplyPreset(
            PresetDefinition preset,
            DisplayEnumerationResult displays,
            DriveSide driveSide)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            if (displays == null)
                throw new ArgumentNullException(nameof(displays));

            var profile = new LayoutProfile
            {
                PresetName = preset.Name,
                DriveSide = driveSide,
                VirtualDesktopBounds = displays.VirtualDesktopBounds,
                PhysicalScreens = displays.Screens.ToList(),
                HasDesktopGaps = displays.HasGaps,
                MainScreenIndex = DisplayLayoutHelper.DefaultMainScreenIndex(displays.Screens.ToList())
            };

            if (TryApplySmartLayout(preset, displays, profile))
            {
                DisplayLayoutHelper.FinalizeProfile(profile);
                return profile;
            }

            for (var i = 0; i < displays.Screens.Count; i++)
            {
                var presetScreen = i < preset.Screens.Count ? preset.Screens[i] : null;
                profile.ScreenLayouts.Add(new ScreenLayoutEntry
                {
                    ScreenIndex = i,
                    Role = ParseRole(presetScreen?.Role),
                    SplitMode = (presetScreen?.Split ?? false)
                        ? AdditionalScreenSplitMode.Two
                        : AdditionalScreenSplitMode.Off,
                    SplitLeftRole = ParseRoleOrDefault(presetScreen?.SplitLeftRole, ViewportRole.Left),
                    SplitRightRole = ParseRoleOrDefault(presetScreen?.SplitRightRole, ViewportRole.Right),
                    SpanNext = presetScreen?.SpanNext ?? false
                });
            }

            return profile;
        }

        static bool TryApplySmartLayout(PresetDefinition preset, DisplayEnumerationResult displays, LayoutProfile profile)
        {
            var layout = preset.Layout;
            if (string.IsNullOrWhiteSpace(layout))
            {
                if (preset.Name != null && preset.Name.IndexOf("primary", StringComparison.OrdinalIgnoreCase) >= 0)
                    layout = preset.Name.IndexOf("side windows /", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "mainSplit_otherCenter"
                        : "mainCenter_otherSplit";
                else if (preset.Name != null && preset.Name.IndexOf("main", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         preset.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0)
                    layout = "mainCenter_otherSplit";
                else if (preset.Name != null && preset.Name.IndexOf("bottom center", StringComparison.OrdinalIgnoreCase) >= 0)
                    layout = "quad2x2_bottomCenter";
                else if (preset.Name != null && preset.Name.IndexOf("stacked", StringComparison.OrdinalIgnoreCase) >= 0)
                    layout = "stackedBottomCenter_topSplit";
            }

            switch (layout)
            {
                case "stackedBottomCenter_topSplit":
                    DisplayLayoutHelper.ApplyStackedBottomCenterTopSplit(profile);
                    return displays.Screens.Count >= 2 && DisplayLayoutHelper.IsVerticallyStacked(displays.Screens.ToList());
                case "mainCenter_otherSplit":
                case "primaryCenter_secondarySplit":
                    ApplyMainOtherLayout(profile, centerOnMain: true);
                    return displays.Screens.Count >= 2;
                case "mainSplit_otherCenter":
                case "primarySplit_secondaryCenter":
                    ApplyMainOtherLayout(profile, centerOnMain: false);
                    return displays.Screens.Count >= 2;
                case "quad2x2_bottomCenter":
                    ApplyQuad2x2BottomCenter(profile);
                    if (profile.ScreenLayouts.Any())
                    {
                        var bottomRow = DisplayLayoutHelper.GetBottomRowScreenIndices(profile.PhysicalScreens.ToList());
                        if (bottomRow.Count > 0)
                            profile.MainScreenIndex = bottomRow[0];
                    }
                    return DisplayLayoutHelper.Is2x2Grid(displays.Screens.ToList());
                default:
                    return false;
            }
        }

        static void ApplyMainOtherLayout(LayoutProfile profile, bool centerOnMain)
        {
            if (centerOnMain)
                DisplayLayoutHelper.ApplyCenterOnMainLayout(profile, splitOtherScreensWithSideWindows: true);
            else
                DisplayLayoutHelper.ApplyCenterOnOtherLayout(profile);
        }

        static void ApplyPrimarySecondary(LayoutProfile profile, bool centerOnPrimary)
        {
            foreach (var screen in profile.PhysicalScreens)
            {
                var isPrimary = screen.IsPrimary;
                var entry = new ScreenLayoutEntry { ScreenIndex = screen.Index };

                if (centerOnPrimary)
                {
                    if (isPrimary)
                        entry.Role = ViewportRole.Center;
                    else
                        entry.EnableTwoPaneSideWindows();
                }
                else
                {
                    if (isPrimary)
                        entry.EnableTwoPaneSideWindows();
                    else
                        entry.Role = ViewportRole.Center;
                }

                profile.ScreenLayouts.Add(entry);
            }
        }

        static void ApplyQuad2x2BottomCenter(LayoutProfile profile)
        {
            var screens = profile.PhysicalScreens.ToList();
            var bottomY = screens.Max(s => s.Bounds.Top);
            var bottomRow = screens.Where(s => s.Bounds.Top == bottomY).OrderBy(s => s.Bounds.X).ToList();

            foreach (var screen in screens)
            {
                var entry = new ScreenLayoutEntry { ScreenIndex = screen.Index };
                if (screen.Bounds.Top != bottomY)
                {
                    entry.Role = ViewportRole.Unused;
                }
                else if (bottomRow.Count >= 2 && screen.Index == bottomRow[0].Index)
                {
                    entry.Role = ViewportRole.Center;
                }
                else if (bottomRow.Count >= 2 && screen.Index == bottomRow[1].Index)
                {
                    entry.EnableTwoPaneSideWindows();
                }
                else
                {
                    entry.Role = ViewportRole.Unused;
                }

                profile.ScreenLayouts.Add(entry);
            }
        }

        public static LayoutProfile CreateBlankProfile(DisplayEnumerationResult displays, DriveSide driveSide)
        {
            var profile = new LayoutProfile
            {
                PresetName = "Custom",
                DriveSide = driveSide,
                VirtualDesktopBounds = displays.VirtualDesktopBounds,
                PhysicalScreens = displays.Screens.ToList(),
                HasDesktopGaps = displays.HasGaps,
                MainScreenIndex = DisplayLayoutHelper.DefaultMainScreenIndex(displays.Screens.ToList())
            };

            foreach (var screen in displays.Screens)
            {
                profile.ScreenLayouts.Add(new ScreenLayoutEntry
                {
                    ScreenIndex = screen.Index,
                    Role = screen.Index == profile.MainScreenIndex ? ViewportRole.Center : ViewportRole.Unused,
                    SplitMode = AdditionalScreenSplitMode.Off,
                    SplitLeftRole = ViewportRole.Left,
                    SplitRightRole = ViewportRole.Right
                });
            }

            DisplayLayoutHelper.FinalizeProfile(profile);
            return profile;
        }

        static ViewportRole ParseRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return ViewportRole.Unused;

            if (Enum.TryParse(role, ignoreCase: true, result: out ViewportRole parsed))
                return parsed;

            return ViewportRole.Unused;
        }

        static ViewportRole ParseRoleOrDefault(string role, ViewportRole fallback)
        {
            if (string.IsNullOrWhiteSpace(role))
                return fallback;
            return Enum.TryParse(role, ignoreCase: true, result: out ViewportRole parsed) ? parsed : fallback;
        }

        static string ResolvePresetDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Presets"),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Presets")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\Presets"))
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
        }
    }
}
