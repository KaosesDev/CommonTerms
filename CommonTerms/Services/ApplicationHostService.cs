using CommonTerms.Views.Pages;
using CommonTerms.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using WPF_UI_Common.Interfaces;

namespace CommonTerms.Services
{
    /// <summary>
    /// Provides application hosting logic for startup and shutdown.
    /// Implements <see cref="IHostedService"/> to integrate with .NET Generic Host.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApplicationHostService> _logger;

        private INavigationWindow? _navigationWindow;

        public ApplicationHostService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ApplicationHostService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// Shows the main window and navigates to the dashboard page.
        /// Also invokes any registered activation handlers once the host is ready.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await HandleActivationAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ApplicationHostService.StartAsync canceled before activation completed.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during HandleActivationAsync");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("StartAsync canceled; skipping activation handlers.");
                return;
            }

            // After window is shown, run activation handlers registered via DI.
            var handlers = _serviceProvider.GetServices<IActivationHandler>();
            foreach (var handler in handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("StartAsync canceled while running activation handlers.");
                    break;
                }

                try
                {
                    if (handler.CanHandle())
                    {
                        // Prefer handlers that accept cancellation if available — if not, call without token.
                        var handleTask = handler.HandleAsync();
                        if (handleTask != null)
                        {
                            await handleTask.ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("An activation handler honored cancellation.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Activation handler failed");
                }
            }
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Attempts to close the main window if available.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_navigationWindow != null)
                {
                    try
                    {
                        _navigationWindow.CloseWindow();
                        _logger.LogInformation("Main window closed by ApplicationHostService.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to close main window during StopAsync.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in StopAsync.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles application activation by creating and showing the main window if it does not already exist.
        /// Ensures UI calls happen on the WPF Dispatcher.
        /// </summary>
        private Task HandleActivationAsync(CancellationToken cancellationToken)
        {
            // All WPF UI interactions must happen on the Dispatcher.
            // Use Application.Current.Dispatcher to ensure the create/show/navigation runs on UI thread.
            if (Application.Current == null)
            {
                _logger.LogWarning("Application.Current is null; skipping UI activation.");
                return Task.CompletedTask;
            }

            var dispatcher = Application.Current.Dispatcher;

            if (dispatcher.CheckAccess())
            {
                DoActivation();
                return Task.CompletedTask;
            }

            // Schedule on the UI dispatcher and await completion.
            var op = dispatcher.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                DoActivation();
            });

            return op.Task;
        }

        private void DoActivation()
        {
            try
            {
                if (!Application.Current.Windows.OfType<MainWindow>().Any())
                {
                    // Use GetRequiredService so a missing registration fails fast.
                    _navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();
                    _navigationWindow.ShowWindow();
                    _navigationWindow.Navigate(typeof(DashboardPage));
                    _logger.LogInformation("Main window shown and navigated to DashboardPage.");
                }
                else
                {
                    _logger.LogInformation("Main window already exists; activation skipped.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while activating main window.");
            }
        }
    }
}