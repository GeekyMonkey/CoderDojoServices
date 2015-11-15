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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;

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
            string userName;
            string firstName;
            string lastName;

            if (adult != null)
            {
                adult.SetLoginDate();
                userId = adult.Id;
                userName = adult.Login;
                firstName = adult.FirstName;
                lastName = adult.LastName;
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
                userName = member.Login;
                firstName = member.FirstName;
                lastName = member.LastName;
            }
            db.SaveChanges();

            HttpCookie cookie = FormsAuthentication.GetAuthCookie(role + "|" + userId.ToString(""), loginModel.Remember);
            cookie.Path = "/";
            Response.Cookies.Add(cookie);

            // create a user info cookie
            HttpCookie memberCookie = new HttpCookie("coderdojomember");
            memberCookie.Domain = ".coderdojoennis.com";
            memberCookie.Values.Add("userid", userId.ToString());
            memberCookie.Values.Add("role", role.ToString());
            memberCookie.Values.Add("username", userName);
            memberCookie.Values.Add("firstname", firstName);
            memberCookie.Values.Add("lastname", lastName);
            memberCookie.Expires = DateTime.Now.AddHours(12);
            Response.Cookies.Add(memberCookie);

            if (!string.IsNullOrEmpty(loginModel.ReturnUrl))
            {
                return View("RedirectExternal", model: loginModel.ReturnUrl);
            }
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
        public ActionResult Passport(string Id)
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Mobile);

            Guid gid = new Guid(Id);

            // Is this a member, or a team
            Member member = db.Members.FirstOrDefault(m => m.Id == gid);
            Team team = null;
            if (member == null)
            {
                team = db.Teams.FirstOrDefault(t => t.Id == gid);
            }

            ViewBag.Team = team;

            return View("Passport", member);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult BeltCertificate(string Id)
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Mobile);

            Guid gid = new Guid(Id);

            // Is this a member, or a team
            Member member = db.Members.FirstOrDefault(m => m.Id == gid);

            return View("BeltCertificate", member);
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
            context.Clients.All.OnAttendanceChange(sessionDate.ToString("yyyy-MM-dd"), member.Id.ToString("N"), member.MemberName, (member.TeamId ?? Guid.Empty).ToString("N"), true.ToString().ToLower(), sessionCount, dojoAttendanceCount, "", member.ImageUrl);
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

        [HttpPost]
        public ActionResult ImageUpload(HttpPostedFileBase file, string filename)
        {
            if (file != null)
            {
                try
                {
                    string originalFileName = System.IO.Path.GetFileName(file.FileName);

                    var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnection"].ConnectionString);
                    var blobStorage = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobStorage.GetContainerReference("coderdojomember");
                    if (container.CreateIfNotExists())
                    {
                        // configure container for public access
                        var permissions = container.GetPermissions();
                        permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                        container.SetPermissions(permissions);
                    }
                    string uniqueBlobName = filename + ".jpg";
                    var blob = container.GetBlockBlobReference(uniqueBlobName);
                    blob.Properties.ContentType = file.ContentType;
                    blob.UploadFromStream(file.InputStream);

                }
                catch (Exception ex)
                {
                    return Json(new { error = ex.Message });
                }
            }

            return Json(new { success = true });
        }
    }
}
