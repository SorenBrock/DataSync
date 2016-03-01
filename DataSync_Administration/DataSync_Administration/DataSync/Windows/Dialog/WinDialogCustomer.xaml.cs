using System;
using System.ComponentModel;
using System.Windows;

namespace DataSync.Windows.Dialog
{
    public partial class WinDialogCustomer : Window
    {

        private readonly Service.Service _service = Service.Service.GetServiceInstance();
        public Customer Customer { get; set; }
        private readonly BackgroundWorker _backgroundWorker;

        public WinDialogCustomer()
        {
            InitializeComponent();
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            _backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = Customer;
        }

        #region GENERELT [tabbar]

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxName.Text.Length == 0)
            {
                MessageBox.Show("Indtast navn på kunde!");
                return;
            }

            Customer.Name = TextBoxName.Text;
            Customer.WebLogin = TextBoxWebLogin.Text;
            Customer.WebPassword = TextBoxWebPassword.Text;
            Customer.VTigerUsername = TextBoxVTigerUsername.Text;
            Customer.VTigerUrl = TextBoxVTigerUrl.Text;
            Customer.VTigerAccessKey = TextBoxVTigerAccessKey.Text;
            Customer.EconomicPublicAPI = TextBoxEconomicPublicAPI.Text;
            Customer.EconomicPrivateAPI = TextBoxEconomicPrivateAPI.Text;

            Customer.IsEconomicOK = TextBoxEconomicStatus.Text == "True" ? true : false;
            Customer.IsVTigerOK = TextBoxVTigerStatus.Text == "True" ? true : false;
            Customer.IsActive = TextBoxCustomerStatus.Text == "True" ? true : false;

