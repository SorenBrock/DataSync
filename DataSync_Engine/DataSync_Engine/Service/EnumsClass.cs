using System;
using System.ComponentModel;

namespace DataSync_Engine.Service
{

    public enum InvoiceType
    {
        [Description("Parat [e-conomic]")]
        Ready = 1,
        [Description("Modtaget [e-conomic]")]
        Completed = 2,
    }


    public enum FeedBackType
    {
        [Description("[none]")]
        None = 1,
        [Description("[update]")]
        Update = 2,
        [Description("[create]")]
        Create = 3,
        [Description("[sync]")]
        Syncronize = 4,
        [Description("[success]")]
        Success = 5,
        [Description("[begin]")]
        Begin = 6,
        [Description("[end]")]
        End = 7

    }

    public enum SyncErrorType
    {
        [Description("[connection lost]")]
        ConnectionLost = 1,
        [Description("[productgroup not found]")]
        ProductGroupMissing = 2,
        [Description("[productcode not found]")]
        ProductCodeError = 3,
        [Description("[id not found]")]
        ACCESS_DENIED = 4,
        [Description("[login failed]")]
        LoginDenied = 5,
        [Description("[product not found]")]
        ProductMissing = 6,
        [Description("[customer not found]")]
        CustomerMissing =7,


    }

    public static class AttributesHelperExtension
    {
        public static string EnumValue(this Enum value)
        {
            var descriptionAttribute = (DescriptionAttribute[])(value.GetType().GetField(value.ToString())).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return descriptionAttribute.Length > 0 ? descriptionAttribute[0].Description : value.ToString();
        }
    }
}