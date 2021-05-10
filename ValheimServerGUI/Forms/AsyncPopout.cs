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

            this.CloseButton.Click += this.BuildEventHandler(this.Close);
            this.TaskFinished += this.BuildEventHandler(this.OnTaskFinished);
        }

        public AsyncPopout(Task task, Action<AsyncPopoutOptions> optionsBuilder = null)
            : this()
        {
            if (task == null) throw new ArgumentException("Task cannot be null", nameof(task));

            var options = new AsyncPopoutOptions();
            optionsBuilder?.Invoke(options);
            this.Options = options;

            this.Task = task;
            this.Text = options.Title ?? this.Text;
            this.LoadingLabel.Text = options.Text ?? this.LoadingLabel.Text;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Task = this.Task.ContinueWith(this.TaskContinuationHandler);
        }

        protected void OnTaskFinished()
        {
            if (this.AutoCloseOnFinished)
            {
                this.Close();
                return;
            }

            this.LoadingLabel.Text = this.FinishedMessage ?? "Task Complete!";
            this.CloseButton.Text = "Close";
            this.ProgressBar.Visible = false;
        }

        private Task TaskContinuationHandler(Task task)
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                if (this.Options.CloseOnSuccess) return this.CloseAsync();

                this.FinishedMessage = this.Options.SuccessMessage;
            }
            else if (task.Exception != null)
            {
                if (this.Options.CloseOnFailure) return this.CloseAsync();

                var errMessage = this.Options.FailureMessage ?? "The task was cancelled due to an error.";
                var ex = task.Exception.GetPrimaryException();
                this.FinishedMessage = $"{errMessage}\r\n{ex.GetType().Name}\r\n{ex.Message}";
            }
            else
            {
                if (this.Options.CloseOnFailure) return this.CloseAsync();

                this.FinishedMessage = this.Options.FailureMessage ?? "The task was cancelled due to an unknown error.";
            }

            this.TaskFinished?.Invoke(this, EventArgs.Empty);

            return Task.CompletedTask;
        }

        private Task CloseAsync()
        {
            this.AutoCloseOnFinished = true;
            this.TaskFinished?.Invoke(this, EventArgs.Empty);
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