            this.DialogResult = true;
        }

        private void ButtonCheckVTiger_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxVTigerUsername.Text.Length == 0)
            {
                MessageBox.Show("Indtast VTiger user");
                return;
            }

            if (TextBoxVTigerUrl.Text.Length == 0)
            {
                MessageBox.Show("Indtast VTiger url (x.x.x.x:xxxx)");
                return;
            }

            if (TextBoxVTigerAccessKey.Text.Length == 0)
            {
                MessageBox.Show("Indtast VTiger AccessKey");
                return;
            }

            var result = _service.VTigerCheck(TextBoxVTigerUsername.Text, TextBoxVTigerUrl.Text, TextBoxVTigerAccessKey.Text);
            MessageBox.Show("Succes: " + result);
            if (!result) TextBoxCustomerStatus.Text = "False";
            TextBoxVTigerStatus.Text = result.ToString();

        }

        private void ButtonCheckEconomic_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxEconomicPublicAPI.Text.Length == 0)
            {
                MessageBox.Show("Indtast Public API ID for E-conomic");
                return;
            }

            if (TextBoxEconomicPrivateAPI.Text.Length == 0)
            {
                MessageBox.Show("Indtast Private API ID for E-conomic");
                return;
            }

            var result = _service.EconomicCheck(TextBoxEconomicPublicAPI.Text, TextBoxEconomicPrivateAPI.Text);
            MessageBox.Show("Succes: " + result);
            if (!result) TextBoxCustomerStatus.Text = "False";
            TextBoxEconomicStatus.Text = result.ToString();
        }

        private void ButtonCustomerIsActive_Click(object sender, RoutedEventArgs e)
        {
            TextBoxCustomerStatus.Text = "False";

            if (TextBoxVTigerStatus.Text != "True")
            {
                MessageBox.Show("Vtiger Status er false");
                return;
            }

            if (TextBoxEconomicStatus.Text != "True")
            {
                MessageBox.Show("E-conomic Status er False");
                return;
            }
            TextBoxCustomerStatus.Text = "True";

        }

        #endregion GENERELT [tabbar]

        #region ECONOMICS->VTIGER [tabbar]

        #region BACKGROUNDWORKER

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            TextBoxLog.Text += (e.UserState) + "\n";
            TextBoxLog.ScrollToEnd();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var buttonCommand = (string)e.Argument;
            _backgroundWorker.ReportProgress(0, "");
            _backgroundWorker.ReportProgress(0, "Start: " + DateTime.Now);

            switch (buttonCommand)
            {
                case "Producter":
                    CopyProducts_EconomicToVTiger(Customer);
                    break;

                case "Customer":
                    CopyDebtorAccounts_EconomicToVTiger(Customer);
                    break;
            }

            if (_backgroundWorker.CancellationPending)
            {
                e.Cancel = true;
                _backgroundWorker.ReportProgress(0);
                return;
            }

            _backgroundWorker.ReportProgress(100);
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                TextBoxLog.Text += "Time: " + DateTime.Now + " | Cancelled.\n\nReady...";
            }
            else if (e.Error != null)
            {
                TextBoxLog.Text += "Time: " + DateTime.Now + " | Error while performing background operation.\n\nReady...";
            }
            else
            {
                TextBoxLog.Text += "Time: " + DateTime.Now + " | Completed.\n\nReady...";
            }

            TextBoxLog.ScrollToEnd();
            ButtonsIsEnabled(true);
        }

        #endregion BACKGROUNDWORKER

        #region BUTTONS

        private void ButtonProducter_Click(object sender, RoutedEventArgs e)
        {
            ButtonsIsEnabled(false);
            _backgroundWorker.RunWorkerAsync("Producter");
        }

        private void ButtonCustomer_Click(object sender, RoutedEventArgs e)
        {
            ButtonsIsEnabled(false);
            _backgroundWorker.RunWorkerAsync("Customer");
        }

        private void ButtonCancelBackgroundWorker_Click(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorker.IsBusy)
            {
                _backgroundWorker.CancelAsync();
            }
        }

        private void ButtonsIsEnabled(Boolean isEnabled)
        {
            ButtonProducter.IsEnabled = isEnabled;
            ButtonCustomer.IsEnabled = isEnabled;
            ButtonCustomerIsActive.IsEnabled = isEnabled;
            ButtonCheckVTiger.IsEnabled = isEnabled;
            ButtonCheckEconomic.IsEnabled = isEnabled;
            ButtonCancel.IsEnabled = isEnabled;
            ButtonSave.IsEnabled = isEnabled;
            ButtonCancelBackgroundWorker.IsEnabled = !isEnabled;
        }

        #endregion BUTTONS

        private void CopyProducts_EconomicToVTiger(Customer customer)
        {
            #region LOGIN PROCEDURE
            var session = _service.EconomicLogin(customer);
            if (session == null)
            {
                _backgroundWorker.ReportProgress(0, "EconomicLogin Failed");
                return;
            }
            _backgroundWorker.ReportProgress(0, "EconomicLogin Succeced");

            var resulVTiger = _service.VTigerLogin(customer);
            if (resulVTiger.success != true)
            {
                _backgroundWorker.ReportProgress(0, resulVTiger.error.message);
                return;
            }
            var VtigerLogin = resulVTiger.result;
            if (VtigerLogin == null)
            {
                _backgroundWorker.ReportProgress(0, "VTigerLogin: error NULL");
                return;
            }
            _backgroundWorker.ReportProgress(0, "VTigerLogin: " + VtigerLogin.sessionName);

            #endregion LOGIN PROCEDURE

            var productHandles = session.Product_GetAll();
            var i = 0;
            var productCount = productHandles.Length;

            _backgroundWorker.ReportProgress(0, "Time: + " + DateTime.Now + " | Antal produkter: " + productCount);

            foreach (var productHandle in productHandles)
            {
                i++;
                if (_backgroundWorker.CancellationPending) return;

                var result = _service.GetOrCreateVTigerProduct_EconomicToVTiger(VtigerLogin, session, customer, productHandle);
                if (result != null)
                    _backgroundWorker.ReportProgress(0, "[" + i + "/" + productCount + "] " + result.productname);
                else
                    _backgroundWorker.ReportProgress(0, "[" + i + "/" + productCount + "] Error: E-conomic produktid: " + productHandle.Number);
            }
        }

        private void CopyDebtorAccounts_EconomicToVTiger(Customer customer)
        {
            #region LOGIN PROCEDURE
            var session = _service.EconomicLogin(customer);
            if (session == null)
            {
                _backgroundWorker.ReportProgress(0, "EconomicLogin Failed");
                return;
            }
            _backgroundWorker.ReportProgress(0, "EconomicLogin Succeced");

            var resulVTiger = _service.VTigerLogin(customer);
            if (resulVTiger.success != true)
            {
                _backgroundWorker.ReportProgress(0, resulVTiger.error.message);
                return;
            }
            var vtigerLogin = resulVTiger.result;
            if (vtigerLogin == null)
            {
                _backgroundWorker.ReportProgress(0, "VTigerLogin: null error");
                return;
            }
            _backgroundWorker.ReportProgress(0, "VTigerLogin: " + vtigerLogin.sessionName);

            #endregion

            var debtorHandles = session.Debtor_GetAll();
            var i = 0;
            var debtorCount = debtorHandles.Length;

            _backgroundWorker.ReportProgress(0, "Time: + " + DateTime.Now + " | Antal kunder: " + debtorCount);

            foreach (var debtorHandle in debtorHandles)
            {
                i++;
                if (_backgroundWorker.CancellationPending) return;

                var result = _service.GetOrCreateVTigerAccount_EconomicToVTiger(vtigerLogin, session, customer, debtorHandle);
                if (result != null)
                    _backgroundWorker.ReportProgress(0, "[" + i + "/" + debtorCount + "] " + result.accountname);
                else
                    _backgroundWorker.ReportProgress(0, "[" + i + "/" + debtorCount + "] Error: E-conomic kundeid: " + debtorHandle.Number);
            }
        }

        #endregion ECONOMICS->VTIGER [tabbar]
    }
}