namespace CommonTerms.Models
{
    /// <summary>
    /// Represents application configuration settings and is now observable so the UI can bind directly to it.
    /// Inherits the project's ObservableObject so property changes raise notifications.
    /// </summary>
    public class AppConfig : ObservableObject
    {
        private string _configurationsFolder = App.AppNameShort;

        public string ConfigurationsFolder
        {
            get => _configurationsFolder;
            set => SetProperty(ref _configurationsFolder, value);
        }

        private string _appPropertiesFileName = "appProperties";

        public string AppPropertiesFileName
        {
            get => _appPropertiesFileName;
            set => SetProperty(ref _appPropertiesFileName, value);
        }

        private string _privacyStatement = string.Empty;

        public string PrivacyStatement
        {
            get => _privacyStatement;
            set => SetProperty(ref _privacyStatement, value);
        }

        private bool _isCloseToTray = false;

        public bool IsCloseToTray
        {
            get => _isCloseToTray;
            set => SetProperty(ref _isCloseToTray, value);
        }

        private bool _hasTrayIcon = false;

        public bool HasTrayIcon
        {
            get => _hasTrayIcon;
            set => SetProperty(ref _hasTrayIcon, value);
        }
    }
}