using Newtonsoft.Json;
using ResultArchiver.Classes;
using ResultArchiver.Settings;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Topshelf;

namespace ResultArchiver
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {
                x.Service<MainService>(s =>
                {
                    s.ConstructUsing(mainService => new MainService());
                    s.WhenStarted(mainService => mainService.Start());
                    s.WhenStopped(mainService => mainService.Stop()); ;
                });

                x.RunAsLocalSystem();

                x.SetServiceName("ResultArchiverService");
                x.SetDisplayName("ResultArchiver Service");
                x.SetDescription("ResultArchiver Service");
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }
    }
}