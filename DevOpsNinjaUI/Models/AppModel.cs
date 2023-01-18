using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DevOpsNinjaUI.Models
{
    internal class AppModel
    {
    }


    public class D365SolutionEventArgs : EventArgs
    {
        public IEnumerable<D365Solution> SelectedSolutions
        {
            get; set;
        }

        public CrmServiceClient CrmSvcClient { get; set; }
    }

    public enum SolutionStep
    {
        ImportSolutionStep,
        WaitStep
    }

    public class CloneSolutionEventArgs : EventArgs
    {
        public string DisplayName { get; set; }

        public string Version { get; set; }

        public string ParentSolutionName { get; set; }
    }

    public class ProgressIndicator : INotifyPropertyChanged
    {
        public string SolutionName { get; set; }

        public double ProgressValue { get; set; }

        public string Status { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void NotifyAll()
        {
            NotifyPropertyChanged("SolutionName");
            NotifyPropertyChanged("ProgressValue");
            NotifyPropertyChanged("Status");
        }
    }

    public class FolderEventArgs : EventArgs
    {
        public ObservableCollection<AddSetpItemEventArgs> FolderSolutions { get; set; }
    }

    public class AddSetpItemEventArgs : EventArgs
    {

        public D365Solution Solution { get; set; }


        public short Order { get; set; }
        public bool IsUpgrade { get; set; }

        public ExportType SolutionExportType { get; set; }

        public bool IsSelectedForImport { get; set; }

        public string FullZipFilePath { get; set; }
        public string UpgradeMessage
        {
            get
            {
                return IsUpgrade ? "Solution upgrade : True" : "No solution upgrade";
            }

            set
            {

            }
        }
        public SolutionStep Step { get; set; }
    }
    public class Environment
    {
        public string EnvironmentDisplyname { get; set; }
        public string EnvironmentURL { get; set; }
    }

    public enum ExportType { Managed = 0, Unmanaged = 1, None = 2 }


    public class D365Solution
    {
        public D365Solution()
        {

        }

        public bool IsManaged { get; set; }

        public string URL { get; set; }
        public bool ExportingAsManaged
        {
            get
            {
                return SolutionExportType == ExportType.Managed;
            }
        }

        public string SolutionName { get; set; }
        public string FriendlyName { get; set; }
        public Guid SolutionID { get; set; }
        public string SolutionVersion { get; set; }

        public ExportType SolutionExportType { get; set; }

        public string ManagedUnmanaged
        {
            get
            {
                return IsManaged ? "Managed" : "Unmanged";
            }
        }
        public bool Selected { get; set; }
    }
}
