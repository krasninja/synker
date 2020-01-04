using System;
using Topshelf;

namespace Synker.Service
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var rc = HostFactory.Run(hf =>
            {
                hf.Service<AppService>();
                hf.RunAsLocalService();

                hf.SetDescription("Synker - Settings Synchronization Service");
                hf.SetDisplayName("Synker");
                hf.SetServiceName("Synker");
            });

            var exitCode = (int) Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}
