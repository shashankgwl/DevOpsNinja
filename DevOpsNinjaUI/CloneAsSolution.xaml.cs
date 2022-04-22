using DevOpsNinjaUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
