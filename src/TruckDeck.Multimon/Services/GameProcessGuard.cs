using System;
using System.Diagnostics;
using System.Linq;

namespace TruckDeck.Multimon.Services
{
    public static class GameProcessGuard
    {
        static readonly string[] ProcessNames = { "eurotrucks2", "amtrucks" };

        public static bool IsGameRunning(out string runningGameLabel)
        {
            runningGameLabel = null;
            foreach (var processName in ProcessNames)
            {
                if (Process.GetProcessesByName(processName).Any())
                {
                    runningGameLabel = processName == "eurotrucks2" ? "Euro Truck Simulator 2" : "American Truck Simulator";
                    return true;
                }
            }
            return false;
        }
    }
}
