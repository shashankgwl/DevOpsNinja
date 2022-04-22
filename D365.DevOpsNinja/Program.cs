using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;
using static System.Environment;

namespace D365.DevOpsNinja
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string sourceurl = "https://cfsa-dev1.crm9.dynamics.com";
            // e.g. you@yourorg.onmicrosoft.com
            // e.g. y0urp455w0rd 
            string AppId = "1421b392-3531-427e-99db-6a9fff01dc91";
            string ClientSecret = "vta7Q~35O9HM60ZS8f55oHgc-vhegJ1apd8W7";
            var solutionName = new string[]
            {
                "MCSHHS_STAAND_WebResources",
                "MCSHHS_STAAND_Config",
                "MCSHHS_STAAND_Dashboards",
                "MCSHHS_STAAND_EnvironmentVariables",
                "MCSHHS_STAAND_Plugins",
                "MCSHHS_STAAND_SecurityRoles",
                "MCSHHS_STAAND_Sitemaps"
            };
            string conn = $@"
    Url = {sourceurl};
    AuthType=ClientSecret;
    ClientId={AppId};
ClientSecret={ClientSecret}";


            var stopWatch = new Stopwatch();
            stopWatch.Start();
            using (var svc = new CrmServiceClient(conn))
            {
                svc.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(4);

                var currentRunDirectory = $"{Environment.GetFolderPath(SpecialFolder.LocalApplicationData)}\\DevOpsNinja\\{Guid.NewGuid()}\\";

                if (!Directory.Exists(currentRunDirectory))
                {
                    Directory.CreateDirectory(currentRunDirectory);
                }

                foreach (var solutionItem in solutionName)
                {
                    var request = new ExportSolutionRequest
                    {
                        Managed = true,
                        SolutionName = $"{solutionItem}"
                    };

                    Console.WriteLine($"Now exporting {solutionItem}");
                    var response = svc.Execute(request) as ExportSolutionResponse;
                    Console.WriteLine($"Export for {solutionItem} complete");
                    File.WriteAllBytes(currentRunDirectory + $"{solutionItem}.zip", response.ExportSolutionFile);
                    CloneAsSolutionRequest raq = new CloneAsSolutionRequest();
                    
                }


                stopWatch.Stop();

                Console.WriteLine($"Exported total {solutionName.Count()} solutions, total time taken {stopWatch.Elapsed.Hours} : {stopWatch.Elapsed.Minutes} : {stopWatch.Elapsed.Seconds}");
                Console.ReadLine();
            }
        }
    }
}
