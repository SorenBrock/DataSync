﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataSync_Website.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class DataSyncDBEntities : DbContext
    {
        public DataSyncDBEntities()
            : base("name=DataSyncDBEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<CustomerSet> CustomerSet { get; set; }
        public virtual DbSet<LogSet> LogSet { get; set; }
        public virtual DbSet<WebSiteInboxSet> WebSiteInboxSet { get; set; }
    }
}
