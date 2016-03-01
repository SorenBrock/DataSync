using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using DataSync_Engine.EconomicWSDL;
using DataSync_Engine.Service;
using DataSync_Engine.Service.VTiger;
using Timer = System.Timers.Timer;

namespace DataSync_Engine
{

    public partial class MainWindow : Window
    {
        private readonly Service.Service _service = Service.Service.GetServiceInstance();
        private readonly BackgroundWorker _backgroundWorker;
        private IDictionary<CustomerSet, Task> _runningTasks;
        private Timer _myTimer;
        private const int SyncLoopMinut = 5;
        private const int TextBoxLogMaxLines = 100;
        private const string HomePageForInternetCheck = "http://www.google.com";

        public MainWindow()
        {
            InitializeComponent();
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            _backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _runningTasks = new Dictionary<CustomerSet, Task>();

            _myTimer = new Timer();
            _myTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            _myTimer.Interval = 1000 * 60 * SyncLoopMinut;

            TextBoxLog.Text += ("Ready...\n");
        }

        #region BACKGROUNDWORKER

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            TextBoxLog.Text += (e.UserState);
            while (TextBoxLog.LineCount > TextBoxLogMaxLines)
                TextBoxLog.Text = TextBoxLog.Text.Remove(0, TextBoxLog.GetLineLength(0));
            TextBoxLog.ScrollToEnd();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var buttonCommand = (string)e.Argument;
            _backgroundWorker.ReportProgress(0, GetStringReportProgress("Start Service"));

            _myTimer.Start();
            SyncronizeRun();

            while (!_backgroundWorker.CancellationPending)
            {
            }

            _myTimer.Stop();

            _backgroundWorker.ReportProgress(0, GetStringReportProgress("Stopping Service..."));

            var allTasks = _runningTasks.Values.ToArray();
            Task.WaitAll(allTasks);

            e.Cancel = true;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                TextBoxLog.Text += GetStringReportProgress("Service Stopped\n\nReady...\n");
            else if (e.Error != null)
                TextBoxLog.Text += GetStringReportProgress("Service Error\n\nReady...\n");
            TextBoxLog.ScrollToEnd();
            ButtonsIsEnabled(true);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            SyncronizeRun();
        }

        private string GetStringReportProgress(string output, bool insertEnter = true, bool insertTime = true)
        {
            var returnString = "";
            var appendix = "";
            if (insertEnter) appendix = "\n";
            if (insertTime) returnString = String.Format("{0} | {1} {2}", DateTime.Now, output, appendix);
            else returnString = String.Format("{0} {1}", output, appendix);
            return returnString;
        }

        private string GetStringReportProgress(CustomerSet customer, string output, bool insertEnter = true)
        {
            var appendix = "";
            if (insertEnter) appendix = "\n";
            return String.Format("{0} | Kunde: {1} | {2} {3}", DateTime.Now, customer.Name, output, appendix);
        }

