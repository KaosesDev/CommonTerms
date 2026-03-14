using Serilog;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace CommonTerms.ViewModels.Pages.Settings
{
    public partial class StylingViewModel : ObservableObject, INavigationAware
    {
        /// <summary>
        /// Indicates whether the view model has been initialized.
        /// </summary>
        private bool _isInitialized = false;

        private bool _isDarkTheme;
        private bool _suppressIsDarkThemeChangeActions;

        /// <summary>
        /// Gets or sets the current application theme.
        /// </summary>
        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        /// <summary>
        /// Exposes a boolean for binding the ToggleSwitch (true => dark, false => light).
        /// Setting this will invoke the existing theme-change logic (unless suppressed).
        /// </summary>
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme == value)
                    return;

                _isDarkTheme = value;
                OnPropertyChanged(nameof(IsDarkTheme));

                if (!_suppressIsDarkThemeChangeActions)
                {
                    // Reuse the existing command logic by calling the same handler
                    OnChangeTheme(value ? "theme_dark" : "theme_light");
                }
            }
        }

        /// <summary>
        /// Called when the page is navigated to.
        /// Initializes the view model if not already initialized.
        /// Also checks the INavigationParameterStore for toast parameters.
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
        /// Initializes the view model by generating a collection of random colors.
        /// </summary>
        private void InitializeViewModel()
        {
            CurrentTheme = ApplicationThemeManager.GetAppTheme();

            // Set IsDarkTheme to reflect the current theme without triggering the change handler.
            _suppressIsDarkThemeChangeActions = true;
            IsDarkTheme = CurrentTheme == ApplicationTheme.Dark;
            _suppressIsDarkThemeChangeActions = false;

            _isInitialized = true;
        }

        /// <summary>
        /// Called when the generated CurrentTheme property changes.
        /// Keep IsDarkTheme in sync without re-invoking theme change logic.
        /// </summary>
        /// <param name="value">New ApplicationTheme value.</param>
        partial void OnCurrentThemeChanged(ApplicationTheme value)
        {
            _suppressIsDarkThemeChangeActions = true;
            IsDarkTheme = value == ApplicationTheme.Dark;
            _suppressIsDarkThemeChangeActions = false;
        }

        /// <summary>
        /// Changes the application theme based on the provided parameter.
        /// </summary>
        /// <param name="parameter">The theme identifier ("theme_light" or other).</param>
        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            Log.Debug($"OnChangeTheme: Changing theme to: {parameter}");

            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;

                    break;

                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;

                    break;
            }
        }
    }
}