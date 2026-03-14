using CommonTerms.ViewModels.Pages.Settings;
using Wpf.Ui.Abstractions.Controls;

namespace CommonTerms.Views.Pages.Settings
{
    /// <summary>
    /// Represents the data page view in the application.
    /// Implements <see cref="INavigableView{DataViewModel}"/> for navigation and view model binding.
    /// </summary>
    public partial class Styling : INavigableView<StylingViewModel>
    {
        /// <summary>
        /// Gets the view model for the data page.
        /// </summary>
        public StylingViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPage"/> class.
        /// Sets the view model and data context, and initializes the component.
        /// </summary>
        /// <param name="viewModel">The data page view model.</param>
        public Styling(StylingViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}