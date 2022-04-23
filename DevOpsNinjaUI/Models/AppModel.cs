using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;

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

    public class ProgressIndicator
    {
        public string SolutionName { get; set; }

        public float ProgressValue { get; set; }

        public string Status { get; set; }
    }

    public class AddSetpItemEventArgs : EventArgs
    {
        public string SelectedSolutionUniqueName { get; set; }

        public string SelectedSolutionVersion { get; set; }
        public int Order { get; set; }
        public bool IsUpgrade { get; set; }

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


    public class D365Solution
    {
        public D365Solution()
        {

        }

        public bool IsManaged { get; set; }
        public string SolutionName { get; set; }
        public string FriendlyName { get; set; }
        public Guid SolutionID { get; set; }
        public string SolutionVersion { get; set; }

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
