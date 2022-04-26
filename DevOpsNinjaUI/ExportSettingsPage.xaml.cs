
namespace DevOpsNinjaUI
{
    using DevOpsNinjaUI.Models;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;
    using Microsoft.Xrm.Tooling.Connector;
    using Newtonsoft.Json;
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security;
    using System.ServiceModel;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Xml.Linq;
    using static System.Environment;
    /// <summary>
    /// Interaction logic for ExportSettingsPage.xaml
    /// </summary>
    public partial class ExportSettingsPage : Window
    {
        public ExportSettingsPage()
        {
            InitializeComponent();
            this.FillEnvironmentCombo();
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

        public CrmServiceClient CrmCleint { get; set; }

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

            if (this.Solutions == null || this.Solutions.Count == 0)
            {
                MessageBox.Show("Nothing to export.");
                return;
            }

            await Task.Factory.StartNew(async () =>
            {
                await AddProgressText($"Exporting solutions to {currentRunDirectory}");
                await ExportSolutions((from solution in
                                           Solutions
                                       where solution.Selected
                                       select solution.SolutionName).ToArray(), this.CrmCleint, currentRunDirectory);
            });

            await AddProgressText($"Solution export complete.");

            ////if (OnExportSubmitted != null)
            ////{
            ////    OnExportSubmitted(new D365SolutionEventArgs
            ////    {
            ////        SelectedSolutions = Solutions,
            ////        CrmSvcClient = this.CrmCleint
            ////    });
            ////}

            ////this.Close();
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
                        MessageBox.Show("OrganizationServiceProxy is null, please check if the environment is up and running and not in admin mode.");
                        ////btnConnect.IsEnabled = true;
                    });

