using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CDKeyMiner
{
    class SerilogEventSink : ILogEventSink
    {
        private static SerilogEventSink inst;

        private SerilogEventSink() { }

        public static SerilogEventSink Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new SerilogEventSink();
                }
                return inst;
            }
        }

        public event EventHandler<string> OnLogMessage;

        public void Emit(LogEvent logEvent)
        {
            var ts = logEvent.Timestamp.ToString(@"[HH\:mm\:ss]");
            string severity = "";
            switch (logEvent.Level) {
                case LogEventLevel.Debug:
                    severity = "DEBUG";
                    break;
                case LogEventLevel.Error:
                    severity = "ERROR";
                    break;
                case LogEventLevel.Fatal:
                    severity = "FATAL";
                    break;
                case LogEventLevel.Information:
                    severity = "INFO";
                    break;
                case LogEventLevel.Verbose:
                    severity = "VERB";
                    break;
                case LogEventLevel.Warning:
                    severity = "WARN";
                    break;
            }
            var msg = $"{ts} {severity} - {logEvent.RenderMessage()}";
            OnLogMessage?.Invoke(inst, msg);
        }
    }
}
