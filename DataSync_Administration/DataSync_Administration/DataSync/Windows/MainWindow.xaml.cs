using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DataSync.ViewModels;
using DataSync.Windows.Dialog;

namespace DataSync.Windows
{
    public partial class MainWindow : Window
    {
        private readonly Service.Service _service = Service.Service.GetServiceInstance();
        private ObservableCollection<CustomerViewModel> _collectionCustomerViewModel;
        private ObservableCollection<LogViewModel> _collectionLogViewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //_service.CreateAndStoreSomeObjects();
            _service.DeleteAllLogBefore90Days();
            _collectionCustomerViewModel = new ObservableCollection<CustomerViewModel>();
            _collectionLogViewModel = new ObservableCollection<LogViewModel>();
            LoadCustomerData();
            LoadLogDataAll();
        }

        #region LOG

        private void LoadLogDataAll()
        {
            _collectionLogViewModel.Clear();
            _service.GetAllLog().ToList().ForEach(x => _collectionLogViewModel.Add(new LogViewModel(x)));
            DataGridLog.DataContext = _collectionLogViewModel;
        }

        private void LoadLogDataByCustomer()
        {
            if (DataGridCustomer.SelectedItem == null)
                return;
            var customer = (Customer)((CustomerViewModel)DataGridCustomer.SelectedItem).Customer;
            _collectionLogViewModel.Clear();
            _service.GetAllLogByCustomerAndSearch(customer, TextBoxSearchLog.Text).ToList().ForEach(x => _collectionLogViewModel.Add(new LogViewModel(x)));
            DataGridLog.DataContext = _collectionLogViewModel;
        }

        #region LOG : BUTTONS / SELECTION

        private void DataGridLog_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateAndSetButtons();
        }

        private void DataGridLog_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataGridLog.SelectedItem == null)
                return;
            var windowDialog = new WinDialogLog { Log = (Log)((LogViewModel)DataGridLog.SelectedItem).Log };
            var result = windowDialog.ShowDialog();
        }

        private void ButtonRefreshLog_Click(object sender, RoutedEventArgs e)
        {
            LoadLogDataAll();
            UpdateAndSetButtons();
        }

        private void ButtonSearchLog_Click(object sender, RoutedEventArgs e)
        {
            LoadLogDataByCustomer();
            UpdateAndSetButtons();
        }

        private void ButtonLogClear_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCustomer.SelectedItem == null)
                _service.DeleteAllLog();
            else
                _service.DeleteAllLogByCustomer((Customer)((CustomerViewModel)DataGridCustomer.SelectedItem).Customer);
            LoadLogDataByCustomer();
            UpdateAndSetButtons();
        }

        private void ButtonLogDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridLog.SelectedItem == null)
                return;
            _service.DeleteLog((Log)((LogViewModel)DataGridLog.SelectedItem).Log);
            LoadLogDataByCustomer();
            UpdateAndSetButtons();
        }

        #endregion LOG : BUTTONS / SELECTION

        #endregion LOG

        #region CUSTOMER

        private void LoadCustomerData()
        {
            _collectionCustomerViewModel.Clear();
            _collectionLogViewModel.Clear();
            _service.GetSearchCustomers(TextBoxSearchCustomer.Text).ToList().ForEach(x => _collectionCustomerViewModel.Add(new CustomerViewModel(x)));
            DataGridCustomer.DataContext = _collectionCustomerViewModel;
        }

        #region CUSTOMER : BUTTONS / SELECTION

        private void ButtonSearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            LoadCustomerData();
        }

        private void ButtonRefreshCustomer_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = DataGridCustomer.SelectedIndex;
            DataGridCustomer.SelectedIndex = -1;
            LoadCustomerData();
            if (selectedIndex >= 0) DataGridCustomer.SelectedIndex = selectedIndex;
        }

        private void ButtonCustomerEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCustomer.SelectedItem == null)
                return;
            var windowDialog = new WinDialogCustomer()
            {
                Customer = (Customer)((CustomerViewModel)DataGridCustomer.SelectedItem).Customer
            };
            var result = windowDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                _service.EditCustomer();
                LoadCustomerData();
            }
        }

        private void ButtonCustomerDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCustomer.SelectedItem == null)
                return;
            var customer = (Customer)((CustomerViewModel)DataGridCustomer.SelectedItem).Customer;
            if (MessageBox.Show(string.Format("Vil du slette Kunde: {0} ",
                          customer.Name), caption: "Confirmation", button: MessageBoxButton.YesNo) ==
                          MessageBoxResult.Yes)
                _service.DeleteCustomer(customer);
            LoadCustomerData();
        }

        private void ButtonCustomerNew_Click(object sender, RoutedEventArgs e)
        {
            var windowDialog = new WinDialogCustomer();
            windowDialog.Customer = new Customer()
            {
                Name = "Ny kunde"
            };
            var result = windowDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                _service.AddCustomer(windowDialog.Customer);
                LoadCustomerData();
            }
        }

        private void DataGridCustomer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LoadLogDataByCustomer();
            UpdateAndSetButtons();
        }

        #endregion CUSTOMER : BUTTONS

        #endregion CUSTOMER

        private void UpdateAndSetButtons()
        {
            ButtonCustomerEdit.IsEnabled = DataGridCustomer.SelectedItem != null;
            ButtonCustomerDelete.IsEnabled = DataGridCustomer.SelectedItem != null;
            ButtonLogClear.IsEnabled = DataGridCustomer.SelectedItem != null;
            ButtonLogDelete.IsEnabled = DataGridLog.SelectedItem != null;
        }

    }
}