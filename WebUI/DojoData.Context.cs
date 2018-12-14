﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CoderDojo
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class CoderDojoData : DbContext
    {
        public CoderDojoData()
            : base("name=CoderDojoData")
        {
            // Disable initializer
            // Disables migrations so connectionstring can be set in azure portal
            Database.SetInitializer<CoderDojoData>(null);
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Adult> Adults { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<Belt> Belts { get; set; }
        public DbSet<MemberAttendance> MemberAttendances { get; set; }
        public DbSet<MemberBadge> MemberBadges { get; set; }
        public DbSet<MemberBelt> MemberBelts { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MemberParent> MemberParents { get; set; }
        public DbSet<BadgeCategory> BadgeCategories { get; set; }
        public DbSet<Dojo> Dojos { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<AdultAttendance> AdultAttendances { get; set; }
    }
}
