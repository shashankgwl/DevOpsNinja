namespace DevOpsNinjaUI
{
    using Microsoft.Xrm.Tooling.Connector;
    using System.Windows;
    using System;
    using System.Windows.Controls;
    using Microsoft.Crm.Sdk.Messages;
    using System.Configuration;

    /// <summary>
    /// Interaction logic for LoginControl.xaml
    /// </summary>
    public partial class LoginControl : UserControl
    {
        private string loginText;
        private bool enabled;

        public LoginControl()
        {
            InitializeComponent();
        }

        private void btnLoginOauth_Click(object sender, RoutedEventArgs e)
        {
            var constr = this.GetConnectionStringExport();
            if (string.IsNullOrEmpty(constr))
            {
                MessageBox.Show("App.config is missing OAuth connection string, please contact administratory", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var connection = new CrmServiceClient(constr);
            if (connection != null && connection.OrganizationWebProxyClient != null)
            {
                connection.OrganizationWebProxyClient.InnerChannel.OperationTimeout = TimeSpan.FromHours(10);
                SConn = connection;
                ////var request = new ExecuteFetchRequest { FetchXml = RESX.Resources.FetchLoadUnmanagedSolutions };
                ////var response = connection.Execute(request); ;
                MessageBox.Show("Connection successful.");
            }
            else
            {
                MessageBox.Show("Authentication not successful. Please try again.");
            }
        }

        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                btnLoginOauth.IsEnabled = value;
            }
        }

        public string LoginText
        {
            get
            {
                return loginText;
            }

            set
            {
                loginText = value;
                btnLoginOauth.Content = value;
            }
        }
        public string OrgUrl { get; set; }
        public CrmServiceClient SConn { get; set; }

        ////public void SetEnabled(bool isEnabled)
        ////{
        ////    btnLoginOauth.IsEnabled = isEnabled;
        ////}
        private string GetConnectionStringExport()
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["OAuthLoginConnectionString"]))
            {
                return string.Empty;
            }

            string cstr = string.Format(ConfigurationManager.AppSettings["OAuthLoginConnectionString"], this.OrgUrl);
            ////string cstr = $@"AuthType=OAuth;Username=;Password=;Url={this.OrgUrl};AppId=1421b392-3531-427e-99db-6a9fff01dc91; RedirectUri=app://1421b392-3531-427e-99db-6a9fff01dc91;LoginPrompt=Auto";
            return cstr;
        }
    }
}
