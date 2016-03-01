using System;
using DataSync_Engine.EconomicWSDL;
using DataSync_Engine.Service.VTiger;

namespace DataSync_Engine.Service
{
    public class ServiceObject
    {
        public ServiceObject() { }
        public ServiceObject(VTigerLogin vTigerLogin, EconomicWebServiceSoapClient session, CustomerSet customer, DateTime dateBeginUpdate)
        {
            this.VTigerLogin = vTigerLogin;
            this.Session = session;
            this.IsConnectionLost = false;
            this.Customer = customer;
            this.DateBeginUpdate = dateBeginUpdate;
        }

        public VTigerLogin VTigerLogin { get; set; }
        public EconomicWebServiceSoapClient Session { get; set; }
        public CustomerSet Customer { get; set; }
        public DateTime DateBeginUpdate { get; set; }
        public bool IsConnectionLost { get; set; }
        string _feedbackToBackgroundWorker;

        public string FeedbackToBackgroundWorker
        {
            get
            {
                var returnString = _feedbackToBackgroundWorker;
                _feedbackToBackgroundWorker = "";
                return returnString;
            }
            set
            {
                _feedbackToBackgroundWorker = value;
            }
        }

        string _messageToBackgroundWorker;
        public string MessageToBackgroundWorker
        {
            get
            {
                var returnString = _messageToBackgroundWorker;
                _messageToBackgroundWorker = "";
                return returnString;
            }
            set
            {
                _messageToBackgroundWorker = value;
            }
        }
    }
}