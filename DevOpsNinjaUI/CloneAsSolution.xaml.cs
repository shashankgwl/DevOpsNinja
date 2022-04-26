using DevOpsNinjaUI.Models;
using System.Windows;

namespace DevOpsNinjaUI
{
    /// <summary>
    /// Interaction logic for CloneAsSolution.xaml
    /// </summary>
    public partial class CloneAsSolution : Window
    {
        public CloneAsSolution()
        {
            InitializeComponent();
        }

        public delegate void CloneSolutionHandler(CloneSolutionEventArgs args);
        public event CloneSolutionHandler OnCloneSolution;
        private string parentSolName;

        public string ParentSolutionName
        {
            get { return parentSolName; }
            set
            {
                parentSolName = value;
                this.lblParentSolution.Text = value;
                this.txtDisplayName.Text = value;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txtDisplayName.Text.Length <= 0 || txtNewVersion.Text.Length <= 0)
            {
                return;
            }

            this.Close();
            if (OnCloneSolution != null)
            {
                OnCloneSolution(new CloneSolutionEventArgs
                {
                    DisplayName = txtDisplayName.Text,
                    Version = txtNewVersion.Text,
                    ParentSolutionName = this.lblParentSolution.Text
                });
            }
        }
    }
}
