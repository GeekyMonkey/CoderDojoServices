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
    
    public partial class AdultAttendance
    {
        public System.Guid Id { get; set; }
        public System.DateTime Date { get; set; }
        public System.Guid AdultId { get; set; }
    
        public virtual Adult Adult { get; set; }
    }
}