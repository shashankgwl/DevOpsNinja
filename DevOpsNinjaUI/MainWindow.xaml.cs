using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static System.Environment;

namespace DevOpsNinjaUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string[] solutionName = new string[]
           {
                "MCSHHS_STAAND_WebResources",
                "MCSHHS_STAAND_Workflows",
                "MCSHHS_STAAND_Config",
                "MCSHHS_STAAND_Dashboards",
                "MCSHHS_STAAND_EnvironmentVariables",
                "MCSHHS_STAAND_Plugins",
                "MCSHHS_STAAND_SecurityRoles",
                "MCSHHS_STAAND_Sitemaps"
           };



        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var exportSettings = new ExportSettingsPage()
            {
                ////AllSolutions = solutionName,
            };

            exportSettings.OnExportSubmitted += OnExportSubmitted;
            exportSettings.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            exportSettings.ShowDialog();
        }

        private async void OnExportSubmitted(Models.D365SolutionEventArgs solution)
        {
            if (solution.SelectedSolutions == null || solution.CrmSvcClient == null)
                return;

            btnExport.IsEnabled = false;
            btnExport.Content = "Please wait..";

            await Task.Factory.StartNew(async () =>
            {

                #region comment
                ////Thread.Sleep(1000);
                ////await AddProgressText(Environment.NewLine + "Starting Export");
                ////Thread.Sleep(1000);
                ////await AddProgressText("adding new line");
                ////Thread.Sleep(1000);
                ////await AddProgressText("adding new line1");
                ////Thread.Sleep(1000);
                ////await AddProgressText("adding new line2");
                ////Thread.Sleep(1000);
                ////await AddProgressText("Export complete");
                #endregion
                await ExportSolutions((from solItem
                                       in solution.SelectedSolutions
                                       where solItem.Selected
                                       select solItem.SolutionName).ToArray<string>(), solution.CrmSvcClient);
            });

            btnExport.Content = "Export";

            btnExport.IsEnabled = true;
        }



        private async Task ExportSolutions(string[] sols, CrmServiceClient crmServiceClient)
        {
            if(sols.Length <=0)
            {
                await OnDispatcher(() => { MessageBox.Show("No solutions received to export"); });
                return;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var conString = ""; //GetConnectionString();
            await AddProgressText($"Please wait while we export your {sols.Count()} solutions.");
            ////crmServiceClient.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(4);
            var currentRunDirectory = $"{Environment.GetFolderPath(SpecialFolder.LocalApplicationData)}\\DevOpsNinja\\{Guid.NewGuid()}\\";
            await AddProgressText($"Solution storage directory {currentRunDirectory}");
            CreateDirIfNotExists(currentRunDirectory);

            foreach (var solutionItem in sols)
            {
                var request = new ExportSolutionRequest
                {
                    Managed = true,
                    SolutionName = $"{solutionItem}"
                };

                ExportSolutionResponse response = null;
                bool hasError = false;
                await AddProgressText($"Now exporting {solutionItem}");
                try
                {
                    response = crmServiceClient.Execute(request) as ExportSolutionResponse;
                }
                catch (Exception error)
                {
                    hasError = true;
                    await AddProgressText(error.Message);
                    await AddProgressText(error.StackTrace);
                }

                if (!hasError && null != response)
                {
                    await AddProgressText($"Export for {solutionItem} complete");
                    await AddProgressText($"Writing file {solutionItem}.zip");
                    File.WriteAllBytes(currentRunDirectory + $"{solutionItem}.zip", response.ExportSolutionFile);
                }
            }

            stopWatch.Stop();

            await AddProgressText($"Exported total {sols.Count()} solutions, total time taken {stopWatch.Elapsed.Hours}H : {stopWatch.Elapsed.Minutes}M : {stopWatch.Elapsed.Seconds}S");
            await AddProgressText($"Export complete..");
            using (var svc = new CrmServiceClient(conString))
            {

            }
        }

        private static void CreateDirIfNotExists(string currentRunDirectory)
        {
            if (!Directory.Exists(currentRunDirectory))
            {
                Directory.CreateDirectory(currentRunDirectory);
            }
        }

        private async Task OnDispatcher(Action action)
        {
            Dispatcher.Invoke(action);
        }

        async Task AddProgressText(string text)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                txtProgress.Text += $"{text}{Environment.NewLine}";
            }));
        }

        private void txtProgress_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }
    }
}
