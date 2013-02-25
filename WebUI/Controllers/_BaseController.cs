using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CoderDojo
{
    public class BaseController : Controller
    {
        protected CoderDojoData db = new CoderDojoData();

        public Member GetCurrentMember()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                Guid currentUserId = HttpContext.User.Identity.GetUserId();
                return db.Members.FirstOrDefault(m => m.Id == currentUserId);
            }
            else
            {
                return null;
            }
        }

        public Adult GetCurrentAdult()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                Guid currentUserId = HttpContext.User.Identity.GetUserId();
                return db.Adults.FirstOrDefault(m => m.Id == currentUserId);
            }
            else
            {
                return null;
            }
        }

        public Guid CurrentUserId
        {
            get
            {
                Guid currentUserId = HttpContext.User.Identity.GetUserId();
                return currentUserId;
            }
        }
    }
}