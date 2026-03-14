using System.Windows.Input;
using CommonTerms.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace CommonTerms.Views.Pages
{
    /// <summary>
    /// Represents the settings page view in the application.
    /// Implements <see cref="INavigableView{SettingsViewModel}"/> for navigation and view model binding.
    /// </summary>
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        /// <summary>
        /// Gets the view model for the settings page.
        /// </summary>
        public SettingsViewModel ViewModel { get; }

        private readonly INavigationService _navigationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// Sets the view model and data context, and initializes the component.
        /// Injects the WPF-UI navigation service so navigation goes through the app's navigation stack.
        /// </summary>
        /// <param name="viewModel">The settings page view model.</param>
        /// <param name="navigationService">The WPF-UI navigation service from DI.</param>
        public SettingsPage(SettingsViewModel viewModel, INavigationService navigationService)
        {
            ViewModel = viewModel;
            _navigationService = navigationService;
            DataContext = this;

            InitializeComponent();
        }

        private void OnSettingsCardClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Card card && card.Tag is string pageName)
            {
                // Try to resolve the page type by searching loaded assemblies so Type is found regardless of assembly qualification.
                var pageFullName = $"CommonTerms.Views.Pages.Settings.{pageName}";
                var pageType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(pageFullName))
                    .FirstOrDefault(t => t != null);

                if (pageType != null)
                {
                    // Use the injected WPF-UI navigation service so navigation goes through the app's registered navigation control and page provider.
                    _navigationService.Navigate(pageType);
                }
            }
        }
    }
}