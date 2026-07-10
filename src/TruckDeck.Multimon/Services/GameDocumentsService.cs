using System;
using System.Collections.Generic;
using System.IO;
using TruckDeck.Multimon.Models;

namespace TruckDeck.Multimon.Services
{
    public static class GameDocumentsService
    {
        public const string Ets2Folder = "Euro Truck Simulator 2";
        public const string AtsFolder = "American Truck Simulator";

        public static string GetGameDocumentsPath(string gameFolder)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, gameFolder);
        }

        public static string GetMultimonConfigPath(string gameFolder) =>
            Path.Combine(GetGameDocumentsPath(gameFolder), "multimon_config.sii");

        public static string GetConfigCfgPath(string gameFolder) =>
            Path.Combine(GetGameDocumentsPath(gameFolder), "config.cfg");

        public static string GetGameLogPath(string gameFolder) =>
            Path.Combine(GetGameDocumentsPath(gameFolder), "game.log.txt");

        public static IList<string> ResolveTargetFolders(GameTarget target)
        {
            var folders = new List<string>();
            switch (target)
            {
                case GameTarget.Ets2:
                    folders.Add(Ets2Folder);
                    break;
                case GameTarget.Ats:
                    folders.Add(AtsFolder);
                    break;
                case GameTarget.Both:
                    folders.Add(Ets2Folder);
                    folders.Add(AtsFolder);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
            return folders;
        }

        public static void EnsureGameFolderExists(string gameFolder)
        {
            var path = GetGameDocumentsPath(gameFolder);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
