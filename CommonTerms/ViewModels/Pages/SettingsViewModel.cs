using Wpf.Ui.Abstractions.Controls;

namespace CommonTerms.ViewModels.Pages
{
    /// <summary>
    /// View model for the settings page, handling theme changes and application version display.
    /// Implements <see cref="INavigationAware"/> for navigation lifecycle events.
    /// </summary>
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        /// <summary>
        /// Indicates whether the view model has been initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// Called when the page is navigated to.
        /// Initializes the view model if not already initialized.
        /// </summary>
        /// <returns>A completed task.</returns>
        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        /// <returns>A completed task.</returns>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// Initializes the view model properties, including theme and application version.
        /// </summary>
        private void InitializeViewModel()
        {
            _isInitialized = true;
        }
    }
}