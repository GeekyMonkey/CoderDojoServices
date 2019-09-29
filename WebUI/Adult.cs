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
    
    public partial class Adult
    {
        public Adult()
        {
            this.Deleted = false;
            this.MemberParents = new HashSet<MemberParent>();
            this.AdultAttendances = new HashSet<AdultAttendance>();
            this.BadgeCategories = new HashSet<AdultBadgeCategory>();
        }
    
        public System.Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsParent { get; set; }
        public bool IsMentor { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string GithubLogin { get; set; }
        public string XboxGamertag { get; set; }
        public string ScratchName { get; set; }
        public bool Deleted { get; set; }
        public bool GardaVetted { get; set; }
        public Nullable<System.DateTime> LoginDate { get; set; }
        public Nullable<System.DateTime> LoginDatePrevious { get; set; }
        public Nullable<int> FingerprintId { get; set; }
    
        public virtual ICollection<MemberParent> MemberParents { get; set; }
        public virtual ICollection<AdultAttendance> AdultAttendances { get; set; }
        public virtual ICollection<AdultBadgeCategory> BadgeCategories { get; set; }
    }
}
