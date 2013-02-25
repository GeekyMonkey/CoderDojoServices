using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoderDojo
{
    public class AttendanceModel
    {
        public Guid MemberId { get; set; }
        public string MemberName { get; set; }
        public bool Present { get; set; }
    }
}