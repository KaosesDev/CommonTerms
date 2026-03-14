using CommonTerms.Helpers;
using CommonTerms.Models;
using Kaoses.Core.System.Interfaces.Services;
using Serilog;
using WPF_UI_Common.Services;

namespace CommonTerms.Services
{
    /// <summary>
    /// Centralized persister: listens to changes on the singleton AppConfig and saves to disk (debounced).
    /// Register this as a singleton so it listens for the app lifetime.
    /// </summary>
    public class AppConfigPersister : AppConfigPersisterBase
    {
        private readonly AppConfig _appConfig;

        public AppConfigPersister(AppConfig appConfig, IFileService fileService)
            : base(fileService)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

            _folder = AppPaths.GetLocalApplicationDataFolder(App.AppNameShort);
            _fileName = AppPaths.SavedConfigFileName;
            AppPaths.EnsureFolderExists(_folder);

            try
            {
                _appConfig.PropertyChanged += AppConfig_PropertyChanged;
                //Log.Debug("AppConfigPersister: Subscribed to AppConfig.PropertyChanged (folder={Folder}, file={File})", _folder, _fileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AppConfigPersister: Exception while subscribing to PropertyChanged");
                throw;
            }

            // Timer is used to implement debounce behavior. Start disabled.
            _debounceTimer = new Timer(OnTimerElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        private void OnTimerElapsed(object? state)
        {
            // Prevent overlapping invocations if somehow timer fires again.
            // Disable timer immediately to ensure single execution until next DebounceSave.
            try
            {
                _debounceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AppConfigPersister: Exception while stopping debounce timer");
            }

            if (_disposed)
            {
                return;
            }

            try
            {
                // Run Save on thread-pool to avoid long-running work on timer callback thread.
                Task.Run(() => Save());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AppConfigPersister: Exception scheduling save task from timer callback");
            }
        }

        public void Save()
        {
            try
            {
                //Log.Debug("AppConfigPersister: Saving AppConfig to {Folder}\\{File}", _folder, _fileName);

                // Wrap the saved object so configuration binding with GetSection(nameof(AppConfig)) works.
                // This will produce: { "AppConfig": { ... } }
                var wrapper = new { AppConfig = _appConfig };
                _fileService.Save(_folder, _fileName, wrapper);

                //Log.Debug("AppConfigPersister: Save completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AppConfigPersister: Failed to save AppConfig to {Folder}\\{File}", _folder, _fileName);
            }
        }
    }
}