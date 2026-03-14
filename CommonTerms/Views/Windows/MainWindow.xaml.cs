using System.ComponentModel;
using CommonTerms.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CommonTerms.Views.Windows
{
    /// <summary>
    /// Represents the main window of the application, providing navigation and theme management.
    /// Implements <see cref="INavigationWindow"/> for navigation control integration.
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        /// <summary>
        /// Gets the view model for the main window.
        /// </summary>
        public MainWindowViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets up navigation, theme watching, and dependency injection.
        /// </summary>
        /// <param name="viewModel">The main window view model.</param>
        /// <param name="navigationViewPageProvider">The navigation view page provider service.</param>
        /// <param name="navigationService">The navigation service.</param>
        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            // Watches for system theme changes and applies them to the window.
            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            // Sets the navigation control for the navigation service.
            navigationService.SetNavigationControl(RootNavigation);
        }

        #region INavigationWindow methods

        /// <summary>
        /// Gets the navigation view control.
        /// </summary>
        /// <returns>The root navigation view control.</returns>
        public INavigationView GetNavigation() => RootNavigation;

        /// <summary>
        /// Navigates to the specified page type.
        /// </summary>
        /// <param name="pageType">The type of the page to navigate to.</param>
        /// <returns>True if navigation succeeded; otherwise, false.</returns>
        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        /// <summary>
        /// Sets the page provider service for navigation.
        /// </summary>
        /// <param name="navigationViewPageProvider">The navigation view page provider service.</param>
        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        /// <summary>
        /// Shows the window.
        /// </summary>
        public void ShowWindow() => Show();

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event and shuts down the application.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Not implemented. Throws <see cref="NotImplementedException"/>.
        /// </summary>
        /// <returns>Nothing; always throws.</returns>
        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented. Throws <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        // In MainWindow.xaml.cs
/*        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            // Optionally log or show tray notification here
        }*/
    }
}