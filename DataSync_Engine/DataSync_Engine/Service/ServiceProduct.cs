using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using DataSync_Engine.EconomicWSDL;
using DataSync_Engine.Service.VTiger;

namespace DataSync_Engine.Service
{
   public partial class Service
    {

       #region VTIGER PRODUCT DATA [return]

       internal VTigerProduct VTigerUpdateProductFromEconomic(ServiceObject serviceObject, VTigerProduct vTigerProduct,
           ProductData productData, bool forceupdate = false)
       {
           if (!forceupdate && !IsValuesInProductsDifferent(serviceObject, productData, vTigerProduct))
           {
               serviceObject.FeedbackToBackgroundWorker = FeedBackType.None.EnumValue();
               return vTigerProduct;
           }

           var productDataUnitName = "";
           var productDataProductGroup = "";
           try
           {
               if (productData.UnitHandle != null) productDataUnitName = serviceObject.Session.Unit_GetData(productData.UnitHandle).Name;
               productDataProductGroup = serviceObject.Session.ProductGroup_GetData(productData.ProductGroupHandle).Name;
           }
           catch (Exception)
           {
               WriteToLog(serviceObject.Customer, string.Format(
                   "VTiger: ProduktID: {0} blev ikke opdateret. [connection lost]", productData.Number),
                   "Problem: Ved kald til E-conomic returnerede session null\n" +
                   "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                   "Teknik: DataSync_Engine: Service: VTigerUpdateProductFromEconomic");
               serviceObject.IsConnectionLost = true;
               return null;
           }

           vTigerProduct.productname = productData.Name;
           vTigerProduct.unit_price = FormatEconomicPriceToStringForVTiger(productData.SalesPrice);
           vTigerProduct.description = productData.Description;
           vTigerProduct.productcode = productData.Number;
           vTigerProduct.serial_no = productData.Number;
           vTigerProduct.usageunit = productDataUnitName;
           vTigerProduct.productcategory = productDataProductGroup;
           vTigerProduct.discontinued = "1";

           var element = VTigerapiService.GetSerializeObject(vTigerProduct);
           var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl, "update", String.Format("sessionName={0}&element={1}",
               serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
           var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerProduct>(strResult);

           if (retrieveResult.success == true)
           {
               serviceObject.FeedbackToBackgroundWorker = FeedBackType.Update.EnumValue();
               return retrieveResult.result;
           }
           WriteToLog(serviceObject.Customer, string.Format(
               "VTiger: ProduktID: {0} blev ikke opdateret. [connection lost]", productData.Number),
               "Problem: Ved update til VTiger returnerede result false\n" +
               "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
               "Teknik: DataSync_Engine: Service:  VTigerUpdateSameProductFromEconomic");
           serviceObject.IsConnectionLost = true;
           return null;
       }

       internal VTigerProduct VTigerUpdateSameProductFromEconomic(ServiceObject serviceObject, IEnumerable<VTigerProduct> vTigerProductCollection,
           ProductData productData, bool forceUpdate = false )
       {
           VTigerProduct vTigerProduct = null;
           var checkupdate = FeedBackType.None.EnumValue();
           foreach (var product in vTigerProductCollection.ToList())
           {
               vTigerProduct = VTigerUpdateProductFromEconomic(serviceObject, product, productData, forceUpdate);
               if (!serviceObject.FeedbackToBackgroundWorker.Equals(FeedBackType.None.EnumValue()))
                   checkupdate = serviceObject.FeedbackToBackgroundWorker;
               if (vTigerProduct == null) return null;
           }
           serviceObject.FeedbackToBackgroundWorker = checkupdate;
           return vTigerProduct;
       }

       internal VTigerProduct VTigerCreateProductFromEconomic(ServiceObject serviceObject, ProductData productData)
       {
           var productDataUnitName = "";
           var productDataProductGroup = "";
           try
           {
               if (productData.UnitHandle != null) productDataUnitName = serviceObject.Session.Unit_GetData(productData.UnitHandle).Name;
               productDataProductGroup = serviceObject.Session.ProductGroup_GetData(productData.ProductGroupHandle).Name;
           }
           catch (Exception)
           {
               WriteToLog(serviceObject.Customer, string.Format(
                   "VTiger: ProduktID: {0} blev ikke skabt. [connection lost]", productData.Number),
                   "Problem: Ved kald til E-conomic returnerede session null\n" +
                   "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                   "Teknik: DataSync_Engine: Service: VTigerCreateProductFromEconomic");
               serviceObject.IsConnectionLost = true;
               return null;
           }

           var vTigerProduct = new VTigerProduct()
           {
               assigned_user_id = serviceObject.VTigerLogin.userId,
               productname = productData.Name,
               unit_price = "" + productData.SalesPrice,
               description = productData.Description,
               productcode = productData.Number,
               serial_no = productData.Number,
               usageunit = productDataUnitName,
               productcategory = productDataProductGroup,
               discontinued = "1" // betyder det modsatte !!!
           };

           var product = vTigerProduct;
           var element = VTigerapiService.GetSerializeObject(product);
           const string elementType = "Products";
           var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl, "create", String.Format("sessionName={0}&elementType={1}&element={2}",
               serviceObject.VTigerLogin.sessionName, elementType, HttpUtility.UrlEncode(element)), true);
           var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerProduct>(strResult);

           if (retrieveResult.success == true)
           {
               serviceObject.FeedbackToBackgroundWorker = FeedBackType.Create.EnumValue();
               return retrieveResult.result;
           }

           WriteToLog(serviceObject.Customer, string.Format(
               "VTiger: ProduktID: {0} blev ikke skabt. [connection lost]", productData.Number),
               "Problem: Ved create til VTiger returnerede result false\n" +
               "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
               "Teknik: DataSync_Engine: Service: VTigerCreateProductFromEconomic");
           serviceObject.IsConnectionLost = true;
           return null;
       }

       #endregion VTIGER PRODUCT DATA [return]

       #region E-CONOMIC PRODUCT DATA [return]

       internal ProductHandle GetProductHandleByNumber(ServiceObject serviceObject, string economicNumber, string calledFrom)
       {
           try
           {
               return serviceObject.Session.Product_FindByNumber(economicNumber);
           }
           catch (Exception)
           {
               WriteToLog(serviceObject.Customer, string.Format(
                   "E-conomic: Product: {0} blev ikke hentet. [connection lost]", economicNumber),
                   "Problem: Ved kald til E-conomic returnerede session null\n" +
                   "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                     string.Format("Teknik: DataSync_Engine: {0}", calledFrom));
               serviceObject.IsConnectionLost = true;
               return null; ;
           }
       }

       internal ProductData GetProductDataByHandle(ServiceObject serviceObject, ProductHandle productHandle, string calledFrom)
       {
           try
           {
               var productData = serviceObject.Session.Product_GetData(productHandle);
               if (!productData.IsAccessible)
               {
                   productData.IsAccessible = true;
                   serviceObject.Session.Product_UpdateFromData(productData);
               }
               return productData;
           }
           catch (Exception)
           {
               WriteToLog(serviceObject.Customer, string.Format(
                   "E-conomic: Product: {0} blev ikke hentet. [connection lost]", productHandle.Number),
                   "Problem: Ved kald til E-conomic returnerede session null\n" +
                   "Løsning: Undersøg E-conomic setting og forbindelse til server\n" +
                   string.Format("Teknik: DataSync_Engine: {0}", calledFrom));
               serviceObject.IsConnectionLost = true;
               return null; ;
           }
       }

       internal ProductData EconomicUpdateProductFromVTiger(ServiceObject serviceObject, VTigerProduct vTigerProduct, ProductData productData)
       {
           if (!IsValuesInProductsDifferent(serviceObject, productData, vTigerProduct))
           {
               serviceObject.FeedbackToBackgroundWorker = FeedBackType.None.EnumValue();
               return productData;
           }

           productData.Name = vTigerProduct.productname;
           productData.Description = vTigerProduct.description;
           productData.SalesPrice = (decimal)FormatPriceVTigerPriceToDouble(vTigerProduct.unit_price);

           var productGroupHandle = GetProductGroupHandleFromVTigerProductcategory(serviceObject, vTigerProduct);
           if (productGroupHandle != null) productData.ProductGroupHandle = productGroupHandle;

           var unitHandle = GetUnitHandleFromVTigerusageunit(serviceObject, vTigerProduct);
           if (unitHandle != null) productData.UnitHandle = unitHandle;

           try
           {
               serviceObject.Session.Product_UpdateFromData(productData);
           }
           catch (Exception)
           {
               WriteToLog(serviceObject.Customer, string.Format(
                   "E-Conomic: ProduktID: {0} blev ikke opdateret. [connection lost]", vTigerProduct.serial_no),
                   "Problem: Ved kald til E-conomic returnerede session null\n" +
                   "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                   "Teknik: DataSync_Engine: Service: EconomicUpdateProductFromVTiger: session.Product_UpdateFromData");
               serviceObject.IsConnectionLost = true;
               return null;
           }
           serviceObject.FeedbackToBackgroundWorker = FeedBackType.Update.EnumValue();
           return productData;
       }

       internal ProductHandle EconomicCreateProductFromVTiger(ServiceObject serviceObject, VTigerProduct vTigerProduct, string economicNumber)
       {
           ProductHandle productHandle = null;

           var productData = new ProductData()
           {
               Number = economicNumber,
               Name = vTigerProduct.productname,
               Description = vTigerProduct.description,
               SalesPrice = (decimal)FormatPriceVTigerPriceToDouble(vTigerProduct.unit_price),
               IsAccessible = true
           };

           var productGroupHandle = GetProductGroupHandleFromVTigerProductcategory(serviceObject, vTigerProduct);
           if (productGroupHandle != null) productData.ProductGroupHandle = productGroupHandle;

           var unitHandle = GetUnitHandleFromVTigerusageunit(serviceObject, vTigerProduct);
           if (unitHandle != null) productData.UnitHandle = unitHandle;

           try
           {
               productHandle = serviceObject.Session.Product_CreateFromData(productData);
           }
           catch (Exception)
           {
               WriteToLog(serviceObject.Customer, string.Format(
                "E-Conomic: Product: {0} blev ikke skabt. [connection lost]", vTigerProduct.serial_no),
                "Problem: Ved kald til E-conomic returnerede session null\n" +
                "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                "Teknik: DataSync_Engine: Service: EconomicUpdateProductFromVTiger: session.Unit_CreateFromData");
               serviceObject.IsConnectionLost = true;
               return null;
           }

           serviceObject.FeedbackToBackgroundWorker = FeedBackType.Create.EnumValue();
           return productHandle;
       }

       #endregion E-CONOMIC PRODUCT DATA [return]

       #region SUBFUNKTIONER

       private ProductGroupHandle GetProductGroupHandleFromVTigerProductcategory(ServiceObject serviceObject, VTigerProduct vTigerProduct)
       {
           ProductGroupHandle productGroupHandle = null;

           var productGroupHandles = new ProductGroupHandle[] { };
           if (vTigerProduct.productcategory != "")
           {
               try
               {
                   productGroupHandles = serviceObject.Session.ProductGroup_FindByName(vTigerProduct.productcategory);
               }
               catch (Exception)
               {
                   WriteToLog(serviceObject.Customer, string.Format(
                       "E-Conomic: Produktgruppe: {0} blev ikke opdateret. [connection lost]", vTigerProduct.productcategory),
                       "Problem: Ved kald til E-conomic returnerede session null\n" +
                       "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                       "Teknik: DataSync_Engine: Service: GetProductGroupHandleFromVTigerProductcategory: session.ProductGroup_FindByName");
                   serviceObject.IsConnectionLost = true;
                   return null;
               }
           }

           if (productGroupHandles != null && productGroupHandles.Length == 0)
           {
               WriteToWebSiteInbox(serviceObject.Customer,
                             String.Format("Produktgruppe [{0}] findes ikke i E-conomic", vTigerProduct.productcategory),
                             String.Format("Ved synkronisering af VTiger produkt [{0}] manglede produktgruppe [{1}] i E-conomic\n"+
                             "Opret produktgruppen [{1}] i E-conomic eller vælg anden produktgruppe i VTiger for produktet", vTigerProduct.productname, vTigerProduct.productcategory));
               
               WriteToLog(serviceObject.Customer, string.Format(
                   "E-conomic: Produktgruppe {0} eksisterer ikke.", vTigerProduct.productcategory),
                   "Problem: Der fandtes ikke produktgruppe. Der blev returneret første\n" +
                   "Løsning: Opret produktgruppe med samme navn i E-conomic eller vælg andet i VTiger\n" +
                   "Teknik: DataSync_Engine: Service: GetProductGroupHandleFromVTigerProductcategory");
               
               serviceObject.MessageToBackgroundWorker =
                   string.Format("[TigerProduct: {0} fandt ikke produktgruppe: {1} i E-conomic| Opret produktgruppe i E-conomic]", vTigerProduct.productname, vTigerProduct.productcategory);

               productGroupHandle = serviceObject.Session.ProductGroup_FindByNumber(1);
           }

           if (productGroupHandles != null && productGroupHandles.Length > 0)
               productGroupHandle = productGroupHandles[0];

           return productGroupHandle;
       }

       private UnitHandle GetUnitHandleFromVTigerusageunit(ServiceObject serviceObject, VTigerProduct vTigerProduct)
       {
           UnitHandle unitHandle = null;
           var unithandles = new UnitHandle[] { };
           if (vTigerProduct.usageunit != "")
           {
               try
               {
                   unithandles = serviceObject.Session.Unit_FindByName(vTigerProduct.usageunit);
               }
               catch (Exception)
               {
                   WriteToLog(serviceObject.Customer, string.Format(
                       "E-Conomic: ProduktID: {0} blev ikke opdateret. [connection lost]", vTigerProduct.serial_no),
                       "Problem: Ved kald til E-conomic returnerede session null\n" +
                       "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                       "Teknik: DataSync_Engine: Service: EconomicUpdateProductFromVTiger: session.Unit_FindByName");
                   serviceObject.IsConnectionLost = true;
                   return null;
               }
           }

           if (unithandles != null && unithandles.Length == 0 && vTigerProduct.usageunit != "")
           {
               var unitData = new UnitData() { Name = vTigerProduct.usageunit };
               try
               {
                   unitHandle = serviceObject.Session.Unit_CreateFromData(unitData);
               }
               catch (Exception)
               {
                   WriteToLog(serviceObject.Customer, string.Format(
                    "E-Conomic: Unit: {0} blev ikke skabt. [connection lost]", vTigerProduct.serial_no),
                    "Problem: Ved kald til E-conomic returnerede session null\n" +
                    "Løsning: Undersøg E-conomic settings og forbindelse til server\n" +
                    "Teknik: DataSync_Engine: Service: GetUnitHandleFromVTigerusageunit: session.Unit_CreateFromData");
                   serviceObject.IsConnectionLost = true;
                   return null;
               }
           }
           if (unithandles != null && unithandles.Length > 0)
               unitHandle = unithandles[0];

           return unitHandle;
       }

       internal bool VTigerProductSetEconomicNumber(ServiceObject serviceObject, VTigerProduct vTigerProduct,
           string economicNumber)
       {
           if (vTigerProduct.productcode.Equals(economicNumber) && vTigerProduct.serial_no.Equals(economicNumber)) return false;

           vTigerProduct.productcode = economicNumber;
           vTigerProduct.serial_no = economicNumber;

           var element = VTigerapiService.GetSerializeObject(vTigerProduct);
           var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl,
               "update", String.Format("sessionName={0}&element={1}", serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
           var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerProduct>(strResult);
           if (retrieveResult.success == true) return true;

           WriteToLog(serviceObject.Customer, string.Format(
               "VTiger: KundeID: {0} blev ikke synced til VTiger. [connection lost]", economicNumber),
               "Problem: Ved sync til VTiger returnerede result false\n" +
               "Løsning: Vent på næste sync. Undersøg VTiger settings og forbindelse til server\n" +
               "Teknik: DataSync_Engine: ServiceProduct : VTigerProductSetEconomicNumber");
           serviceObject.IsConnectionLost = true;
           return false;
       }

       internal double FormatPriceVTigerPriceToDouble(string vTigerPrice)
       {
           double salesPrice;
           if (!Double.TryParse(vTigerPrice.Replace(",", "").Replace(".", ","), out salesPrice))
               salesPrice = 0.00;
           return salesPrice;
       }

       internal string FormatEconomicPriceToStringForVTiger(decimal amount)
       {
           return amount.ToString(CultureInfo.CurrentCulture).Replace(",", ".");
       }

       private bool IsValuesInProductsDifferent(ServiceObject serviceObject, ProductData productData, VTigerProduct vTigerProduct)
       {
           var productDataUnitName = "";
           var productDataDescription = productData.Description ?? "";
           try
           {
               if (productData.UnitHandle != null) productDataUnitName = serviceObject.Session.Unit_GetData(productData.UnitHandle).Name;
           }
           catch (Exception)
           {
               serviceObject.IsConnectionLost = true;
               return false;
           }
           if (!productData.Name.Equals(vTigerProduct.productname) ||
               !productDataDescription.Equals(vTigerProduct.description) ||
               !productDataUnitName.Equals(vTigerProduct.usageunit) ||
               !serviceObject.Session.ProductGroup_GetData(productData.ProductGroupHandle).Name.Equals(vTigerProduct.productcategory) ||
              (double)productData.SalesPrice != FormatPriceVTigerPriceToDouble(vTigerProduct.unit_price)
               ) return true;

           return false;
       }

       private string GetNextProductNumberFromEconomic(ServiceObject serviceObject, string productCode)
       {
           var found = false;
           while (!found)
           {
               ProductHandle handle;
               try
               {
                   handle = serviceObject.Session.Product_FindByNumber(productCode);
               }
               catch (Exception)
               {
                   WriteToLog(serviceObject.Customer, string.Format(
                       "VTiger: ProduktID: {0} blev ikke opdateret. [connection lost]", productCode),
                       "Problem: Ved update til VTiger returnerede result false\n" +
                       "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
                       "Teknik: DataSync_Engine: Service: GetNextProductNumberFromEconomic: session.Product_FindByNumber");
                   serviceObject.IsConnectionLost = true;
                   return "";
               }
               if (handle == null) found = true;
               else
                   productCode = GetNewProductNumber(productCode);
           }    
           return productCode;
       }

       private string GetNewProductNumber(string s)
       {
           var result = 0;
           var lastIndexOfString = s.LastIndexOf("_", StringComparison.Ordinal);
           return int.TryParse(s.Substring(lastIndexOfString + 1), out result) ? string.Format("{0}_{1}", s.Substring(0, lastIndexOfString), (++result)) : string.Format("{0}_1", s);
       }

       internal bool VTigerProductFixProductCode(ServiceObject serviceObject, VTigerProduct vTigerProduct)
       {
           vTigerProduct.productcode = vTigerProduct.serial_no;
           var element = VTigerapiService.GetSerializeObject(vTigerProduct);
           var strResult = VTigerapiService.VTigerExecuteOperation(serviceObject.Customer.VTigerUrl, "update", String.Format("sessionName={0}&element={1}",
               serviceObject.VTigerLogin.sessionName, HttpUtility.UrlEncode(element)), true);
           var retrieveResult = VTigerapiService.GetDeSerializeObject<VTigerProduct>(strResult);
           serviceObject.FeedbackToBackgroundWorker = vTigerProduct.serial_no.Equals("") ? "[productcode removed]" : "[fixed]";

           if (retrieveResult.success == true) return true;

           WriteToLog(serviceObject.Customer, string.Format(
               "VTiger: ProduktID: {0} blev ikke opdateret. [connection lost]", vTigerProduct.serial_no),
               "Problem: Ved update til VTiger returnerede result false\n" +
               "Løsning: Undersøg VTiger settings og forbindelse til server\n" +
               "Teknik: DataSync_Engine: Service: VTigerProductFixProductCode");
           serviceObject.IsConnectionLost = true;
           return false;
       }

       internal String GetTimeForVTigerSearch(DateTime value)
       {
           return value.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss");
       }

       #endregion SUBFUNKTIONER
    }
}