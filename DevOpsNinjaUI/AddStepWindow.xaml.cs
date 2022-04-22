using DevOpsNinjaUI.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace DevOpsNinjaUI
{
    /// <summary>
    /// Interaction logic for AddStepWindow.xaml
    /// </summary>
    public partial class AddStepWindow : Window
    {
        private ObservableCollection<D365Solution> solutionModel;

        public delegate void AddStepHandler(AddSetpItemEventArgs args);
        public event AddStepHandler OnStepAdded;
        public AddStepWindow()
        {
            InitializeComponent();
        }

        public ObservableCollection<D365Solution> Solutions
        {
            get
            {
                return solutionModel;
            }
            set
            {
                solutionModel = value;
                drpSolutions.ItemsSource = solutionModel;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (OnStepAdded != null)
            {
                if (drpSolutions.SelectedValue != null)
                {
                    OnStepAdded(new AddSetpItemEventArgs
                    {
                        SelectedSolutionUniqueName = drpSolutions.SelectedValue.ToString(),
                        Step = SolutionStep.ImportSolutionStep,
                        IsUpgrade = chkUpgrade.IsChecked.Value,
                        SelectedSolutionVersion = (drpSolutions.SelectedItem as D365Solution).SolutionVersion
                    });

                    this.Close();
                }
            }
        }
    }
}
