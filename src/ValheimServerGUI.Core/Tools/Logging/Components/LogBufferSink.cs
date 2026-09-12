using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Logging.Components
{
    public class LogBufferSink : ILogEventSink
    {
        private readonly ConcurrentBuffer<string> Buffer;

        public IEnumerable<string> Logs => Buffer;

        public LogBufferSink(int bufferSize)
        {
            Buffer = new(bufferSize);
        }

        public void Emit(LogEvent logEvent)
        {
            Buffer.Enqueue(logEvent.RenderMessage());
        }
    }
}
