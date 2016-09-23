using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PurgeESignTemporaryFiles
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        static void Main()
        {
#if DEBUG
            PurgeService s = new PurgeService();
            s.StartTimer(new TimeSpan(09, 36, 0), new TimeSpan(0, 2, 0));
            //var o = new object();
            //s.RunService(o);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new PurgeService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
