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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DevOpsNinjaUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WaterMarkTextBox : UserControl
    {
        public WaterMarkTextBox()
        {
            InitializeComponent();
        }

        public string Text
        {
            get
            {
                return txtUserEntry.Text;
            }

            set
            {
                txtUserEntry.Text = value;
            }
        }

        public string WaterMarkText
        {
            get
            {
                return lblWaterMark.Text;
            }

            set
            {
                lblWaterMark.Text = value;
            }

        }

        public void Clear()
        {
            txtUserEntry.Clear();
        }
    }
}
