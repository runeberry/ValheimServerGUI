using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class AsyncPopout : Form
    {
        protected Task Task;

        protected AsyncPopoutOptions Options;

        private event EventHandler TaskFinished;

        private bool AutoCloseOnFinished;

        private string FinishedMessage;

        private AsyncPopout()
        {
            InitializeComponent();
            this.AddApplicationIcon();

            CloseButton.Click += this.BuildEventHandler(Close);
            TaskFinished += this.BuildEventHandler(OnTaskFinished);
        }

        public AsyncPopout(Task task, Action<AsyncPopoutOptions> optionsBuilder = null)
            : this()
        {
            if (task == null) throw new ArgumentException("Task cannot be null", nameof(task));

            var options = new AsyncPopoutOptions();
            optionsBuilder?.Invoke(options);
            Options = options;

            Task = task;
            Text = options.Title ?? Text;
            LoadingLabel.Text = options.Text ?? LoadingLabel.Text;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Task = Task.ContinueWith(TaskContinuationHandler);
        }

        protected void OnTaskFinished()
        {
            if (AutoCloseOnFinished)
            {
                Close();
                return;
            }

            LoadingLabel.Text = FinishedMessage ?? "Task Complete!";
            CloseButton.Text = "Close";
            ProgressBar.Visible = false;
        }

        private Task TaskContinuationHandler(Task task)
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                if (Options.CloseOnSuccess) return CloseAsync();

                FinishedMessage = Options.SuccessMessage;
            }
            else if (task.Exception != null)
            {
                if (Options.CloseOnFailure) return CloseAsync();

                var errMessage = Options.FailureMessage ?? "The task was cancelled due to an error.";
                var ex = task.Exception.GetPrimaryException();
                FinishedMessage = $"{errMessage}\r\n{ex.GetType().Name}\r\n{ex.Message}";
            }
            else
            {
                if (Options.CloseOnFailure) return CloseAsync();

                FinishedMessage = Options.FailureMessage ?? "The task was cancelled due to an unknown error.";
            }

            TaskFinished?.Invoke(this, EventArgs.Empty);

            return Task.CompletedTask;
        }

        private Task CloseAsync()
        {
            AutoCloseOnFinished = true;
            TaskFinished?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public class AsyncPopoutOptions
        {
            public string Title { get; set; }

            public string Text { get; set; }

            public bool CloseOnSuccess { get; set; }

            public string SuccessMessage { get; set; }

            public bool CloseOnFailure { get; set; }

            public string FailureMessage { get; set; }
        }
    }
}
