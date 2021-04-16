using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ValheimServerGUI.Tools
{
    public interface IExceptionHandler
    {
        void HandleException(Exception e, string additionalMessage = null);
    }

    public class ExceptionHandler : IExceptionHandler
    {
        private static readonly string NL = Environment.NewLine;

        public void HandleException(Exception e, string additionalMessage = null)
        {
            if (e == null) return;
            additionalMessage ??= "Unhandled Exception";

            var message = "An unhandled exception has been thrown, and ValheimServerGUI will be terminated.";
            //var stackTrace = string.Join(NL, e.StackTrace.Split(NL).Take(3));
            message += $"{NL}{NL}{BuildMessageBody(e, additionalMessage)}";

            var result = MessageBox.Show(
                message,
                additionalMessage,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            //if (result == DialogResult.Yes)
            //{
            //    try
            //    {
            //        var body = BuildMessageBody(e, additionalMessage);
            //        SendEmail("ValheimServerGUI - Automated bug report", additionalMessage);

            //        MessageBox.Show("Bug report sent. Thank you!", additionalMessage, MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    }
            //    catch(Exception e2)
            //    {
            //        MessageBox.Show("Failed to send bug report. Sorry!" + e2.Message, additionalMessage, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    }
            //}
        }

        //private void SendEmail(string subject, string body)
        //{
        //    var mailClient = new SmtpClient("");
        //    var mailMessage = new MailMessage();

        //    mailMessage.From = new MailAddress("");
        //    mailMessage.From = new MailAddress("");
        //    mailMessage.To.Add(new MailAddress(""));

        //    mailMessage.Subject = subject;
        //    mailMessage.Body = body;

        //    mailClient.Send(mailMessage);
        //    mailMessage.Dispose();
        //}

        private string BuildMessageBody(Exception e, string additionalMessage)
        {
            var os = Environment.OSVersion;

            var body =
                $"{e.GetType().Name}: {e.Message}{NL}" +
                $"Timestamp: {DateTime.UtcNow:O}{NL}" +
                $"Context: {additionalMessage}{NL}" +
                $"Source: {e.Source}{NL}" +
                $"TargetSite: {e.TargetSite}{NL}" +
                NL +
                $"ValheimServerGUI version: {AssemblyHelper.GetApplicationVersion()}{NL}" +
                $"OS Version: {os.VersionString}{NL}" +
                NL +
                $"Stack trace:{NL}{e.StackTrace}";

            return body;
        }
    }
}
