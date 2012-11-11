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
            var quinn = db.Members.First(m => m.FirstName == "Quinn");
            quinn.PasswordHash = db.GeneratePasswordHash("robot");
            var molly = db.Members.First(m => m.FirstName == "Molly");
            molly.PasswordHash = db.GeneratePasswordHash("princess");
            var brian = db.Adults.First(a => a.FirstName == "Brian");
            brian.PasswordHash = db.GeneratePasswordHash("princess");
            db.SaveChanges();

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
