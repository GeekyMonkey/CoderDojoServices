using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CoderDojo.Views
{
    public partial class MentorController
    {
        [HttpGet]
        public ActionResult ExportMentorEmails()
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("First Name,Last Name,Email Address");
            var mentors = db.Adults
                .Where(a => a.Deleted == false && a.IsMentor == true && a.Email != null && a.Email != "")
                .OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName);
            foreach (var adult in mentors)
            {
                csv.AppendLine(adult.FirstName.Replace(',', '-') + "," + adult.LastName.Replace(',', '-') + "," + adult.Email.Replace(',', '-'));
            }
            return ExportCsv(csv.ToString(), "MentorEmails");
        }
        [HttpGet]
        public ActionResult ExportParentEmails()
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("First Name,Last Name,Email Address");
            var mentors = db.Adults
                .Where(a => a.Deleted == false && a.IsParent == true && a.Email != null && a.Email != "")
                .OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName);
            foreach (var adult in mentors)
            {
                csv.AppendLine(adult.FirstName.Replace(',', '-') + "," + adult.LastName.Replace(',', '-') + "," + adult.Email.Replace(',', '-'));
            }
            return ExportCsv(csv.ToString(), "ParentEmails");
        }
        private ActionResult ExportCsv(string csv, string fileName)
        {
            return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", fileName + ".csv");
        }

        [HttpGet]
        public ActionResult RecentBelts(DateTime? since = null)
        {
            if (since == null)
            {
                since = DateTime.Today.AddMonths(-24);
            }

            List<MemberBelt> memberBelts = (from b in db.MemberBelts
                                    where b.Awarded != null && b.Awarded >= since.Value
                                    orderby b.Awarded.Value descending
                                    select b).ToList();

            List<DateTime> sessionDates = db.GetSessionDates(DateTime.Today).ToList();
            DateTime sessionDate = sessionDates[0];
            ViewBag.PresentMemberIds = db.MemberAttendances
                .Where(a => a.Date == sessionDate)
                .OrderBy(a => a.MemberId)
                .Select(a => a.MemberId)
                .ToList();

            ViewBag.SelectedMemberId = null;
            ViewBag.ShowBackButton = true;
            return View("RecentBelts", memberBelts);
        }
    }
}
