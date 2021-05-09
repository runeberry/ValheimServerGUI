using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class SplashForm : Form
    {
        private Form MainForm;

        private readonly List<Task> StartupTasks = new();
        private readonly List<Task> FinishedTasks = new();
        private event EventHandler<Task> TaskFinished;

        private readonly IFormProvider FormProvider;
        private readonly IIpAddressProvider IpAddressProvider;
        private readonly ISoftwareUpdateProvider SoftwareUpdateProvider;

        public SplashForm(
            IFormProvider formProvider,
            IIpAddressProvider ipAddressProvider,
            ISoftwareUpdateProvider softwareUpdateProvider)
        {
            this.FormProvider = formProvider;
            this.IpAddressProvider = ipAddressProvider;
            this.SoftwareUpdateProvider = softwareUpdateProvider;

            InitializeComponent();
            InitializeAppName();
            InitializeMainForm();
            InitializeStartupTasks();
        }

        private void InitializeAppName()
        {
            this.AppNameLabel.Text = $"ValheimServerGUI v{AssemblyHelper.GetApplicationVersion()}";
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

            this.AddStartupTask(this.IpAddressProvider.GetExternalIpAddressAsync());
            this.AddStartupTask(this.IpAddressProvider.GetInternalIpAddressAsync());
            this.AddStartupTask(this.SoftwareUpdateProvider.CheckForUpdatesAsync(false));
            //this.AddStartupTask(this.ArtificialDelay(3000));

            this.TaskFinished?.Invoke(this, null);
        }

        // For testing
        private Task ArtificialDelay(int ms) { return Task.Delay(ms); }

        private void AddStartupTask(Task task)
        {
            this.StartupTasks.Add(task.ContinueWith(t =>
            {
                // Ignoring all errors for now
                this.TaskFinished?.Invoke(this, t);
                return Task.CompletedTask;
            }));
        }

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
            }

            var numTasks = this.StartupTasks.Count;
            var numTasksFinished = this.FinishedTasks.Count;
            var pctTasksFinished = numTasksFinished * 100 / numTasks;

            this.Label.Text = $"Loading... ({pctTasksFinished}%)";

            if (numTasksFinished >= numTasks)
            {
                // Close the splash screen once all startup tasks have finished
                this.FinishStartup();
                return;
            }
        }

        private void FinishStartup()
        {
            this.MainForm.Show();

            // Hide the splash screen so it's no longer visible once the application is loaded
            this.Hide();
        }

        private void OnMainFormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }
    }
}
