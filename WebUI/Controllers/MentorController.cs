using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CoderDojo.Views
{
    [AuthorizeMentor]
    [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
    public class MentorController : BaseController
    {
        //
        // GET: /Mentor/
        public ActionResult Index()
        {
            Adult mentor = GetCurrentAdult();
            return View("Index", mentor);
        }

        /// <summary>
        /// Attendance View
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Attendance(Guid? memberId = null)
        {
            DateTime sessionDate = DateTime.Today;
            var presentMemberIds = db.MemberAttendances.Where(a => a.Date == sessionDate).OrderBy(a => a.MemberId).Select(a => a.MemberId).ToList();
            List<AttendanceModel> attendance = (from m in db.Members
                                                where m.Deleted == false
                                                orderby m.FirstName, m.LastName
                                                select new AttendanceModel
                                                {
                                                    MemberId = m.Id,
                                                    MemberName = m.FirstName + " " + m.LastName,
                                                    Present = presentMemberIds.Contains(m.Id)
                                                }).ToList();
            ViewBag.ShowBackButton = true;
            ViewBag.SelectedMemberId = memberId; //todo - scroll here
            return View(attendance);
        }

        [HttpPost]
        public ActionResult AttendanceChange(string memberId, bool present)
        {
            Guid membergId = new Guid(memberId);
            AttendanceSet(membergId, present);
            return Json("OK");
        }

        /// <summary>
        /// Save attendance change to database
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="present"></param>
        private void AttendanceSet(Guid memberId, bool present)
        {
            MemberAttendance attendance = db.MemberAttendances.Where(ma => ma.MemberId == memberId && ma.Date == DateTime.Today).FirstOrDefault();
            bool hasAttendance = (attendance != null);
            if (present == true && hasAttendance == false)
            {
                attendance = new MemberAttendance
                {
                    Id = Guid.NewGuid(),
                    MemberId = memberId,
                    Date = DateTime.Today
                };
                db.MemberAttendances.Add(attendance);
                db.SaveChanges();
            }
            else if (present == false && hasAttendance == true)
            {
                db.MemberAttendances.Remove(attendance);
                db.SaveChanges();
            }
        }

        [HttpGet]
        public ActionResult Adults(Guid? id = null)
        {
            List<Adult> adults = (from m in db.Adults
                                  where m.Deleted == false
                                  orderby m.FirstName, m.LastName
                                  select m).ToList();
            ViewBag.SelectedMemberId = id; //todo - scroll here
            ViewBag.ShowBackButton = true;
            ViewBag.Title = "Parents & Mentors";
            ViewBag.AdultMode = "Adult";
            return View("Adults", adults);
        }


        [HttpGet]
        public ActionResult Parents(Guid? id = null)
        {
            List<Adult> adults = (from m in db.Adults
                                  where m.Deleted == false
                                  && m.IsParent == true
                                  orderby m.FirstName, m.LastName
                                  select m).ToList();
            ViewBag.SelectedMemberId = id; //todo - scroll here
            ViewBag.ShowBackButton = true;
            ViewBag.Title = "Parents";
            ViewBag.AdultMode = "Parent";
            return View("Adults", adults);
        }

        [HttpGet]
        public ActionResult Mentors(Guid? id = null)
        {
            List<Adult> adults = (from m in db.Adults
                                  where m.Deleted == false
                                  && m.IsMentor == true
                                  orderby m.FirstName, m.LastName
                                  select m).ToList();
            ViewBag.SelectedMemberId = id; //todo - scroll here
            ViewBag.ShowBackButton = true;
            ViewBag.Title = "Mentors";
            ViewBag.AdultMode = "Mentor";
            return View("Adults", adults);
        }

        [HttpGet]
        public ActionResult Adult(Guid id)
        {
            Adult adult = db.Adults.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            ViewBag.AdultMode = adult.IsMentor ? "Mentor" : "Parent";
            return View("Adult", adult);
        }

        [HttpGet]
        public ActionResult ParentKids(Guid id)
        {
            Adult adult = db.Adults.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            ViewBag.AdultMode = "Parent";
            return View("ParentKids", adult);
        }

        [HttpGet]
        public ActionResult AdultSignup(string adultMode)
        {
            // Set defaults
            Adult newAdult = new Adult();
            if (adultMode == "Mentor")
            {
                newAdult.IsMentor = true;
            }
            else
            {
                newAdult.IsParent = true;
            }
            ViewBag.AdultMode = adultMode;
            return View("Adult", newAdult);
        }

        [HttpPost]
        public ActionResult MentorSave(Adult adultChanges)
        {
            return AdultSave(adultChanges, "Mentor");
        }

        [HttpPost]
        public ActionResult ParentSave(Adult adultChanges)
        {
            return AdultSave(adultChanges, "Parent");
        }

        [HttpPost]
        public ActionResult AdultSave(Adult adultChanges)
        {
            return AdultSave(adultChanges, "Adult");
        }

        private ActionResult AdultSave(Adult adultChanges, string adultMode)
        {
            if (ModelState.IsValid)
            {
                Adult adult = null;

                if (adultChanges.Id != null)
                {
                    adult = db.Adults.Find(adultChanges.Id);
                }

                // New Adult
                if (adult == null)
                {
                    adult = new Adult();
                    adult.Id = Guid.NewGuid();
                    db.Adults.Add(adult);
                }

                // Save changes
                adult.FirstName = adultChanges.FirstName;
                adult.LastName = adultChanges.LastName;
                adult.Email = adultChanges.Email;
                adult.Phone = adultChanges.Phone;
                adult.IsParent = adultChanges.IsParent;
                adult.IsMentor = adultChanges.IsMentor;
                adult.GithubLogin = adultChanges.GithubLogin;
                adult.XboxGamertag = adultChanges.XboxGamertag;
                adult.ScratchName = adultChanges.ScratchName;
                adult.Login = adultChanges.Login;

                // Password change
                if (string.IsNullOrEmpty(adultChanges.NewPassword) == false)
                {
                    adult.PasswordHash = db.GeneratePasswordHash(adultChanges.NewPassword);
                }
                db.SaveChanges();
            }
            return RedirectClient("/Mentor/" + adultMode + "s");
        }

        [HttpGet]
        public ActionResult MemberSignup(string Mode = "")
        {
            MemberSignupModel memberSignup = new MemberSignupModel();
            memberSignup.Mode = Mode;
            return View("MemberSignup", memberSignup);
        }

        [HttpPost]
        public ActionResult MemberSignupSave(MemberSignupModel memberSignup)
        {
            //MemberSignupModel memberSignup = new MemberSignupModel();
            //UpdateModel(memberSignup);

            Member newMember = new Member
            {
                FirstName = memberSignup.FirstName,
                LastName = memberSignup.LastName,
                BirthYear = memberSignup.BirthYear
            };
            db.Members.Add(newMember);
            db.SaveChanges();

            if (memberSignup.Mode == "Attendance")
            {
                AttendanceSet(newMember.Id, true);
                return RedirectClient("/Mentor/Attendance?id=" + newMember.Id);
            }
            return RedirectClient("/Mentor/Members?id=" + newMember.Id);
        }

        [HttpGet]
        public ActionResult Members(Guid? id = null)
        {
            List<Member> members = (from m in db.Members
                                    where m.Deleted == false
                                    orderby m.FirstName, m.LastName
                                    select m).ToList();
            ViewBag.SelectedMemberId = id; //todo - scroll here
            ViewBag.ShowBackButton = true;
            return View("Members", members);
        }

        [HttpGet]
        public ActionResult Member(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            return View("Member", member);
        }

        [HttpPost]
        public ActionResult MemberSave(Member memberChanges)
        {
            Member member = null;

            if (ModelState.IsValid)
            {
                if (memberChanges.Id != null)
                {
                    member = db.Members.Find(memberChanges.Id);
                }

                // New Adult
                if (member == null)
                {
                    member = new Member();
                    member.Id = Guid.NewGuid();
                    db.Members.Add(member);
                }

                // Save changes
                member.FirstName = memberChanges.FirstName;
                member.LastName = memberChanges.LastName;
                member.BirthYear = memberChanges.BirthYear;
                member.Login = memberChanges.Login;
                member.GithubLogin = memberChanges.GithubLogin;
                member.ScratchName = memberChanges.ScratchName;
                member.XboxGamertag = memberChanges.XboxGamertag;

                // Password change
                if (string.IsNullOrEmpty(memberChanges.NewPassword) == false)
                {
                    member.PasswordHash = db.GeneratePasswordHash(memberChanges.NewPassword);
                }
                db.SaveChanges();
                return RedirectClient("/Mentor/Member?id=" + member.Id);
            }
            return Json("Validation error"); // todo
        }

        [HttpGet]
        public ActionResult MemberParents(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            return View("MemberParents", member);
        }
        [HttpGet]
        public ActionResult MemberAttendance(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            return View("MemberAttendance", member);
        }
        [HttpGet]
        public ActionResult MemberBadges(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            return View("MemberBadges", member);
        }
        [HttpGet]
        public ActionResult MemberBelts(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            return View("MemberBelts", member);
        }

        private void AddMember(string firstName, string lastName, string scratch, int birthYear)
        {
            Member m = new Member
            {
                FirstName = firstName,
                LastName = lastName,
                ScratchName = scratch,
                BirthYear = birthYear
            };
            db.Members.Add(m);
            db.SaveChanges();
        }

        /*
        [HttpGet]
        public ActionResult ImportData()
        {
            AddMember("Katie", "Arthur", "", 2004);
            AddMember("Emmanuel", "Atijohn", "", 2000);
            AddMember("Peter", "Bailey", "", 1998);
            AddMember("John", "Bailey", "", 2006);
            AddMember("Amy", "Bailey", "", 2001);
            AddMember("Tiernan", "Boyce", "darkvoid", 1999);
            AddMember("Jack", "Brennan", "", 2000);
            AddMember("Rory", "Brennan", "", 1999);
            AddMember("Ephram", "Brotherton", "", 2005);
            AddMember("Garry", "Cahill", "", 1996);
            AddMember("Padraig", "Cahill", "", 2001);
            AddMember("William", "Cahir Whelan", "", 2004);
            AddMember("Andrew", "Cahir Whelan", "", 2006);
            AddMember("Kane", "Cantrell", "", 2001);
            AddMember("Slaine", "Carey", "", 1996);
            AddMember("Aobha", "Carey", "", 2000);
            AddMember("Oran", "Carey", "", 2003);
            AddMember("Catherine", "Casey", "", 1998);
            AddMember("Andrew", "Casey", "", 2002);
            AddMember("Colm", "Cleary", "", 2003);
            AddMember("Kieran", "Clohessy", "", 2000);
            AddMember("Jack", "Conlon", "", 2002);
            AddMember("Patrick", "Conlon", "", 2000);
            AddMember("Joseph", "Connaughton", "", 2000);
            AddMember("Oisin", "Connole", "", 2001);
            AddMember("Jack", "Corry", "", 1999);
            AddMember("Niall", "Corry", "", 2001);
            AddMember("Ben", "Cosgrove", "cnebdragon", 2001);
            AddMember("Michael", "Costello", "", 2003);
            AddMember("Thomas", "Costello", "", 2001);
            AddMember("Ally", "Crimmins", "", 2001);
            AddMember("Oisin", "Crimmins", "", 2003);
            AddMember("Vincent", "Crowley", "", 2006);
            AddMember("Dylan", "Cummins", "", 1999);
            AddMember("James", "Doherty", "", 2004);
            AddMember("Ella", "Doherty", "", 2002);
            AddMember("Noah", "Doherty", "", 2004);
            AddMember("Sean", "Donnelly", "Seandon", 1999);
            AddMember("Diarmuid", "Edwards", "", 1996);
            AddMember("Liam", "Fahy", "", 1999);
            AddMember("Gillian", "Fahy", "", 2000);
            AddMember("Roisin", "fahy", "", 2001);
            AddMember("Ronan", "Fallon", "", 1996);
            AddMember("Barry", "Fitzgerald", "", 1998);
            AddMember("Mark", "Fitzgerald", "", 1999);
            AddMember("Abbie", "Fitzgerald", "", 2003);
            AddMember("Diarmuid", "Fitzgerald", "", 2001);
            AddMember("Colum", "Francis O Dalaigh", "", 2002);
            AddMember("Conor", "Francis O Dalaigh", "", 2000);
            AddMember("Joannah", "Francis O Dalaigh", "", 2003);
            AddMember("Oliver", "Gavin", "", 1995);
            AddMember("Carla", "Griffin", "", 2003);
            AddMember("Alma", "Griffin", "", 2005);
            AddMember("Kevin", "Hayes", "", 1999);
            AddMember("Sean", "Hehir", "", 2001);
            AddMember("Ciaran", "Hickey", "", 1998);
            AddMember("Alana", "Higgins", "", 2003);
            AddMember("Caleb", "Higgins", "", 2000);
            AddMember("Joseph", "Kelly", "", 1996);
            AddMember("Dan", "Kennedy", "", 2002);
            AddMember("Jack", "Kirrane", "", 1999);
            AddMember("Luke", "Kirrane", "", 2001);
            AddMember("Cian", "Lahiffe", "", 2003);
            AddMember("Caoimhe", "Lally", "", 2003);
            AddMember("Oisin", "Lally", "", 1998);
            AddMember("Aoibhinn", "Leyden", "", 2003);
            AddMember("Ruth", "Leyden", "", 2005);
            AddMember("Abdelqahman", "Liani", "", 2005);
            AddMember("Jamila", "Liani", "", 2003);
            AddMember("Sara", "Liani", "", 2004);
            AddMember("Darragh", "Liddy", "", 2000);
            AddMember("Seadhna", "Liddy", "", 2003);
            AddMember("Eliza", "Liddy", "", 2001);
            AddMember("Reuben", "Liddy", "", 2004);
            AddMember("Cormac", "Lynam", "", 2005);
            AddMember("Jack", "Lynch", "", 2004);
            AddMember("Daniel", "Maguire", "", 2000);
            AddMember("Cormac", "Maher", "", 2005);
            AddMember("Mark", "Mc Govern", "", 1995);
            AddMember("Jack", "Mc Morrough", "", 2000);
            AddMember("John", "McArdle", "", 2004);
            AddMember("Brian", "McArdle", "", 2006);
            AddMember("Roisin", "McAteer", "", 1999);
            AddMember("Caomhe", "McAteer", "", 2002);
            AddMember("Conor", "McCarthy", "", 2002);
            AddMember("Sean", "McCarthy", "", 2003);
            AddMember("Ben", "McNamara", "", 1996);
            AddMember("Luke", "Meade", "", 2002);
            AddMember("Aaron", "Meade", "", 2000);
            AddMember("Christopher", "Meaney", "", 1997);
            AddMember("Aidan", "Meaney", "", 2003);
            AddMember("Cian", "Meere", "", 2003);
            AddMember("Sean", "Meere", "", 2000);
            AddMember("Ciara", "Miesle", "", 2004);
            AddMember("Liam", "Morgan", "", 2004);
            AddMember("Sadie", "Morgan", "", 2006);
            AddMember("Dean", "Morrissey", "", 2002);
            AddMember("Caoimhe", "Murch", "", 2005);
            AddMember("Eoghan", "Murch", "", 2003);
            AddMember("Doireann", "Ni Bhraoiin", "", 2006);
            AddMember("Sarah", "Ni Cheallaigh", "", 2002);
            AddMember("Teide", "Nolan", "", 2002);
            AddMember("Christopher", "Notre", "", 2001);
            AddMember("Fiachra", "O Braoin", "", 2005);
            AddMember("Eoin", "O Ceallaigh", "", 1999);
            AddMember("Daithi", "O Cearbhallain", "Condroid", 1999);
            AddMember("Aaron", "O'Brien", "", 2005);
            AddMember("Liam", "O'Brien", "", 2002);
            AddMember("Roan", "O'Flaherty", "", 2002);
            AddMember("Seoda", "O'Keeffe", "", 2003);
            AddMember("Cadhla", "O'Keeffe", "", 2005);
            AddMember("Tadhg", "O'Keeffe", "", 2007);
            AddMember("Fintan", "O'Kelly", "", 2002);
            AddMember("Tom", "O'Looney", "", 2002);
            AddMember("Matt", "O'Looney", "", 2003);
            AddMember("James", "O'Neill", "Jammysquirrel", 1999);
            AddMember("Sami", "O'Regan", "", 1998);
            AddMember("Ciaran", "Otuos", "", 2003);
            AddMember("Kieran", "Parry", "", 2001);
            AddMember("Alexander", "Peach", "", 2002);
            AddMember("Cian", "Perill", "sethanic", 2002);
            AddMember("Donnacha", "Ralph", "", 2003);
            AddMember("Emma", "Ryan", "", 2000);
            AddMember("Conor", "Shanahan", "", 2003);
            AddMember("Ciara", "Shanahan", "", 2004);
            AddMember("Jack", "Sheehan", "", 1999);
            AddMember("Emmett", "Shivers", "", 2000);
            AddMember("Abbie", "Sullivan", "", 1996);
            AddMember("Crea", "Sullivan", "", 1999);
            AddMember("Nora", "Tchadjobo", "", 2002);
            AddMember("Dillon", "Toumanguelov", "", 2002);
            AddMember("Tamlyn", "Toumanguelov", "", 2005);
            AddMember("Oran", "Treacy", "", 1999);
            AddMember("Ronan", "Wall", "", 2000);
            return Content("Data imported");
        }
        */

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentAdult = this.GetCurrentAdult();
            base.OnActionExecuting(filterContext);
        }
    }
}
