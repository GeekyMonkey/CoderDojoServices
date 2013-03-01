using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CoderDojo.Views
{
    [AuthorizeMember]
    [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
    public class MemberController : BaseController
    {
        //
        // GET: /Member/
        public ActionResult Index()
        {
            Member member = GetCurrentMember();
            return View("Index", member);
        }

        [HttpGet]
        public ActionResult Profile()
        {
            Member member = GetCurrentMember();
            return View("Profile", member);
        }

        [HttpPost]
        public ActionResult ProfileSave(Member memberChanges)
        {
            Member member = GetCurrentMember();
            member.GithubLogin = memberChanges.GithubLogin;
            member.XboxGamertag = memberChanges.XboxGamertag;
            member.ScratchName = memberChanges.ScratchName;
            db.SaveChanges();
            return RedirectClient("/Member/Profile");
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
