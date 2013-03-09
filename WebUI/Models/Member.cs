using System;
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
                messages.Add("<h3>Welcome " + FirstName + ".</h3>");
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
                string beltMessage = "<strong>You have been awareded the <span style='color:" + mb.Belt.HexCode + ";'>" + mb.Belt.Color + "</span> belt!</strong>";
                if (!string.IsNullOrEmpty(mb.AwardedNotes))
                {
                    beltMessage += "<br /><blockquote>" + mb.AwardedNotes + " - " + mb.AwardedByAdult.FullName + "</blockquote>";
                }
                messages.Add(beltMessage);
            }

            //ToDo: Find badges awarded

            string html = "";
            foreach (string message in messages)
            {
                html += "<p>" + message + "</p>";
            }
            return html;
        }
    }
}
