using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace CDKeyMiner
{
    static class PreventSleep
    {
        private static DispatcherTimer timer;

        static PreventSleep()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += StopSleep;
        }

        [FlagsAttribute]
        private enum EXECUTION_STATE : uint
        {
            ES_INVALID = 0,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public static void DisableSleep()
        {
            timer.Start();
        }

        public static void EnableSleep()
        {
            timer.Stop();
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        private static void StopSleep(object sender, EventArgs e)
        {
            var state = SetThreadExecutionState(
                EXECUTION_STATE.ES_CONTINUOUS |
                EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                EXECUTION_STATE.ES_AWAYMODE_REQUIRED);

            if (state == EXECUTION_STATE.ES_INVALID)
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
            }
        }
    }
}
