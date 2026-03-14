using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommonTerms.Helpers;
using CommonTerms.Models;
using CommonTerms.Services;
using CommonTerms.ViewModels.Pages;
using CommonTerms.ViewModels.Pages.Settings;
using CommonTerms.ViewModels.Windows;
using CommonTerms.Views.Pages;
using CommonTerms.Views.Pages.Settings;
using CommonTerms.Views.Windows;
using Kaoses.Core.System.Interfaces.Services;
using Kaoses.Core.System.Logging;
using Kaoses.Core.System.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using WPF_UI_Common.Interfaces;
using WPF_UI_Common.Services;
using WPF_UI_Common.Services.Naviagtion;

namespace CommonTerms
{
    /// <summary>
    /// Application entry point and composition root.
    /// Builds and starts the .NET Generic Host, configures dependency injection,
    /// initializes application services (including tray), and handles application lifecycle events.
    /// </summary>
    public partial class App
    {
        public static readonly string AppName = "Common Terms";
        public static readonly string AppNameShort = "CommonTerms";

        /// <summary>
        /// The generic host instance used for dependency injection and service management.
        /// </summary>
        private static IHost? _host;

        /// <summary>
        ///  Used to enforce single-instance application. The mutex is named uniquely to prevent conflicts with other applications.
        /// </summary>
        private static Mutex _mutex;

        // Named constants for instance coordination.
        private const string MutexName = "CommonTerms_SingleInstance";
        private const string InstanceEventName = "CommonTerms_SingleInstance_Event";

        // Event used to signal the primary instance when a secondary instance starts.
        private static EventWaitHandle? _instanceEvent;

        // Tracks whether this process became the primary (first) instance.
        private static bool _isPrimaryInstance;

        /// <summary>
        /// Retrieves a registered service from the host's service provider.
        /// Returns null if the service is not registered or host is not initialized.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        /// <returns>Instance of the requested service or null.</returns>
        public T? GetService<T>()
            where T : class
            => _host?.Services.GetService(typeof(T)) as T;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// Enforces single-instance using a named <see cref="Mutex"/> and initializes XAML resources.
        /// If another instance is detected the code now signals the existing instance to activate its main window
        /// instead of immediately exiting with no UX feedback.
        /// </summary>
        public App()
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);
            _isPrimaryInstance = createdNew;

            if (!_isPrimaryInstance)
            {
                // Signal the primary instance (if it has created the named EventWaitHandle) so it can bring itself to the foreground.
                try
                {
                    try
                    {
                        using EventWaitHandle existingEvent = EventWaitHandle.OpenExisting(InstanceEventName);
                        existingEvent.Set();
                    }
                    catch (WaitHandleCannotBeOpenedException)
                    {
                        // Primary instance may not have created the event yet; ignore and still exit.
                        // We don't block waiting for it to create the event.
                    }
                }
                catch
                {
                    // Swallow any errors here - signaling is best-effort.
                }

                // Exit this secondary instance after signaling.
                Environment.Exit(0);
            }
            else
            {
                // Primary instance: create the named event so future instances can signal this one.
                try
                {
                    bool createdEvent;
                    _instanceEvent = new EventWaitHandle(false, EventResetMode.AutoReset, InstanceEventName, out createdEvent);

                    // Listen for signals from secondary instances on a background task.
                    Task.Run(() =>
                    {
                        try
                        {
                            while (true)
                            {
                                _instanceEvent?.WaitOne();

                                // Bring main window to foreground on UI thread when signaled.
                                Application.Current?.Dispatcher?.Invoke(() =>
                                {
                                    try
                                    {
                                        var mainWindow = Application.Current?.MainWindow;
                                        if (mainWindow != null)
                                        {
                                            if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
                                            {
                                                mainWindow.WindowState = System.Windows.WindowState.Normal;
                                            }

                                            // Activate and briefly toggle Topmost to ensure it appears in front.
                                            mainWindow.Activate();
                                            mainWindow.Topmost = true;
                                            mainWindow.Topmost = false;
                                            mainWindow.Focus();
                                        }
                                    }
                                    catch
                                    {
                                        // Ignore UI activation errors.
                                    }
                                });
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Event disposed during shutdown - normal termination of listener.
                        }
                        catch
                        {
                            // Swallow other exceptions to keep the background listener robust.
                        }
                    });
                }
                catch
                {
                    // If event creation fails, continue without cross-instance signaling.
                }
            }

            InitializeComponent();
            // Register global exception handlers early so startup crashes are captured in a file when
            // running as a single-file bundle (or when Serilog isn't configured yet).
            RegisterGlobalExceptionHandlers();
        }