                    return false;
                }

                CrmCleint = connection;
                CrmCleint.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(8);
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
                    MessageBox.Show(ex.Message);
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
            var response = CrmCleint.Execute(request); ;
            var results = response.Results.Values.FirstOrDefault().ToString();
            var xmlResult = XElement.Parse(results);
            var solutions = from xml in xmlResult.Descendants("result")
                            select
                            new D365Solution
                            {
                                SolutionName = xml.Element(XName.Get("uniquename")).Value,
                                FriendlyName = xml.Element(XName.Get("friendlyname")).Value,
                                IsManaged = xml.Element(XName.Get("isapimanaged")).Value == "0" ? false : true,
                                SolutionVersion = xml.Element(XName.Get("version")).Value,
                            };

            this.Solutions = new ObservableCollection<D365Solution>(solutions);
            dgSolutions.ItemsSource = this.Solutions;
        }

        private async void btnLoadUnmanaged_Click(object sender, RoutedEventArgs e)
        {
            if (drpEnv.SelectedItem == null)
            {
                MessageBox.Show("Please select an environment");
                return;
            }

            btnLoadUnmanaged.IsEnabled = false;
            if (CrmCleint == null)
            {
                if (await ConnectToDynamics())
                {
                    await ExecutFetchQuery(RESX.Resources.FetchLoadUnmanagedSolutions);
                }
            }
            else
            {
                await ExecutFetchQuery(RESX.Resources.FetchLoadUnmanagedSolutions);
            }

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
            this.CrmCleint = null;
            this.dgSolutions.ItemsSource = null;
        }

        private void btnCloneToSolution_Click(object sender, RoutedEventArgs e)
        {
            if (this.CrmCleint == null)
            {
                MessageBox.Show("Please connect to your dataverse instance", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parentSolutionName = from solution in this.Solutions
                                     where solution.Selected
                                     select solution.SolutionName;

            if (parentSolutionName.Count() <= 0)
            {
                MessageBox.Show("Please select the parent solution which you want to clone", "Select solution", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var response = this.CrmCleint.Execute(request) as CloneAsSolutionResponse;
                this.dgSolutions.ItemsSource = null;
                btnLoadUnmanaged_Click(null, null);
                MessageBox.Show("Solution merge completed.");

            }
            catch (FaultException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        async Task AddProgressText(string text)
        {
            Dispatcher.Invoke((Action)(async () =>
            {
                txtProgress.Text += $"{text}{System.Environment.NewLine}";
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

            args.Order = this.StepCollection.Count;

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

        private async Task<string> ExportSolutions(string[] sols, CrmServiceClient crmServiceClient, string currentRunDirectory)
        {
            if (sols.Length <= 0)
            {
                await OnDispatcher(() => { MessageBox.Show("No solutions to export"); });
                return "";
            }

            ////await AddProgressText($"Please wait while we export your {sols.Count()} solutions.");
            ////await AddProgressText($"Solution storage directory {currentRunDirectory}");
            CreateDirIfNotExists(currentRunDirectory);
            var solutionFile = string.Empty;

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
                    solutionFile = currentRunDirectory + $"{solutionItem}.zip";
                    await AddProgressText($"Export for {solutionItem} complete");
                    await AddProgressText($"Writing file {solutionItem}.zip");
                    File.WriteAllBytes(solutionFile, response.ExportSolutionFile);
                }
            }


            return solutionFile;

            ////stopWatch.Stop();

            ////await AddProgressText($"Exported {sols.Count()} solution(s), total time taken {stopWatch.Elapsed.Hours}H : {stopWatch.Elapsed.Minutes}M : {stopWatch.Elapsed.Seconds}S");
        }

        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (drpEnvImport.SelectedItem == null)
            {
                MessageBox.Show("Please select the target environment.");
                return;
            }

            var userResponse = MessageBox.Show($"Ok bro, so you have selected -{this.StepCollection.Count}- solutions for import, do you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (userResponse == MessageBoxResult.No)
            {
                return;
            }

            if (null == this.ProgressTracker)
            {
                this.ProgressTracker = new ObservableCollection<ProgressIndicator>();
            }

            this.ProgressTracker.Clear();

            btnImport.IsEnabled = false;
            var importSucessful = true;
            var stopWatchOverall = new Stopwatch();
            stopWatchOverall.Start();
            await AddProgressText("Please wait...");
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


            btnImport.IsEnabled = true;
            btnImport.Content = "Import";
            if (importSucessful)
            {
                await AddProgressText($"All solutions imported{ System.Environment.NewLine}");
            }
            else
            {
                await AddProgressText($"Import failed.");
            }

            await AddProgressText($"Overall time taken for import is { stopWatchOverall.Elapsed.Hours}hrs : { stopWatchOverall.Elapsed.Minutes}mins : { stopWatchOverall.Elapsed.Seconds}seconds");
            stopWatchOverall.Stop();
        }

        private async Task<dynamic> ImportSolutionAsyncRequest(AddSetpItemEventArgs step, string currentRunDirectory)
        {
            string importedfile = string.Empty;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await AddProgressText("Creating connection to dataverse export org");
            try
            {
                using (var svc = new CrmServiceClient(GetConnectionStringExport()))
                {
                    await AddProgressText($"Connected to {svc.ConnectedOrgFriendlyName}");
                    svc.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(8);
                    importedfile = await ExportSolutions(new string[] { step.SelectedSolutionUniqueName }, svc, currentRunDirectory);
                    await AddProgressText($"export time for {step.SelectedSolutionUniqueName} = {stopWatch.Elapsed.Hours}hrs :{stopWatch.Elapsed.Minutes}mins : {stopWatch.Elapsed.Seconds}seconds");
                    stopWatch.Restart();
                }
            }
            catch (Exception ex)
            {
                await AddProgressText(ex.Message);
            }

            await AddProgressText("Creating connection to dataverse Import org");

            try
            {
                using (var svcImport = new CrmServiceClient(GetConnectionStringImport()))
                {
                    svcImport.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(8);
                    await AddProgressText($"Connected to {svcImport.ConnectedOrgFriendlyName}");
                    var importJobId = Guid.NewGuid();

                    await AddNewProgressItem(step);

                    var importRequest = new ImportSolutionRequest
                    {
                        CustomizationFile = File.ReadAllBytes(importedfile),
                        HoldingSolution = step.IsUpgrade,
                        ImportJobId = importJobId,
                    };

                    var asyncRequest = new ExecuteAsyncRequest
                    {
                        Request = importRequest
                    };

                    await AddProgressText($"Import of solution {step.SelectedSolutionUniqueName} started. Import job ID {importJobId}");

                    svcImport.Execute(asyncRequest);
                    var importResponse = await WaitForImportComplete(svcImport, importJobId, step.SelectedSolutionUniqueName);
                    if (importResponse != null)
                    {
                        if (!importResponse.Success)
                        {
                            return await FailedResponse(step, importResponse);
                        }
                    }

                    await AddProgressText($"import of solution {step.SelectedSolutionUniqueName} complete.");
                    await AddProgressText($"import time taken for {step.SelectedSolutionUniqueName} = {stopWatch.Elapsed.Hours}hrs :{stopWatch.Elapsed.Minutes}mins : {stopWatch.Elapsed.Seconds}seconds");
                    stopWatch.Restart();

                    if (step.IsUpgrade)
                    {
                        await AddProgressText($"Taking a break of 1 min before upgrade.");
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        await AddProgressText($"Applying solution upgrade for the solution {step.SelectedSolutionUniqueName}");
                        var deleteAndPromoteRequest = new DeleteAndPromoteRequest
                        {
                            UniqueName = step.SelectedSolutionUniqueName,
                        };

                        var asyncRequestUpgrade = new ExecuteAsyncRequest
                        {
                            Request = deleteAndPromoteRequest
                        };

                        await AddNewProgressItem(step, step.SelectedSolutionUniqueName + " -Upgrade");
                        var response = svcImport.Execute(asyncRequestUpgrade) as ExecuteAsyncResponse;
                        var upgradeRequestId = response.AsyncJobId;
                        var upgradeResponse = await this.WaitForUpgradeComplete(svcImport, upgradeRequestId, step.SelectedSolutionUniqueName + " -Upgrade");

                        if (upgradeResponse != null)
                        {
                            if (!upgradeResponse.Success)
                            {
                                return await FailedResponse(step, upgradeResponse);
                            }
                        }

                        await AddProgressText($"Apply solution upgrade complete.");
                        await AddProgressText($"solution upgrade time taken for {step.SelectedSolutionUniqueName} = {stopWatch.Elapsed.Hours} :{stopWatch.Elapsed.Minutes} : {stopWatch.Elapsed.Seconds}");
                    }

                    await AddProgressText($"import of solution {step.SelectedSolutionUniqueName} completed");
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
                    SolutionName = string.IsNullOrEmpty(solutionName) ? step.SelectedSolutionUniqueName : solutionName,
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
            await AddProgressText($"{importResponse.Message}");
            await OnDispatcher(() =>
            {
                var progItem = this.ProgressTracker.FirstOrDefault(item => item.SolutionName == step.SelectedSolutionUniqueName);
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
                        if(code ==30)
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

                        else if(code ==31 || code == 32)
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

                Thread.Sleep(10000);
            } while (true);

            ///return importResponse;
        }
        private async Task<dynamic> WaitForImportComplete(CrmServiceClient svcImport, Guid importJobId, string solutionName)
        {
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
                                progItem.ProgressValue = float.Parse(job["progress"].ToString());
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
                return;
            }

            this.StepCollection.Clear();
            lstSteps.ItemsSource = this.StepCollection;
            lstProgressMeter.ItemsSource = null;
            txtProgress.Clear();
        }

        private void btnCopyLogs_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtProgress.Text);
            ////TestMethod();

        }

        private void TestMethod()
        {
            var guid = "202C0C2B-E8C4-EC11-983E-001DD8034FA4";
            using (var svcImport = new CrmServiceClient(GetConnectionStringImport()))
            {
                var job = svcImport.Retrieve("asyncoperation", Guid.Parse(guid), new ColumnSet(true));

            }

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
    }
}
