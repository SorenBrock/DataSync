using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Windows.Forms;
using DataSync.EconomicWSDL;
using DataSync.Service.VTiger;
using DataSync_Engine.Service.VTiger;

namespace DataSync.Service
{
    public class Service
    {
        private static readonly ModelADOContainer Db = new ModelADOContainer();

        private static readonly VTigerService VTigerapiService = VTigerService.GetVTigerServiceInstance();
        private static Service _serviceInstance;

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

        internal VTigerResult<VTigerLogin> VTigerLogin(Customer customer)
        {
            string[] strResult = { VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "getchallenge", "username=" + customer.VTigerUsername, false) };
            if (strResult[0] == null)
                return new VTigerResult<VTigerLogin> { success = false };

            var getchallenge = VTigerapiService.GetDeSerializeObject<VTigerToken>(strResult[0]);
            var key = VTigerapiService.GetMd5Hash(getchallenge.result.token + customer.VTigerAccessKey);
            strResult[0] = VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "login", "username=" + customer.VTigerUsername + "&accessKey=" + key, true);
            var vResult = VTigerapiService.GetDeSerializeObject<VTigerLogin>(strResult[0]);

            return vResult;
        }

        internal bool VTigerLogoff(string sessionName)
        {
            var strResult = VTigerapiService.VTigerExecuteOperation("logout", "sessionName=" + sessionName, false);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerLogin>(strResult);
            return retrieveResult.success;
        }

        internal EconomicWebServiceSoapClient EconomicLogin(Customer customer)
        {
            var session = new EconomicWebServiceSoapClient();
            ((BasicHttpBinding)session.Endpoint.Binding).AllowCookies = true;
            try
            {
                session.ConnectWithToken(customer.EconomicPublicAPI, customer.EconomicPrivateAPI);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return session;
        }

        internal bool EconomicCheck(string token, string appToken)
        {
            var result = false;
            var session = new EconomicWebServiceSoapClient();
            ((BasicHttpBinding)session.Endpoint.Binding).AllowCookies = true;
            try
            {
                session.ConnectWithToken(token, appToken);
                result = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }

        internal bool VTigerCheck(string vTigerUsername, string vTigerUrl, string vTigerAccessKey)
        {
            string[] strResult = { VTigerapiService.VTigerExecuteOperation(vTigerUrl, "getchallenge", "username=" + vTigerUsername, false) };
            if (strResult[0] == null) return false;
            var getchallenge = VTigerapiService.GetDeSerializeObject<VTigerToken>(strResult[0]);
            var key = VTigerapiService.GetMd5Hash(getchallenge.result.token + vTigerAccessKey);
            strResult[0] = VTigerapiService.VTigerExecuteOperation(vTigerUrl, "login", "username=" + vTigerUsername + "&accessKey=" + key, true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerLogin>(strResult[0]);
            return retrieveResult.success;
        }

        internal List<T> VTigerGetCollectionBySearch<T>(Customer customer, VTigerLogin vTigerLogin, string searchText)
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
            const int vTigerLimit = 100; // VTigers limit 
            var count = vTigerLimit;
            var totalCount = 0;

            while (count == vTigerLimit && (maxCount > totalCount || maxCount == 0))
            {
                var query = String.Format("SELECT * FROM {0} where {1} order by id limit {2}, {3} ;", tableName, searchText, totalCount, vTigerLimit);
                var strResult = VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "query",
                    String.Format("sessionName={0}&query={1}", vTigerLogin.sessionName, HttpUtility.UrlEncode(query)), false);
                var queryResult = VTigerapiService.GetDeSerializeObject<T[]>(strResult);

                if (queryResult.success)
                {
                    collectionBySearch.AddRange(queryResult.result);
                    count = queryResult.result.Count();
                    totalCount = count;
                }
                else
                {
                    return null;
                }
            }
            return collectionBySearch;
        }

        #endregion VTIGER LOGON | COLLECTION

        #region DATABASE

        #region CUSTOMER

        internal Customer GetFirstCustomer()
        {
            return Db.CustomerSet.FirstOrDefault();
        }

        internal IEnumerable<Customer> GetAllCustomers()
        {
            var entity = Db.CustomerSet.Take(50);
            ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
            return entity;
        }

        internal IEnumerable<Customer> GetSearchCustomers(string searchstring)
        {
            var entity = Db.CustomerSet.Where(x => x.Name.Contains(searchstring)).Take(50);
            ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
            return entity;
        }

        internal void AddCustomer(Customer customer)
        {
            try
            {
                var newCustomer = new Customer()
                {
                    Name = customer.Name,
                    WebLogin = customer.WebLogin,
                    WebPassword = customer.WebPassword,
                    VTigerAccessKey = customer.VTigerAccessKey,
                    VTigerUrl = customer.VTigerUrl,
                    EconomicPrivateAPI = customer.EconomicPrivateAPI,
                    EconomicPublicAPI = customer.EconomicPublicAPI,

                    DateCreated = DateTime.Now,
                    DateLastUpdated = DateTime.Now
                };
                Db.CustomerSet.Add(newCustomer);
                Db.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        internal void EditCustomer()
        {
            Db.SaveChanges();
        }

        internal void DeleteCustomer(Customer customer)
        {
            try
            {
                Db.WebSiteInboxSet.RemoveRange(customer.WebSiteInbox);
                Db.LogSet.RemoveRange(customer.Log);
                Db.CustomerSet.Remove(customer);
                Db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        #endregion CUSTOMER

        #region LOG

        internal IEnumerable<Log> GetAllLog()
        {
            var entity = Db.LogSet.Take(50).OrderByDescending(x=>x.DateCreated);
            ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
            return entity;
        }

        internal IEnumerable<Log> GetAllLogByCustomerAndSearch(Customer customer, string searchstring)
        {
            var entity = Db.LogSet.Where(x => x.Customer.Id == customer.Id && x.Name.Contains(searchstring)).Take(50).OrderByDescending(x=>x.DateCreated);;
            ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
            return entity;
        }

        internal void DeleteAllLog()
        {
            try
            {
                var entity = Db.LogSet;
                ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
                Db.LogSet.RemoveRange(entity.AsEnumerable().ToList());
                Db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        internal void DeleteAllLogByCustomer(Customer customer)
        {
            try
            {
                var entity = Db.LogSet.Where(x => x.Customer.Id == customer.Id);
                ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
                Db.LogSet.RemoveRange(entity.AsEnumerable().ToList());
                Db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        internal void DeleteAllLogBefore90Days()
        {
            try
            {
                var entity = Db.LogSet.Where(x => x.DateCreated < DateTime.Now.AddDays(-90));
                ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
                Db.LogSet.RemoveRange(entity.AsEnumerable().ToList());
                Db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        internal void DeleteLog(Log log)
        {
            try
            {
                Db.LogSet.Remove(log);
                Db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        #endregion LOG

        #endregion DATABASE

        #region VTIGER PRODUCT DATA

        internal VTigerProduct VTigerUpdateProductsFromEconomic(VTigerLogin vTigerLogin, Customer customer, EconomicWebServiceSoapClient session, List<VTigerProduct> vTigerProductCollection,
            ProductData productData)
        {
            VTigerProduct vTigerProduct = null;
            var productDataUnitName = "";
            foreach (var product in vTigerProductCollection.ToList())
            {
                product.productname = productData.Name;
                product.unit_price = productData.SalesPrice.ToString(CultureInfo.CurrentCulture).Replace(",", ".");
                product.description = productData.Description;
                product.productcode = productData.Number;
                product.serial_no = productData.Number;
                if (productData.UnitHandle != null) productDataUnitName = session.Unit_GetData(productData.UnitHandle).Name;
                product.usageunit = productDataUnitName;
                product.productcategory = session.ProductGroup_GetData(productData.ProductGroupHandle).Name;
                product.discontinued = "1";

                var element = VTigerapiService.GetSerializeObject(product);
                var strResult = VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "update", String.Format("sessionName={0}&element={1}", vTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
                var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerProduct>(strResult);
                if (retrieveResult.success != true)
                {
                    MessageBox.Show(String.Format("Connection lost\nProdukt blev ikke opdateret\n{0}", retrieveResult.error.message),
                        @"Product updatering", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                vTigerProduct = product;
            }
            return vTigerProduct;
        }

        internal VTigerProduct VTigerCreateProductFromEconomic(VTigerLogin vTigerLogin, Customer customer,
            EconomicWebServiceSoapClient session, ProductData productData)
        {
            var productDataUnitName = "";
            if (productData.UnitHandle != null) productDataUnitName = session.Unit_GetData(productData.UnitHandle).Name;
            var vTigerProduct = new VTigerProduct()
            {
                assigned_user_id = vTigerLogin.userId,
                productname = productData.Name,
                unit_price = productData.SalesPrice.ToString(CultureInfo.CurrentCulture).Replace(",", "."),
                description = productData.Description,
                productcode = productData.Number,
                serial_no = productData.Number,
                usageunit = productDataUnitName,
                productcategory = session.ProductGroup_GetData(productData.ProductGroupHandle).Name,
                discontinued = "1"
            };
            var element = VTigerapiService.GetSerializeObject(vTigerProduct);
            const string elementType = "Products";
            var strResult = VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "create", String.Format("sessionName={0}&elementType={1}&element={2}", vTigerLogin.sessionName, elementType, HttpUtility.UrlEncode(element)), true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerProduct>(strResult);

            if (retrieveResult.success != true)
            {
                MessageBox.Show(
                    String.Format("Connection lost\nProdukt blev ikke skabt\n{0}", retrieveResult.error.message),
                    @"Product updatering", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return retrieveResult.result;
        }

        #endregion VTIGER PRODUCT DATA

        #region VTIGER ACCOUNT DATA

        internal VTigerAccount VTigerCreateAccountFromEconomic(VTigerLogin vTigerLogin, Customer customer,
            EconomicWebServiceSoapClient session, DebtorData debtorData)
        {
            string shipStreet, shipCode, shipCity, shipCountry;
            var deliveryLocationHandle = session.Debtor_GetDeliveryLocations(debtorData.Handle);

            if (deliveryLocationHandle.Length != 0)
            {
                var deliveryLocation = session.DeliveryLocation_GetData(deliveryLocationHandle[0]);
                shipStreet = deliveryLocation.Address;
                shipCode = deliveryLocation.PostalCode;
                shipCity = deliveryLocation.City;
                shipCountry = deliveryLocation.Country;
            }
            else
            {
                shipStreet = debtorData.Address;
                shipCode = debtorData.PostalCode;
                shipCity = debtorData.City;
                shipCountry = debtorData.Country;
            }

            var vTigerAccount = new VTigerAccount()
            {
                assigned_user_id = vTigerLogin.userId,
                tickersymbol = debtorData.Number,
                accountname = debtorData.Name,
                siccode = debtorData.CINumber,
                bill_street = debtorData.Address,
                bill_code = debtorData.PostalCode,
                bill_city = debtorData.City,
                bill_country = debtorData.Country,
                phone = debtorData.TelephoneAndFaxNumber,
                email1 = debtorData.Email,
                ship_street = shipStreet,
                ship_code = shipCode,
                ship_city = shipCity,
                ship_country = shipCountry
            };

            const string elementType = "Accounts";
            var element = VTigerapiService.GetSerializeObject(vTigerAccount);
            var strResult = VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "create", String.Format("sessionName={0}&elementType={1}&element={2}", vTigerLogin.sessionName, elementType, HttpUtility.UrlEncode(element)), true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerAccount>(strResult);
            if (retrieveResult.success != true)
            {
                MessageBox.Show(
                    String.Format("Connection lost\nKunde blev ikke skabt\n{0}", retrieveResult.error.message),
                    @"Product updatering", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return retrieveResult.result;
        }

        internal VTigerAccount VTigerUpdateAccountsFromEconomic(VTigerLogin vTigerLogin, Customer customer,
            EconomicWebServiceSoapClient session, List<VTigerAccount> vTigerAccountCollection,
            DebtorData debtorData)
        {
            VTigerAccount vTigerAccount = null;
            foreach (var account in vTigerAccountCollection.ToList())
            {
                account.tickersymbol = "" + debtorData.Number;
                account.accountname = "" + debtorData.Name;
                account.siccode = "" + debtorData.CINumber;
                account.phone = "" + debtorData.TelephoneAndFaxNumber;
                account.email1 = "" + debtorData.Email;
                account.bill_street = "" + debtorData.Address;
                account.bill_code = "" + debtorData.PostalCode;
                account.bill_city = "" + debtorData.City;
                account.bill_country = "" + debtorData.Country;

                var element = VTigerapiService.GetSerializeObject(account);
                var strResult = VTigerapiService.VTigerExecuteOperation(customer.VTigerUrl, "update", String.Format("sessionName={0}&element={1}", vTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
                var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerAccount>(strResult);
                if (retrieveResult.success != true)
                {
                    MessageBox.Show(
                        String.Format("Connection lost\nKunde blev ikke opdateret\n{0}", retrieveResult.error.message),
                        @"Product updatering", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                vTigerAccount = account;
            }
            return vTigerAccount;
        }

        internal string GetEconomicSearchField(DebtorData debtorData)
        {
            var result = "";
            if (debtorData.CINumber != null && !debtorData.CINumber.Equals(""))
                result = string.Format("siccode='{0}'", debtorData.CINumber);
            else if (debtorData.TelephoneAndFaxNumber != null && !debtorData.TelephoneAndFaxNumber.Equals(""))
                result = string.Format("phone='{0}'", debtorData.TelephoneAndFaxNumber);
            //else if (debtorData.Email != null && !debtorData.Email.Equals(""))
            //    result = string.Format("email1='{0}'", debtorData.Email); 
            return result;
        }

        #endregion VTIGER ACCOUNT DATA

        #region [VISIO]

        internal VTigerAccount GetOrCreateVTigerAccount_EconomicToVTiger(VTigerLogin vTigerLogin, EconomicWebServiceSoapClient session,
            Customer customer, DebtorHandle debtorHandle)
        {
            VTigerAccount vTigerAccount;
            var debtorData = session.Debtor_GetData(debtorHandle);

            //VTiger: Field contains EconomicNumber (tickersymbol er det skjulte felt for synkronisering af VTiger og Economic)
            var vTigerAccountCollection = VTigerGetCollectionBySearch<VTigerAccount>(customer, vTigerLogin, string.Format("tickersymbol='{0}'", debtorData.Number));

            if (vTigerAccountCollection.Count() != 0)
            {
                //VTiger: Update VTigerAccount with debtorData
                vTigerAccount = VTigerUpdateAccountsFromEconomic(vTigerLogin, customer, session, vTigerAccountCollection, debtorData);
            }
            else
            {
                //Force new VTigerAccount
                if (customer.ForceNewDebtor == true)
                {
                    //VTiger: Create vTigerAccount with debtorData
                    vTigerAccount = VTigerCreateAccountFromEconomic(vTigerLogin, customer, session, debtorData);
                }
                else
                { 
                    //VTiger: Match debtorData
                    vTigerAccountCollection = new List<VTigerAccount>();
                    var economicSearchField = GetEconomicSearchField(debtorData);
                    if (!economicSearchField.Equals(""))
                        vTigerAccountCollection = VTigerGetCollectionBySearch<VTigerAccount>(customer, vTigerLogin, economicSearchField);
                    if (vTigerAccountCollection.Count() != 0)
                    {
                        //VTiger: (force) Update VTigerAccount with debtorData
                        vTigerAccount = VTigerUpdateAccountsFromEconomic(vTigerLogin, customer, session, vTigerAccountCollection, debtorData);
                    }
                    else
                    {
                        //VTiger: Create vTigerAccount with debtorData
                        vTigerAccount = VTigerCreateAccountFromEconomic(vTigerLogin, customer, session, debtorData);
                    }
                }
            }
            return vTigerAccount;
        }

        internal VTigerProduct GetOrCreateVTigerProduct_EconomicToVTiger(VTigerLogin vTigerLogin, EconomicWebServiceSoapClient session, Customer customer, ProductHandle productHandle)
        {
            VTigerProduct vTigerProduct;
            var productData = session.Product_GetData(productHandle);

            //VTiger: serial_no contains EconomicNumber
            var vTigerProductCollection = VTigerGetCollectionBySearch<VTigerProduct>(customer, vTigerLogin, string.Format("serial_no='{0}'", productData.Number));
            if (vTigerProductCollection.Count() != 0)
            {
                //VTiger: Update vTigerProduct with productData
                vTigerProduct = VTigerUpdateProductsFromEconomic(vTigerLogin, customer, session, vTigerProductCollection, productData);
            }
            else
            {
                //Force new vTigerProduct
                if (customer.ForceNewProduct == true)
                {
                    //VTiger: Create vTigerProduct with productData
                    vTigerProduct = VTigerCreateProductFromEconomic(vTigerLogin, customer, session, productData);
                }
                else
                {
                    //VTiger: Match productData (productcode)
                    vTigerProductCollection = VTigerGetCollectionBySearch<VTigerProduct>(customer, vTigerLogin, string.Format("productcode='{0}'", productData.Number));
                    if (vTigerProductCollection.Count() != 0)
                    {
                        //VTiger: (Force) update vTigerProduct with productData
                        vTigerProduct = VTigerUpdateProductsFromEconomic(vTigerLogin, customer, session, vTigerProductCollection, productData);
                    }
                    else
                    {
                        //VTiger: Create vTigerProduct with productData
                        vTigerProduct = VTigerCreateProductFromEconomic(vTigerLogin, customer, session, productData);
                    }
                }
            }
            return vTigerProduct;
        }

        #endregion [VISIO]

        internal void CreateAndStoreSomeObjects()
        {
            Db.LogSet.RemoveRange(Db.LogSet);
            Db.WebSiteInboxSet.RemoveRange(Db.WebSiteInboxSet);
            Db.CustomerSet.RemoveRange(Db.CustomerSet);
            Db.SaveChanges();

            var customer = new Customer()
            {
                Name = "Structure IT Test",
                WebLogin = "admin",
                WebPassword = "1234",
                IsActive = true,
                VTigerUsername = "admin",
                VTigerUrl = "http://crm.structureit.dk:3389/",
                VTigerAccessKey = "NbJ8qp0AuIHeUGAp",
                IsVTigerOK = true,
                EconomicPublicAPI = "jQcdfqdsfy9McxZs6muNqA1VnweOkpN4P8Z-pwAr8F41",
                EconomicPrivateAPI = "zmsXHttuoJAGJ3AVmevV9dFmDVPp2uw1p-ANPH7o33I1",
                IsEconomicOK = true,
                ForceNewDebtor = false,
                ForceNewProduct = false,
                DateCreated = DateTime.Now,
                DateLastUpdated = DateTime.Now,
                IsWriteToLogFile = false

            };

            Db.CustomerSet.Add(customer);
            Db.SaveChanges();

            var log1 = new Log()
            {
                Customer = customer,
                Name = "VTiger: Produkt: 1012 blev ikke opdateret. [connection lost]",
                Info = "Problem: Ved kald til E-conomic returnerede session null\n" +
                       "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                       "Teknik: DataSync_Engine: MainWindow : CustomerSyncronizeAll : " +
                       "productData = session.Product_GetData(productHandle)",
                DateCreated = DateTime.Now,
                IsError = true
            };

            var log2 = new Log()
            {
                Customer = customer,
                Name = "VTiger: Produkt: 1012 blev ikke opdateret. [connection lost]",
                Info = "Problem: Ved kald til E-conomic er synkroniseret vare slettet\n" +
                       "Løsning: Fejlen er rettet ved at fjerne productcode i VTiger\n" +
                       "Teknik: DataSync_Engine : MainWindow : CustomerSyncronizeAll : " +
                       "session.Product_FindByNumber : productHandle == null ",
                DateCreated = DateTime.Now,
                IsError = true
            };

            Db.LogSet.Add(log1);
            Db.LogSet.Add(log2);
            Db.SaveChanges();

            var webSiteInboxMessage1 = new WebSiteInbox()
           {
               Customer = customer,
               From = "DataSync",
               Subject = "Produktgruppe [Hardware] findes ikke i E-conomic",
               Message = "Ved synkronisering af VTiger produkt [pro1] manglede produktgruppe [Hardware] i E-conomic\n" +
               "Opret produktgruppen [Hardware] i E-conomic eller vælg anden produktgruppe i VTiger for produkt [pro1]",
               DateCreated = DateTime.Now
           };

            var webSiteInboxMessage2 = new WebSiteInbox()
            {
                Customer = customer,
                From = "DataSync",
                Subject = "Tilbud [qou] ikke overført til E-conomic",
                Message = "Ved synkronisering af VTiger tilbud var kunde[%] slettet\n" +
                "Gennemse tilbud [qou] og vælg kunde\n",
                DateCreated = DateTime.Now
            };

            var webSiteInboxMessage3 = new WebSiteInbox()
            {
                Customer = customer,
                From = "DataSync",
                Subject = "Tilbud [qou] blev ikke overført til E-conomic til fakturering",
                Message = "Ved synkronisering af VTiger tilbud var produkt [%] slettet\n" +
                "Gennemse tilbud [qou] for slettede produkter",
                DateCreated = DateTime.Now
            };

            Db.WebSiteInboxSet.Add(webSiteInboxMessage1);
            Db.WebSiteInboxSet.Add(webSiteInboxMessage2);
            Db.WebSiteInboxSet.Add(webSiteInboxMessage3);
            Db.SaveChanges();
        }
    }
}