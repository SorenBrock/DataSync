﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ModelADOContainer : DbContext
    {
        public ModelADOContainer()
            : base("name=ModelADOContainer")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Customer> CustomerSet { get; set; }
        public virtual DbSet<Log> LogSet { get; set; }
        public virtual DbSet<WebSiteInbox> WebSiteInboxSet { get; set; }
    }
}
