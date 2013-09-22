using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
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
                return db.Members.Include(m => m.Team).FirstOrDefault(m => m.Id == currentUserId);
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

        /// <summary>
        /// Before action
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Set defaults
            ViewBag.HideMenuButton = false;

            base.OnActionExecuting(filterContext);
        }

        public ActionResult RedirectClient(string url)
        {
            return View("Redirect", model: url);
        }

        public string TrimNullableString(string val)
        {
            if (val != null)
            {
                val = val.Trim();
            }
            return val;
        }
    }
}