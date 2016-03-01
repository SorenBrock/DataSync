using System;

namespace DataSync_Engine.Service.VTiger
{

    #region VTIGER API Class

    public class VTigerResult<T>
    {
        public bool success { get; set; }
        public VTigerError error { get; set; }
        public T result { get; set; }
    }

    public class VTigerError
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    public class VTigerLogin
    {
        public string sessionName { get; set; }
        public string userId { get; set; }
        public string version { get; set; }
        public string vtigerVersion { get; set; }
        public int serverTimeStamp { get; set; }
    }

    public class VTigerToken
    {
        public string token { get; set; }
        public int serverTime { get; set; }
        public int expireTime { get; set; }
    }

    #endregion VTIGER API Class

    #region VTIGER CLASS

    public class VTigerAccount
    {
        public VTigerAccount() { }

        public string id { get; set; }
        public string account_id { get; set; }
        public string accountname { get; set; } //Obligatorisk
        public string account_no { get; set; }
        public string phone { get; set; }
        public string website { get; set; }
        public string fax { get; set; }
        public string tickersymbol { get; set; } // EconomicNumber / sync
        public string email2 { get; set; }
        public string otherphone { get; set; }
        public string email1 { get; set; }
        public string employees { get; set; }
        public string ownership { get; set; }
        public string rating { get; set; }
        public string industry { get; set; }
        public string siccode { get; set; }
        public string accounttype { get; set; }
        public string annual_revenue { get; set; }
        public string emailoptout { get; set; }
        public string notify_owner { get; set; }
        public string assigned_user_id { get; set; } //Obligatorisk
        public string createdtime { get; set; }
        public string modifiedtime { get; set; }
        public string bill_street { get; set; }
        public string ship_street { get; set; }
        public string bill_city { get; set; }
        public string ship_city { get; set; }
        public string bill_state { get; set; }
        public string ship_state { get; set; }
        public string bill_code { get; set; }
        public string ship_code { get; set; }
        public string bill_country { get; set; }
        public string ship_country { get; set; }
        public string bill_pobox { get; set; }
        public string ship_pobox { get; set; }
        public string description { get; set; }
    }

    public class VTigerProduct
    {
        public VTigerProduct() { }
        public string id { get; set; }
        public string productname { get; set; } //Obligatorisk
        public string product_no { get; set; }
        public string productcode { get; set; }
        public string discontinued { get; set; } //Bool
        public string productcategory { get; set; } //
        public string serial_no { get; set; } // DETTE NUMMER BRUDES TIL AT SYNCE TIL Economic
        public string createdtime { get; set; } // DateTime
        public string modifiedtime { get; set; } // DateTime
        public string unit_price { get; set; } // double
        public string usageunit { get; set; }
        public string description { get; set; }
        public string assigned_user_id { get; set; } // Obligatorisk
        public string manufacturer { get; set; }
        public string qty_per_unit { get; set; } // double
        public string sales_start_date { get; set; }
        public string sales_end_date { get; set; }
        public string start_date { get; set; }
        public string expiry_date { get; set; }
        public string website { get; set; }
        public string vendor_id { get; set; }
        public string mfr_part_no { get; set; }
        public string vendor_part_no { get; set; }
        public string productsheet { get; set; }
        public string glacct { get; set; }
        public string commissionrate { get; set; } // double
        public string taxclass { get; set; }
        public string qtyinstock { get; set; } // double
        public string reorderlevel { get; set; } // int
        public string qtyindemand { get; set; } // int
    }

    public class VTigerQuote
    {
        public string id { get; set; }
        public string quote_no { get; set; }
        public string subject { get; set; } //Obligatorisk
        public string potential_id { get; set; }
        public string quotestage { get; set; } //Obligatorisk // Quotestage
        public string validtill { get; set; }
        public string contact_id { get; set; }
        public string carrier { get; set; }
        public string hdnSubTotal { get; set; }     // double
        public string shipping { get; set; }
        public string assigned_user_id1 { get; set; }
        public string txtAdjustment { get; set; }// double
        public string hdnGrandTotal { get; set; }// double
        public string hdnTaxType { get; set; }
        public string hdnDiscountPercent { get; set; }// double
        public string hdnDiscountAmount { get; set; }// double
        public string hdnS_H_Amount { get; set; }// double
        public string account_id { get; set; } //Obligatorisk
        public string assigned_user_id { get; set; } //Obligatorisk
        public string createdtime { get; set; }
        public string modifiedtime { get; set; }
        public string modifiedby { get; set; }
        public string productid { get; set; }
        public string listprice { get; set; }
        public string quantity { get; set; }
        public string lineitems { get; set; }
        public string pdoInformation { get; set; }
        public string comment { get; set; }
        public string tax1 { get; set; }
        public string tax2 { get; set; }
        public string tax3 { get; set; }
        public string currency_id { get; set; }
        public string conversion_rate { get; set; }// double
        public string bill_street { get; set; } //Obligatorisk
        public string ship_street { get; set; } //Obligatorisk
        public string bill_city { get; set; }
        public string ship_city { get; set; }
        public string bill_state { get; set; }
        public string ship_state { get; set; }
        public string bill_code { get; set; }
        public string ship_code { get; set; }
        public string bill_country { get; set; }
        public string ship_country { get; set; }
        public string bill_pobox { get; set; }
        public string ship_pobox { get; set; }
        public string description { get; set; }
        public string terms_conditions { get; set; }
    }

    #endregion VTIGER CLASS

}