using System;
using System.Drawing;
using TruckDeck.Multimon.Services;

namespace TruckDeck.Multimon.Models
{
    /// <summary>
    /// Extra camera panel placed freely on the MAIN screen (PiP over / carved into the cabin view).
    /// Coordinates are normalized 0–1 relative to the MAIN physical screen.
    /// </summary>
    public sealed class MainPipPanel
    {
        public const float MinFov = 12f;
        public const float MaxFov = 120f;
        public const float MinHeading = -120f;
        public const float MaxHeading = 120f;
        public const float MinPitch = -45f;
        public const float MaxPitch = 30f;

        public ViewportRole Role { get; set; } = ViewportRole.Left;

        /// <summary>Left edge as fraction of MAIN width (0–1).</summary>
        public float X { get; set; }

        /// <summary>Top edge as fraction of MAIN height (0–1).</summary>
        public float Y { get; set; }

        public float Width { get; set; } = 0.22f;
        public float Height { get; set; } = 0.28f;

        /// <summary>NaN = use role default when applying.</summary>
        public float HeadingOffset { get; set; } = float.NaN;

        /// <summary>NaN = use role default when applying.</summary>
        public float PitchOffset { get; set; } = float.NaN;

        /// <summary>NaN = use role default when applying.</summary>
        public float HorizontalFovOverride { get; set; } = float.NaN;

        public Rectangle ToPixelBounds(Rectangle mainBounds)
        {
            var x = mainBounds.Left + (int)(X * mainBounds.Width);
            var y = mainBounds.Top + (int)(Y * mainBounds.Height);
            var w = Math.Max(64, (int)(Width * mainBounds.Width));
            var h = Math.Max(48, (int)(Height * mainBounds.Height));

            if (x < mainBounds.Left)
                x = mainBounds.Left;
            if (y < mainBounds.Top)
                y = mainBounds.Top;
            if (x + w > mainBounds.Right)
                x = Math.Max(mainBounds.Left, mainBounds.Right - w);
            if (y + h > mainBounds.Bottom)
                y = Math.Max(mainBounds.Top, mainBounds.Bottom - h);
            w = Math.Min(w, mainBounds.Right - x);
            h = Math.Min(h, mainBounds.Bottom - y);

            return new Rectangle(x, y, Math.Max(32, w), Math.Max(24, h));
        }

        public void SetFromPixelBounds(Rectangle pixels, Rectangle mainBounds)
        {
            if (mainBounds.Width <= 0 || mainBounds.Height <= 0)
                return;

            X = (float)(pixels.Left - mainBounds.Left) / mainBounds.Width;
            Y = (float)(pixels.Top - mainBounds.Top) / mainBounds.Height;
            Width = (float)pixels.Width / mainBounds.Width;
            Height = (float)pixels.Height / mainBounds.Height;
            Clamp();
        }

        public void Clamp()
        {
            Width = ClampRange(Width, 0.08f, 0.6f);
            Height = ClampRange(Height, 0.08f, 0.6f);
            X = ClampRange(X, 0f, 1f - Width);
            Y = ClampRange(Y, 0f, 1f - Height);
        }

        public void ApplyRoleDefaults(DriveSide driveSide)
        {
            var camera = CameraDefaults.ForRole(Role, driveSide);
            HeadingOffset = camera.HeadingOffset;
            PitchOffset = camera.PitchOffset;
            HorizontalFovOverride = camera.HorizontalFovOverride;
        }

        public void ResolveCamera(DriveSide driveSide, out float heading, out float pitch, out float fov)
        {
            var defaults = CameraDefaults.ForRole(Role, driveSide);
            heading = float.IsNaN(HeadingOffset) ? defaults.HeadingOffset : HeadingOffset;
            pitch = float.IsNaN(PitchOffset) ? defaults.PitchOffset : PitchOffset;
            fov = float.IsNaN(HorizontalFovOverride) ? defaults.HorizontalFovOverride : HorizontalFovOverride;
        }

        public void NudgeHeading(float delta, DriveSide driveSide)
        {
            EnsureExplicitCamera(driveSide);
            HeadingOffset = ClampRange(HeadingOffset + delta, MinHeading, MaxHeading);
        }

        public void NudgePitch(float delta, DriveSide driveSide)
        {
            EnsureExplicitCamera(driveSide);
            PitchOffset = ClampRange(PitchOffset + delta, MinPitch, MaxPitch);
        }

        public void NudgeFov(float delta, DriveSide driveSide)
        {
            EnsureExplicitCamera(driveSide);
            if (HorizontalFovOverride <= 0f)
                HorizontalFovOverride = 70f;
            HorizontalFovOverride = ClampRange(HorizontalFovOverride + delta, MinFov, MaxFov);
        }

        void EnsureExplicitCamera(DriveSide driveSide)
        {
            ResolveCamera(driveSide, out var heading, out var pitch, out var fov);
            HeadingOffset = heading;
            PitchOffset = pitch;
            HorizontalFovOverride = fov;
        }

        static float ClampRange(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
