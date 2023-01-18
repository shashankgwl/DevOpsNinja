
namespace DevOpsNinjaUI
{
    using DevOpsNinjaUI.Models;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;
    using Microsoft.Xrm.Tooling.Connector;
    using System;
    using System.Collections.ObjectModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.ServiceModel;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Xml.Linq;
    using ToastNotifications;
    using ToastNotifications.Lifetime;
    using ToastNotifications.Position;
    using ToastNotifications.Messages;

    using static System.Environment;
    using WPFMessageUI = System.Windows;
    /// <summary>
    /// Interaction logic for ExportSettingsPage.xaml
    /// </summary>
    public partial class ExportSettingsPage : Window
    {

        Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: WPFMessageUI.Application.Current.MainWindow,
                corner: Corner.BottomRight,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = WPFMessageUI.Application.Current.Dispatcher;
        });



        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }



        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public ExportSettingsPage()
        {
            InitializeComponent();
            this.FillEnvironmentCombo();
            this.WindowState = WindowState.Maximized;
        }

        private void FillEnvironmentCombo()
        {

            var envs = ConfigurationManager.AppSettings["COMMA_SEPARATED_EXPORT_ENVIRONMENT"].Split(',');
            foreach (var env in envs)
            {
                drpEnv.Items.Add(new ComboBoxItem { Content = env });
            }

            var envsImport = ConfigurationManager.AppSettings["COMMA_SEPARATED_IMPORT_ENVIRONMENT"].Split(',');
            foreach (var env in envsImport)
            {
                drpEnvImport.Items.Add(new ComboBoxItem { Content = env });
            }
        }

        private ObservableCollection<D365Solution> solutionModel;
        private string[] solutions;
        public delegate void ExportHandler(D365SolutionEventArgs solution);
        public event ExportHandler OnExportSubmitted;
        public ObservableCollection<D365Solution> Solutions
        {
            get
            {
                return solutionModel;
            }
            set
            {


                solutionModel = value;
            }
        }


        public string ClientIDExport
        {
            get
            {
                return ConfigurationManager.AppSettings["ClientIdExport"];
            }
        }

        public string ClientSecretExport
        {
            get
            {
                return ConfigurationManager.AppSettings["ClientSecretExport"];
            }
        }

        public string ClientIDImport
        {
            get
            {
                return ConfigurationManager.AppSettings["ClientIdImport"];
            }
        }

        public string ClientSecretImport
        {
            get
            {
                return ConfigurationManager.AppSettings["ClientSecretImport"];
            }
        }

        public string CurrentEnvironment
        {
            get
            {
                return (drpEnv.SelectedItem as ComboBoxItem).Content.ToString();
            }
        }

        private string GetConnectionStringImport()
        {
            string cstr = string.Empty;
            Dispatcher.Invoke(() =>
            {
                string env = (drpEnvImport.SelectedItem as ComboBoxItem).Content.ToString();

                cstr = $@"
    Url = {env};
    AuthType=ClientSecret;
    ClientId={this.ClientIDImport};
ClientSecret={this.ClientSecretImport}";
            });
            return cstr;
        }


        private string GetConnectionStringExport()
        {
            string cstr = string.Empty;
            Dispatcher.Invoke(() =>
            {
                string env = (drpEnv.SelectedItem as ComboBoxItem).Content.ToString();

                cstr = $@"
    Url = {env};
    AuthType=ClientSecret;
    ClientId={this.ClientIDExport};
ClientSecret={this.ClientSecretExport}";
            });
            return cstr;
        }

        ////public CrmServiceClient CrmCleint { get; set; }

        public ObservableCollection<AddSetpItemEventArgs> StepCollection { get; set; }

        public ObservableCollection<ProgressIndicator> ProgressTracker { get; set; }

        public string[] SelectedSolutions
        {
            get
            {
                return (from item in Solutions
                        where item.Selected == true
                        select item.SolutionName).ToArray<string>();
            }
        }

        private async void btnSelectSolution_Click(object sender, RoutedEventArgs e)
        {
            var currentRunDirectory = $"{GetFolderPath(SpecialFolder.LocalApplicationData)}\\DevOpsNinja\\{Guid.NewGuid()}\\";

            if (this.Solutions == null || (from solution in Solutions where solution.Selected select solution).Count() == 0)
            {
                WPFMessageUI.MessageBox.Show("Nothing to export.");
                return;
            }

            if (WPFMessageUI.MessageBox.Show($"Please confirm to export {(from solution in Solutions where solution.Selected select solution).Count()} solutions", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            btnSelectSolution.IsEnabled = false;
            await Task.Factory.StartNew(async () =>
        {
            await AddProgressText($"Exporting solutions to {currentRunDirectory}", true);
            await AddProgressText($"Solution export started at {DateTime.Now:MMM,dd,yyyy : HH:mm}");
            var file = await ExportSolutions((from solution in
                                       Solutions
                                              where solution.Selected
                                              select solution).ToArray(), this.ctlLoginSource.SConn, currentRunDirectory, ExportType.None, false);
        });

            await AddProgressText($"Solution export completed at {DateTime.Now:MMM,dd,yyyy : HH:mm}");
            ////await AddProgressText($"Solution export complete.", true);
            btnSelectSolution.IsEnabled = true;
        }

        private async Task OnDispatcher(Action action)
        {
            Dispatcher.Invoke(action);
        }

        private async Task<bool> ConnectToDynamics()
        {
            try
            {
                var constr = GetConnectionStringExport();
                var connection = new CrmServiceClient(constr);
                if (null == connection.OrganizationWebProxyClient)
                {
                    await OnDispatcher(async () =>
                    {
                        WPFMessageUI.MessageBox.Show("OrganizationServiceProxy is null, please check if the environment is up and running and not in admin mode.");
                        ////btnConnect.IsEnabled = true;
                    });

                    return false;
                }

                ////CrmCleint = connection;
                ////CrmCleint.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(8);
                return true;
                ////await OnDispatcher(async () =>
                ////{
                ////    MessageBox.Show($"Connected successfully to {CurrentEnvironment}");
                ////});
            }
            catch (System.Exception ex)
            {
                await OnDispatcher(async () =>
                {
                    WPFMessageUI.MessageBox.Show(ex.Message);
                    ////btnConnect.IsEnabled = true;
                });
                return false;
            }


            ////await Task.Factory.StartNew(async () =>
            ////{

            ////});

        }

        private async Task ExecutFetchQuery(string query)
        {
            var request = new ExecuteFetchRequest { FetchXml = query };
            var response = ctlLoginSource.SConn.Execute(request); ;
            string url = string.Empty;

            var results = response.Results.Values.FirstOrDefault().ToString();
            var xmlResult = XElement.Parse(results);
            if (ctlLoginSource.SConn != null && ctlLoginSource.SConn.OrganizationDetail != null && ctlLoginSource.SConn.OrganizationDetail.Endpoints != null & ctlLoginSource.SConn.OrganizationDetail.Endpoints.Count > 0)
            {
                url = ctlLoginSource.SConn.OrganizationDetail.Endpoints.First().Value + "tools/solution/edit.aspx?id={0}";
            }
            var solutions = from xml in xmlResult.Descendants("result")
                            select
                            new D365Solution
                            {
                                SolutionName = xml.Element(XName.Get("uniquename")).Value,
                                FriendlyName = xml.Element(XName.Get("friendlyname")).Value,
                                IsManaged = xml.Element(XName.Get("isapimanaged")).Value == "0" ? false : true,
                                SolutionVersion = xml.Element(XName.Get("version")).Value,
                                URL = string.Format(url, xml.Element(XName.Get("solutionid")).Value)
                            };

            this.Solutions = new ObservableCollection<D365Solution>(solutions);
            dgSolutions.ItemsSource = this.Solutions;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var url = ((e.OriginalSource as System.Windows.Documents.Hyperlink).DataContext as D365Solution).URL;
            Process.Start(url);
        }

        private async void btnLoadUnmanaged_Click(object sender, RoutedEventArgs e)
        {
            if (drpEnv.SelectedItem == null)
            {
                WPFMessageUI.MessageBox.Show("Please select an environment");
                return;
            }

            if (ctlLoginSource.SConn == null)
            {
                WPFMessageUI.MessageBox.Show("Connection not available. Please login using the login button.");
                return;
            }

            await ExecutFetchQuery(RESX.Resources.FetchLoadUnmanagedSolutions);
            ////var resp = ctlLoginSource.SConExport.Execute(new ExecuteFetchRequest { FetchXml = RESX.Resources.FetchLoadUnmanagedSolutions });

            btnLoadUnmanaged.IsEnabled = false;
            ////if (CrmCleint == null)
            ////{
            ////    if (await ConnectToDynamics())
            ////    {
            ////        await ExecutFetchQuery(RESX.Resources.FetchLoadUnmanagedSolutions);
            ////    }
            ////}
            ////else
            ////{
            ////    await ExecutFetchQuery(RESX.Resources.FetchLoadUnmanagedSolutions);
            ////}

            btnLoadUnmanaged.IsEnabled = true;

        }

        private void btnfilter_Click(object sender, RoutedEventArgs e)
        {
            if (txtFilter.Text.Length <= 0 || this.Solutions == null)
            {
                dgSolutions.ItemsSource = this.Solutions;
                return;
            }

            dgSolutions.ItemsSource = new ObservableCollection<D365Solution>(
                from solution in this.Solutions
                where solution.SolutionName.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0
                select solution
                );
        }

        private void drpEnv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ctlLoginSource.SConn = null;
            ctlLoginSource.Enabled = true;
            this.dgSolutions.ItemsSource = null;
            ctlLoginSource.OrgUrl = (drpEnv.SelectedItem as ComboBoxItem).Content.ToString();
        }

        private void btnCloneToSolution_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlLoginSource.SConn == null)
            {
                WPFMessageUI.MessageBox.Show("Please connect to your dataverse instance", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parentSolutionName = from solution in this.Solutions
                                     where solution.Selected
                                     select solution.SolutionName;

            if (parentSolutionName.Count() <= 0)
            {
                WPFMessageUI.MessageBox.Show("Please select the parent solution which you want to clone", "Select solution", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CloneAsSolution window = new CloneAsSolution();
            window.OnCloneSolution += Window_OnCloneSolution;
            window.ParentSolutionName = parentSolutionName.FirstOrDefault();
            window.ShowDialog();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        }

        private void Window_OnCloneSolution(CloneSolutionEventArgs args)
        {
            dgSolutions.ItemsSource = null;
            var request = new CloneAsSolutionRequest();
            request.DisplayName = args.DisplayName;
            request.VersionNumber = args.Version;
            request.ParentSolutionUniqueName = args.ParentSolutionName;

            try
            {
                var response = this.ctlLoginSource.SConn.Execute(request) as CloneAsSolutionResponse;
                this.dgSolutions.ItemsSource = null;
                btnLoadUnmanaged_Click(null, null);
                WPFMessageUI.MessageBox.Show("Solution merge completed.");

            }
            catch (FaultException ex)
            {
                WPFMessageUI.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        async Task AddProgressText(string text, bool changeTitle = false)
        {
            Dispatcher.Invoke((Action)(async () =>
            {
                txtProgress.Text += $"{text}{System.Environment.NewLine}";
                if (changeTitle)
                {
                    this.Title = text;
                }
            }));
        }

        private void btnAddStep_Click(object sender, RoutedEventArgs e)
        {
            var addStepWindow = new AddStepWindow();
            addStepWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            addStepWindow.Solutions = this.Solutions;
            addStepWindow.OnStepAdded += AddStepWindow_OnStepAdded;
            addStepWindow.ShowDialog();
        }

        private void AddStepWindow_OnStepAdded(AddSetpItemEventArgs args)
        {
            if (this.StepCollection == null)
            {
                this.StepCollection = new ObservableCollection<AddSetpItemEventArgs>();
                args.Order = 1;
            }

            args.Order = (short)this.StepCollection.Count;

            this.StepCollection.Add(args);
            lstSteps.ItemsSource = this.StepCollection;
        }

        private static void CreateDirIfNotExists(string currentRunDirectory)
        {
            if (!Directory.Exists(currentRunDirectory))
            {
                Directory.CreateDirectory(currentRunDirectory);
            }
        }

        private async Task<string> ExportSolutions(D365Solution[] sols, CrmServiceClient crmServiceClient, string currentRunDirectory, ExportType exportType, bool fromImport)
        {
            if (sols.Length <= 0)
            {
                await OnDispatcher(() => { WPFMessageUI.MessageBox.Show("No solutions to export"); });
                return "";
            }

            ////await AddProgressText($"Please wait while we export your {sols.Count()} solutions.");
            ////await AddProgressText($"Solution storage directory {currentRunDirectory}");
            CreateDirIfNotExists(currentRunDirectory);
            var solutionFile = string.Empty;

            foreach (var solutionItem in sols)
            {
                var managedUnmanaged = fromImport ? exportType == ExportType.Managed ? true : false : solutionItem.ExportingAsManaged;
                var managedUnmanagedStr = managedUnmanaged ? "Managed" : "Unmanaged";
                var request = new ExportSolutionAsyncRequest
                {
                    Managed = managedUnmanaged,
                    SolutionName = $"{solutionItem.SolutionName}",
                };

                ExportSolutionAsyncResponse response = null;
                bool hasError = false;
                await AddProgressText($"Now exporting {solutionItem.SolutionName} : {managedUnmanagedStr} ");
                DownloadSolutionExportDataResponse downloadResponse = new DownloadSolutionExportDataResponse();
                try
                {
                    response = crmServiceClient.Execute(request) as ExportSolutionAsyncResponse;
                    do
                    {
                        try
                        {
                            var job = crmServiceClient.Retrieve("asyncoperation", response.AsyncOperationId, new ColumnSet(true));
                            if (job.Contains("statuscode"))
                            {
                                var code = (job["statuscode"] as OptionSetValue).Value;
                                var statusCodeText = GetStatusTextByCode(code);
                                if (code == 30)
                                {
                                    DownloadSolutionExportDataRequest download = new DownloadSolutionExportDataRequest { ExportJobId = response.ExportJobId };
                                    downloadResponse = crmServiceClient.Execute(download) as DownloadSolutionExportDataResponse;

                                    solutionFile = currentRunDirectory + $"{solutionItem.SolutionName}.zip";
                                    await AddProgressText($"Export for {solutionItem.SolutionName} complete", true);
                                    await AddProgressText($"Writing file {solutionItem.SolutionName}.zip");
                                    File.WriteAllBytes(solutionFile, downloadResponse.ExportSolutionFile);
                                    break;
                                }
                                else if (code == 31 || code == 32)
                                {
                                    await AddProgressText($"Import of solution failed. Message : {job["friendlymessage"]}");
                                    hasError = true;
                                    break;
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            hasError = true;
                            await AddProgressText(error.Message);
                            await AddProgressText(error.StackTrace);
                            await ShowToast(error.Message);
                        }
                        Thread.Sleep(6000);
                    } while (true);
                }
                catch (Exception error)
                {
                    hasError = true;
                    await AddProgressText(error.Message);
                    await AddProgressText(error.StackTrace);
                    await ShowToast(error.Message);
                }

                if (!hasError && null != response)
                {
                    solutionFile = currentRunDirectory + $"{solutionItem.SolutionName}.zip";
                    await AddProgressText($"Export for {solutionItem.SolutionName} complete", true);
                    await AddProgressText($"Writing file {solutionItem.SolutionName}.zip");
                    File.WriteAllBytes(solutionFile, downloadResponse.ExportSolutionFile);
                }
            }
            return solutionFile;

            ////stopWatch.Stop();
            ////await AddProgressText($"Exported {sols.Count()} solution(s), total time taken {stopWatch.Elapsed.Hours}H : {stopWatch.Elapsed.Minutes}M : {stopWatch.Elapsed.Seconds}S");
        }

        private async Task ShowToast(string message)
        {
            await OnDispatcher(() =>
            {
                notifier.ShowError(message);
            });

        }

        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (drpEnvImport.SelectedItem == null || this.StepCollection == null || this.StepCollection.Count <= 0)
            {
                WPFMessageUI.MessageBox.Show("Please select the target environment and solutions.");
                return;
            }

            if (null == ctlLoginTarget.SConn)
            {
                WPFMessageUI.MessageBox.Show("Please login to the target environment using the login button.", "Error", MessageBoxButton.OK);
                return;
            }

            var userResponse = WPFMessageUI.MessageBox.Show($"Alright, so you have selected -{this.StepCollection.Count}- solutions for import, do you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (userResponse == MessageBoxResult.No)
            {
                return;
            }

            bool importSucessful;
            Stopwatch stopWatchOverall;
            PreImportOperations(out importSucessful, out stopWatchOverall);
            await AddProgressText("Please wait...");
            await AddProgressText($"Solution import started at {DateTime.Now:MMM,dd,yyyy : HH:mm}");

            await Task.Factory.StartNew(async () =>
            {
                var currentRunDirectory = $"{GetFolderPath(SpecialFolder.LocalApplicationData)}\\DevOpsNinja\\{Guid.NewGuid()}\\";
                await AddProgressText($"Exporting solutions to {currentRunDirectory}");
                await OnDispatcher(() => { lstProgressMeter.ItemsSource = this.ProgressTracker; });
                foreach (var step in this.StepCollection)
                {
                    var importResponse = await ImportSolutionAsyncRequest(step, currentRunDirectory);
                    if (importResponse != null)
                    {
                        if (!importResponse.Success)
                        {
                            importSucessful = false;
                            break;
                        }
                    }
                }
            });
            await AddProgressText($"Solution import completed at {DateTime.Now:MMM,dd,yyyy : HH:mm}");

            await PostImportOperations(importSucessful, stopWatchOverall);
        }

        private async Task PostImportOperations(bool importSucessful, Stopwatch stopWatchOverall)
        {
            btnImport.IsEnabled = true;
            btnImport.Content = "Import";
            btnLoadFolder.IsEnabled = true;
            if (importSucessful)
            {
                await AddProgressText($"All solutions imported{System.Environment.NewLine}", true);
            }
            else
            {
                await AddProgressText($"Import failed.", true);
            }

            await AddProgressText($"Overall time taken for import is {stopWatchOverall.Elapsed.Hours}hrs : {stopWatchOverall.Elapsed.Minutes}mins : {stopWatchOverall.Elapsed.Seconds}seconds");
            stopWatchOverall.Stop();
        }

        private void PreImportOperations(out bool importSucessful, out Stopwatch stopWatchOverall)
        {
            if (null == this.ProgressTracker)
            {
                this.ProgressTracker = new ObservableCollection<ProgressIndicator>();
            }

            this.ProgressTracker.Clear();

            btnImport.IsEnabled = false;
            btnLoadFolder.IsEnabled = false;
            importSucessful = true;
            stopWatchOverall = new Stopwatch();
            stopWatchOverall.Start();
        }

        private async Task<dynamic> ImportSolutionAsyncRequest(AddSetpItemEventArgs step, string currentRunDirectory, bool exportRequired = true, string zipfilePath = "")
        {
            string importedfile = string.Empty;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            if (!exportRequired && string.IsNullOrEmpty(zipfilePath))
            {
                await OnDispatcher(() =>
                {
                    WPFMessageUI.MessageBox.Show("Solution zip file path not available. Please check again.");
                });

                return null;
            }

            if (!exportRequired)
            {
                importedfile = zipfilePath;
            }

            if (exportRequired)
            {
                await AddProgressText("Creating connection to dataverse export org");
                try
                {
                    ////using (var svc = new CrmServiceClient(GetConnectionStringExport()))
                    {
                        await AddProgressText($"Connected to {ctlLoginSource.SConn.ConnectedOrgFriendlyName}");
                        ////svc.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(8);
                        importedfile = await ExportSolutions(new D365Solution[] { step.Solution }, ctlLoginSource.SConn, currentRunDirectory, step.SolutionExportType, true);
                        await AddProgressText($"export time for {step.Solution.SolutionName} = {stopWatch.Elapsed.Hours}hrs :{stopWatch.Elapsed.Minutes}mins : {stopWatch.Elapsed.Seconds}seconds");

                        stopWatch.Restart();
                    }
                }
                catch (Exception ex)
                {
                    await AddProgressText(ex.Message);
                    await ShowToast(ex.Message);
                }
            }

            await AddProgressText("Creating connection to dataverse Import org");

            try
            {
                ////using (var svcImport = new CrmServiceClient(GetConnectionStringImport()))
                {
                    ////ctlLoginTarget.SConn.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(8);
                    await AddProgressText($"Connected to {ctlLoginTarget.SConn.ConnectedOrgFriendlyName}");
                    var importJobId = Guid.NewGuid();

                    await AddNewProgressItem(step);

                    ////var importRequest = 

                    var asyncRequest = new ExecuteAsyncRequest
                    {
                        Request = new ImportSolutionRequest
                        {
                            CustomizationFile = File.ReadAllBytes(importedfile),
                            HoldingSolution = step.IsUpgrade,
                            ImportJobId = importJobId,
                            PublishWorkflows = true,
                            OverwriteUnmanagedCustomizations = true,
                        }
                    };


                    var asresponse = ctlLoginTarget.SConn.Execute(asyncRequest) as ExecuteAsyncResponse;
                    // response.
                    await AddProgressText($"Import of solution {step.Solution.SolutionName} started. Import job ID {importJobId}");
                    var importResponse = await WaitForImportComplete(ctlLoginTarget.SConn, importJobId, step.Solution.SolutionName);
                    if (importResponse != null)
                    {
                        if (!importResponse.Success)
                        {
                            return await FailedResponse(step, importResponse);
                        }
                    }

                    await AddProgressText($"import of solution {step.Solution.SolutionName} complete.");
                    await AddProgressText($"import time taken for {step.Solution.SolutionName} = {stopWatch.Elapsed.Hours}hrs :{stopWatch.Elapsed.Minutes}mins : {stopWatch.Elapsed.Seconds}seconds");
                    stopWatch.Restart();

                    if (step.IsUpgrade)
                    {
                        await AddProgressText($"Taking a break of 1 min before upgrade.");
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        await AddProgressText($"Applying solution upgrade for the solution {step.Solution.SolutionName}");
                        var asyncRequestUpgrade = new ExecuteAsyncRequest
                        {
                            Request = new DeleteAndPromoteRequest
                            {
                                UniqueName = step.Solution.SolutionName,
                            }
                        };

                        await AddNewProgressItem(step, step.Solution.SolutionName + " -Upgrade");
                        var response = ctlLoginTarget.SConn.Execute(asyncRequestUpgrade) as ExecuteAsyncResponse;
                        var upgradeRequestId = response.AsyncJobId;
                        var upgradeResponse = await this.WaitForUpgradeComplete(ctlLoginTarget.SConn, upgradeRequestId, step.Solution.SolutionName + " -Upgrade");

                        if (upgradeResponse != null)
                        {
                            if (!upgradeResponse.Success)
                            {
                                return await FailedResponse(step, upgradeResponse);
                            }
                        }

                        await AddProgressText($"Apply solution upgrade complete.", true);
                        await AddProgressText($"solution upgrade time taken for {step.Solution.SolutionName} = {stopWatch.Elapsed.Hours} :{stopWatch.Elapsed.Minutes} : {stopWatch.Elapsed.Seconds}");
                    }

                    return importResponse;
                }
            }
            catch (Exception ex)
            {
                await AddProgressText(ex.Message);
                return new
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private async Task AddNewProgressItem(AddSetpItemEventArgs step, string solutionName = "")
        {
            await OnDispatcher(() =>
            {
                var progressItem = new ProgressIndicator
                {
                    SolutionName = string.IsNullOrEmpty(solutionName) ? step.Solution.SolutionName : solutionName,
                    ProgressValue = 0.0F,
                    Status = "Queued"
                };

                this.ProgressTracker.Add(progressItem);
                this.lstProgressMeter.ItemsSource = this.ProgressTracker;
                progressItem.NotifyAll();
            });
        }

        private async Task<dynamic> FailedResponse(AddSetpItemEventArgs step, dynamic importResponse)
        {
            await AddProgressText($"Import failed with below message");
            await ShowToast(importResponse.Message);
            await AddProgressText($"{importResponse.Message}");
            await OnDispatcher(() =>
            {
                var progItem = this.ProgressTracker.FirstOrDefault(item => item.SolutionName == step.Solution.SolutionName);
                if (progItem != null)
                {
                    progItem.ProgressValue = 0f;
                    progItem.Status = "Failed";
                    progItem.NotifyAll();
                }
            });
            return importResponse;
        }

        private string GetStatusTextByCode(int code)
        {
            switch (code)
            {
                case 0:
                    return "Waiting For Resources";
                case 10:
                    return "Waiting";
                case 20:
                    return "In Progress";
                case 21:
                    return "Pausing";
                case 22:
                    return "Canceling";
                case 30:
                    return "Succeeded";
                case 31:
                    return "Failed";
                case 32:
                    return "Canceled";
                default:
                    return "NOSTATUS";
            }
        }

        private async Task<dynamic> WaitForUpgradeComplete(CrmServiceClient svcImport, Guid upgradeJobId, string solutionName)
        {
            ////dynamic importResponse = null;
            do
            {
                try
                {
                    var job = svcImport.Retrieve("asyncoperation", upgradeJobId, new ColumnSet(true));
                    var progItem = this.ProgressTracker.FirstOrDefault(item => item.SolutionName == solutionName);

                    if (job.Contains("statuscode"))
                    {
                        var code = (job["statuscode"] as OptionSetValue).Value;
                        var statusCodeText = GetStatusTextByCode(code);
                        if (code == 30)
                        {
                            await OnDispatcher(() =>
                            {
                                if (progItem != null)
                                {
                                    progItem.ProgressValue = 100f;
                                    progItem.Status = "Complete";
                                    progItem.NotifyAll();
                                }
                            });
                            return new
                            {
                                Success = true,
                                Message = "Successful."
                            };
                        }

                        else if (code == 31 || code == 32)
                        {
                            await OnDispatcher(() =>
                            {
                                if (progItem != null)
                                {
                                    progItem.ProgressValue = 0;
                                    progItem.Status = "Failed";
                                    progItem.NotifyAll();
                                }
                            });

                            return new
                            {
                                Success = false,
                                Message = "Failed"
                            };
                        }

                        else
                        {
                            await OnDispatcher(() =>
                            {
                                if (progItem != null)
                                {
                                    progItem.ProgressValue = code;
                                    progItem.Status = statusCodeText;
                                    progItem.NotifyAll();
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new
                    {
                        Success = false,
                        Message = ex.Message
                    };
                }

                Thread.Sleep(6000);
            } while (true);

            ///return importResponse;
        }
        private async Task<dynamic> WaitForImportComplete(CrmServiceClient svcImport, Guid importJobId, string solutionName)
        {
            await AddProgressText($"Waiting for 30 seconds for the import job ID to become available in the system.");
            Thread.Sleep(TimeSpan.FromSeconds(30));
            await AddProgressText($"Import started.", true);
            dynamic importResponse = null;
            do
            {
                try
                {
                    var job = svcImport.Retrieve("importjob", importJobId, new ColumnSet(true));
                    var progItem = this.ProgressTracker.FirstOrDefault(item => item.SolutionName == solutionName);

                    if (job.Contains("progress"))
                    {
                        await OnDispatcher(() =>
                        {
                            if (progItem != null)
                            {
                                progItem.ProgressValue = Math.Round(double.Parse(job["progress"].ToString()));
                                progItem.Status = "Importing";
                                progItem.NotifyAll();
                            }
                        });
                    }

                    if (job.Contains("completedon"))
                    {
                        importResponse = this.CheckeFailureAndReturnMessage(job["data"].ToString());
                        if (!importResponse.Success)
                        {
                            if (progItem != null)
                            {
                                progItem.ProgressValue = 0f;
                                progItem.Status = "Failed";
                                progItem.NotifyAll();
                            }
                        }

                        else
                        {
                            if (progItem != null)
                            {
                                progItem.ProgressValue = float.Parse(job["progress"].ToString()); ;
                                progItem.Status = "Completed";
                                progItem.NotifyAll();
                            }
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    return new
                    {
                        Success = false,
                        Message = ex.Message
                    };
                }

                Thread.Sleep(10000);
            } while (true);

            return importResponse;
        }

        private object CheckeFailureAndReturnMessage(string xmlData)
        {
            if (string.IsNullOrEmpty(xmlData))
            {
                return null;
            }

            var xml = XDocument.Parse(xmlData);
            string failedText = string.Empty;
            if (xml != null && xml.Document != null && xml.Document.Element(XName.Get("importexportxml")) != null)
            {
                var failureMessage = xml.FirstNode.Document.Element("importexportxml").Attribute("succeeded");
                if (failureMessage != null && failureMessage.Value == "failure")
                {
                    failedText = xml.FirstNode.Document.Element("importexportxml").Attribute("status").Value;
                }
            }

            return new
            {
                Success = failedText.Length == 0,
                Message = failedText
            };
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (this.StepCollection == null || this.StepCollection.Count() <= 0)
            {
                txtProgress.Clear();
                return;
            }

            this.StepCollection.Clear();
            lstSteps.ItemsSource = this.StepCollection;
            lstProgressMeter.ItemsSource = null;
            txtProgress.Clear();
        }

        private void btnCopyLogs_Click(object sender, RoutedEventArgs e)
        {
            WPFMessageUI.Clipboard.SetText(txtProgress.Text);
            ////TestMethod();

        }

        private void TestMethod()
        {
            ////Task.Factory.StartNew(() =>
            ////{
            ////    Thread.Sleep(2000);
            ////    OnDispatcher(() =>
            ////    {
            ////    });
            ////});
        }



        private void btnCopy1_Click(object sender, RoutedEventArgs e)
        {
            var failed = this.ProgressTracker.First(item => item.Status == "Failed");
            failed.Status = "Complete";
            failed.ProgressValue = 100f;
            failed.NotifyPropertyChanged("status");
            failed.NotifyPropertyChanged("ProgressValue");
            lstProgressMeter.ItemsSource = this.ProgressTracker;
        }

        private void btnLoadFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ctlLoginTarget.SConn == null)
            {
                WPFMessageUI.MessageBox.Show("Please first login to the target environment using login button.");
                return;
            }

            var showFolder = new ShowFolder();
            showFolder.OnFolderSelected += ShowFolder_OnFolderSelected;
            showFolder.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            showFolder.ShowDialog();
        }

        private async void ShowFolder_OnFolderSelected(FolderEventArgs args)
        {
            if (args.FolderSolutions.Count == 0)
            {
                return;
            }

            var userResponse = WPFMessageUI.MessageBox.Show($"Alright, so you have selected -{args.FolderSolutions.Count}- solutions for import, do you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (userResponse == MessageBoxResult.No)
            {
                return;
            }

            this.StepCollection = args.FolderSolutions;
            lstSteps.ItemsSource = this.StepCollection;

            if (drpEnvImport.SelectedItem == null)
            {
                WPFMessageUI.MessageBox.Show("Please select the target environment and solutions.");
                return;
            }


            bool importSucessful;
            Stopwatch stopWatchOverall;
            PreImportOperations(out importSucessful, out stopWatchOverall);
            await Task.Factory.StartNew(async () =>
            {
                foreach (var step in args.FolderSolutions)
                {
                    var importResponse = await ImportSolutionAsyncRequest(step, "", false, step.FullZipFilePath);
                    if (importResponse != null)
                    {
                        if (!importResponse.Success)
                        {
                            importSucessful = false;
                            break;
                        }
                    }
                }
            });

            await PostImportOperations(importSucessful, stopWatchOverall);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
        }

        private void btnOpenSettings_Click(object sender, RoutedEventArgs e)
        {


            // Process.Start(new ProcessStartInfo("chrome.exe", settingsUrl));
        }

        private void cmbOpenSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null == drpEnv.SelectedItem)
            {
                WPFMessageUI.MessageBox.Show("Please select an environment.");
                drpEnv.IsDropDownOpen = true;
                return;
            }

            OpenDynamicsSettingsPage(drpEnv.SelectedItem as ComboBoxItem, cmbOpenSettings.SelectedIndex);
        }

        private void OpenDynamicsSettingsPage(ComboBoxItem item, int selectedIndex)
        {
            var settingsUrl = (item.Content.ToString() + "/main.aspx?settingsonly=true").Trim();

            switch (selectedIndex)
            {
                case 0:
                    Process.Start(settingsUrl);
                    break;

                case 1:
                    if (Process.GetProcessesByName("chrome").Length > 0)
                    {
                        Process.Start(new ProcessStartInfo("chrome", settingsUrl));
                    }
                    else
                    {
                        WPFMessageUI.MessageBox.Show("Please check if Chrome is installed on this machine.");
                    }
                    break;

                case 2:
                    if (Process.GetProcessesByName("opera").Length > 0)
                    {
                        Process.Start(new ProcessStartInfo("opera", settingsUrl));
                    }
                    else
                    {
                        WPFMessageUI.MessageBox.Show("Please check if Opera is installed on this machine.");
                    }
                    break;

                case 3:
                    if (Process.GetProcessesByName("firefox").Length > 0)
                    {
                        Process.Start(new ProcessStartInfo("firefox", settingsUrl));
                    }
                    else
                    {
                        WPFMessageUI.MessageBox.Show("Please check if firefox is installed on this machine.");
                    }
                    break;

                default:
                    break;
            }
        }

        private void cmbOpenSettingsTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null == drpEnvImport.SelectedItem)
            {
                WPFMessageUI.MessageBox.Show("Please select an environment.");
                drpEnvImport.IsDropDownOpen = true;
                return;
            }

            OpenDynamicsSettingsPage(drpEnvImport.SelectedItem as ComboBoxItem, cmbOpenSettingsTarget.SelectedIndex);
        }

        private void drpEnvImport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != ctlLoginTarget)
            {
                ctlLoginTarget.Enabled = true;
                ctlLoginTarget.OrgUrl = (drpEnvImport.SelectedItem as ComboBoxItem).Content.ToString();
                ctlLoginTarget.SConn = null;
            }
        }

        private void btnAddEnv_Click(object sender, RoutedEventArgs e)
        {
            if ((e.OriginalSource as WPFMessageUI.Controls.Button).Name == "btnAddEnvSource")
                popSourceEnv.IsOpen = true;
            else if ((e.OriginalSource as WPFMessageUI.Controls.Button).Name == "btnAddTargetEnv")
                popTargetEnv.IsOpen = true;
        }

        private void btnAddEnvironment_Click(object sender, RoutedEventArgs e)
        {
            string urlTextBox = null;
            WPFMessageUI.Controls.ComboBox combo = null;
            bool sourcePopup = false;
            bool targetPopup = false;
            if ((e.OriginalSource as WPFMessageUI.Controls.Button).Name == "btnAddEnvironmentTarget")
            {
                urlTextBox = txtURLTarget.Text;
                combo = drpEnvImport;
                targetPopup = true;
            }
            else if ((e.OriginalSource as WPFMessageUI.Controls.Button).Name == "btnAddEnvironmentSource")
            {
                urlTextBox = txtURLSource.Text;
                combo = drpEnv;
                sourcePopup = true;
            }

            Uri uriResult;
            bool correctURL = Uri.TryCreate(urlTextBox, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (correctURL)
            {
                var newItem = new ComboBoxItem { Content = urlTextBox };
                combo.Items.Add(newItem);
                if (sourcePopup)
                    popSourceEnv.IsOpen = false;
                else if (targetPopup)
                    popTargetEnv.IsOpen = false;
                combo.SelectedItem = newItem;
                if (combo == drpEnv)
                    drpEnv_SelectionChanged(null, null);
                else if (combo == drpEnvImport)
                    drpEnvImport_SelectionChanged(null, null);
            }

            else
            {
                WPFMessageUI.MessageBox.Show("Incorrect URL format", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, WPFMessageUI.MessageBoxOptions.DefaultDesktopOnly);
            }

        }

        private void btnClosePopup_Click(object sender, RoutedEventArgs e)
        {
            if ((e.OriginalSource as WPFMessageUI.Controls.Button).Name == "btnClosePopupSource")
                popSourceEnv.IsOpen = false;
            else if ((e.OriginalSource as WPFMessageUI.Controls.Button).Name == "btnClosePopupTarget")
                popTargetEnv.IsOpen = false;
        }
    }
}
