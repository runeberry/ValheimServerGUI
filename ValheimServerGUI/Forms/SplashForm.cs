using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class SplashForm : Form
    {
#if DEBUG
        private static readonly bool SimulateLongRunningStartup = false;
        private static readonly bool SimulateStartupTaskException = false;
        private static readonly bool SimulateAsyncPopoutOnStart = false;
#endif
        private readonly List<MainWindow> MainWindows = new();
        private bool IsFirstShown = true;
        private bool CloseAfterExceptionHandled = false;

        private readonly List<Func<Task>> StartupTasks = new();
        private readonly List<Task> FinishedTasks = new();
        private event EventHandler<Task> TaskFinished;

        private readonly IFormProvider FormProvider;
        private readonly IIpAddressProvider IpAddressProvider;
        private readonly ISoftwareUpdateProvider SoftwareUpdateProvider;
        private readonly IExceptionHandler ExceptionHandler;
        private readonly IUserPreferencesProvider UserPrefsProvider;
        private readonly IServerPreferencesProvider ServerPrefsProvider;
        private readonly IPlayerDataRepository PlayerDataRepository;
        private readonly ILogger Logger;

        public SplashForm(
            IFormProvider formProvider,
            IIpAddressProvider ipAddressProvider,
            ISoftwareUpdateProvider softwareUpdateProvider,
            IExceptionHandler exceptionHandler,
            IUserPreferencesProvider userPrefsProvider,
            IServerPreferencesProvider serverPrefsProvider,
            IPlayerDataRepository playerDataRepository,
            ILogger logger)
        {
            FormProvider = formProvider;
            IpAddressProvider = ipAddressProvider;
            SoftwareUpdateProvider = softwareUpdateProvider;
            ExceptionHandler = exceptionHandler;
            UserPrefsProvider = userPrefsProvider;
            ServerPrefsProvider = serverPrefsProvider;
            PlayerDataRepository = playerDataRepository;
            Logger = logger;

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                InitializeComponent();
                this.AddApplicationIcon();
                InitializeAppName();
                InitializeFormEvents();
            }
            catch (Exception e)
            {
                HandleException(e, "Startup Init Exception", true);
            }
        }

        #region Initialization

        private void InitializeAppName()
        {
            AppNameLabel.Text = $"ValheimServerGUI v{AssemblyHelper.GetApplicationVersion()}";
        }

        private void InitializeFormEvents()
        {
            Shown += this.BuildEventHandler(SplashForm_OnShown);
            ExceptionHandler.ExceptionHandled += this.BuildEventHandler(OnExceptionHandled);
        }

        #endregion

        #region Public methods

        public MainWindow CreateNewMainWindow(string startProfile, bool startServer)
        {
            var mainWindow = FormProvider.GetForm<MainWindow>();

            // Since the splash screen is the application's main form, it must continue running in the background
            // So listen for whenever the MainWindow closes, and close the splash screen as well, in order to close the application
            mainWindow.FormClosed += OnMainWindowClosed;
            mainWindow.StartProfile = startProfile;
            mainWindow.StartServerAutomatically = startServer;
            mainWindow.SplashIndex = MainWindows.Count;

            MainWindows.Add(mainWindow);
            Logger.LogDebug("Created {name} [{index}] for profile {name}", nameof(MainWindow), mainWindow.SplashIndex, startProfile);

            return mainWindow;
        }

        #endregion

        #region Form events

        protected void SplashForm_OnShown()
        {
            if (IsFirstShown)
            {
                IsFirstShown = false;

                // For some reason the form is not actually fully rendered at this point
                // (labels appear as white boxes) so I'm forcing a redraw here
                Refresh();

                OnFirstShown();
            }
        }

        protected void OnFirstShown()
        {
            try
            {
                if (!VersionCheck())
                {
                    Close();
                    return;
                }

                PrepareMainWindows();
                PrepareStartupTasks();
                RunStartupTasks();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Startup Run Exception", true);
            }
        }

        #endregion

        #region Event handlers

        private void OnTaskFinished(Task task)
        {
            if (!StartupTasks.Any())
            {
                // Close the splash screen if there are no startup tasks
                FinishStartup();
                return;
            }

            if (task != null)
            {
                FinishedTasks.Add(task);

                if (!task.IsCompletedSuccessfully)
                {
                    Logger.LogWarning("Error encountered during startup task");
                    HandleException(task.Exception, "Startup Task Exception", true);
                    return;
                }
            }

            var numTasks = StartupTasks.Count;
            var numTasksFinished = FinishedTasks.Count;
            var pctTasksFinished = numTasksFinished * 100 / numTasks;

            ProgressBar.Value = pctTasksFinished;

            if (numTasksFinished >= numTasks)
            {
                // Close the splash screen once all startup tasks have finished
                FinishStartup();
                return;
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var isMainWindowVisible = MainWindows.Any((m) => m?.Visible == true);
            HandleException(e.ExceptionObject as Exception, "Unhandled Exception", !isMainWindowVisible);
        }

        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            var isMainWindowVisible = MainWindows.Any((m) => m?.Visible == true);
            HandleException(e.Exception, "Thread Exception", !isMainWindowVisible);
        }

        private void OnExceptionHandled()
        {
            if (CloseAfterExceptionHandled) Close();
        }

        private void OnMainWindowClosed(object sender, FormClosedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, FormClosedEventArgs>(OnMainWindowClosed), new object[] { sender, e });
                return;
            }

            if (sender is not MainWindow mainWindow) return;

            Logger.LogDebug("Closed {name} [{index}]", nameof(MainWindow), mainWindow.SplashIndex);

            MainWindows[mainWindow.SplashIndex] = null;
            var stillOpen = MainWindows.Count(m => m != null);
            if (stillOpen > 0)
            {

                Logger.LogDebug("{stillOpen} windows are still open", stillOpen);
                return;
            }

            Logger.LogDebug($"All windows closed, shutting down application");
            Close();
        }

        #endregion

        #region Common methods

        private void PrepareMainWindows()
        {
            var allPrefs = ServerPrefsProvider.LoadPreferences();
            var autoStartServers = allPrefs.Where(p => p != null && p.AutoStart);
            if (autoStartServers.Any())
            {
                Logger.LogInformation("Loading server profiles with auto-start enabled");
                foreach (var autoStartServer in autoStartServers)
                {
                    CreateNewMainWindow(autoStartServer.ProfileName, true);
                }
                return;
            }

            var lastSavedServer = allPrefs.OrderByDescending(p => p.LastSaved).FirstOrDefault();
            if (lastSavedServer != null)
            {
                Logger.LogInformation("Loading most recently saved profile: {name}", lastSavedServer.ProfileName);
                CreateNewMainWindow(lastSavedServer.ProfileName, false);
                return;
            }

            var newPrefs = new ServerPreferences { ProfileName = Resources.DefaultServerProfileName };
            ServerPrefsProvider.SavePreferences(newPrefs);
            Logger.LogInformation("User preferences not found, creating new file");
            CreateNewMainWindow(newPrefs.ProfileName, false);
        }

        private void AddStartupTask(Func<Task> taskFunc, string taskName)
        {
            StartupTasks.Add(async () =>
            {
                Logger.LogDebug("Starting startup task: {name}", taskName);
                var sw = Stopwatch.StartNew();

                await taskFunc();

                Logger.LogDebug("Finished startup task: {name} ({dur}ms)", taskName, sw.ElapsedMilliseconds);
            });
        }

        private void PrepareStartupTasks()
        {
            TaskFinished += this.BuildEventHandler<Task>(OnTaskFinished);

            AddStartupTask(() => SoftwareUpdateProvider.CheckForUpdatesAsync(false), "Check for updates");
            AddStartupTask(PlayerDataRepository.LoadAsync, "Load player data");

#if DEBUG
            if (SimulateLongRunningStartup)
            {
                AddStartupTask(() => Task.Delay(2000), "2s delay");
                AddStartupTask(() => Task.Delay(2500), "2.5 delay");
                AddStartupTask(() => Task.Delay(3000), "3s delay");
            }

            if (SimulateStartupTaskException)
            {
                AddStartupTask(async () =>
                {
                    await Task.Delay(500);
                    throw new InvalidOperationException("Intentional exception thrown for testing");
                }, "Intentional exception");
            }
#endif
        }

        private void RunStartupTasks()
        {
            //var i = 1;
            foreach (var taskFunc in StartupTasks)
            {
                //this.Logger.LogTrace($"Beginning startup task #{i++}");

                Task.Run(() => taskFunc().ContinueWith(t =>
                {
                    TaskFinished?.Invoke(this, t);
                    return Task.CompletedTask;
                }));
            }
        }

        private bool VersionCheck()
        {
            var dotnetVersion = AssemblyHelper.GetDotnetRuntimeVersion();

            if (dotnetVersion.Major < 6)
            {
                Logger.LogWarning($"Incompatible .NET version detected: {dotnetVersion}");

                var nl = Environment.NewLine;
                var result = MessageBox.Show(
                    $"ValheimServerGUI requires the .NET 6.0 Desktop Runtime (or higher) to be installed.{nl}" +
                    $"You are currently using .NET {dotnetVersion}.{nl}{nl}" +
                    "Would you like to go to the download page now?",
                    ".NET Upgrade Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    OpenHelper.OpenWebAddress(Resources.UrlDotnetDownload);
                }

                return false;
            }

            return true;
        }

        private void HandleException(Exception exception, string contextMessage, bool closeAfterHandle)
        {
            Logger.LogError($"Encountered exception - {exception.GetType().Name}: {exception.Message}");

            CloseAfterExceptionHandled = closeAfterHandle;

            ExceptionHandler.HandleException(exception, contextMessage);
        }

        private void FinishStartup()
        {
            var userPrefs = UserPrefsProvider.LoadPreferences();

            foreach (var mainWindow in MainWindows)
            {
                mainWindow.Show();
                if (userPrefs.StartMinimized)
                {
                    mainWindow.WindowState = FormWindowState.Minimized;
                }
            }

#if DEBUG
            if (SimulateAsyncPopoutOnStart)
            {
                var asyncPopout = new AsyncPopout(Task.Delay(5000), options =>
                {
                    options.Text = "Testing AsyncPopout...";
                    options.Title = "Testing AsyncPopout";
                    options.SuccessMessage = "Task succeeded!";
                    options.FailureMessage = "Task failed!";
                });

                asyncPopout.Show();
            }
#endif
            // Hide the splash screen so it's no longer visible once the application is loaded
            Hide();
        }

        #endregion
    }
}
