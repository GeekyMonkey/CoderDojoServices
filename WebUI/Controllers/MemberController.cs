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
            var belts = from belt in db.Belts
                        where belt.Deleted == false
                        orderby belt.SortOrder
                        select belt;
            return View("Belts", belts.ToList());
        }

        [HttpGet]
        public ActionResult Attendance()
        {
            Member member = GetCurrentMember();
            return View("Attendance", member);
        }

        [HttpPost]
        public ActionResult BeltApplication(Guid id, string message)
        {
            Guid beltId = id;
            MemberBelt application = new MemberBelt
            {
                MemberId = GetCurrentMember().Id,
                BeltId = beltId,
                ApplicationDate = DateTime.UtcNow,
                ApplicationNotes = message
            };
            db.MemberBelts.Add(application);
            db.SaveChanges();
            return Json("OK");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentMember = this.GetCurrentMember();
            base.OnActionExecuting(filterContext);
        }
    }
}
