using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDKeyMiner.Hardware
{
    public struct GPUInfo
    {
        public string Provider;
        public string Description;
        public string DacType;
        public long MemoryBytes;
        public string DriverDate;
    }

    public struct CPUInfo
    {

    }

    public class HWReport
    {
        public List<GPUInfo> GPU;
        public List<CPUInfo> CPU;

        public HWReport()
        {
            GPU = new List<GPUInfo>();
            CPU = new List<CPUInfo>();
        }
    }

    public struct HWResponse
    {
        public string[] Algos;
        public string BestGPU;
    }
}
