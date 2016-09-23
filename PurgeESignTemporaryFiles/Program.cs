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
            PurgeService service = new PurgeService();            
            var o = new object();
            service.RunService(o);
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
