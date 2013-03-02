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
    
    public partial class MemberBelt
    {
        public System.Guid Id { get; set; }
        public System.Guid MemberId { get; set; }
        public System.Guid BeltId { get; set; }
        public Nullable<System.DateTime> Awarded { get; set; }
        public Nullable<System.Guid> AwardedByAdultId { get; set; }
        public string AwardedNotes { get; set; }
        public Nullable<System.DateTime> ApplicationDate { get; set; }
        public string ApplicationNotes { get; set; }
        public Nullable<System.DateTime> RejectedDate { get; set; }
        public Nullable<System.Guid> RejectedByAdultId { get; set; }
        public string RejectedNotes { get; set; }
    
        public virtual Adult Adult { get; set; }
        public virtual Belt Belt { get; set; }
        public virtual Member Member { get; set; }
    }
}
