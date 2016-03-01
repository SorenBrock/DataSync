using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
        Random rnd = new Random(Environment.TickCount);
        IDictionary<string, Task> _dictTasks = new Dictionary<string, Task>();
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

            Task.WaitAll(_dictTasks.Values.ToArray());

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

        internal void SyncronizeRun()
        {
            _backgroundWorker.ReportProgress(0, GetStringReportProgress("Starter kunde loop"));

            for (var i = 1; i <= 200; i++)
            {
                if (!_dictTasks.ContainsKey(i.ToString()))
                {
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress("Start kunde: " + i));
                    _dictTasks.Add(i.ToString(),
            Task.Factory.StartNew((object myState) =>
            {
                var talCustomer = (int)myState;
                TaelTil10(talCustomer);
                return talCustomer;

            }, i).ContinueWith(t => RemoveTaskFromDictionary(t.Result.ToString())));
                }
                else
                {
                    _backgroundWorker.ReportProgress(0, GetStringReportProgress("Igang i forvejen: kunde: " + i));
                }
            }
        }

        internal void RemoveTaskFromDictionary(string key)
        {
            if (!_dictTasks.ContainsKey(key)) return;
            _dictTasks.Remove(key);
            _backgroundWorker.ReportProgress(0, GetStringReportProgress("Slut kunde: " + key));
        }

        private int TaelTil10(int j)
        {
            for (var i = 1; i <= 10; i++)
            {
                Thread.Sleep(rnd.Next(200, 3000));
                _backgroundWorker.ReportProgress(0, GetStringReportProgress("Kunde nr:" + j + " tæller " + i));
            }
            return j;
        }


        #endregion SYNCRONIZE

    }
}