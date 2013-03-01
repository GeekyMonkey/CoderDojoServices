using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CoderDojo.Views
{
    [AuthorizeParent]
    [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
    public class ParentController : BaseController
    {
        //
        // GET: /Parent/
        public ActionResult Index()
        {
            Adult parent = GetCurrentAdult();
            return View("index", parent);
        }

        [HttpGet]
        public ActionResult MyKids(Guid? id = null)
        {
            List<Member> members = (from m in db.Members
                                    from mp in db.MemberParents
                                    where m.Id == mp.MemberId
                                    && m.Deleted == false
                                    && mp.AdultId == CurrentUserId
                                    orderby m.FirstName, m.LastName
                                    select m).ToList();
            ViewBag.SelectedMemberId = id; //todo - scroll here
            ViewBag.ShowBackButton = true;
            return View("MyKids", members);
        }

        [HttpGet]
        public ActionResult MyKid(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            if (member.MemberParents.Any(mp => mp.AdultId == CurrentUserId) == false)
            {
                throw new Exception("Attempt to view child record not associated with parent");
            }
            ViewBag.ShowBackButton = true;
            return View("MyKid", member);
        }

        [HttpGet]
        public ActionResult MyAccount()
        {
            Adult parent = GetCurrentAdult();
            return View("MyAccount", parent);
        }

        [HttpPost]
        public ActionResult MyAccountSave(Adult adultChanges)
        {
            Adult adult = db.Adults.Find(adultChanges.Id);
            if (adult == null)
            {
                throw new Exception("Account not found");
            }
            if (string.IsNullOrEmpty(adultChanges.NewPassword) == false)
            {
                adult.PasswordHash = db.GeneratePasswordHash(adultChanges.NewPassword);
            }
            adult.FirstName = adultChanges.FirstName;
            adult.LastName = adultChanges.LastName;
            adult.Email = adultChanges.Email;
            adult.Phone = adultChanges.Phone;
            db.SaveChanges();

            return RedirectClient("/Parent/MyAccount");
        }
    }
}
