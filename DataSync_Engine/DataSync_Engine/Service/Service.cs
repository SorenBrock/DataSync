using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Web;
using DataSync_Engine.EconomicWSDL;
using DataSync_Engine.Service.VTiger;

namespace DataSync_Engine.Service
{
    public partial class Service
    {
        private static readonly DataSyncDBEntities Db = new DataSyncDBEntities();
        private static readonly VTigerService VTigerapiService = VTigerService.GetVTigerServiceInstance();
        private static Service _serviceInstance;
        //Hvis kunde (efter cvr / tlf check) skal matches på email [VISIO]
        private const bool UseEmailAsMatch = true;

        private Service()
        {
        }

        public static Service GetServiceInstance()
        {
            if (_serviceInstance != null) return _serviceInstance;
            _serviceInstance = new Service();
            return _serviceInstance;
        }

        #region VTIGER LOGON | COLLECTION

        internal bool CheckForInternetConnection(string url)
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead(url))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        internal VTigerResult<VTigerLogin> VTigerLogin(CustomerSet customer)
        {
            string[] strResult = { VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "getchallenge", "username=" + customer.VTigerUsername, false) };
            if (strResult[0] == null)
                return new VTigerResult<VTigerLogin> { success = false };

            var getchallenge = VTigerapiService.GetDeSerializeObject<VTigerToken>(strResult[0]);
            var key = VTigerapiService.GetMd5Hash(getchallenge.result.token + customer.VTigerAccessKey);
            strResult[0] = VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "login", "username=" + customer.VTigerUsername + "&accessKey=" + key, true);
            var vResult = VTigerapiService.GetDeSerializeObject<VTigerLogin>(strResult[0]);

            vResult.result.serverTimeStamp = getchallenge.result.serverTime;

            return vResult;
        }

        internal bool VTigerLogoff(string sessionName)
        {
            var strResult = VTigerapiService.VTigerExecuteOperation("logout", "sessionName=" + sessionName, false);
            var vResult = VTigerapiService.GetDeSerializeObject<Object>(strResult);
            return vResult.success;
        }

        internal EconomicWebServiceSoapClient EconomicLogin(CustomerSet customer)
        {
            EconomicWebServiceSoapClient session = new EconomicWebServiceSoapClient();
            ((BasicHttpBinding)session.Endpoint.Binding).AllowCookies = true;
            try
            {
                session.ConnectWithToken(customer.EconomicPublicAPI, customer.EconomicPrivateAPI);
            }
            catch (Exception)
            {
                return null;
            }
            return session;
        }

        internal bool VTigerUpdateElement<T>(ServiceObject serviceObject, T updateElement)
        {
            var element = VTigerapiService.GetSerializeObject(updateElement);
            var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl, "update", String.Format("sessionName={0}&element={1}",
    serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<T>(strResult);
            if (retrieveResult.success)
            {
                serviceObject.FeedbackToBackgroundWorker = FeedBackType.Success.EnumValue();
                return true;
            }
            WriteToLog(serviceObject.Customer, string.Format(
                "VTiger: Et element blev ikke opdateret. [connection lost]"),
                "Problem: Ved update til VTiger returnerede result false\n" +
                "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
                "Teknik: DataSync_Engine: Service:  VTigerUpdateElement");
            serviceObject.IsConnectionLost = true;
            return false;
        }

        internal T VTigerGetObjectById<T>(ServiceObject serviceObject, string objectId)
        {
            var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl,
            "retrieve", String.Format("sessionName={0}&id={1}", serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(objectId)), false);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<T>(strResult);

            if (retrieveResult.success)
            {
                serviceObject.FeedbackToBackgroundWorker = FeedBackType.Success.EnumValue();
                return retrieveResult.result;
            }

            if (!retrieveResult.error.code.Equals(SyncErrorType.ACCESS_DENIED.ToString()))
                serviceObject.IsConnectionLost = true;
            return default(T);
        }

        internal List<T> VTigerGetCollectionBySearch<T>(ServiceObject serviceObject, string searchText)
        {
            var collectionBySearch = new List<T>();
            var tableName = "";
            if (typeof(T) == typeof(VTigerAccount))
                tableName = "Accounts";
            else if (typeof(T) == typeof(VTigerProduct))
                tableName = "Products";
            else if (typeof(T) == typeof(VTigerQuote))
                tableName = "Quotes";

            const int maxCount = 0; // 0=Alle
            const int vTigerLimit = 100; 
            var count = vTigerLimit;
            var totalCount = 0;

            while (count == vTigerLimit && (maxCount > totalCount || maxCount == 0))
            {
                var query = String.Format("SELECT * FROM {0} where {1} order by id limit {2}, {3} ;", tableName, searchText, totalCount, vTigerLimit);
                var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl, "query",
                    String.Format("sessionName={0}&query={1}", serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(query)), false);
                var queryResult = VTigerapiService.GetDeSerializeObject<T[]>(strResult);

                if (queryResult.success)
                {
                    collectionBySearch.AddRange(queryResult.result);
                    count = queryResult.result.Count();
                    totalCount = count;
                }
                else
                {
                    serviceObject.IsConnectionLost = true;
                    return null;
                }
            }
            return collectionBySearch;
        }

        #endregion VTIGER LOGON | COLLECTION

        #region DATABASE | SERVICEOBJECT

        internal IEnumerable<CustomerSet> GetAllCustomersIsActive()
        {
            var entity = Db.CustomerSet.Where(x => x.IsActive == true);
            ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
            return entity;
        }

        internal void WriteToLog(CustomerSet customer, string name, string info)
        {
            try
            {
                var log = new LogSet()
                {
                    CustomerSet = customer,
                    Name = name,
                    Info = info,
                    IsError = true,
                    DateCreated = DateTime.Now
                };
                Db.LogSet.Add(log);
                Db.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        internal void WriteToWebSiteInbox(CustomerSet customer, string subject, string message)
        {
            try
            {
                var websiteInbox = new WebSiteInboxSet()
                {
                    CustomerSet = customer,
                    From = "DataSync",
                    DateCreated = DateTime.Now,
                    Subject = subject,
                    Message = message
                };
                Db.WebSiteInboxSet.Add(websiteInbox);
                Db.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        internal void CustomerDateLastUpdated(CustomerSet customer, DateTime dateBeginUpdate)
        {
            var entity = Db.CustomerSet.FirstOrDefault(x => x.Id == customer.Id);
            ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
            if (entity != null) entity.DateLastUpdated = dateBeginUpdate;
            Db.SaveChanges();
        }

        internal ServiceObject CreateServiceObject(VTigerLogin vTigerLogin, EconomicWebServiceSoapClient session, CustomerSet customer, DateTime dateTime)
        {
            return new ServiceObject(vTigerLogin, session, customer, dateTime);
        }

        #endregion DATABASE | SERVICEOBJECT

        #region [VISIO]

        internal VTigerAccount GetOrCreateVTigerAccount_EconomicToVTiger(ServiceObject serviceObject, DebtorHandle debtorHandle)
        {
            VTigerAccount vTigerAccount;

            var debtorData = GetDebtorDataByHandle(serviceObject, debtorHandle, "Service: GetOrCreateVTigerAccount_EconomicToVTiger: GetDebtorDataByHandle");
            if (serviceObject.IsConnectionLost) return null;

            //VTiger: tickersymbol contains EconomicNumber (tickersymbol er det skjulte felt for synkronisering af VTiger og Economic)
            var vTigerAccountCollection = VTigerGetCollectionBySearch<VTigerAccount>(serviceObject, string.Format("tickersymbol='{0}'", debtorData.Number));
            if (serviceObject.IsConnectionLost) return null;
            if (vTigerAccountCollection.Count() != 0)
            {
                //VTiger: Update vTigerAccount with debtorData
                vTigerAccount = VTigerUpdateSameAccountsFromEconomic(serviceObject, vTigerAccountCollection, debtorData);
            }
            else
            {
                //Force new product
                if (serviceObject.Customer.ForceNewDebtor == true)
                {
                    //VTiger: Create vTigerAccount with debtorData
                    vTigerAccount = VTigerCreateAccountFromEconomic(serviceObject, debtorData);
                }
                else
                {
                    //VTiger: Match debtorData
                    var economicSearchField = GetEconomicSearchField(debtorData);
                    if (!economicSearchField.Equals(""))
                        vTigerAccountCollection = VTigerGetCollectionBySearch<VTigerAccount>(serviceObject, economicSearchField);
                    if (serviceObject.IsConnectionLost) return null;
                    if (!economicSearchField.Equals("") && vTigerAccountCollection.Count() != 0)
                    {
                        //VTiger: Force update vTigerAccount with debtorData
                        vTigerAccount = VTigerUpdateSameAccountsFromEconomic(serviceObject, vTigerAccountCollection, debtorData, true);
                        serviceObject.FeedbackToBackgroundWorker = FeedBackType.Syncronize.EnumValue();
                    }
                    else
                    {
                        //VTiger: Create vTigerAccount with debtorData
                        vTigerAccount = VTigerCreateAccountFromEconomic(serviceObject, debtorData);
                    }
                }
            }
            return vTigerAccount;
        }

        internal VTigerProduct GetOrCreateVTigerProduct_EconomicToVTiger(ServiceObject serviceObject, ProductHandle productHandle)
        {
            VTigerProduct vTigerProduct;

            var productData = GetProductDataByHandle(serviceObject, productHandle,
                "Service: GetOrCreateVTigerProduct_EconomicToVTiger: GetProductDataByHandle");
            if (serviceObject.IsConnectionLost) return null;

            //VTiger: serial_no contains EconomicNumber
            var vTigerProductCollection = VTigerGetCollectionBySearch<VTigerProduct>(serviceObject, string.Format("serial_no='{0}'", productData.Number));
            if (serviceObject.IsConnectionLost) return null;

            if (vTigerProductCollection.Count() != 0)
            {
                //VTiger: Update vTigerProduct with productData
                vTigerProduct = VTigerUpdateSameProductFromEconomic(serviceObject, vTigerProductCollection, productData);
            }
            else
            {
                //Force new vTigerProduct
                if (serviceObject.Customer.ForceNewProduct == true)
                {
                    //VTiger: Create vTigerProduct with productData
                    vTigerProduct = VTigerCreateProductFromEconomic(serviceObject, productData);
                }
                else
                {
                    //VTiger: Match productData (productcode)
                    vTigerProductCollection = VTigerGetCollectionBySearch<VTigerProduct>(serviceObject, string.Format("productcode='{0}'", productData.Number));
                    if (serviceObject.IsConnectionLost) return null;
                    if (vTigerProductCollection.Count() != 0)
                    {
                        //VTiger: Force update vTigerProduct with productData
                        vTigerProduct = VTigerUpdateSameProductFromEconomic(serviceObject, vTigerProductCollection, productData, true);
                        serviceObject.FeedbackToBackgroundWorker = FeedBackType.Syncronize.EnumValue();
                    }
                    else
                    {
                        //VTiger: Create vTigerProduct with productData
                        vTigerProduct = VTigerCreateProductFromEconomic(serviceObject, productData);
                    }
                }
            }
            return vTigerProduct;
        }

        internal DebtorData GetOrCreateDebtorData_VTigerToEconomic(ServiceObject serviceObject, VTigerAccount vTigerAccount)
        {
            DebtorHandle debtorHandle;
            DebtorData debtorData;
            string economicNumber;

            //VTiger: MemberOf contains ID [account_id]
            if (!vTigerAccount.account_id.Equals(""))
            {
                var accountCollectionVTiger = VTigerGetCollectionBySearch<VTigerAccount>(serviceObject, string.Format("id='{0}'", vTigerAccount.account_id));
                if (serviceObject.IsConnectionLost) return null;
                var vTigerMemberOfAccount = accountCollectionVTiger.FirstOrDefault();
                if (vTigerMemberOfAccount != null) vTigerAccount = vTigerMemberOfAccount;
            }

            //VTiger: tickerSymbol contains economicNumber
            if (!vTigerAccount.tickersymbol.Equals(""))
            {
                economicNumber = vTigerAccount.tickersymbol;
                //E-conomic: EconomicNumber exists
                debtorHandle = GetDebtorHandleByNumber(serviceObject, economicNumber,
                    "Service: GetOrCreateDebtorData_VTigerToEconomic: GetDebtorHandleByNumber");
                if (serviceObject.IsConnectionLost) return null;
                if (debtorHandle != null)
                {
                    //VTiger: Update vTigerAccount with debtorData 
                    debtorData = GetDebtorDataByHandle(serviceObject, debtorHandle,
                        "Service : GetOrCreateDebtorData_VTigerToEconomic: GetDebtorDataByHandle");
                    if (serviceObject.IsConnectionLost) return null;
                    VTigerUpdateAccountFromEconomic(serviceObject, vTigerAccount, debtorData);
                    if (serviceObject.IsConnectionLost) return null;
                }
                else
                {
                    //E-conomic: Create Debtor from vTigerAccount with economicNumber
                    debtorHandle = EconomicCreateDebtorFromVTiger(serviceObject, vTigerAccount, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;

                    //VTiger: Update tickersymbol with economicNumber
                    VTigerAccountSetEconomicNumber(serviceObject, vTigerAccount, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;
                }
            }
            else
            {
                //Force new debtor
                if (serviceObject.Customer.ForceNewDebtor == true)
                {
                    //E-conomic: Get next DebtorNumber
                    economicNumber = GetNextDebtorNumber(serviceObject).ToString();
                    if (serviceObject.IsConnectionLost) return null;

                    //E-conomic: Create Debtor from vTigerAccount with economicNumber
                    debtorHandle = EconomicCreateDebtorFromVTiger(serviceObject, vTigerAccount, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;

                    //VTiger: Update tickersymbol with economicNumber
                    VTigerAccountSetEconomicNumber(serviceObject, vTigerAccount, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;
                }
                else
                {
                    //E-conomic: Match vTigerAccount
                    debtorHandle = FindDebtorHandleMatchingVTigerAccount(serviceObject, vTigerAccount);
                    if (serviceObject.IsConnectionLost) return null;
                    if (debtorHandle != null)
                    {
                        //VTiger: Force update vTigerAccount with debtorData
                        debtorData = GetDebtorDataByHandle(serviceObject, debtorHandle,
                            "Service: GetOrCreateDebtorData_VTigerToEconomic: GetDebtorDataByHandle");
                        if (serviceObject.IsConnectionLost) return null;
                        VTigerUpdateAccountFromEconomic(serviceObject, vTigerAccount, debtorData, true);
                        if (serviceObject.IsConnectionLost) return null;
                        serviceObject.FeedbackToBackgroundWorker = FeedBackType.Syncronize.EnumValue();
                    }
                    else
                    {
                        //E-conomic: Get next DebtorNumber
                        economicNumber = GetNextDebtorNumber(serviceObject).ToString();
                        if (serviceObject.IsConnectionLost) return null;

                        //E-conomic: Create Debtor from vTigerAccount with economicNumber
                        debtorHandle = EconomicCreateDebtorFromVTiger(serviceObject, vTigerAccount, economicNumber);
                        if (serviceObject.IsConnectionLost) return null;

                        //VTiger: Update tickersymbol with economicNumber
                        VTigerAccountSetEconomicNumber(serviceObject, vTigerAccount, economicNumber);
                        if (serviceObject.IsConnectionLost) return null;
                    }
                }
            }
            debtorData = GetDebtorDataByHandle(serviceObject, debtorHandle,
                "Service: GetOrCreateDebtorData_VTigerToEconomic: return");
            return debtorData;
        }

        internal ProductData GetOrCreateProductData_VTigerToEconomic(ServiceObject serviceObject,
            VTigerProduct vTigerProduct)
        {
            ProductHandle productHandle;
            ProductData productData;
            string economicNumber;

            //VTiger: serial_no contains economicNumber
            if (!vTigerProduct.serial_no.Equals(""))
            {
                economicNumber = vTigerProduct.serial_no;
                //E-conomic: EconomicNumber exists
                productHandle = GetProductHandleByNumber(serviceObject, economicNumber,
                    "Service: GetOrCreateProductData_VTigerToEconomic: GetProductHandleByNumber");

                if (serviceObject.IsConnectionLost) return null;
                if (productHandle != null)
                {
                    //VTiger: Update vTigerProduct with productData 
                    productData = GetProductDataByHandle(serviceObject, productHandle,
                     "Service : GetOrCreateProductData_VTigerToEconomic: GetProductDataByHandle");
                    if (serviceObject.IsConnectionLost) return null;
                    VTigerUpdateProductFromEconomic(serviceObject, vTigerProduct, productData);
                    if (serviceObject.IsConnectionLost) return null;
                }
                else
                {
                    //E-conomic: Create product from vTigerProduct with economicNumber
                    productHandle = EconomicCreateProductFromVTiger(serviceObject, vTigerProduct, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;

                    //VTiger: Update serial_no, productcode with economicNumber
                    VTigerProductSetEconomicNumber(serviceObject, vTigerProduct, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;
                }
            }
            else
            {
                //if productCode="" productCode = product_no
                if (vTigerProduct.productcode.Equals(""))
                    vTigerProduct.productcode = vTigerProduct.product_no;

                //Force new debtor
                if (serviceObject.Customer.ForceNewProduct == true)
                {

                    economicNumber = GetNextProductNumberFromEconomic(serviceObject, vTigerProduct.productcode);
                    if (serviceObject.IsConnectionLost) return null;

                    //E-conomic: Create product from vTigerProduct with economicNumber
                    productHandle = EconomicCreateProductFromVTiger(serviceObject, vTigerProduct, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;

                    //VTiger: Update serial_no, productcode with economicNumber
                    VTigerProductSetEconomicNumber(serviceObject, vTigerProduct, economicNumber);
                    if (serviceObject.IsConnectionLost) return null;
                }
                else
                {
                    //E-conomic: Match ProductCode
                    productHandle = GetProductHandleByNumber(serviceObject, vTigerProduct.productcode,
                   "Service: GetOrCreateProductData_VTigerToEconomic: GetProductHandleByNumber");

                    if (serviceObject.IsConnectionLost) return null;
                    if (productHandle != null)
                    {
                        //VTiger: Force update vTigerProduct with productData
                        productData = GetProductDataByHandle(serviceObject, productHandle,
                            "Service: GetOrCreateProductData_VTigerToEconomic: GetProductDataByHandle");
                        if (serviceObject.IsConnectionLost) return null;
                        VTigerUpdateProductFromEconomic(serviceObject, vTigerProduct, productData, true);
                        if (serviceObject.IsConnectionLost) return null;
                        serviceObject.FeedbackToBackgroundWorker = FeedBackType.Syncronize.EnumValue();
                    }
                    else
                    {
                        economicNumber = GetNextProductNumberFromEconomic(serviceObject, vTigerProduct.productcode);
                        if (serviceObject.IsConnectionLost) return null;

                        //E-conomic: Create product from vTigerProduct with economicNumber
                        productHandle = EconomicCreateProductFromVTiger(serviceObject, vTigerProduct, economicNumber);
                        if (serviceObject.IsConnectionLost) return null;

                        //VTiger: Update serial_no, productcode with economicNumber
                        VTigerProductSetEconomicNumber(serviceObject, vTigerProduct, economicNumber);
                        if (serviceObject.IsConnectionLost) return null;
                    }
                }
            }

            productData = GetProductDataByHandle(serviceObject, productHandle,
           "Service: GetOrCreateProductData_VTigerToEconomic: return");
            return productData;
        }

        #endregion [VISIO]

    }
}