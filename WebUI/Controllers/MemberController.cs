using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CoderDojo.Views
{
    [AuthorizeMember]
    public class MemberController : BaseController
    {
        //
        // GET: /Member/
        public ActionResult Index()
        {
            Member member = GetCurrentMember();
            return View("Profile", member);
        }

        [HttpGet]
        public ActionResult Profile()
        {
            Member member = GetCurrentMember();
            return View("Profile", member);
        }

        [HttpGet]
        public ActionResult Badges()
        {
            Member member = GetCurrentMember();
            return View("Badges", member);
        }

        [HttpGet]
        public ActionResult Belts()
        {
            Member member = GetCurrentMember();
            return View("Belts", member);
        }

        [HttpGet]
        public ActionResult Attendance()
        {
            Member member = GetCurrentMember();
            return View("Attendance", member);
        }
    }
}
