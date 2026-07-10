using System.Diagnostics;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public static class GameLaunchService
    {
        public const string Ets2SteamUri = "steam://run/227300";
        public const string AtsSteamUri = "steam://run/270880";

        public static bool TryLaunch(GameTarget target, out string error)
        {
            error = null;
            if (GameProcessGuard.IsGameRunning(out var running))
            {
                error = $"{running} is already running.";
                return false;
            }

            switch (target)
            {
                case GameTarget.Ets2:
                    LaunchUri(Ets2SteamUri);
                    return true;
                case GameTarget.Ats:
                    LaunchUri(AtsSteamUri);
                    return true;
                case GameTarget.Both:
                    LaunchUri(Ets2SteamUri);
                    LaunchUri(AtsSteamUri);
                    return true;
                default:
                    error = "Unknown game target.";
                    return false;
            }
        }

        static void LaunchUri(string uri)
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
    }
}
