//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class BadgeCategory
    {
        public BadgeCategory()
        {
            this.Badges = new HashSet<Badge>();
        }
    
        public System.Guid Id { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }
        public bool Deleted { get; set; }
    
        public virtual ICollection<Badge> Badges { get; set; }
    }
}
