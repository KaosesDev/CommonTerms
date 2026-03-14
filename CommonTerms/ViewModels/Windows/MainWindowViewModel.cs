using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace CommonTerms.ViewModels.Windows
{
    /// <summary>
    /// View model for the main window, providing application title and navigation menu items.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the application title displayed in the main window.
        /// </summary>
        [ObservableProperty]
        private string _applicationTitle = "Common Terms";

        /// <summary>
        /// Gets or sets the collection of main navigation menu items.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
/*            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            }*/
        };

        /// <summary>
        /// Gets or sets the collection of footer navigation menu items.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
/*            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }*/
        };

        /// <summary>
        /// Gets or sets the collection of tray menu items.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}