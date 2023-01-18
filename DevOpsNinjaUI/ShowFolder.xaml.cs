namespace DevOpsNinjaUI
{
    using DevOpsNinjaUI.Models;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using System.Xml.Linq;
    using WPFMessageUI = System.Windows;

    /// <summary>
    /// Interaction logic for ShowFolder.xaml
    /// </summary>
    public partial class ShowFolder : Window
    {
        public ShowFolder()
        {
            InitializeComponent();
        }

        public delegate void FolderSelectedHandler(FolderEventArgs args);

        public event FolderSelectedHandler OnFolderSelected;

        public ObservableCollection<AddSetpItemEventArgs> Solutions { get; set; }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txtPath.Text == string.Empty || !Directory.Exists(txtPath.Text))
            {
                WPFMessageUI.MessageBox.Show("Please provide a valid directory path");
                txtPath.Clear();
                return;
            }

            if (null == this.Solutions)
            {
                this.Solutions = new ObservableCollection<AddSetpItemEventArgs>();
            }
            else
            {
                this.Solutions.Clear();
            }

            var solutionFiles = Directory.GetFiles(txtPath.Text, "*.zip");

            if (solutionFiles.Length == 0)
            {
                WPFMessageUI.MessageBox.Show("No zip files found in folder.");
                return;
            }

            foreach (var solutionFile in solutionFiles)
            {
                using (var zipArchive = ZipFile.OpenRead(new FileInfo(solutionFile).FullName))
                {
                    foreach (var zipArchiveEntry in zipArchive.Entries)
                    {
                        if (zipArchiveEntry.Name.Equals("solution.xml", StringComparison.OrdinalIgnoreCase))
                        {
                            Stream stream = zipArchiveEntry.Open();
                            using (var memoryStream = new MemoryStream())
                            {
                                await stream.CopyToAsync(memoryStream);
                                var xDocument = XDocument.Parse(Encoding.UTF8.GetString(memoryStream.ToArray()));
                                var solutionName = xDocument.Descendants("SolutionManifest").Elements("UniqueName").FirstOrDefault().Value;
                                var solutionVersion = xDocument.Descendants("SolutionManifest").Elements("Version").FirstOrDefault().Value;
                                var exportType = xDocument.Descendants("SolutionManifest").Elements("Managed").FirstOrDefault().Value == "0" ? ExportType.Unmanaged : ExportType.Managed;
                                this.Solutions.Add(new AddSetpItemEventArgs
                                {
                                    Solution = new D365Solution
                                    {
                                        SolutionName = solutionName,
                                        SolutionVersion = solutionVersion
                                    },

                                    FullZipFilePath = solutionFile,
                                    IsSelectedForImport = true,
                                    IsUpgrade = false,
                                    SolutionExportType = exportType,
                                    Order = 0
                                });
                                ;
                            }
                        }
                    }

                    this.dgSolutions.ItemsSource = this.Solutions;
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (OnFolderSelected == null)
            {
                return;
            }

            if (this.Solutions == null)
            {
                WPFMessageUI.MessageBox.Show("No solution available for import");
                return;
            }

            var selectedSolutions = from solution in this.Solutions
                                    where solution.IsSelectedForImport
                                    orderby solution.Order
                                    select solution;

            if (selectedSolutions.Count() <= 0)
            {
                WPFMessageUI.MessageBox.Show("No solution selected for import");
                return;
            }

            this.Close();

            OnFolderSelected(new FolderEventArgs
            {
                FolderSolutions = new ObservableCollection<AddSetpItemEventArgs>(selectedSolutions)
            });

        }

        ////private void txtPath_KeyDown(object sender, WPFMessageUI.Input.KeyEventArgs e)
        ////{
        ////    if (e.Key == WPFMessageUI.Input.Key.Enter)
        ////    {
        ////        Button_Click(null, null);
        ////    }
        ////}
    }
}
