using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tests
{
    public partial class BaseTest
    {
        protected async Task ListenForEventAsync(Action<EventHandler> assigner, Action<EventListenerOptions> optionsBuilder = null)
        {
            var options = new EventListenerOptions();
            optionsBuilder?.Invoke(options);

            var resolved = false;

            EventHandler handler = (_, _) => resolved = true;
            assigner(handler);

            var sw = Stopwatch.StartNew();

            do
            {
                CheckTimeout(sw, options);
                await Task.Delay(options.Interval);
            }
            while (!resolved);
        }

        protected async Task<TArgs> ListenForEventAsync<TArgs>(Action<EventHandler<TArgs>> assigner, Action<EventListenerOptions> optionsBuilder = null)
        {
            var options = new EventListenerOptions();
            optionsBuilder?.Invoke(options);

            var resolved = false;
            TArgs eventArgs = default;

            EventHandler<TArgs> handler = (_, args) =>
            {
                resolved = true;
                eventArgs = args;
            };
            assigner(handler);

            var sw = Stopwatch.StartNew();

            do
            {
                CheckTimeout(sw, options);
                await Task.Delay(options.Interval);
            }
            while (!resolved);

            return eventArgs;
        }

        private void CheckTimeout(Stopwatch sw, EventListenerOptions options)
        {
            if (sw.ElapsedMilliseconds > options.Timeout)
            {
                throw new TimeoutException($"The event was not fired during the timeout period ({options.Timeout}ms).");
            }
        }

        public class EventListenerOptions
        {
            public int Timeout { get; set; } = 1000;

            public int Interval { get; set; } = 50;
        }
    }
}
