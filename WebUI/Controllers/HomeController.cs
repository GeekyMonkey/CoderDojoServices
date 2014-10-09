using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Web.Security;
using System.Web.WebPages;
using Microsoft.AspNet.SignalR;

namespace CoderDojo.Controllers
{
    [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
    public class HomeController : BaseController
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            var member = GetCurrentMember();
            if (member != null)
            {
                return RedirectToAction(actionName: "Index", controllerName: "Member");
            }
            var adult = GetCurrentAdult();
            if (adult != null)
            {
                if (adult.IsMentor)
                {
                    return RedirectToAction(actionName: "Index", controllerName: "Mentor");
                }
                return RedirectToAction(actionName: "Index", controllerName: "Parent");
            }
            return Login();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Mobile);
            LoginModel loginModel = new LoginModel();
            return View("Login", loginModel);
        }

        [HttpPost]
        [AllowAnonymous]
        // [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel loginModel)
        {
            /* Emergency password reset
            var me = db.Adults.FirstOrDefault(a => a.Login == "--loginname--");
            me.PasswordHash = db.GeneratePasswordHash("--newpassword--");
            db.SaveChanges();
            */

            Adult adult = null;
            Member member = null;
            if (!string.IsNullOrEmpty(loginModel.Password))
            {
                string passwordHash = db.GeneratePasswordHash(loginModel.Password);
                adult = db.Adults.FirstOrDefault(
                    a => ( a.Login == loginModel.Username || ((a.FirstName + " " + a.LastName) == loginModel.Username) )
                        && a.PasswordHash == passwordHash
                        && a.Deleted == false);
                member = db.Members.FirstOrDefault(
                        m => ( m.Login == loginModel.Username || ((m.FirstName + " " + m.LastName) == loginModel.Username) )
                        && m.PasswordHash == passwordHash
                        && m.Deleted == false);
            }

            if (adult == null && member == null)
            {
                ViewBag.ValidationMessage = "Username or password is not correct.";
                return View("Login");
            }

            Guid userId;
            UserRoles role;

            if (adult != null)
            {
                adult.SetLoginDate();
                userId = adult.Id;
                if (adult.IsMentor)
                {
                    role = UserRoles.Mentor;
                }
                else
                {
                    role = UserRoles.Parent;
                }
            }
            else
            {
                member.SetLoginDate();
                userId = member.Id;
                role = UserRoles.Member;
            }
            db.SaveChanges();

            HttpCookie cookie = FormsAuthentication.GetAuthCookie(role + "|" + userId.ToString(""), loginModel.Remember);
            cookie.Path = "/";
            Response.Cookies.Add(cookie);

            return View("Redirect", model: "/" + role.ToString() + "/Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult LogOut()
        {
            FormsAuthentication.SetAuthCookie(null, false);
            FormsAuthentication.SignOut();
            return View("Redirect", model: "/Home/Login");
        }

        [HttpGet]
        [AllowAnonymous]
        [SignInModeFilter]
        public ActionResult SignIn()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Mobile);
            DateTime sessionDate = DateTime.Today;
            ViewBag.SignedInMembers = GetSignedInMembers();
            ViewBag.SessionDate = sessionDate;
            ViewBag.Teams = db.Teams.OrderBy(t => t.TeamName).ToList();
            return View("SignIn", new LoginModel());
        }

        [HttpGet]
        [AllowAnonymous]
        [SignInModeFilter]
        public ActionResult SignInQR()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Mobile);
            DateTime sessionDate = DateTime.Today;
            ViewBag.SignedInMembers = GetSignedInMembers();
            ViewBag.SessionDate = sessionDate;
            ViewBag.Teams = db.Teams.OrderBy(t => t.TeamName).ToList();
            return View("SignInQR", new LoginModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult SignInQR(string Id)
        {
            Member member = null;
            try
            {
                Guid gid = new Guid(Id);
                member = db.Members.FirstOrDefault(m => m.Id == gid);
            }
            catch
            {
                member = null;
            }

            if (member == null)
            {
                return Json(new
                {
                    ValidationMessage = "Unknown QR Code: " + Id
                });
            }

            return SignInMember(member);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult SignIn(LoginModel loginModel)
        {
            Member member = null;
            if (!string.IsNullOrEmpty(loginModel.Password))
            {
                string passwordHash = db.GeneratePasswordHash(loginModel.Password);
                member = db.Members.FirstOrDefault(
                    m => ( m.Login == loginModel.Username || ((m.FirstName + " " + m.LastName) == loginModel.Username) )
                        && m.PasswordHash == passwordHash
                        && m.Deleted == false);
            }

            if (member == null)
            {
                return Json(new
                {
                    ValidationMessage = "Username or password is not correct."
                });
            }

            return SignInMember(member);
        }

        private ActionResult SignInMember(Member member)
        {
            member.SetLoginDate();
            db.SaveChanges();

            DateTime sessionDate = DateTime.Today;
            int sessionCount = db.AttendanceSet(member.Id, true, sessionDate);
            int dojoAttendanceCount = db.MemberAttendances.Count(ma => ma.Date == sessionDate);
            // Notify other members looking at this screen
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<AttendanceHub>();
            context.Clients.All.OnAttendanceChange(sessionDate.ToString("yyyy-MM-dd"), member.Id.ToString("N"), member.MemberName, (member.TeamId ?? Guid.Empty).ToString("N"), true.ToString().ToLower(), sessionCount, dojoAttendanceCount, "");
            string message = member.GetLoginMessage();

            return Json(new {
                memberId = member.Id.ToString("N"),
                memberName = member.MemberName,
                memberSessionCount = sessionCount,
                memberMessage = message
            });
        }

        private IEnumerable<MemberAttendance> GetSignedInMembers()
        {
            return from ma in db.MemberAttendances.Include("Member")
                                      where ma.Date == DateTime.Today
                                      orderby ma.Member.FirstName, ma.Member.LastName
                                      select ma;
        }
    }
}