        private bool IsConnectionLost(bool isConnectionLost)
        {
            if (isConnectionLost)
            {
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(SyncErrorType.ConnectionLost.EnumValue()));
                _backgroundWorker.ReportProgress(0, GetStringReportProgress("[syncronization terminated]"));
            }
            return isConnectionLost;
        }

        #endregion BACKGROUNDWORKER

        #region BUTTONS

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            ButtonsIsEnabled(false);
            _backgroundWorker.RunWorkerAsync("Customer");
        }

        private void ButtonRunNow_Click(object sender, RoutedEventArgs e)
        {
            _myTimer.Stop();
            _myTimer.Start();
            SyncronizeRun();
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {

            ButtonsIsEnabled(true);
            ButtonStart.IsEnabled = false;
            if (_backgroundWorker.IsBusy)
                _backgroundWorker.CancelAsync();
        }

        private void ButtonsIsEnabled(Boolean isEnabled)
        {
            ButtonStart.IsEnabled = isEnabled;
            ButtonStop.IsEnabled = !isEnabled;
            ButtonRunNow.IsEnabled = !isEnabled;
        }

        #endregion BUTTONS

        #region SYNCRONIZE

        private void SyncronizeRun()
        {
            if (!_service.CheckForInternetConnection(HomePageForInternetCheck))
            {
                IsConnectionLost(true);
                return;
            }

            foreach (var customer in _service.GetAllCustomersIsActive().ToList())
            {
                if (!_runningTasks.ContainsKey(customer))
                {
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, string.Format("synkronisering {0}", FeedBackType.Begin.EnumValue())));
                    _runningTasks.Add(customer, Task.Factory.StartNew((object customerObject) =>
                   {
                       var customerTaskObject = (CustomerSet)customerObject;
                       SyncronizeCustomer(customerTaskObject);
                       return customerTaskObject;
                   }
                   , customer).ContinueWith(t => RemoveTaskFromDictionary(t.Result)));
                }
            }
        }

        private void RemoveTaskFromDictionary(CustomerSet customer)
        {
            if (!_runningTasks.ContainsKey(customer)) return;
            _runningTasks.Remove(customer);
            _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, string.Format("synkronisering {0}", FeedBackType.End.EnumValue()), true));
        }

        private void SyncronizeCustomer(CustomerSet customer)
        {
            #region LOGIN PROCEDURE

            var session = _service.EconomicLogin(customer);
            if (session == null)
            {
                _service.WriteToLog(customer, "E-conomic: Login Error", "Ved Login til E-conomic returnerede _service.EconomicLogin(customer) NULL. Login blev cancelled");
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "E-conomic: Login Error"));
                return;
            }
            _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, string.Format("E-conomic: Login {0}", FeedBackType.Success.EnumValue())));

            var resulVTiger = _service.VTigerLogin(customer);
            if (resulVTiger.success != true)
            {
                _service.WriteToLog(customer, "VTiger: Login error (resulVTiger.success=False)", resulVTiger.error.message);
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "VTiger: Login Error [" + resulVTiger.error.message + "]"));
                return;
            }
            var vtigerLogin = resulVTiger.result;
            if (vtigerLogin == null)
            {
                _service.WriteToLog(customer, "VTiger: Login error (NULL)", "Ved login til VTiger returnede resulVTiger.result NULL. Login blev cancelled");
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "VTiger: Login Error [NULL]"));
                return;
            }
            _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, string.Format("VTiger: Login {0}", FeedBackType.Success.EnumValue())));

            #endregion LOGIN PROCEDURE

            var servertime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            servertime = servertime.AddSeconds(value: resulVTiger.result.serverTimeStamp).ToLocalTime();

            var serviceObject = _service.CreateServiceObject(vtigerLogin, session, customer, servertime);

            SyncronizeCustomersProduct(serviceObject);
            SyncronizeCustomersAccountAndDebtor(serviceObject);

            #region QUTOES -> INVOICE
            var dateLastUpdated = (DateTime)customer.DateLastUpdated;

            var quoteCollectionVTiger = _service.VTigerGetCollectionBySearch<VTigerQuote>(serviceObject, string.Format("quotestage='{0}' AND modifiedtime>'{1}'", InvoiceType.Ready.EnumValue(),
                _service.GetTimeForVTigerSearch(dateLastUpdated)));

            var quoteGroupListVTiger = quoteCollectionVTiger.OrderBy(w => w.quote_no).GroupBy(x => x.quote_no).ToList();
            var invoiceIndex = 0;
            var invoiceCount = quoteGroupListVTiger.Count();

            foreach (var quoteGroup in quoteGroupListVTiger.ToList())
            {
                var exitedInner = false;
                invoiceIndex++;
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer,
                    string.Format("Quote [{0}/{1}]: {2} {3}", invoiceIndex, invoiceCount, quoteGroup.Key, FeedBackType.Begin.EnumValue())));

                var vTigerQuote = quoteGroup.FirstOrDefault();
                var vTigerAccount = _service.VTigerGetObjectById<VTigerAccount>(serviceObject, vTigerQuote.account_id);
                if (vTigerAccount == null)
                {
                    _service.WriteToLog(serviceObject.Customer, string.Format(
                       "E-conomic: Quote{0} blev ikke overført til faktura - Account mangler. ", quoteGroup.Key),
                     "Problem: Ved forespørgsel til VTiger returnerede vTigerAccount null\n" +
                     "Løsning: Besked sendt til Website Inbox. Ret fejlfaktura og vent på næste kørsel\n" +
                     "Teknik: DataSync_Engine: MainWindow: SyncronizeCustomer: _service.VTigerGetObjectById<VTigerAccount>");

                    _service.WriteToWebSiteInbox(customer,
                    String.Format("Tilbud [{0}] blev ikke overført til E-conomic til fakturering", quoteGroup.Key),
                    String.Format("Ved synkronisering af VTiger tilbud var kunde [%] slettet\n" +
                    "Gennemse tilbud [{0}] for slettede produkter", quoteGroup.Key));

                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(SyncErrorType.CustomerMissing.EnumValue(), true, false));
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress("Quote [terminated]"));

                    continue;
                }
                var debtorData = _service.GetOrCreateDebtorData_VTigerToEconomic(serviceObject, vTigerAccount);
                if (IsConnectionLost(serviceObject.IsConnectionLost)) break;

                CurrentInvoiceHandle currentInvoiceHandle;
                CurrentInvoiceData currentInvoiceData;
                CurrencyHandle currencyHandle;
                try
                {
                    currentInvoiceHandle = session.CurrentInvoice_Create(debtorData.Handle);
                    currentInvoiceData = session.CurrentInvoice_GetData(currentInvoiceHandle);
                    currencyHandle = session.Currency_FindByCode("DKK");
                }
                catch (Exception)
                {
                    _service.WriteToLog(serviceObject.Customer, string.Format(
                     "E-conomic: Quote{0} blev ikke overført til faktura. [connection lost]", quoteGroup.Key),
                     "Problem: Ved forespørgsel til e-conomoc returnerede session null\n" +
                     "Løsning: Slet fejlfaktura og vent på næste kørsel\n Undersøg E-conomic settings og forbindelse til server\n" +
                     "Teknik: DataSync_Engine: MainWindow: SyncronizeCustomer: CurrentInvoice_UpdateFromData");
                    serviceObject.IsConnectionLost = true;
                    break;
                }

                currentInvoiceData.CurrencyHandle = currencyHandle;
                currentInvoiceData.DebtorPostalCode = vTigerQuote.bill_code;
                currentInvoiceData.DebtorAddress = vTigerQuote.bill_street;
                currentInvoiceData.DebtorCity = vTigerQuote.bill_city;
                currentInvoiceData.DebtorCountry = vTigerQuote.bill_country;
                currentInvoiceData.DeliveryAddress = vTigerQuote.ship_street;
                currentInvoiceData.DeliveryPostalCode = vTigerQuote.ship_code;
                currentInvoiceData.DeliveryCity = vTigerQuote.ship_city;
                currentInvoiceData.DeliveryCountry = vTigerQuote.ship_country;
                currentInvoiceData.OtherReference = quoteGroup.Key;

                var currentInvoiceList = new List<CurrentInvoiceLineData>();
                var quoteGroupIndex = 0;
                var quoteGroupCount = quoteGroup.Count();
                foreach (var quoteLine in quoteGroup)
                {
                    quoteGroupIndex++;
                    var vTigerProduct = _service.VTigerGetObjectById<VTigerProduct>(serviceObject, quoteLine.productid);

                    if (vTigerProduct == null)
                    {
                        _service.WriteToLog(serviceObject.Customer, string.Format(
                            "E-conomic: Quote{0} blev ikke overført til faktura - Product mangler. ", quoteGroup.Key),
                            "Problem: Ved forespørgsel til VTiger returnerede vTigerProduct null\n" +
                            "Løsning: Besked sendt til Website Inbox. Ret fejlfaktura og vent på næste kørsel\n" +
                            "Teknik: DataSync_Engine: MainWindow: SyncronizeCustomer: _service.VTigerGetObjectById<VTigerProduct>");

                        _service.WriteToWebSiteInbox(customer,
                            String.Format("Tilbud [{0}] blev ikke overført til E-conomic til fakturering", quoteGroup.Key),
                            String.Format("Ved synkronisering af VTiger tilbud var produkt [%] slettet\n" +
                            "Gennemse tilbud [{0}] for slettede produkter", quoteGroup.Key));

                        _backgroundWorker.ReportProgress(0, GetStringReportProgress(SyncErrorType.ProductMissing.EnumValue(), true, false));
                        _backgroundWorker.ReportProgress(0, GetStringReportProgress("Quote [terminated]"));
                        exitedInner = true;
                        break;
                    }

                    var productData = _service.GetOrCreateProductData_VTigerToEconomic(serviceObject, vTigerProduct);
                    if (IsConnectionLost(serviceObject.IsConnectionLost)) break;

                    var currentInvoiceLineData = new CurrentInvoiceLineData()
                    {
                        InvoiceHandle = currentInvoiceHandle,
                        ProductHandle = productData.Handle,
                        Description = productData.Name,
                        Quantity = (int?)_service.FormatPriceVTigerPriceToDouble(quoteLine.quantity),
                        UnitNetPrice = (decimal?)_service.FormatPriceVTigerPriceToDouble(quoteLine.listprice)
                    };

                    _backgroundWorker.ReportProgress(0,
                    GetStringReportProgress(customer, string.Format("Add InvoiceLine [{0}/{1}]: {2}", quoteGroupIndex, quoteGroupCount, currentInvoiceLineData.Description), false));
                    currentInvoiceList.Add(currentInvoiceLineData);
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(FeedBackType.Success.EnumValue(), true, false));
                }

                if (exitedInner) continue;

                try
                {
                    session.CurrentInvoice_UpdateFromData(currentInvoiceData);
                    session.CurrentInvoiceLine_CreateFromDataArray(currentInvoiceList.ToArray());
                }
                catch (Exception)
                {
                    _service.WriteToLog(serviceObject.Customer, string.Format(
                     "E-conomic: Quote{0} blev ikke overført til faktura. [connection lost]", quoteGroup.Key),
                     "Problem: Ved forespørgsel til e-conomic returnerede session null\n" +
                     "Løsning: Slet fejlfaktura og vent på næste kørsel\n Undersøg E-conomic settings og forbindelse til server\n" +
                     "Teknik: DataSync_Engine: MainWindow: SyncronizeCustomer: CurrentInvoice_UpdateFromData");
                    serviceObject.IsConnectionLost = true;
                    break;
                }

                if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer,
                    string.Format("Quote [{0}/{1}]: {2} {3}", invoiceIndex, invoiceCount, quoteGroup.Key, FeedBackType.End.EnumValue())));
            }

            #endregion QUTOES -> INVOICE

            _service.CustomerDateLastUpdated(serviceObject.Customer, serviceObject.DateBeginUpdate);
        }

        private void SyncronizeCustomersProduct(ServiceObject serviceObject)
        {
            var session = serviceObject.Session;
            var customer = serviceObject.Customer;
            var dateLastUpdated = (DateTime)customer.DateLastUpdated;

            #region COLLECTION

            ProductHandle[] productHandlesEconomic;
            try
            {
                productHandlesEconomic = session.Product_GetAllUpdated(dateLastUpdated.AddMinutes(-1), false);
            }
            catch (Exception)
            {
                _service.WriteToLog(customer,
                            "Synkronisering af produkter ingen collection [connection lost]",
                            "Problem: Ved kald til E-conomic returnerede session null\n" +
                            "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                            "Teknik: DataSync_Engine: MainWindow : SyncronizeCustomer : session.Product_GetAllUpdated");
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "[connection lost]"));
                return;
            }

            var productHandlesEconomicList = new List<ProductHandle>();
            if (productHandlesEconomic != null)
                productHandlesEconomicList = productHandlesEconomic.ToList();

            var productCollectionVTiger = _service.VTigerGetCollectionBySearch<VTigerProduct>(serviceObject, string.Format("modifiedtime>'{0}'",
                _service.GetTimeForVTigerSearch(dateLastUpdated)));
            var productListVTiger = productCollectionVTiger.ToList();
            productListVTiger.RemoveAll(x => x.serial_no.Equals("") || productHandlesEconomicList.Any(y => y.Number.Equals(x.serial_no)));

            var productAllIndex = 0;
            var productAllCount = productHandlesEconomicList.Count() + productListVTiger.Count();

            #endregion COLLECTION

            #region PRODUKTER : E-CONOMIC -> VTIGER

            if (productHandlesEconomic != null)
            {
                foreach (var productHandle in productHandlesEconomic.ToList())
                {
                    ProductData productData;
                    productAllIndex++;
                    try
                    {
                        productData = session.Product_GetData(productHandle);
                    }
                    catch (Exception)
                    {
                        _service.WriteToLog(customer, string.Format(
                            "VTiger: ProduktID: {0} blev ikke opdateret. [connection lost]", productHandle.Number),
                            "Problem: Ved kald til E-conomic returnerede session null\n" +
                            "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                            "Teknik: DataSync_Engine: MainWindow : SyncronizeCustomer : productData = session.Product_GetData(productHandle);");
                        _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "[connection lost]"));
                        break;
                    }

                    _backgroundWorker.ReportProgress(0,
                        GetStringReportProgress(customer, string.Format("Product [{0}/{1}]: {2} [{3}]", productAllIndex, productAllCount, productData.Name, productData.Number), false));

                    var innerProductCollectionVTiger = _service.VTigerGetCollectionBySearch<VTigerProduct>(serviceObject, string.Format("serial_no='{0}'", productData.Number));
                    if (innerProductCollectionVTiger != null && innerProductCollectionVTiger.Count() != 0)
                    {
                        _service.VTigerUpdateSameProductFromEconomic(serviceObject, innerProductCollectionVTiger, productData);
                        if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                        _backgroundWorker.ReportProgress(0,
                            GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                    }
                    else
                    {
                        _service.GetOrCreateVTigerProduct_EconomicToVTiger(serviceObject, productHandle);
                        if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                        _backgroundWorker.ReportProgress(0,
                            GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                    }
                }
            }

            #endregion PRODUKTER : E-CONOMIC -> VTIGER

            #region PRODUKTER : VTIGER -> E-CONOMIC

            foreach (var vTigerProduct in productListVTiger)
            {
                productAllIndex++;
                ProductHandle productHandle = null;
                ProductData productData = null; ;

                if (!vTigerProduct.serial_no.Equals(vTigerProduct.productcode))
                {
                    _backgroundWorker.ReportProgress(0,
                        GetStringReportProgress(customer, string.Format("Product [productcode error]: {0} [{1}]", vTigerProduct.productname, vTigerProduct.serial_no), false));
                    _service.VTigerProductFixProductCode(serviceObject, vTigerProduct);
                    if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                }

                _backgroundWorker.ReportProgress(0,
              GetStringReportProgress(customer, string.Format("Product [{0}/{1}]: {2}", productAllIndex, productAllCount, vTigerProduct.productname), false));

                try
                {
                    productHandle = session.Product_FindByNumber(vTigerProduct.serial_no);
                }
                catch (Exception)
                {
                    _service.WriteToLog(customer, string.Format(
                         "E-Conomic: ProduktID: {0} blev ikke opdateret. [connection lost]", vTigerProduct.serial_no),
                         "Problem: Ved kald til E-conomic returnerede session null\n" +
                         "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                         "Teknik: DataSync_Engine : MainWindow : SyncronizeCustomer : session.Product_FindByNumber(vTigerProduct.serial_no)");
                    if (IsConnectionLost(true)) break;
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "[connection lost]"));
                }

                if (productHandle == null)
                {
                    _service.WriteToLog(customer, string.Format(
                         "E-Conomic: ProduktID: {0} blev ikke opdateret.", vTigerProduct.serial_no),
                         "Problem: Ved kald til E-conomic er synkroniseret vare slettet\n" +
                         "Løsning: Fejlen er rettet ved at fjerne productcode i VTiger\n" +
                         "Teknik: DataSync_Engine : MainWindow : SyncronizeCustomer : session.Product_FindByNumber : productHandle == null ");
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(SyncErrorType.ProductCodeError.EnumValue(), false));
                    vTigerProduct.serial_no = "";

                    _service.VTigerProductFixProductCode(serviceObject, vTigerProduct);
                    if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                    continue;
                }

                try
                {
                    productData = session.Product_GetData(productHandle);
                }
                catch (Exception)
                {
                    _service.WriteToLog(customer, string.Format(
                        "E-Conomic: ProduktID: {0} blev ikke opdateret. [connection lost]", vTigerProduct.serial_no),
                        "Problem: Ved kald til E-conomic returnerede session null\n" +
                        "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                        "Teknik: DataSync_Engine : MainWindow : SyncronizeCustomer : productData = session.Product_GetData(productHandle)");
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "[connection lost]"));
                    break;
                }

                _service.EconomicUpdateProductFromVTiger(serviceObject, vTigerProduct, productData);
                if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                var message = "" + serviceObject.MessageToBackgroundWorker;
                if (!message.Equals("")) _backgroundWorker.ReportProgress(0, GetStringReportProgress(message));
            }



            #endregion PRODUKTER : VTIGER -> E-CONOMIC

        }

        private void SyncronizeCustomersAccountAndDebtor(ServiceObject serviceObject)
        {
            var session = serviceObject.Session;
            var customer = serviceObject.Customer;
            var dateLastUpdated = (DateTime)customer.DateLastUpdated;


            #region COLLECTION

            DebtorHandle[] debtorHandlesEconomic;
            try
            {
                debtorHandlesEconomic = session.Debtor_GetAllUpdated(dateLastUpdated.AddMinutes(-1), false);
            }
            catch (Exception)
            {
                _service.WriteToLog(customer,
                            "Synkronisering af kunder ingen collection [connection lost]",
                            "Problem: Ved kald til E-conomic returnerede session null\n" +
                            "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                            "Teknik: DataSync_Engine: MainWindow : SyncronizeCustomer : session.Debtor_GetAllUpdated");
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, "[connection lost]"));
                return;
            }

            var debtorHandlesEconomicList = new List<DebtorHandle>();
            if (debtorHandlesEconomic != null)
                debtorHandlesEconomicList = debtorHandlesEconomic.ToList();

            var accountCollectionVTiger = _service.VTigerGetCollectionBySearch<VTigerAccount>(serviceObject, string.Format("modifiedtime>'{0}'",
                _service.GetTimeForVTigerSearch(dateLastUpdated)));
            var accountListVTiger = accountCollectionVTiger.ToList();
            accountListVTiger.RemoveAll(x => x.tickersymbol.Equals("") || debtorHandlesEconomicList.Any(y => y.Number.Equals(x.tickersymbol)));

            var debtorAccountAllIndex = 0;
            var debtorAccountAllCount = debtorHandlesEconomicList.Count() + accountListVTiger.Count();

            #endregion COLLECTION

            #region KUNDER : E-CONOMIC -> VTIGER

            if (debtorHandlesEconomic != null)
            {
                foreach (var debtorHandle in debtorHandlesEconomic.ToList())
                {
                    DebtorData debtorData;
                    debtorAccountAllIndex++;
                    debtorData = _service.GetDebtorDataByHandle(serviceObject, debtorHandle,
                        "MainWindow : SyncronizeCustomer : service.GetDebtorDataByHandle");
                    if (IsConnectionLost(serviceObject.IsConnectionLost)) break;

                    _backgroundWorker.ReportProgress(0,
                        GetStringReportProgress(customer, string.Format("Customer [{0}/{1}]: {2} [{3}]", debtorAccountAllIndex, debtorAccountAllCount, debtorData.Name, debtorData.Number), false));

                    var innerAccountCollectionVTiger = _service.VTigerGetCollectionBySearch<VTigerAccount>(serviceObject, string.Format("tickersymbol='{0}'", debtorData.Number));
                    if (innerAccountCollectionVTiger != null && innerAccountCollectionVTiger.Count() != 0)
                    {
                        _service.VTigerUpdateSameAccountsFromEconomic(serviceObject, innerAccountCollectionVTiger, debtorData);
                        if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                        _backgroundWorker.ReportProgress(0,
                            GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                    }
                    else
                    {
                        _service.GetOrCreateVTigerAccount_EconomicToVTiger(serviceObject, debtorHandle);
                        if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                        _backgroundWorker.ReportProgress(0,
                            GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                    }
                }
            }

            #endregion KUNDER : E-CONOMIC -> VTIGER

            #region KUNDER : VTIGER -> E-CONOMIC

            foreach (var vTigerAccount in accountListVTiger)
            {
                debtorAccountAllIndex++;
                DebtorHandle debtorHandle;
                DebtorData debtorData;

                _backgroundWorker.ReportProgress(0, GetStringReportProgress(customer, string.Format("Customer [{0}/{1}]: {2}", debtorAccountAllIndex, debtorAccountAllCount, vTigerAccount.accountname), false));

                debtorHandle = _service.GetDebtorHandleByNumber(serviceObject, vTigerAccount.tickersymbol, "SyncronizeCustomer : GetDebtorHandleByNumber");
                if (debtorHandle == null)
                {
                    _service.WriteToLog(customer, string.Format(
                         "E-Conomic: KundeID: {0} blev ikke opdateret.", vTigerAccount.tickersymbol),
                         "Problem: Ved kald til E-conomic er synkroniseret kunde slettet\n" +
                         "Løsning: Fejlen er rettet automatisk ved at fjerne synkronisering til kunden i VTiger\n" +
                         "Teknik: DataSync_Engine : MainWindow : SyncronizeCustomer : session.Debtor_FindByNumber : debtorHandle == null ");
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress("[error: e-conomic kunde misssing]", false));

                    _service.VTigerAccountRemoveNumberToEconomic(serviceObject, vTigerAccount);
                    if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));
                    continue;
                }

                debtorData = _service.GetDebtorDataByHandle(serviceObject, debtorHandle, "SyncronizeCustomer : GetDebtorDataByHandle");
                if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                _service.EconomicUpdateDebtorFromVTiger(serviceObject, vTigerAccount, debtorData);
                if (IsConnectionLost(serviceObject.IsConnectionLost)) break;
                _backgroundWorker.ReportProgress(0, GetStringReportProgress(serviceObject.FeedbackToBackgroundWorker, true, false));

            }

            #endregion KUNDER : VTIGER -> E-CONOMIC

        }

        #endregion SYNCRONIZE

    }
}