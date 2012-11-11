using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;

namespace CoderDojo.Controllers
{
    public class HomeController : Controller
    {
        CoderDojoData db = new CoderDojoData();

        public ActionResult Index()
        {
            return Login();
        }

        [HttpGet]
        public ActionResult Login()
        {
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
            return View("Login");
        }

        [HttpPost]
        public ActionResult Login(string UsernameInput = null, string PasswordInput = null)
        {
            string passwordHash = db.GeneratePasswordHash(PasswordInput);
            var adult = db.Adults.FirstOrDefault(a => a.Login == UsernameInput && a.PasswordHash == passwordHash);
            var member = db.Members.FirstOrDefault(m => m.Login == UsernameInput && m.PasswordHash == passwordHash);

            if (adult == null && member == null)
            {
                ViewBag.ValidationMessage = "Username or password is not correct.";
                return View("Login");
            }

            if (adult != null)
            {
                if (adult.IsMentor)
                {
                    return RedirectToAction("Index", "Mentor");
                }
                else
                {
                    return RedirectToAction("Index", "Parent");
                }
            }
            else
            {
                return RedirectToAction("Index", "Member");
            }
        }
    }
}