        /// <summary>
        /// Gets the application's service provider for dependency injection.
        /// </summary>
        public static IServiceProvider Services => _host.Services;

        /// <summary>
        /// Registers global exception handlers that write a simple crash log to disk.
        /// Useful for diagnosing single-file startup failures where other logging may not yet be configured.
        /// </summary>
        private void RegisterGlobalExceptionHandlers()
        {
            try
            {
                // UI thread exceptions
                this.DispatcherUnhandledException += OnDispatcherUnhandledException;

                // Non-UI thread exceptions
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try
                    {
                        var exObj = e.ExceptionObject as Exception;
                        var ex = exObj ?? new Exception("Unknown AppDomain unhandled exception");
                        WriteCrashLog(ex, "AppDomain.CurrentDomain.UnhandledException");
                        try { Log.Fatal(ex, "Unhandled exception (AppDomain)"); } catch { }
                    }
                    catch { }
                };

                // Unobserved task exceptions
                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    try
                    {
                        WriteCrashLog(e.Exception, "TaskScheduler.UnobservedTaskException");
                        try { Log.Error(e.Exception, "Unobserved task exception"); } catch { }
                        e.SetObserved();
                    }
                    catch { }
                };
            }
            catch
            {
                // Don't let registration fail the app.
            }
        }

        /// <summary>
        /// Writes exception details to a crash log in the application's base directory.
        /// This is intentionally minimal to avoid depending on other services during startup.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="source">A short source description.</param>
        private void WriteCrashLog(Exception ex, string source)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory ?? Environment.CurrentDirectory ?? ".";
                var file = Path.Combine(baseDir, "crash.log");
                var text = $"[{DateTime.UtcNow:O}] {source}\n{ex}\n\n";
                File.AppendAllText(file, text);
            }
            catch
            {
                // If file logging fails, swallow exceptions to avoid recursive failures.
            }
        }

        /// <summary>
        /// Handles application startup. Builds and starts the generic host, then initializes platform services.
        /// Serilog is configured via the Host's <c>UseSerilog</c> callback so logger configuration can read host configuration and DI.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Startup event arguments.</param>
        private async void OnStartup(object sender, StartupEventArgs e)
        {

            var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            try
            {
                _host = Host.CreateDefaultBuilder(e.Args)
                    .ConfigureAppConfiguration(c =>
                    {
                        c.SetBasePath(appLocation);
                    })
                    .UseSerilog((context, services, loggerConfig) =>
                    {
                        LoggingSetup.ConfigureLogging(context, loggerConfig);
                        loggerConfig.ReadFrom.Configuration(context.Configuration);
                        loggerConfig.ReadFrom.Services(services).Enrich.FromLogContext();
                    })
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        string localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), App.AppNameShort);
                        string savedConfigPath = Path.Combine(localFolder, "savedConfig.json");

                        // Load saved config first ()
                        try
                        {
                            // Ensure the folder exists BEFORE wiring configuration with reloadOnChange.
                            // FileSystemWatcher (used by reloadOnChange) can throw when the watched directory doesn't exist.
                            AppPaths.EnsureFolderExists(localFolder);

                            // If you expect the file to be absent on first run, it's safe to create a minimal JSON file ("{}")
                            // This prevents FileNotFound/FileSystemWatcher races and avoids config providers throwing on some platforms.
                            if (!File.Exists(savedConfigPath))
                            {
                                File.WriteAllText(savedConfigPath, "{}", System.Text.Encoding.UTF8);
                            }

                            // Now add the saved config (lowest priority).
                            config.AddJsonFile(savedConfigPath, optional: true, reloadOnChange: true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error while adding saved config file {SavedConfigPath} to IConfiguration: {Full}", savedConfigPath, ex.ToString());
                        }

                        // Load appsettings.json second (overwrites saved values, Highest Priority)
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    })
                    .ConfigureServices(ConfigureServices)
                    .Build();

                Log.Information("OnStartup: Host built.");

                await _host.StartAsync();
                Log.Information("OnStartup: Host started successfully.");

                try
                {
                    // Ensure AppConfigPersister is constructed so it subscribes to AppConfig.PropertyChanged
                    var persister = _host.Services.GetRequiredService<AppConfigPersister>();
                    Log.Debug("OnStartup: AppConfigPersister resolved and initialized.");
                }
                catch (Exception exPersister)
                {
                    Log.Error(exPersister, "OnStartup: Failed to resolve/init AppConfigPersister");
                }


            }
            catch (Exception ex)
            {
                // Unwrap aggregate/inner exceptions and log all details
                var agg = ex as AggregateException;
                if (agg != null)
                {
                    foreach (var inner in agg.Flatten().InnerExceptions)
                    {
                        Log.Fatal(inner, "OnStartup: Host failed to start - inner exception");
                    }
                }
                else
                {
                    Log.Fatal(ex, "OnStartup: Host failed to start.");
                }

                Log.Fatal(ex, "OnStartup: Host failed to start.");
                // rethrow so VS will break (optional)
                //throw;
            }
        }

        /// <summary>
        /// Registers application services, view models and pages with the DI container.
        /// </summary>
        /// <param name="context">The host builder context.</param>
        /// <param name="services">Service collection to configure.</param>
        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddNavigationViewPageProvider();

            // hosted service
            services.AddHostedService<ApplicationHostService>();

            // Register TrayService and map the interface to the same instance.

            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ITaskBarService, TaskBarService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // e.g. in your DI setup
            services.AddSingleton<INavigationParameterStore, NavigationParameterStore>();

            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IPersistAndRestoreService, PersistAndRestoreService>();
            services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();

            // Keep existing options configuration for compatibility
            services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));

            // Create a singleton AppConfig instance bound from configuration so UI can bind directly to it.
            var appConfigInstance = new Models.AppConfig();
            context.Configuration.GetSection(nameof(AppConfig)).Bind(appConfigInstance);
            services.AddSingleton(appConfigInstance);

            // Register the persister that listens to property changes and saves automatically.
            services.AddSingleton<AppConfigPersister>();

            // Registers custom SQLite service.
            //services.AddSingleton<ISqliteService>(provider => new SqliteService("Data Source=app.db"));

            // Alternate: Pull the connection string from configuration
            /*
            services.AddSingleton<ISqliteService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var connStr = config.GetConnectionString("Default");
                return new SqliteService(connStr);
            });
            */

            // Alternate: Registers an EF Core DbContext (or similar database infrastructure).
            // DatabaseSetup.AddDatabase<AppDbContext>(services, "Data Source=app.db");

            services.AddSingleton<INavigationWindow, MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<DashboardPage>();
            services.AddSingleton<DashboardViewModel>();

            services.AddSingleton<SettingsPage>();
            services.AddSingleton<SettingsViewModel>();

            // Settings sub-pages

            services.AddSingleton<Styling>();
            services.AddSingleton<StylingViewModel>();

            // example page and viewmodel registrations


            // example Data services
            //services.AddSingleton<ISampleDataService, SampleDataService>();
        }

        /// <summary>
        /// Handles application exit: disposes tray, stops the host and shuts down logging.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Exit event arguments.</param>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            Log.Information("OnExit: Application exiting");


            if (_host != null)
            {
                // Use a small shutdown timeout to avoid hangs during application exit.
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
                _host = null;
            }

            // Dispose cross-instance synchronization primitives.
            try
            {
                if (_isPrimaryInstance)
                {
                    try
                    {
                        _mutex?.ReleaseMutex();
                    }
                    catch
                    {
                        // Ignore release errors.
                    }
                }
            }
            catch { }

            try
            {
                _mutex?.Dispose();
                _mutex = null;
            }
            catch { }

            try
            {
                _instanceEvent?.Close();
                _instanceEvent?.Dispose();
                _instanceEvent = null;
            }
            catch { }

            // Close and flush Serilog once.
            Log.CloseAndFlush();
        }

        /// <summary>
        /// Global UI thread exception handler. Logs the exception and shows an error message to the user.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">DispatcherUnhandledException event arguments.</param>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "OnDispatcherUnhandledException: Unhandled exception occurred");
            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}