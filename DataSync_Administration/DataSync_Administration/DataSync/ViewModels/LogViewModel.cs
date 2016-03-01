namespace DataSync.ViewModels
{
    class LogViewModel
    {
        private Log _log;
        public Log Log
        {
            get { return _log; }
            set { value = _log; }
        }

        public LogViewModel(Log log)
        {
            _log = log;
        }

        public string LogCustomerName
        {
            get { return "" + _log.Customer.Name; }
            private set { }
        }

        public string LogToolTip
        {
            get { return string.Format("{0}: {1}\n{2}", _log.DateCreated.ToString(), _log.Name, _log.Info); }
            private set { }
        }

        public string LogName
        {
            get { return "" + _log.Name; }
            private set { }
        }

        public string LogDateCreated
        {
            get { return "" + _log.DateCreated; }
            private set { }
        }
    }
}