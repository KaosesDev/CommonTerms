using System.IO;

namespace CommonTerms.Helpers
{
    /// <summary>
    /// Centralized helpers for application paths and config filenames.
    /// Use the appName parameter so this helper remains decoupled from App class.
    /// </summary>
    public static class AppPaths
    {
        public const string SavedConfigFileName = "savedConfig.json";

        public static string GetLocalApplicationDataFolder(string appName)
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);

        public static string GetSavedConfigPath(string appName)
            => Path.Combine(GetLocalApplicationDataFolder(appName), SavedConfigFileName);

        public static void EnsureFolderExists(string folderPath)
            => Directory.CreateDirectory(folderPath);
    }
}