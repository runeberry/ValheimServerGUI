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
            this.FormProvider = formProvider;
            this.IpAddressProvider = ipAddressProvider;
            this.SoftwareUpdateProvider = softwareUpdateProvider;
            this.ExceptionHandler = exceptionHandler;
            this.UserPrefsProvider = userPrefsProvider;
            this.ServerPrefsProvider = serverPrefsProvider;
            this.PlayerDataRepository = playerDataRepository;
            this.Logger = logger;

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
                this.HandleException(e, "Startup Init Exception", true);
            }
        }

        #region Initialization

        private void InitializeAppName()
        {
            this.AppNameLabel.Text = $"ValheimServerGUI v{AssemblyHelper.GetApplicationVersion()}";
        }

        private void InitializeFormEvents()
        {
            this.Shown += this.BuildEventHandler(this.SplashForm_OnShown);
            this.ExceptionHandler.ExceptionHandled += this.BuildEventHandler(this.OnExceptionHandled);
        }

        #endregion

        #region Public methods

        public MainWindow CreateNewMainWindow(string startProfile, bool startServer)
        {
            var mainWindow = this.FormProvider.GetForm<MainWindow>();

            // Since the splash screen is the application's main form, it must continue running in the background
            // So listen for whenever the MainWindow closes, and close the splash screen as well, in order to close the application
            mainWindow.FormClosed += this.OnMainWindowClosed;
            mainWindow.StartProfile = startProfile;
            mainWindow.StartServerAutomatically = startServer;
            mainWindow.SplashIndex = this.MainWindows.Count;

            this.MainWindows.Add(mainWindow);
            this.Logger.LogDebug("Created {name} [{index}] for profile {name}", nameof(MainWindow), mainWindow.SplashIndex, startProfile);

            return mainWindow;
        }

        #endregion

        #region Form events

        protected void SplashForm_OnShown()
        {
            if (this.IsFirstShown)
            {
                this.IsFirstShown = false;

                // For some reason the form is not actually fully rendered at this point
                // (labels appear as white boxes) so I'm forcing a redraw here
                this.Refresh();

                this.OnFirstShown();
            }
        }

        protected void OnFirstShown()
        {
            try
            {
                if (!this.VersionCheck())
                {
                    this.Close();
                    return;
                }

                this.PrepareMainWindows();
                this.PrepareStartupTasks();
                this.RunStartupTasks();
            }
            catch (Exception ex)
            {
                this.HandleException(ex, "Startup Run Exception", true);
            }
        }

        #endregion

        #region Event handlers

        private void OnTaskFinished(Task task)
        {
            if (!this.StartupTasks.Any())
            {
                // Close the splash screen if there are no startup tasks
                this.FinishStartup();
                return;
            }

            if (task != null)
            {
                this.FinishedTasks.Add(task);

                if (!task.IsCompletedSuccessfully)
                {
                    this.Logger.LogWarning("Error encountered during startup task");
                    this.HandleException(task.Exception, "Startup Task Exception", true);
                    return;
                }
            }

            var numTasks = this.StartupTasks.Count;
            var numTasksFinished = this.FinishedTasks.Count;
            var pctTasksFinished = numTasksFinished * 100 / numTasks;

            this.ProgressBar.Value = pctTasksFinished;

            if (numTasksFinished >= numTasks)
            {
                // Close the splash screen once all startup tasks have finished
                this.FinishStartup();
                return;
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var isMainWindowVisible = this.MainWindows.Any((m) => m?.Visible == true);
            this.HandleException(e.ExceptionObject as Exception, "Unhandled Exception", !isMainWindowVisible);
        }

        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            var isMainWindowVisible = this.MainWindows.Any((m) => m?.Visible == true);
            this.HandleException(e.Exception, "Thread Exception", !isMainWindowVisible);
        }

        private void OnExceptionHandled()
        {
            if (CloseAfterExceptionHandled) this.Close();
        }

        private void OnMainWindowClosed(object sender, FormClosedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, FormClosedEventArgs>(this.OnMainWindowClosed), new object[] { sender, e });
                return;
            }

            if (sender is not MainWindow mainWindow) return;

            this.Logger.LogDebug("Closed {name} [{index}]", nameof(MainWindow), mainWindow.SplashIndex);

            this.MainWindows[mainWindow.SplashIndex] = null;
            var stillOpen = this.MainWindows.Count(m => m != null);
            if (stillOpen > 0)
            {

                this.Logger.LogDebug("{stillOpen} windows are still open", stillOpen);
                return;
            }

            this.Logger.LogDebug($"All windows closed, shutting down application");
            this.Close();
        }

        #endregion

        #region Common methods

        private void PrepareMainWindows()
        {
            var allPrefs = this.ServerPrefsProvider.LoadPreferences();

            var autoStartServers = allPrefs.Where(p => p != null && p.AutoStart);
            if (autoStartServers.Any())
            {
                this.Logger.LogInformation("Loading server profiles with auto-start enabled");
                foreach (var prefs in autoStartServers)
                {
                    this.CreateNewMainWindow(prefs.ProfileName, true);
                }
            }
            else
            {
                var prefs = allPrefs
                    .OrderByDescending(p => p.LastSaved)
                    .FirstOrDefault();

                if (prefs != null)
                {
                    this.Logger.LogInformation("Loading most recently saved profile: {name}", prefs.ProfileName);
                    this.CreateNewMainWindow(prefs.ProfileName, false);
                    return;
                }
            }
        }

        private void AddStartupTask(Func<Task> taskFunc, string taskName)
        {
            this.StartupTasks.Add(async () =>
            {
                this.Logger.LogDebug("Starting startup task: {name}", taskName);
                var sw = Stopwatch.StartNew();

                await taskFunc();

                this.Logger.LogDebug("Finished startup task: {name} ({dur}ms)", taskName, sw.ElapsedMilliseconds);
            });
        }

        private void PrepareStartupTasks()
        {
            this.TaskFinished += this.BuildEventHandler<Task>(this.OnTaskFinished);

            this.AddStartupTask(() => this.SoftwareUpdateProvider.CheckForUpdatesAsync(false), "Check for updates");
            this.AddStartupTask(this.PlayerDataRepository.LoadAsync, "Load player data");

#if DEBUG
            if (SimulateLongRunningStartup)
            {
                this.AddStartupTask(() => Task.Delay(2000), "2s delay");
                this.AddStartupTask(() => Task.Delay(2500), "2.5 delay");
                this.AddStartupTask(() => Task.Delay(3000), "3s delay");
            }

            if (SimulateStartupTaskException)
            {
                this.AddStartupTask(async () =>
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
            foreach (var taskFunc in this.StartupTasks)
            {
                //this.Logger.LogTrace($"Beginning startup task #{i++}");

                Task.Run(() => taskFunc().ContinueWith(t =>
                {
                    this.TaskFinished?.Invoke(this, t);
                    return Task.CompletedTask;
                }));
            }
        }

        private bool VersionCheck()
        {
            var dotnetVersion = AssemblyHelper.GetDotnetRuntimeVersion();

            if (dotnetVersion.Major < 6)
            {
                this.Logger.LogWarning($"Incompatible .NET version detected: {dotnetVersion}");

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
            this.Logger.LogError($"Encountered exception - {exception.GetType().Name}: {exception.Message}");

            this.CloseAfterExceptionHandled = closeAfterHandle;

            this.ExceptionHandler.HandleException(exception, contextMessage);
        }

        private void FinishStartup()
        {
            var userPrefs = this.UserPrefsProvider.LoadPreferences();

            foreach (var mainWindow in this.MainWindows)
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
            this.Hide();
        }

        #endregion
    }
}
