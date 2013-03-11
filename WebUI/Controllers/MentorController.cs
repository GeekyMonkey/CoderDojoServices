﻿using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

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

            ViewBag.BeltApplications = (from mb in db.MemberBelts.Include("Member")
                                       where mb.Awarded == null
                                       && mb.RejectedDate == null
                                       orderby mb.Belt.SortOrder, mb.Member.FirstName, mb.Member.LastName
                                       select mb).ToList();

            return View("Index", mentor);
        }

        /// <summary>
        /// Attendance View
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Attendance(string attendanceDate = null, Guid? memberId = null)
        {
            DateTime firstSessionDate = new DateTime(2012, 3, 24);
            DateTime? sessionDate = null;
            if (attendanceDate != null)
            {
                sessionDate = DateTime.ParseExact(attendanceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            /*
            List<DateTime> sessionDates = new List<DateTime>();
            DateTime date = DateTime.Today;
            while (date.DayOfWeek != DayOfWeek.Saturday)
            {
                date = date.AddDays(1.0);
            }
            while (date >= firstSessionDate)
            {
                sessionDates.Add(date);
                date = date.AddDays(-7);
            }
            */

            List<DateTime> sessionDates = db.GetSessionDates(DateTime.Today).ToList();

            if (sessionDate == null)
            {
                sessionDate = sessionDates[0];
            }

            var presentMemberIds = db.MemberAttendances
                .Where(a => a.Date == sessionDate)
                .OrderBy(a => a.MemberId)
                .Select(a => a.MemberId)
                .ToList();
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
            ViewBag.SessionDates = sessionDates;
            ViewBag.AttendanceDate = sessionDate;
            return View(attendance);
        }

        [HttpPost]
        public ActionResult AttendanceChange(string memberId, bool present, string attendanceDate)
        {
            DateTime sessionDate;
            if (attendanceDate != null)
            {
                sessionDate = DateTime.ParseExact(attendanceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
            }
            else
            {
                sessionDate = DateTime.Today;
            }
            Guid membergId = new Guid(memberId);
            int sessionCount = db.AttendanceSet(membergId, present, sessionDate);
            int dojoAttendanceCount = db.MemberAttendances.Count(ma => ma.Date == sessionDate);
            Member member = db.Members.FirstOrDefault(m => m.Id == membergId);

            // Notify other members looking at this screen
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<AttendanceHub>();
            string memberMessage = "";
            if (present)
            {
                memberMessage = member.GetLoginMessage();
            }
            context.Clients.All.OnAttendanceChange(sessionDate.ToString("yyyy-MM-dd"), membergId.ToString("N"), member.MemberName, present.ToString().ToLower(), sessionCount, dojoAttendanceCount, memberMessage);

            return Json("OK");
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
            bool newAdult = false;
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
                    newAdult = true;
                }

                // Save changes
                adult.FirstName = TrimNullableString(adultChanges.FirstName);
                adult.LastName = TrimNullableString(adultChanges.LastName);
                adult.Email = TrimNullableString(adultChanges.Email);
                adult.Phone = TrimNullableString(adultChanges.Phone);
                adult.IsParent = adultChanges.IsParent;
                adult.IsMentor = adultChanges.IsMentor;
                adult.GithubLogin = TrimNullableString(adultChanges.GithubLogin);
                adult.XboxGamertag = TrimNullableString(adultChanges.XboxGamertag);
                adult.ScratchName = TrimNullableString(adultChanges.ScratchName);
                adult.Login = TrimNullableString(adultChanges.Login);

                // Password change
                if (string.IsNullOrEmpty(adultChanges.NewPassword) == false)
                {
                    adult.PasswordHash = db.GeneratePasswordHash(adultChanges.NewPassword);
                }
                db.SaveChanges();
                if (adultMode == "Parent" && newAdult)
                {
                    return RedirectClient("/Mentor/ParentKids/" + adult.Id.ToString("N"));
                }
            }
            return RedirectClient("/Mentor/" + adultMode + "s");
        }

        [HttpGet]
        public ActionResult SearchMembersByName(string name)
        {
            var members = (from m in db.Members
                          where m.FirstName.StartsWith(name) || m.LastName.StartsWith(name)
                          || ((m.FirstName + " " + m.LastName).StartsWith(name))
                          orderby m.FirstName, m.LastName
                          select new {
                            m.FirstName, m.LastName, m.Id
                          }).ToList()
                          .Select(x => new { FirstName = x.FirstName, LastName = x.LastName, Id = x.Id.ToString("N") });
            return Json(members, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult SearchParentsByName(string name)
        {
            var members = (from a in db.Adults
                           where (a.FirstName.StartsWith(name) || a.LastName.StartsWith(name) ||
                           ((a.FirstName + " " + a.LastName).StartsWith(name)))
                           && (a.IsParent == true)
                           orderby a.FirstName, a.LastName
                           select new
                           {
                               a.FirstName,
                               a.LastName,
                               a.Id
                           }).ToList()
                          .Select(x => new { FirstName = x.FirstName, LastName = x.LastName, Id = x.Id.ToString("N") });
            return Json(members, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult MemberSignup(string previousPage = "")
        {
            ViewBag.PreviousPage = previousPage;
            return View("Member", new Member());
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
        public ActionResult Member(Guid id, string previousPage = "")
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.PreviousPage = previousPage;
            ViewBag.ShowBackButton = true;
            return View("Member", member);
        }

        [HttpPost]
        public ActionResult MemberSave(Member memberChanges, string previousPage)
        {
            Member member = null;

            if (ModelState.IsValid)
            {
                if (memberChanges.Id != null)
                {
                    member = db.Members.Find(memberChanges.Id);
                }

                // New Member
                if (member == null)
                {
                    member = new Member();
                    member.Id = Guid.NewGuid();
                    db.Members.Add(member);
                }

                // Save changes
                member.FirstName = TrimNullableString(memberChanges.FirstName);
                member.LastName = TrimNullableString(memberChanges.LastName);
                member.BirthYear = memberChanges.BirthYear;
                member.Login = TrimNullableString(memberChanges.Login);
                member.GithubLogin = TrimNullableString(memberChanges.GithubLogin);
                member.ScratchName = TrimNullableString(memberChanges.ScratchName);
                member.XboxGamertag = TrimNullableString(memberChanges.XboxGamertag);

                // Password change
                if (string.IsNullOrEmpty(memberChanges.NewPassword) == false)
                {
                    member.PasswordHash = db.GeneratePasswordHash(memberChanges.NewPassword);
                }
                db.SaveChanges();
                if (previousPage == "Attendance")
                {
                    db.AttendanceSet(member.Id, true, DateTime.Today);
                    return RedirectClient("/Mentor/Attendance?id=" + member.Id);
                }
                return RedirectClient("/Mentor/Members?id=" + member.Id);
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
            ViewBag.Belts = db.Belts.OrderBy(b => b.SortOrder).ToList();
            return View("MemberBelts", member);
        }

        [HttpPost]
        public ActionResult BeltApprove(Guid id, string message)
        {
            var memberBelt = db.MemberBelts.FirstOrDefault(mb => mb.Id == id);
            memberBelt.Awarded = DateTime.UtcNow;
            memberBelt.AwardedByAdultId = GetCurrentAdult().Id;
            memberBelt.AwardedNotes = message;
            db.SaveChanges();
            return Json("OK");
        }

        [HttpPost]
        public ActionResult BeltReject(Guid id, string message)
        {
            var memberBelt = db.MemberBelts.FirstOrDefault(mb => mb.Id == id);
            memberBelt.RejectedDate = DateTime.UtcNow;
            memberBelt.RejectedByAdultId = GetCurrentAdult().Id;
            memberBelt.RejectedNotes = message;
            db.SaveChanges();
            return Json("OK");
        }

        [HttpPost]
        public ActionResult DeleteRelationship(Guid id)
        {
            var relationship = db.MemberParents.FirstOrDefault(mp => mp.Id == id);
            db.MemberParents.Remove(relationship);
            db.SaveChanges();
            return Json("OK");
        }

        [HttpPost]
        public ActionResult CreateRelationship(Guid parentId, Guid memberId)
        {
            var relationship = new MemberParent
            {
                AdultId = parentId,
                MemberId = memberId
            };
            db.MemberParents.Add(relationship);
            db.SaveChanges();

            relationship = (from mp in db.MemberParents.Include("Adult").Include("Member")
                            where mp.Id == relationship.Id
                            select mp).FirstOrDefault();
            return Json(new {
                relationshipId = relationship.Id.ToString("N"),
                memberId = relationship.MemberId.ToString("N"),
                adultId = relationship.AdultId.ToString("N"),
                adultFullName = relationship.Adult.FullName.Replace("'", "&quot;"),
                memberFullName = relationship.Member.MemberName.Replace("'", "&quot;")
            });
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

        // Mentor is kicking off SignIn mode
        public ActionResult SignInMode()
        {
            FormsAuthentication.SetAuthCookie(null, false);
            FormsAuthentication.SignOut();
            Response.SetCookie(new HttpCookie("SignInCookie", DateTime.Today.ToString("yyyy-MM-dd")));
            return RedirectClient("/SignIn");
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

        /*
        [HttpGet]
        public ActionResult ImportAttendance()
        {
            List<DateTime> dates = new List<DateTime>();
            dates.Add(new DateTime(2012,9,8));
            dates.Add(new DateTime(2012,9,15));
            dates.Add(new DateTime(2012,9,22));
            dates.Add(new DateTime(2012,9,29));
            dates.Add(new DateTime(2012,10,6));
            dates.Add(new DateTime(2012,10,20));
            dates.Add(new DateTime(2012,11,3));
            dates.Add(new DateTime(2012,11,10));
            dates.Add(new DateTime(2012,11,17));
            dates.Add(new DateTime(2012,11,24));
            dates.Add(new DateTime(2012,12,1));
            dates.Add(new DateTime(2012,12,8));
            dates.Add(new DateTime(2012,12,15));
            dates.Add(new DateTime(2013,1,12));
            dates.Add(new DateTime(2013,1,19));
            dates.Add(new DateTime(2013,1,26));
            dates.Add(new DateTime(2013,2,2));
            dates.Add(new DateTime(2013,2,9));
            dates.Add(new DateTime(2013,2,16));
            dates.Add(new DateTime(2013,3,2));

            ImportAttendanceLine(dates, new [] {"Katie","Arthur","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Emmanuel","Atijohn","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Peter","Bailey","0","0","1","1","1","1","1","1","0","1","1","1","1","1","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"John","Bailey","0","0","1","1","1","1","1","1","0","1","1","1","1","1","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Amy","Bailey","0","0","0","1","1","1","1","1","0","1","1","1","1","1","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Tiernan","Boyce","1","1","1","1","1","1","1","1","0","1","1","1","1","1","1","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Jack","Brennan","0","1","1","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Rory","Brennan","0","0","0","0","0","1","1","1","1","1","0","1","0","0","1","1","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Ephram","Brotherton","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Garry","Cahill","0","1","1","1","1","1","1","1","1","1","1","1","1","1","1","0","1","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Padraig","Cahill","0","0","0","0","0","0","1","1","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"William","Cahir Whelan","0","1","0","0","0","0","1","1","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Andrew","Cahir Whelan","0","1","0","0","0","0","1","1","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Kane","Cantrell","0","0","0","0","0","0","0","0","0","1","1","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Slaine","Carey","1","1","1","1","1","1","1","1","1","0","0","1","1","0","0","1","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Aobha","Carey","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Oran","Carey","1","1","1","1","1","1","1","1","1","0","0","1","1","0","0","1","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Catherine","Casey","0","0","0","0","0","1","1","0","0","1","1","0","0","1","1","1","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Andrew","Casey","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Colm","Cleary","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Kieran","Clohessy","0","0","1","1","1","1","0","1","1","1","1","0","1","0","1","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Jack","Conlon","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","1","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Patrick","Conlon","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Joseph","Connaughton","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Oisin","Connole","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Jack","Corry","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Niall","Corry","0","0","0","0","0","0","0","1","0","0","0","1","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Ben","Cosgrove","0","0","0","0","0","1","0","1","1","0","0","1","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Michael","Costello","0","0","0","0","0","0","0","0","0","1","1","0","0","0","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Thomas","Costello","0","0","0","0","0","0","0","0","0","1","1","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Ally","Crimmins","0","0","0","0","0","0","0","1","1","1","1","1","1","0","1","0","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Oisin","Crimmins","0","0","0","0","0","0","0","0","0","0","1","1","1","0","1","0","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Vincent","Crowley","0","0","0","0","0","1","1","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Dylan","Cummins","0","0","0","0","0","0","0","1","1","1","1","1","0","1","1","1","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"James","Doherty","0","0","0","0","0","0","0","0","0","0","0","1","0","1","1","0","1","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Ella","Doherty","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Noah","Doherty","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Sean","Donnelly","0","0","0","1","0","0","1","1","1","0","1","1","1","1","1","1","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Diarmuid","Edwards","0","0","0","0","1","1","0","1","0","1","1","1","1","0","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Liam","Fahy","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Gillian","Fahy","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Roisin","fahy","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Ronan","Fallon","0","0","0","0","1","1","0","0","1","1","1","1","1","1","0","0","1","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Barry","Fitzgerald","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Mark","Fitzgerald","0","0","0","0","0","1","1","1","0","1","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Abbie","Fitzgerald","0","0","0","0","0","1","1","1","1","1","0","0","0","0","0","0","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Diarmuid","Fitzgerald","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Colum","Francis O Dalaigh","0","0","0","0","0","0","1","1","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Conor","Francis O Dalaigh","0","0","0","0","0","0","1","1","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Joannah","Francis O Dalaigh","0","0","0","0","0","0","1","1","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Oliver","Gavin","1","1","1","1","1","1","1","1","0","1","1","1","1","1","1","1","1","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Carla","Griffin","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Alma","Griffin","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Kevin","Hayes","0","0","0","1","0","0","0","0","1","1","0","0","0","0","1","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Sean","Hehir","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Ciaran","Hickey","0","1","1","1","1","1","0","0","1","1","0","0","0","0","0","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Alana","Higgins","0","0","1","0","1","1","1","1","0","0","1","0","1","1","0","1","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Caleb","Higgins","0","0","1","1","1","1","1","1","1","1","1","0","1","1","0","1","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Emer","Keane","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Nathan","Keane","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Joseph","Kelly","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Dan","Kennedy","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Jack","Kirrane","1","1","1","0","0","0","0","0","0","0","0","0","1","0","0","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Luke","Kirrane","1","1","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Cian","Lahiffe","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","0","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Caoimhe","Lally","1","0","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Oisin","Lally","1","1","0","1","1","1","1","1","1","1","1","1","1","0","0","1","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Aoibhinn","Leyden","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Ruth","Leyden","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Abdelqahman","Liani","0","1","1","1","1","1","0","1","1","1","1","1","0","1","0","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Jamila","Liani","0","1","1","1","1","1","0","1","1","1","1","1","0","1","0","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Sara","Liani","0","1","1","1","1","1","0","1","1","1","1","1","0","1","0","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Darragh","Liddy","1","0","0","1","1","1","1","1","1","1","1","1","1","1","1","1","1","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Seadhna","Liddy","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","0","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Eliza","Liddy","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","0","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Reuben","Liddy","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","0","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Cormac","Lynam","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","1","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Jack","Lynch","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Daniel","Maguire","0","1","1","0","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Cormac","Maher","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Mark","Mc Govern","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Jack","Mc Morrough","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"John","McArdle","0","0","0","0","0","1","0","1","1","0","0","1","1","0","0","1","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Brian","McArdle","0","0","0","0","0","0","0","1","1","0","0","1","1","0","0","1","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Roisin","McAteer","0","0","0","1","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Caomhe","McAteer","0","0","0","1","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Conor","McCarthy","1","0","0","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Sean","McCarthy","1","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Ben","McNamara","1","0","0","1","1","1","0","1","1","1","0","1","0","1","1","1","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Luke","Meade","0","0","0","0","1","1","1","1","1","1","1","1","0","1","1","1","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Aaron","Meade","0","0","0","0","1","0","0","1","1","1","1","1","0","1","1","1","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Christopher","Meaney","1","1","0","1","1","1","1","1","1","1","1","1","1","1","0","1","1","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Aidan","Meaney","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Cian","Meere","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Sean","Meere","0","1","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Ciara","Miesle","1","1","0","0","1","1","1","0","1","0","1","1","0","1","0","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Liam","Morgan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Sadie","Morgan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Dean","Morrissey","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Caoimhe","Murch","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Eoghan","Murch","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Tim","Murphy-Underhill","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Doireann","Ni Bhraoiin","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Sarah","Ni Cheallaigh","0","0","0","0","0","1","1","0","0","0","0","0","0","0","1","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Teide","Nolan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Christopher","Notre","0","0","0","0","0","0","1","0","1","1","1","0","0","0","0","1","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Fiachra","O Braoin","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Eoin","O Ceallaigh","0","0","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Daithi","O Cearbhallain","0","1","1","1","1","0","0","1","1","1","1","0","1","1","0","1","0","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Aaron","O'Brien","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Liam","O'Brien","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Roan","O'Flaherty","0","0","0","0","0","0","1","1","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Daniel","O'Flanagan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Roisin","O'Flanagan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Meadbh","O'Flanagan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Patrick","O'Grady","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Seoda","O'Keeffe","0","1","0","0","0","0","0","0","0","0","0","0","0","1","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Cadhla","O'Keeffe","0","1","0","0","0","1","0","0","0","0","0","0","0","1","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Tadhg","O'Keeffe","0","1","0","0","0","1","0","0","0","0","0","0","0","1","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Fintan","O'Kelly","0","0","0","0","0","1","0","0","1","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Tom","O'Looney","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Matt","O'Looney","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"James","O'Neill","1","1","1","1","1","1","1","1","0","1","1","1","1","1","1","1","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Sami","O'Regan","0","0","0","1","1","0","0","0","0","0","1","1","0","0","1","1","0","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Ciaran","Otuos","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Quinn","Painter","1","1","1","0","1","1","1","1","1","0","1","1","1","1","1","1","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Molly","Painter","1","1","1","0","1","1","1","1","1","0","1","1","1","1","1","1","1","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Kieran","Parry","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Alexander","Peach","0","0","0","0","0","1","0","1","1","1","0","0","0","1","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Cian","Perill","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Donnacha","Ralph","1","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Emma","Ryan","0","0","0","0","0","1","1","1","1","1","1","1","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Conor","Shanahan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Ciara","Shanahan","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Jack","Sheehan","0","0","0","0","0","0","0","0","1","1","1","0","0","0","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Emmett","Shivers","0","0","1","1","1","1","0","1","1","1","1","0","1","1","0","1","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Abbie","Sullivan","0","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Crea","Sullivan","0","0","0","0","0","1","0","0","0","0","0","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Nora","Tchadjobo","0","0","0","0","0","0","0","0","0","0","1","0","0","0","0","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Dillon","Toumanguelov","0","0","0","0","0","0","0","0","1","1","0","0","0","0","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Tamlyn","Toumanguelov","0","0","0","0","0","0","0","0","1","0","0","0","0","0","1","0","0","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Oran","Treacy","0","1","0","1","1","1","0","0","0","0","0","0","0","0","1","0","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Ronan","Wall","0","0","0","0","0","0","0","1","1","0","0","0","0","0","1","0","0","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Fomek","Zajas","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","0","1","0"});
            return Content("ok");
        }

        [HttpGet]
        public ActionResult ImportAttendance2()
        {
            List<DateTime> dates = new List<DateTime>();
            dates.Add(new DateTime(2012,3,24));
            dates.Add(new DateTime(2012,3,31));
            dates.Add(new DateTime(2012,4,14));
            dates.Add(new DateTime(2012,4,21));

            ImportAttendanceLine(dates, new [] {"Ralph","Brady","0","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Grainne","Brady","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Ephram","Brotherton","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"William","Cahir Whelan","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Andrew","Cahir Whelan","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Ailbhe","Cannon","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Grainne","Cannon","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Slaine","Carey","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Aobha","Carey","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Oran","Carey","1","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Ralph","Collins","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Dylan","Collins","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Fatoumata","Diallo","0","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Samira","Tchadjobo","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Nora","Tchadjobo","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Ronan","Lannigan","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Barry","Fitzgerald","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Aisling","Flouch","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Aoife","Flouch","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Oliver","Gavin","1","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Samuel","Gavin","1","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Kieran","Hosty","0","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Liam","Jones","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Saul","Kenny","0","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Oisin","Lally","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Daniel","Lynch","0","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Conor","McCarthy","1","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Ciara","Miesle","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Conall","Moore","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Roisin","Moore","1","1","0","1"});
            ImportAttendanceLine(dates, new [] {"Adam","Nix","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Cormac","O'Muineachain","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Cian","O'Muineachain","1","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Cadhla","O'Keeffe","1","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Tadhg","O'Keeffe","0","1","1","0"});
            ImportAttendanceLine(dates, new [] {"Seoda","O'Keeffe","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Eimear","O'Loughlin","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Holly","O'Loughlin","0","0","1","0"});
            ImportAttendanceLine(dates, new [] {"Jasvir","Kalsi","0","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Molly","Painter","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Quinn","Painter","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Kieran","Parry","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Cian","Perill","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Cian","Reilly","0","0","1","1"});
            ImportAttendanceLine(dates, new [] {"Sean","Sweeney","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Erin","Sweeney","1","0","0","0"});
            ImportAttendanceLine(dates, new [] {"Tresor","Nijimbere","1","1","1","1"});
            ImportAttendanceLine(dates, new [] {"Evan","Twomey","0","1","0","0"});
            ImportAttendanceLine(dates, new [] {"Sean","Burke","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Sami","O'Reagan","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Molly","MacCriostail","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Michael","Bonfield","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Mark","Bonfield","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Tiarnan","Boyce","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Saoirse","Boyce","0","0","0","1"});
            ImportAttendanceLine(dates, new [] {"Finn","McCarthy","0","0","0","1"});
            ImportAttendanceLine(dates, new[] { "Colum", "McCarthy", "0", "0", "0", "1" });
            return Content("ok");
        }

        private void ImportAttendanceLine(List<DateTime>dates, string[] data)
        {
            string firstName = data[0].Trim();
            string lastName = data[1].Trim();
            Member member = db.Members.FirstOrDefault(m => m.FirstName == firstName && m.LastName == lastName);
            if (member == null)
            {
                throw new Exception("Member not found: " + firstName + " " + lastName);
            }
            if (data.Count() != dates.Count() + 2)
            {
                throw new Exception("Invalid data for " + firstName + " " + lastName);
            }
            for (int i = 0; i < dates.Count(); i++)
            {
                DateTime date = dates[i];
                bool attended = data[i + 2] != "0";
                AttendanceSet(member.Id, attended, date);
            }
        }
        */

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentAdult = this.GetCurrentAdult();
            base.OnActionExecuting(filterContext);
        }
    }
}
