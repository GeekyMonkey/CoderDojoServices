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
    
    public partial class Member
    {
        public Member()
        {
            this.MemberAttendances = new HashSet<MemberAttendance>();
            this.MemberBadges = new HashSet<MemberBadge>();
            this.MemberBelts = new HashSet<MemberBelt>();
            this.MemberParents = new HashSet<MemberParent>();
        }
    
        public System.Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<int> BirthYear { get; set; }
        public string ScratchName { get; set; }
        public string XboxGamertag { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string GithubLogin { get; set; }
    
        public virtual ICollection<MemberAttendance> MemberAttendances { get; set; }
        public virtual ICollection<MemberBadge> MemberBadges { get; set; }
        public virtual ICollection<MemberBelt> MemberBelts { get; set; }
        public virtual ICollection<MemberParent> MemberParents { get; set; }
    }
}
