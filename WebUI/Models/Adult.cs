using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoderDojo
{
    public partial class Adult : BaseEntity
    {
        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        public string NewPassword
        {
            get;
            set;
        }

    }
}