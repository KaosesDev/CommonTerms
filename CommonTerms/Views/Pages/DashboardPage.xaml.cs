using CommonTerms.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace CommonTerms.Views.Pages
{
    /// <summary>
    /// Represents the dashboard page view in the application.
    /// Implements <see cref="INavigableView{DashboardViewModel}"/> for navigation and view model binding.
    /// </summary>
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        /// <summary>
        /// Gets the view model for the dashboard page.
        /// </summary>
        public DashboardViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardPage"/> class.
        /// Sets the view model and data context, and initializes the component.
        /// </summary>
        /// <param name="viewModel">The dashboard page view model.</param>
        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
        }
    }
}