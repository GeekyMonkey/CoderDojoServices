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
    
    public partial class Session
    {
        public System.Guid Id { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
        public string Url { get; set; }
        public string Topic { get; set; }
        public Nullable<System.Guid> AdultId { get; set; }
        public Nullable<System.Guid> Adult2Id { get; set; }
        public bool MentorsOnly { get; set; }
    
        public virtual Adult Adult { get; set; }
        public virtual Adult Adult2 { get; set; }
    }
}
