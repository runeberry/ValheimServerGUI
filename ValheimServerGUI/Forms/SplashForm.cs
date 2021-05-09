using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class SplashForm : Form
    {
#if DEBUG
        private static readonly bool SimulateLongRunningStartup = false;
        private static readonly bool SimulateStartupTaskException = false;
#endif
        private Form MainForm;
        private bool IsFirstShown = true;

        private readonly List<Func<Task>> StartupTasks = new();
        private readonly List<Task> FinishedTasks = new();
        private event EventHandler<Task> TaskFinished;

        private readonly IFormProvider FormProvider;
        private readonly IIpAddressProvider IpAddressProvider;
        private readonly ISoftwareUpdateProvider SoftwareUpdateProvider;
        private readonly IExceptionHandler ExceptionHandler;
        private readonly ILogger Logger;

        public SplashForm(
            IFormProvider formProvider,
            IIpAddressProvider ipAddressProvider,
            ISoftwareUpdateProvider softwareUpdateProvider,
            IExceptionHandler exceptionHandler,
            ILogger logger)
        {
            this.FormProvider = formProvider;
            this.IpAddressProvider = ipAddressProvider;
            this.SoftwareUpdateProvider = softwareUpdateProvider;
            this.ExceptionHandler = exceptionHandler;
            this.Logger = logger;

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                InitializeComponent();
                InitializeAppName();
                InitializeFormEvents();
            }
            catch (Exception e)
            {
                this.HandleException(e);
            }
        }

        private void InitializeAppName()
        {
            this.AppNameLabel.Text = $"ValheimServerGUI v{AssemblyHelper.GetApplicationVersion()}";
        }

        private void InitializeFormEvents()
        {
            this.Shown += this.BuildEventHandler(this.SplashForm_OnShown);
        }

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

                InitializeMainForm();
                InitializeStartupTasks();
                RunStartupTasks();
            }
            catch (Exception ex)
            {
                this.HandleException(ex);
            }
        }

        private void InitializeMainForm()
        {
            this.MainForm = this.FormProvider.GetForm<MainWindow>();

            // Since the splash screen is the application's main form, it must continue running in the background
            // So listen for whenever the MainWindow closes, and close the splash screen as well, in order to close the application
            this.MainForm.FormClosed += this.OnMainFormClosed;
        }

        private void InitializeStartupTasks()
        {
            this.TaskFinished += this.BuildEventHandler<Task>(this.OnTaskFinished);

            this.AddStartupTask(this.IpAddressProvider.GetExternalIpAddressAsync);
            this.AddStartupTask(this.IpAddressProvider.GetInternalIpAddressAsync);
            this.AddStartupTask(() => this.SoftwareUpdateProvider.CheckForUpdatesAsync(false));

#if DEBUG
            if (SimulateLongRunningStartup)
            {
                this.AddStartupTask(() => Task.Delay(2000));
                this.AddStartupTask(() => Task.Delay(2500));
                this.AddStartupTask(() => Task.Delay(3000));
            }
            
            if (SimulateStartupTaskException)
            {
                this.AddStartupTask(async () =>
                {
                    await Task.Delay(500);
                    throw new InvalidOperationException("Intentional exception thrown for testing");
                });
            }
#endif
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
                    this.HandleException(task.Exception);
                    return;
                }

                //this.Logger.LogTrace($"Finishing startup task #{this.FinishedTasks.Count}");
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
            this.HandleException(e.ExceptionObject as Exception, "Unhandled Exception");
        }

        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            this.HandleException(e.Exception, "Thread Exception");
        }

        private void OnExceptionHandled(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnMainFormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Common methods

        private void AddStartupTask(Func<Task> taskFunc)
        {
            this.StartupTasks.Add(taskFunc);
        }

        private void RunStartupTasks()
        {
            var i = 1;
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

            if (dotnetVersion.Major < 5)
            {
                this.Logger.LogWarning($"Incompatible .NET version detected: {dotnetVersion}");

                var nl = Environment.NewLine;
                var result = MessageBox.Show(
                    $"ValheimServerGUI requires the .NET 5.0 Desktop Runtime (or higher) to be installed.{nl}" +
                    $"You are currently using .NET {dotnetVersion}.{nl}{nl}" +
                    "Would you like to go to the download page now?",
                    ".NET Upgrade Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    WebHelper.OpenWebAddress(Resources.UrlDotnetDownload);
                }

                return false;
            }

            return true;
        }

        private void HandleException(Exception exception, string contextMessage = null)
        {
            this.Logger.LogError($"Encountered exception - {exception.GetType().Name}: {exception.Message}");

            this.ExceptionHandler.ExceptionHandled += this.OnExceptionHandled;

            this.ExceptionHandler.HandleException(exception, contextMessage ?? "Startup Exception");
        }

        private void FinishStartup()
        {
            this.MainForm.Show();

            // Hide the splash screen so it's no longer visible once the application is loaded
            this.Hide();
        }

        #endregion
    }
}
