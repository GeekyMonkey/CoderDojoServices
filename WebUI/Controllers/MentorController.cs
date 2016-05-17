using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CoderDojo.Views
{
    [AuthorizeMentor]
    [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
    public partial class MentorController : BaseController
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

            ViewBag.BadgeApplications = (from mb in db.MemberBadges.Include("Member").Include("Badge.BadgeCategory")
                                        where mb.Awarded == null
                                        && mb.RejectedDate == null
                                        orderby mb.Member.FirstName, mb.Member.LastName, mb.Badge.BadgeCategory.CategoryName, mb.Badge.Achievement
                                        select mb).ToList();

            List<DateTime> sessionDates = db.GetSessionDates(DateTime.Today).ToList();
            DateTime sessionDate = sessionDates[0];

            ViewBag.PresentMemberIds = db.MemberAttendances
                .Where(a => a.Date == sessionDate)
                .OrderBy(a => a.MemberId)
                .Select(a => a.MemberId)
                .ToList();

            return View("Index", mentor);
        }

        public ActionResult DataCleanup()
        {
            DateTime oneYearAgo = DateTime.Today.AddYears(-1);
            int membersDeleted = 0;
            int parentsDeleted = 0;

            try
            {
                // Delete parents who haven't attended in a year 
                foreach (var member in db.Members.Include(m => m.MemberAttendances).Where(m => m.Deleted == false).ToList())
                {
                    DateTime lastAttendance = member.LastAttendance;
                    if (lastAttendance < oneYearAgo)
                    {
                        member.Deleted = true;
                        membersDeleted++;
                    }
                }

                // Delete parents who are not mentors and have no active children
                foreach (var parent in db.Adults.Include(p => p.MemberParents).Where(a => a.Deleted == false && a.IsParent == true && a.IsMentor == false).ToList())
                {
                    if (!parent.MemberParents.Any(mp => mp.Member != null && mp.Member.Deleted == false))
                    {
                        parent.Deleted = true;
                        parentsDeleted++;
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.Exception = ex;
            }

            ViewBag.MembersDeleted = membersDeleted;
            ViewBag.ParentsDeleted = parentsDeleted;
            return View("DataCleanup");
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

            var members = (from m in db.Members
                          where m.Deleted == false
                          orderby m.FirstName, m.LastName
                          select m).ToList();

            List<AttendanceModel> attendance = (from m in members
                                                select new AttendanceModel
                                                {
                                                    MemberId = m.Id,
                                                    MemberName = m.FirstName + " " + m.LastName,
                                                    Present = presentMemberIds.Contains(m.Id),
                                                    MemberBeltColorHex = m.BeltColorHex
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
            DoAttendanceChange(membergId, present, sessionDate);

            return Json("OK");
        }

        private void DoAttendanceChange(Guid memberId, bool present, DateTime sessionDate)
        {
            int sessionCount = db.AttendanceSet(memberId, present, sessionDate);
            int dojoAttendanceCount = db.MemberAttendances.Count(ma => ma.Date == sessionDate);
            Member member = db.Members.FirstOrDefault(m => m.Id == memberId);

            // Notify other members looking at this screen
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<AttendanceHub>();
            string memberMessage = "";
            if (present)
            {
                memberMessage = member.GetLoginMessage();
            }
            context.Clients.All.OnAttendanceChange(sessionDate.ToString("yyyy-MM-dd"), memberId.ToString("N"), member.MemberName, (member.TeamId ?? Guid.Empty).ToString("N"), present.ToString().ToLower(), sessionCount, dojoAttendanceCount, memberMessage, member.ImageUrl);
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
        public ActionResult AdultMerge(string adultMode, Guid id)
        {
            Adult adult = db.Adults.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            ViewBag.AdultMode = adultMode;

            return View("AdultMerge", adult);
        }

        [HttpGet]
        public ActionResult MemberMerge(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;

            return View("MemberMerge", member);
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
                          where (m.FirstName.StartsWith(name) || m.LastName.StartsWith(name) || ((m.FirstName + " " + m.LastName).StartsWith(name)))
                          && m.Deleted == false
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
                           where (a.FirstName.StartsWith(name) || a.LastName.StartsWith(name) || ((a.FirstName + " " + a.LastName).StartsWith(name)))
                           && (a.Deleted == false)
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
        public ActionResult SearchAdultMergeByName(string name, Guid adultId)
        {
            var members = (from a in db.Adults
                           where (a.FirstName.StartsWith(name) || a.LastName.StartsWith(name) || ((a.FirstName + " " + a.LastName).StartsWith(name)))
                           && (a.Id != adultId)
                           // Allow merging of deleted accounts  && a.Deleted == false
                           orderby a.FirstName, a.LastName
                           select new
                           {
                               a.FirstName,
                               a.LastName,
                               a.Phone,
                               a.Email,
                               a.Id
                           }).ToList()
                          .Select(x => new { FirstName = x.FirstName, LastName = x.LastName, Id = x.Id.ToString("N"), Email = x.Email, Phone = x.Phone });
            return Json(members, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult SearchMemberMergeByName(string name, Guid memberId)
        {
            var members = (from m in db.Members
                           where (m.FirstName.StartsWith(name) || m.LastName.StartsWith(name) || ((m.FirstName + " " + m.LastName).StartsWith(name)))
                           && (m.Id != memberId)
                           // Allow merging of deleted accounts  && (m.Deleted == false)
                           orderby m.FirstName, m.LastName, m.BirthYear
                           select new
                           {
                               m.FirstName,
                               m.LastName,
                               m.BirthYear,
                               m.ScratchName,
                               m.Id
                           }).ToList()
                          .Select(x => new { FirstName = x.FirstName, LastName = x.LastName, Id = x.Id.ToString("N"), BirthYear = x.BirthYear, ScratchName = x.ScratchName });
            return Json(members, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult MergeAdults(Guid adultId, Guid mergeId, int newPhone, int newEmail)
        {
            var a1 = db.Adults.FirstOrDefault(a => a.Id == adultId);
            var a2 = db.Adults.FirstOrDefault(a => a.Id == mergeId);

            if (newPhone == 1)
            {
                a1.Phone = a2.Phone;
            }

            if (newEmail == 1)
            {
                a1.Email = a2.Email;
            }

            a1.GardaVetted = MergeValues(a1.GardaVetted, a2.GardaVetted);
            a1.GithubLogin = MergeValues(a1.GithubLogin, a2.GithubLogin);
            a1.IsMentor = MergeValues(a1.IsMentor, a2.IsMentor);
            a1.IsParent = MergeValues(a1.IsParent, a2.IsParent);
            a1.XboxGamertag = MergeValues(a1.XboxGamertag, a2.XboxGamertag);
            a1.ScratchName = MergeValues(a1.ScratchName, a2.ScratchName);
            
            // Move linked children
            foreach (var relationship2 in a2.MemberParents.ToList())
            {
                if (a1.MemberParents.FirstOrDefault(mp => mp.MemberId == relationship2.MemberId) == null)
                {
                    db.MemberParents.Add(new MemberParent { Member = relationship2.Member, Adult = a1 });
                }
                db.MemberParents.Remove(relationship2);
            }

            db.Adults.Remove(a2);

            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult MergeMembers(Guid memberId, Guid mergeId, int newBirthYear, int newScratchName)
        {
            var m1 = db.Members.FirstOrDefault(m => m.Id == memberId);
            var m2 = db.Members.FirstOrDefault(m => m.Id == mergeId);

            if (newBirthYear == 1)
            {
                m1.BirthYear = m2.BirthYear;
            }

            if (newScratchName == 1)
            {
                m1.ScratchName = m2.ScratchName;
            }

            if (m1.Team == null)
            {
                m1.Team = m2.Team;
            }

            m1.GithubLogin = MergeValues(m1.GithubLogin, m2.GithubLogin);
            m1.XboxGamertag = MergeValues(m1.XboxGamertag, m2.XboxGamertag);
            m1.Login = MergeValues(m1.Login, m2.Login);

            // Move linked parents
            foreach (var relationship2 in m2.MemberParents.ToList())
            {
                if (m1.MemberParents.FirstOrDefault(mp => mp.AdultId == relationship2.AdultId) == null)
                {
                    db.MemberParents.Add(new MemberParent { Member = m1, Adult = relationship2.Adult });
                }
                db.MemberParents.Remove(relationship2);
            }

            // Merge badges
            foreach (var badge in m2.MemberBadges.Where(mb => mb.Awarded != null).ToList())
            {
                if (m1.MemberBadges.FirstOrDefault(mb => mb.BadgeId == badge.BadgeId && mb.Awarded != null) == null)
                {
                    badge.Member = m1;
                }
                else
                {
                    db.MemberBadges.Remove(badge);
                }
            }

            // Merge belts
            foreach (var belt in m2.MemberBelts.Where(mb => mb.Awarded != null).ToList())
            {
                if (m1.MemberBelts.FirstOrDefault(mb => mb.BeltId == belt.BeltId && mb.Awarded != null) == null)
                {
                    belt.Member = m1;
                }
                else
                {
                    db.MemberBelts.Remove(belt);
                }
            }

            // Merge Attendance
            foreach (var att in m2.MemberAttendances.ToList())
            {
                if (m1.MemberAttendances.FirstOrDefault(a => a.Date == att.Date) == null)
                {
                    att.Member = m1;
                }
                else
                {
                    db.MemberAttendances.Remove(att);
                }
            }

            db.Members.Remove(m2);

            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult MemberSignup(string previousPage = "")
        {
            ViewBag.PreviousPage = previousPage;
            Member newMember = new Member
            {
                AttendedToday = true
            };
            ViewBag.Teams = db.Teams.Where(t => t.Deleted == false).OrderBy(t => t.TeamName).ToList();
            return View("Member", newMember);
        }

        [HttpGet]
        public ActionResult Members(Guid? id = null)
        {
            List<Member> members = (from m in db.Members
                                    where m.Deleted == false
                                    orderby m.FirstName, m.LastName
                                    select m).ToList();

            List<DateTime> sessionDates = db.GetSessionDates(DateTime.Today).ToList();
            DateTime sessionDate = sessionDates[0];
            ViewBag.PresentMemberIds = db.MemberAttendances
                .Where(a => a.Date == sessionDate)
                .OrderBy(a => a.MemberId)
                .Select(a => a.MemberId)
                .ToList();

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
            ViewBag.Teams = db.Teams.Where(t => t.Deleted == false).OrderBy(t => t.TeamName).ToList();
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
                member.FirstName = TrimNullableString(memberChanges.FirstName).Trim();
                member.LastName = TrimNullableString(memberChanges.LastName).Trim();
                member.BirthYear = memberChanges.BirthYear;
                member.Login = TrimNullableString(memberChanges.Login);
                member.GithubLogin = TrimNullableString(memberChanges.GithubLogin);
                member.ScratchName = TrimNullableString(memberChanges.ScratchName);
                member.XboxGamertag = TrimNullableString(memberChanges.XboxGamertag);
                member.TeamId = memberChanges.TeamId;

                // Password change
                if (string.IsNullOrEmpty(memberChanges.NewPassword) == false)
                {
                    member.PasswordHash = db.GeneratePasswordHash(memberChanges.NewPassword);
                }

                // Save
                db.SaveChanges();

                // Did new member attend today
                memberChanges.AttendedToday = (Request.Form["AttendedToday"] ?? "").ToLower().EndsWith("true"); // Problem with jquery mobile checkbox
                if (memberChanges.AttendedToday)
                {
                    DoAttendanceChange(member.Id, true, DateTime.Today);
                }
                else
                {
                    DoAttendanceChange(member.Id, member.MemberAttendances.Where(ma => ma.Date == DateTime.Today).Any(), DateTime.Today);
                }

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
            ViewBag.Badges = db.Badges
                .Include("BadgeCategory")
                .Where(b => !b.Deleted)
                .OrderBy(b => b.BadgeCategory.CategoryName)
                .ThenBy(b => b.Achievement)
                .ToList();
            ViewBag.Mentor = GetCurrentAdult();
            return View("MemberBadges", member);
        }
        [HttpGet]
        public ActionResult MemberBelts(Guid id)
        {
            Member member = db.Members.FirstOrDefault(m => m.Id == id);
            ViewBag.ShowBackButton = true;
            ViewBag.Belts = db.Belts.Where(b => !b.Deleted).OrderBy(b => b.SortOrder).ToList();
            ViewBag.Mentor = GetCurrentAdult();
            return View("MemberBelts", member);
        }

        [HttpPost]
        public ActionResult BadgeApprove(Guid id, string message)
        {
            var memberBadge = db.MemberBadges.FirstOrDefault(mb => mb.Id == id);
            memberBadge.Awarded = DateTime.UtcNow;
            memberBadge.AwardedByAdultId = GetCurrentAdult().Id;
            memberBadge.AwardedNotes = message;
            db.SaveChanges();
            return Json("OK");
        }

        [HttpPost]
        public ActionResult BadgeUndo(Guid id)
        {
            var memberBadge = db.MemberBadges.FirstOrDefault(mb => mb.Id == id);
            memberBadge.Awarded = null;
            memberBadge.AwardedByAdultId = null;
            memberBadge.AwardedNotes = null;
            memberBadge.RejectedDate = null;
            memberBadge.RejectedByAdultId = null;
            memberBadge.RejectedNotes = null;
            db.SaveChanges();
            return Json("OK");
        }

        [HttpPost]
        public ActionResult BadgeReject(Guid id, string message)
        {
            var memberBadge = db.MemberBadges.FirstOrDefault(mb => mb.Id == id);
            memberBadge.RejectedDate = DateTime.UtcNow;
            memberBadge.RejectedByAdultId = GetCurrentAdult().Id;
            memberBadge.RejectedNotes = message;
            db.SaveChanges();
            return Json("OK");
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
        public ActionResult BeltUndo(Guid id)
        {
            var memberBelt = db.MemberBelts.FirstOrDefault(mb => mb.Id == id);
            memberBelt.Awarded = null;
            memberBelt.AwardedByAdultId = null;
            memberBelt.AwardedNotes = null;
            memberBelt.RejectedDate = null;
            memberBelt.RejectedByAdultId = null;
            memberBelt.RejectedNotes = null;
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

        /// <summary>
        /// Belt Maintenance
        /// </summary>
        /// <returns></returns>
        public ActionResult Belts()
        {
            var belts = db.Belts.Where(b => !b.Deleted).OrderBy(b => b.SortOrder).ToList();
            return View("Belts", belts);
        }

        public ActionResult BeltAdd()
        {
            Belt newBelt = new Belt();
            newBelt.SortOrder = (int)(db.Belts.Max(b => b.SortOrder) + 1.0);
            newBelt.HexCode = "#000000";
            return View("Belt", newBelt);
        }

        public ActionResult Belt(Guid id)
        {
            Belt belt = db.Belts.FirstOrDefault(b => b.Id == id);
            return View("Belt", belt);
        }

        [HttpPost]
        public ActionResult BeltSave(Belt b)
        {
            Belt belt;
            if (b.Id == null || b.Id == Guid.Empty)
            {
                belt = new Belt();
                db.Belts.Add(belt);
            } else {
                belt = db.Belts.FirstOrDefault(x => x.Id == b.Id);
            }
            belt.Color = b.Color;
            belt.Description = b.Description;
            belt.HexCode = b.HexCode;
            belt.SortOrder = b.SortOrder;
            db.SaveChanges();
            return RedirectClient("/Mentor/Belts");
        }

        [HttpPost]
        public ActionResult BeltDelete(Guid id)
        {
            if (id != null && id != Guid.Empty)
            {
                Belt belt;
                belt = db.Belts.FirstOrDefault(x => x.Id == id);
                belt.Deleted = true;
                db.SaveChanges();
            }
            return RedirectClient("/Mentor/Belts");
        }

        /// <summary>
        /// BadgeCategory Maintenance
        /// </summary>
        /// <returns></returns>
        public ActionResult BadgeCategories()
        {
            var badgeCategories = GetBadgeCategories();
            return View("BadgeCategories", badgeCategories);
        }

        public ActionResult BadgeCategoryAdd()
        {
            BadgeCategory newBadgeCategory = new BadgeCategory();
            return View("BadgeCategory", newBadgeCategory);
        }

        public ActionResult BadgeCategory(Guid id)
        {
            BadgeCategory badgeCategory = db.BadgeCategories.FirstOrDefault(b => b.Id == id);
            return View("BadgeCategory", badgeCategory);
        }

        [HttpPost]
        public ActionResult BadgeCategorySave(BadgeCategory b)
        {
            BadgeCategory badgeCategory;
            if (b.Id == null || b.Id == Guid.Empty)
            {
                badgeCategory = new BadgeCategory();
                db.BadgeCategories.Add(badgeCategory);
            }
            else
            {
                badgeCategory = db.BadgeCategories.FirstOrDefault(x => x.Id == b.Id);
            }
            badgeCategory.CategoryName = b.CategoryName;
            badgeCategory.CategoryDescription = b.CategoryDescription;
            db.SaveChanges();
            return RedirectClient("/Mentor/BadgeCategories");
        }

        [HttpPost]
        public ActionResult BadgeCategoryDelete(Guid id)
        {
            if (id != null && id != Guid.Empty)
            {
                BadgeCategory badgeCategory;
                badgeCategory = db.BadgeCategories.FirstOrDefault(x => x.Id == id);
                badgeCategory.Deleted = true;
                db.SaveChanges();
            }
            return RedirectClient("/Mentor/BadgeCategories");
        }

        /// <summary>
        /// Badge Maintenance
        /// </summary>
        /// <returns></returns>
        public ActionResult Badges()
        {
            var badges = db.Badges.Include("BadgeCategory").Where(b => !b.Deleted)
                .OrderBy(b => b.BadgeCategory.CategoryName)
                .ThenBy(b => b.Achievement)
                .ToList();
            return View("Badges", badges);
        }

        public ActionResult BadgeAdd()
        {
            Badge newBadge = new Badge
            {
                Achievement = "",
                Description = ""
            };
            ViewBag.BadgeCategories = GetBadgeCategories();
            return View("Badge", newBadge);
        }

        public ActionResult Badge(Guid id)
        {
            Badge badge = db.Badges.FirstOrDefault(b => b.Id == id);
            ViewBag.BadgeCategories = GetBadgeCategories();
            return View("Badge", badge);
        }

        private IEnumerable<BadgeCategory> GetBadgeCategories()
        {
            return db.BadgeCategories
                .Where(bc => bc.Deleted == false)
                .OrderBy(bc => bc.CategoryName)
                .ToList();
        }

        [HttpPost]
        public ActionResult BadgeSave(Badge b)
        {
            Badge badge;
            if (b.Id == null || b.Id == Guid.Empty)
            {
                badge = new Badge();
                db.Badges.Add(badge);
            }
            else
            {
                badge = db.Badges.FirstOrDefault(x => x.Id == b.Id);
            }
            badge.BadgeCategoryId = b.BadgeCategoryId;
            badge.Achievement = b.Achievement;
            badge.Description = b.Description;
            db.SaveChanges();
            return RedirectClient("/Mentor/Badges");
        }

        [HttpPost]
        public ActionResult BadgeDelete(Guid id)
        {
            if (id != null && id != Guid.Empty)
            {
                Badge badge;
                badge = db.Badges.FirstOrDefault(x => x.Id == id);
                badge.Deleted = true;
                db.SaveChanges();
            }
            return RedirectClient("/Mentor/Badges");
        }

        /// <summary>
        /// Team Maintenance
        /// </summary>
        /// <returns></returns>
        public ActionResult Teams()
        {
            var teams = db.Teams.Where(t => !t.Deleted).OrderBy(t => t.TeamName).ToList();
            return View("Teams", teams);
        }

        public ActionResult TeamAdd()
        {
            Team newTeam = new Team();
            newTeam.HexCode = "#000000";
            return View("Team", newTeam);
        }

        public ActionResult Team(Guid id)
        {
            Team team = db.Teams.FirstOrDefault(b => b.Id == id);
            return View("Team", team);
        }

        [HttpPost]
        public ActionResult TeamSave(Team t)
        {
            Team team;
            if (t.Id == null || t.Id == Guid.Empty)
            {
                team = new Team();
                db.Teams.Add(team);
            }
            else
            {
                team = db.Teams.FirstOrDefault(x => x.Id == t.Id);
            }
            team.Goal = t.Goal;
            team.HexCode = t.HexCode;
            team.Notes = t.Notes;
            team.TeamName = t.TeamName;
            db.SaveChanges();
            return RedirectClient("/Mentor/Teams");
        }

        [HttpPost]
        public ActionResult TeamDelete(Guid id)
        {
            if (id != null && id != Guid.Empty)
            {
                Team team;
                team = db.Teams.FirstOrDefault(x => x.Id == id);
                team.Deleted = true;
                db.SaveChanges();
            }
            return RedirectClient("/Mentor/Teams");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentAdult = this.GetCurrentAdult();
            base.OnActionExecuting(filterContext);
        }
    }
}
