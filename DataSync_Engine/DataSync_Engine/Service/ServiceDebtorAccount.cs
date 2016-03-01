using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DataSync_Engine.EconomicWSDL;
using DataSync_Engine.Service.VTiger;

namespace DataSync_Engine.Service
{
    public partial class Service
    {

        #region VTIGERACCOUNT DATA [return]

        internal VTigerAccount VTigerCreateAccountFromEconomic(ServiceObject serviceObject, DebtorData debtorData)
        {
            string shipStreet, shipCode, shipCity, shipCountry;
            DeliveryLocationHandle[] deliveryLocationHandle;
            DeliveryLocationData deliveryLocation;

            try
            {
                deliveryLocationHandle = serviceObject.Session.Debtor_GetDeliveryLocations(debtorData.Handle);
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer, string.Format(
                    "VTiger: KundeId: {0} blev ikke skabt. [connection lost]", debtorData.Number),
                    "Problem: Ved kald til E-conomic returnerede session null\n" +
                    "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                    "Teknik: DataSync_Engine: Service : VTigerCreateAccountFromEconomic");
                serviceObject.IsConnectionLost = true;
                return null;
            }

            if (deliveryLocationHandle.Length != 0)
            {
                try
                {
                    deliveryLocation = serviceObject.Session.DeliveryLocation_GetData(deliveryLocationHandle[0]);
                }
                catch (Exception)
                {
                    WriteToLog(serviceObject.Customer, string.Format(
                        "VTiger: KundeId: {0} blev ikke skabt. [connection lost]", debtorData.Number),
                        "Problem: Ved kald til E-conomic returnerede session null\n" +
                        "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                        "Teknik: DataSync_Engine: Service : VTigerCreateAccountFromEconomic");
                    serviceObject.IsConnectionLost = true;
                    return null;
                }

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
                assigned_user_id = serviceObject.VTigerLogin.userId,
                tickersymbol = debtorData.Number, // synkroniseringsfelt
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

            var account = vTigerAccount;
            var element = VTigerapiService.GetSerializeObject(account);
            const string elementType = "Accounts";
            var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl,
                "create", String.Format("sessionName={0}&elementType={1}&element={2}", serviceObject.VTigerLogin.sessionName, elementType, HttpUtility.UrlEncode(element)), true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerAccount>(strResult);
            if (retrieveResult.success == true)
            {
                serviceObject.FeedbackToBackgroundWorker = FeedBackType.Create.EnumValue();
                return retrieveResult.result;
            }

            WriteToLog(serviceObject.Customer, string.Format(
                "VTiger: KundeID: {0} blev ikke skabt. [connection lost]", debtorData.Number),
                "Problem: Ved create til VTiger returnerede result false\n" +
                "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
                "Teknik: DataSync_Engine: Service : VTigerCreateAccountFromEconomic");
            serviceObject.IsConnectionLost = true;
            return null;
        }

        internal VTigerAccount VTigerUpdateAccountFromEconomic(ServiceObject serviceObject, VTigerAccount vTigerAccount,
            DebtorData debtorData, bool forceUpdate = false)
        {
            if (!IsValuesInDebtorAccountDifferent(serviceObject, debtorData, vTigerAccount) && !forceUpdate)
            {
                serviceObject.FeedbackToBackgroundWorker = FeedBackType.None.EnumValue();
                return vTigerAccount;
            }

            vTigerAccount.tickersymbol = "" + debtorData.Number;
            vTigerAccount.accountname = "" + debtorData.Name;
            vTigerAccount.siccode = "" + debtorData.CINumber;
            vTigerAccount.phone = "" + debtorData.TelephoneAndFaxNumber;
            vTigerAccount.email1 = "" + debtorData.Email;
            vTigerAccount.bill_street = "" + debtorData.Address;
            vTigerAccount.bill_code = "" + debtorData.PostalCode;
            vTigerAccount.bill_city = "" + debtorData.City;
            vTigerAccount.bill_country = "" + debtorData.Country;

            var element = VTigerapiService.GetSerializeObject(vTigerAccount);
            var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl,
                "update", String.Format("sessionName={0}&element={1}", serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerAccount>(strResult);

            if (retrieveResult.success == true)
            {
                serviceObject.FeedbackToBackgroundWorker = FeedBackType.Update.EnumValue();
                return retrieveResult.result;
            }

            WriteToLog(serviceObject.Customer, string.Format(
                "VTiger: KundeID: {0} blev ikke skabt. [connection lost]", debtorData.Number),
                "Problem: Ved create til VTiger returnerede result false\n" +
                "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
                "Teknik: DataSync_Engine: Service : VTigerCreateAccountFromEconomic");
            serviceObject.IsConnectionLost = true;
            return null;
        }

        internal VTigerAccount VTigerUpdateSameAccountsFromEconomic(ServiceObject serviceObject, IEnumerable<VTigerAccount> vTigerAccountCollection,
            DebtorData debtorData, bool forceUpdate = false)
        {
            var checkupdate = FeedBackType.None.EnumValue();
            VTigerAccount vTigerAccount = null;
            foreach (var account in vTigerAccountCollection.ToList())
            {
                vTigerAccount = VTigerUpdateAccountFromEconomic(serviceObject, account, debtorData, forceUpdate);
                if (!serviceObject.FeedbackToBackgroundWorker.Equals(FeedBackType.None.EnumValue()))
                    checkupdate = serviceObject.FeedbackToBackgroundWorker;
                if (vTigerAccount == null) return null;
            }
            serviceObject.FeedbackToBackgroundWorker = checkupdate;
            return vTigerAccount;
        }

        #endregion VTIGERACCOUNT DATA [return]

        #region DEBTOR DATA [return]

        internal DebtorData EconomicUpdateDebtorFromVTiger(ServiceObject serviceObject, VTigerAccount vTigerAccount, DebtorData debtorData)
        {
            debtorData.Name = vTigerAccount.accountname;
            debtorData.CINumber = vTigerAccount.siccode;
            debtorData.TelephoneAndFaxNumber = vTigerAccount.phone;
            debtorData.Email = vTigerAccount.email1;
            debtorData.Address = vTigerAccount.bill_street;
            debtorData.PostalCode = vTigerAccount.bill_code;
            debtorData.City = vTigerAccount.bill_city;
            debtorData.Country = vTigerAccount.bill_country;

            try
            {
                serviceObject.Session.Debtor_UpdateFromData(debtorData);
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer, string.Format(
                    "E-Conomic: KundeId: {0} blev ikke opdateret. [connection lost]", vTigerAccount.tickersymbol),
                    "Problem: Ved kald til E-conomic returnerede session null\n" +
                    "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                    "Teknik: DataSync_Engine : Service : EconomicUpdateDebtorFromVTiger : session.Product_UpdateFromData");
                serviceObject.IsConnectionLost = true;
                return null;
            }

            serviceObject.FeedbackToBackgroundWorker = FeedBackType.Update.EnumValue();
            return debtorData;
        }

        internal DebtorHandle EconomicCreateDebtorFromVTiger(ServiceObject serviceObject, VTigerAccount vTigerAccount, string economicNumber)
        {
            DebtorHandle debtorHandle = null;
            CurrencyHandle currencyHandle = null;

            try
            {
                var debtorGroupHandle = serviceObject.Session.DebtorGroup_FindByNumber(1);
                currencyHandle = serviceObject.Session.Currency_FindByCode("DKK");
                debtorHandle = serviceObject.Session.Debtor_Create(economicNumber, debtorGroupHandle, vTigerAccount.accountname, VatZone.HomeCountry);
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer, string.Format(
                    "E-Conomic: Kunde: {0} med EconomicNumber:{1} blev ikke skabt.", vTigerAccount.accountname, economicNumber),
                    "Problem: Ved kald til E-conomic returnerede Debtor_CreateFromData null\n" +
                    "Løsning: Fejlen burde ikke opstå, vent på næste synkronisering\n" +
                    "Teknik: DataSync_Engine : Service : EconomicCreateDebtorFromVTiger : Debtor_CreateFromData");
                serviceObject.MessageToBackgroundWorker = string.Format("E-Conomic: Kunde: {0} med EconomicNumber:{1} blev ikke skabt.", vTigerAccount.accountname, economicNumber);
                return null;
            }

            var debtorData = GetDebtorDataByHandle(serviceObject, debtorHandle, "Service : EconomicCreateDebtorFromVTiger : GetDebtorDataByHandle");
            debtorData.CINumber = vTigerAccount.siccode;
            debtorData.TelephoneAndFaxNumber = vTigerAccount.phone;
            debtorData.Email = vTigerAccount.email1;
            debtorData.Address = vTigerAccount.bill_street;
            debtorData.PostalCode = vTigerAccount.bill_code;
            debtorData.City = vTigerAccount.bill_city;
            debtorData.Country = vTigerAccount.bill_country;
            debtorData.IsAccessible = true;
            debtorData.CurrencyHandle = currencyHandle;

            try
            {
                serviceObject.Session.Debtor_UpdateFromData(debtorData);
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer, string.Format(
                 "E-Conomic: Kunde: {0} blev ikke skabt korrekt . [connection lost]", debtorHandle.Number),
                 "Problem: Ved kald til E-conomic returnerede session null\n" +
                 "Løsning: Gennemse produkt i E-conomic og E-conomic settings og forbindelse til server\n" +
                 "Teknik: DataSync_Engine : Service : EconomicUpdateProductFromVTiger : session.Debtor_CreateFromData");
                serviceObject.IsConnectionLost = true;
                return null;
            }
            serviceObject.FeedbackToBackgroundWorker = FeedBackType.Create.EnumValue();
            return debtorHandle;
        }

        internal DebtorHandle GetDebtorHandleByNumber(ServiceObject serviceObject, string economicNumber, string calledFrom)
        {
            try
            {
                return serviceObject.Session.Debtor_FindByNumber(economicNumber);
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer, string.Format(
                    "E-conomic: Debtor: {0} blev ikke hentet. [connection lost]", economicNumber),
                    "Problem: Ved kald til E-conomic returnerede session null\n" +
                    "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                      string.Format("Teknik: DataSync_Engine: {0}", calledFrom));
                serviceObject.IsConnectionLost = true;
                return null; ;
            }
        }

        internal DebtorData GetDebtorDataByHandle(ServiceObject serviceObject, DebtorHandle debtorHandle, string calledFrom)
        {
            try
            {
                var debtorData = serviceObject.Session.Debtor_GetData(debtorHandle);
                if (!debtorData.IsAccessible)
                {
                    debtorData.IsAccessible = true;
                    serviceObject.Session.Debtor_UpdateFromData(debtorData);
                }
                return debtorData;
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer, string.Format(
                    "E-conomic: Debtor: {0} blev ikke hentet. [connection lost]", debtorHandle.Number),
                    "Problem: Ved kald til E-conomic returnerede session null\n" +
                    "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                    string.Format("Teknik: DataSync_Engine: {0}", calledFrom));
                serviceObject.IsConnectionLost = true;
                return null; ;
            }
        }

        internal DebtorHandle FindDebtorHandleMatchingVTigerAccount(ServiceObject serviceObject, VTigerAccount vTigerAccount)
        {
            DebtorHandle debtorHandle = null;
            try
            {
                if (!vTigerAccount.siccode.Equals(""))
                    debtorHandle = serviceObject.Session.Debtor_FindByCINumber(vTigerAccount.siccode).FirstOrDefault();
                if (debtorHandle == null && !vTigerAccount.phone.Equals(""))
                    debtorHandle = serviceObject.Session.Debtor_FindByTelephoneAndFaxNumber(vTigerAccount.phone).FirstOrDefault();
                if (UseEmailAsMatch && debtorHandle == null && !vTigerAccount.email1.Equals(""))
                    debtorHandle = serviceObject.Session.Debtor_FindByEmail(vTigerAccount.email1).FirstOrDefault();
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer,
                    "E-conomic: GetNextDebtorNumber. [connection lost]",
                   "Problem: Ved kald til E-conomic returnerede session null\n" +
                   "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                     string.Format("Teknik: DataSync_Engine: Service : FindDebtorHandleMatchingVTigerAccount"));
                serviceObject.IsConnectionLost = true;
            }

            return debtorHandle;
        }

        #endregion DEBTOR DATA [return]

        #region SUBFUNKTIONER

        internal bool VTigerAccountSetEconomicNumber(ServiceObject serviceObject, VTigerAccount vTigerAccount,
            string economicNumber)
        {
            if (vTigerAccount.tickersymbol.Equals(economicNumber)) return false;

            vTigerAccount.tickersymbol = economicNumber;
            var element = VTigerapiService.GetSerializeObject(vTigerAccount);
            var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl,
                "update", String.Format("sessionName={0}&element={1}", serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerAccount>(strResult);
            if (retrieveResult.success == true) return true;

            WriteToLog(serviceObject.Customer, string.Format(
                "VTiger: KundeID: {0} blev ikke synced til VTiger. [connection lost]", economicNumber),
                "Problem: Ved sync til VTiger returnerede result false\n" +
                "Løsning: Vent på næste sync. Undersøg VTiger settings og forbindelse til server\n" +
                "Teknik: DataSync_Engine: Service : VTigerAccountSetEconomicNumber");
            serviceObject.IsConnectionLost = true;
            return false;
        }

        internal bool VTigerAccountRemoveNumberToEconomic(ServiceObject serviceObject, VTigerAccount vTigerAccount)
        {
            vTigerAccount.tickersymbol = "";
            var element = VTigerapiService.GetSerializeObject(vTigerAccount);
            var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl, "update", String.Format("sessionName={0}&element={1}",
                serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
            var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerAccount>(strResult);

            if (retrieveResult.success == true)
            {
                serviceObject.FeedbackToBackgroundWorker = "[sync removed]";
                return true;
            }

            WriteToLog(serviceObject.Customer, string.Format(
                "VTiger: KundeID: {0} blev ikke opdateret. [connection lost]", vTigerAccount.tickersymbol),
                "Problem: Ved update til VTiger returnerede result false\n" +
                "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
                "Teknik: DataSync_Engine : Service : VTigerAcVTigerAccountRemoveNumberToEconomiccountFixProductCode");
            serviceObject.IsConnectionLost = true;
            return false;
        }

        internal string GetEconomicSearchField(DebtorData debtorData)
        {
            var result = "";
            if (debtorData.CINumber != null && !debtorData.CINumber.Equals(""))
                result = string.Format("siccode='{0}'", debtorData.CINumber);
            else if (debtorData.TelephoneAndFaxNumber != null && !debtorData.TelephoneAndFaxNumber.Equals(""))
                result = string.Format("phone='{0}'", debtorData.TelephoneAndFaxNumber);
            else if (UseEmailAsMatch && debtorData.Email != null && !debtorData.Email.Equals(""))
                result = string.Format("email1='{0}'", debtorData.Email);
            return result;
        }

        internal bool IsValuesInDebtorAccountDifferent(ServiceObject serviceObject, DebtorData debtorData, VTigerAccount vTigerAccount)
        {
            var debtorDataCiNumber = debtorData.CINumber ?? "";
            var debtorDataTelephoneAndFaxNumber = debtorData.TelephoneAndFaxNumber ?? "";
            var debtorDataEmail = debtorData.Email ?? "";
            var debtorDataAddress = debtorData.Address ?? "";
            var debtorDataPostalCode = debtorData.PostalCode ?? "";
            var debtorDataCity = debtorData.City ?? "";
            var debtorDataCountry = debtorData.Country ?? "";
            if (
                //!session.DebtorGroup_GetData(debtorData.DebtorGroupHandle).Name.Equals(vTigerAccount.accounttype) ||
                !debtorData.Name.Equals(vTigerAccount.accountname) ||
                !debtorDataCiNumber.Equals(vTigerAccount.siccode) ||
                !debtorDataTelephoneAndFaxNumber.Equals(vTigerAccount.phone) ||
                !debtorDataEmail.Equals(vTigerAccount.email1) ||
                !debtorDataAddress.Equals(vTigerAccount.bill_street) ||
                !debtorDataPostalCode.Equals(vTigerAccount.bill_code) ||
                !debtorDataCity.Equals(vTigerAccount.bill_city) ||
                !debtorDataCountry.Equals(vTigerAccount.bill_country)
                ) return true;

            return false;
        }

        private int GetNextDebtorNumber(ServiceObject serviceObject)
        {
            var result = 0;
            try
            {
                result = serviceObject.Session.Debtor_GetAll().Select(p => int.Parse(p.Number)).Max() + 1;
            }
            catch (Exception)
            {
                WriteToLog(serviceObject.Customer,
                    "E-conomic: GetNextDebtorNumber. [connection lost]",
                   "Problem: Ved kald til E-conomic returnerede session null\n" +
                   "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                     string.Format("Teknik: DataSync_Engine: Service : GetNextDebtorNumber"));
                serviceObject.IsConnectionLost = true;
            }
            return result;
        }

        #endregion SUBFUNKTIONER
    }
}