using System;
using System.IO;

namespace TruckDeck.Multimon.Services
{
    public static class ConfigBackupService
    {
        public static string BackupIfExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(directory, fileName + ".bak." + stamp);
            File.Copy(filePath, backupPath, overwrite: false);
            return backupPath;
        }
    }
}
