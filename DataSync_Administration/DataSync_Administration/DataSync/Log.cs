//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataSync
{
    using System;
    using System.Collections.Generic;
    
    public partial class Log
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Info { get; set; }
        public Nullable<bool> IsError { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public int CustomerId { get; set; }
    
        public virtual Customer Customer { get; set; }
    }
}
