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
    
    public partial class Belt
    {
        public Belt()
        {
            this.MemberBelts = new HashSet<MemberBelt>();
        }
    
        public System.Guid Id { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
    
        public virtual ICollection<MemberBelt> MemberBelts { get; set; }
    }
}