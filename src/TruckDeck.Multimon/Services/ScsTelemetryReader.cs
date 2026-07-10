using System;
using System.IO.MemoryMappedFiles;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Reads Local\SCSTelemetry (RenCloud / scs-sdk-plugin layout).
    /// </summary>
    public static class ScsTelemetryReader
    {
        const string MapName = "Local\\SCSTelemetry";

        // scs-telemetry-common.hpp rev 12 offsets
        const int OffsetSdkActive = 0;
        const int OffsetPaused = 4;
        const int OffsetSpeed = 948;
        const int OffsetElectricEnabled = 1575;
        const int OffsetEngineEnabled = 1576;

        public static bool TryReadSdkActive()
        {
            return TryReadBoolean(OffsetSdkActive, out var value) && value;
        }

        public static bool TryReadPaused()
        {
            return TryReadBoolean(OffsetPaused, out var value) && value;
        }

        public static bool TryReadEngineRunning()
        {
            if (!TryReadSdkActive())
                return false;

            return TryReadBoolean(OffsetEngineEnabled, out var value) && value;
        }

        public static bool TryReadElectricEnabled()
        {
            if (!TryReadSdkActive())
                return false;

            return TryReadBoolean(OffsetElectricEnabled, out var value) && value;
        }

        /// <summary>Speed in m/s (game units).</summary>
        public static bool TryReadSpeed(out float speedMetersPerSecond)
        {
            speedMetersPerSecond = 0f;
            if (!TryReadSdkActive())
                return false;

            try
            {
                using (var map = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read))
                using (var accessor = map.CreateViewAccessor(0, OffsetSpeed + 4, MemoryMappedFileAccess.Read))
                {
                    speedMetersPerSecond = accessor.ReadSingle(OffsetSpeed);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsInCab()
        {
            if (!TryReadSdkActive())
                return false;

            if (TryReadBoolean(OffsetPaused, out var paused) && paused)
                return false;

            if (TryReadEngineRunning())
                return true;

            if (TryReadSpeed(out var speed) && Math.Abs(speed) > 0.3f)
                return true;

            if (TryReadEngineRpm(out var rpm) && rpm > 400f)
                return true;

            return false;
        }

        public static bool TryReadEngineRpm(out float rpm)
        {
            rpm = 0f;
            if (!TryReadSdkActive())
                return false;

            try
            {
                using (var map = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read))
                using (var accessor = map.CreateViewAccessor(0, 956, MemoryMappedFileAccess.Read))
                {
                    rpm = accessor.ReadSingle(952);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        static bool TryReadBoolean(int offset, out bool value)
        {
            value = false;
            try
            {
                using (var map = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read))
                using (var accessor = map.CreateViewAccessor(0, offset + 1, MemoryMappedFileAccess.Read))
                {
                    value = accessor.ReadBoolean(offset);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
