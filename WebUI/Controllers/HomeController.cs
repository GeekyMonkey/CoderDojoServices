using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Web.Security;
using System.Web.WebPages;

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
    /* todo - delete this
            Member quinn = new Member
            {
                FirstName = "Quinn",
                LastName = "Painter",
                BirthYear = 2002,
                ScratchName = "quinnpainter",
                XboxGamertag = "quinnbot1",
                Login = "QuinnP",
                GithubLogin = "QuinnPainter",
                PasswordHash = db.GeneratePasswordHash("robot")
            };
            db.Members.Add(quinn);

            Member molly = new Member
{
    FirstName = "Molly",
    LastName = "Painter",
    BirthYear = 2005,
    ScratchName = "mallaidh",
    XboxGamertag = "mallaidh",
    Login = "MollyP",
    PasswordHash = db.GeneratePasswordHash("princess")
};
            db.Members.Add(molly);

            Adult russ = new Adult
            {
                FirstName="Russ", LastName="Painter", IsMentor=true, IsParent=true, Phone="0879969992", PasswordHash=db.GeneratePasswordHash("beer"), GithubLogin="geekymonkey", Email="russ@geekymonkey.com", Login="RussPainter", XboxGamertag="GeekyMonkey8"
            };
            db.Adults.Add(russ);

            Adult brian = new Adult
            {
                FirstName="Brian", LastName="Moore", IsParent=true, Login="BrianMoore", PasswordHash=db.GeneratePasswordHash("princess")
            };

            db.SaveChanges();
            */
            LoginModel loginModel = new LoginModel();
            return View("Login", loginModel);
        }

        [HttpPost]
        [AllowAnonymous]
        // [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel loginModel)
        {
            /* Emergency password reset
            var me = db.Adults.FirstOrDefault(a => a.Login == "RussPainter");
            me.PasswordHash = db.GeneratePasswordHash("--newpassword--");
            db.SaveChanges();
            */

            Adult adult = null;
            Member member = null;
            if (!string.IsNullOrEmpty(loginModel.Password))
            {
                string passwordHash = db.GeneratePasswordHash(loginModel.Password);
                adult = db.Adults.FirstOrDefault(a => a.Login == loginModel.Username && a.PasswordHash == passwordHash && a.Deleted == false);
                member = db.Members.FirstOrDefault(m => m.Login == loginModel.Username && m.PasswordHash == passwordHash && m.Deleted == false);
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
                userId = member.Id;
                role = UserRoles.Member;
            }

            HttpCookie cookie = FormsAuthentication.GetAuthCookie(role + "|" + userId.ToString(""), loginModel.Remember);
            cookie.Path = "/";
            Response.Cookies.Add(cookie);

            return View("Redirect", model: "/" + role.ToString() + "/Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return View("Redirect", model: "/Home/Login");
        }
    }
}
