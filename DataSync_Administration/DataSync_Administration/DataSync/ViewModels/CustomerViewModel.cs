namespace DataSync.ViewModels
{
    class CustomerViewModel
    {
          private Customer _customer;
        public Customer Customer
        {
            get { return _customer; }
            set { value = _customer; }
        }

        public CustomerViewModel(Customer customer)
        {
            _customer = customer;
        }

        public string CusID
        {
            get { return "" + _customer.Id; }
            private set { }
        }

        public string CusName
        {
            get { return "" + _customer.Name; }
            private set { }
        }

        public string CusDateLastUpdated
        {
            get { return "" + _customer.DateLastUpdated; }
            private set { }
        }

        public string CusIsActive
        {
            get { return "" + _customer.IsActive; }
            private set { }
        }
    }
}