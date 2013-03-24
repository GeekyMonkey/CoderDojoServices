﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoderDojo
{
    public partial class Member : BaseEntity, IUser
    {
        public string MemberName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        public string NewPassword
        {
            get;
            set;
        }

        public string GetLoginMessage()
        {
            List<string> messages = new List<string>();
            Int32 sessionCount = this.MemberAttendances.Count();

            // Welcome message
            if (LoginDatePrevious == null)
            {
                messages.Add("<h3>Welcome " + FirstName + "</h3>");
            }
            else
            {
                messages.Add("<h3>Welcome back " + FirstName + ".</h3>");
                messages.Add("Your last login was at: " + LoginDatePrevious.Value.ToString("yyyy-MM-dd HH:mm"));
            }
            messages.Add("This is your " + sessionCount + sessionCount.IntegerSuffix() + " Coder Dojo session.");

            // Find belts awarded
            var newBelts = from mb in this.MemberBelts
                           where mb.Awarded >= (LoginDatePrevious ?? DateTime.Today.ToUniversalTime()).Date
                           orderby mb.Belt.SortOrder
                           select mb;
            foreach (var mb in newBelts)
            {
                string beltMessage = "<p><strong>You have been awarded the <span style='color:" + mb.Belt.HexCode + ";'>" + mb.Belt.Color + "</span> belt!</strong>";
                if (!string.IsNullOrEmpty(mb.AwardedNotes))
                {
                    beltMessage += "<br /><em>" + mb.AwardedNotes + " - " + mb.AwardedByAdult.FullName + "</em>";
                }
                beltMessage += "</p>";
                messages.Add(beltMessage);
            }

            // Find belts rejected
            var rejectedBelts = from mb in this.MemberBelts
                           where mb.RejectedDate >= (LoginDatePrevious ?? DateTime.Today.ToUniversalTime()).Date
                           orderby mb.Belt.SortOrder
                           select mb;
            foreach (var mb in rejectedBelts)
            {
                string beltMessage = "<p><strong>Your application for the <span style='color:" + mb.Belt.HexCode + ";'>" + mb.Belt.Color + "</span> belt has been rejected. You can apply again any time.</strong>";
                if (!string.IsNullOrEmpty(mb.RejectedNotes))
                {
                    beltMessage += "<br /><em>" + mb.RejectedNotes + " - " + mb.RejectedByAdult.FullName + "</em>";
                }
                beltMessage += "</p>";
                messages.Add(beltMessage);
            }

            // Find badges awarded
            var newBadges = from mb in this.MemberBadges
                           where mb.Awarded >= (LoginDatePrevious ?? DateTime.Today.ToUniversalTime()).Date
                           orderby mb.Badge.BadgeCategory.CategoryName, mb.Badge.Achievement
                           select mb;
            foreach (var mb in newBadges)
            {
                string badgeMessage = "<p><strong>You have been awarded the " + mb.Badge.BadgeCategory.CategoryName + " - " + mb.Badge.Achievement + " badge.</strong>";
                if (!string.IsNullOrEmpty(mb.AwardedNotes))
                {
                    badgeMessage += "<br /><em>" + mb.AwardedNotes + " - " + mb.AwardedByAdult.FullName + "</em>";
                }
                badgeMessage += "</p>";
                messages.Add(badgeMessage);
            }

            // Find badges rejected
            // exclude badges that have been re-applied for
            var appliedBadgeIds = from mb in this.MemberBadges
                                  where mb.RejectedDate == null && mb.Awarded == null
                                  select mb.BadgeId;
            var rejectedBadges = from mb in this.MemberBadges
                            where mb.RejectedDate >= (LoginDatePrevious ?? DateTime.Today.ToUniversalTime()).Date
                            && !appliedBadgeIds.Contains(mb.BadgeId)
                            orderby mb.Badge.BadgeCategory.CategoryName, mb.Badge.Achievement
                            select mb;
            foreach (var mb in rejectedBadges)
            {
                string badgeMessage = "<p><strong>Your application for the " + mb.Badge.BadgeCategory.CategoryName + " - " + mb.Badge.Achievement + " badge has been rejected. You can apply again any time.</strong>";
                if (!string.IsNullOrEmpty(mb.RejectedNotes))
                {
                    badgeMessage += "<br /><em>" + mb.RejectedNotes + " - " + mb.RejectedByAdult.FullName + "</em>";
                }
                badgeMessage += "</p>";
                messages.Add(badgeMessage);
            }

            string html = "";
            foreach (string message in messages)
            {
                if (message.StartsWith("<"))
                {
                    html += message;
                }
                else
                {
                    html += "<p>" + message + "</p>";
                }
            }
            return html;
        }
    }
}
