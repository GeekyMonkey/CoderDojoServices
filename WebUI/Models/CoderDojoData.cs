using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace CoderDojo
{
    public partial class CoderDojoData
    {
        /// <summary>
        /// Generate a password hash
        /// </summary>
        /// <param name="password">Input password</param>
        /// <returns>Hashed string</returns>
        public string GeneratePasswordHash(string password)
        {
            MD5 encryptor = MD5.Create();
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] bytes = encoding.GetBytes(password ?? string.Empty);
            string passwordHash = Encoding.UTF8.GetString(encryptor.ComputeHash(bytes));
            return passwordHash;
        }

        /// <summary>
        /// Get a list of all of the coder dojo sessions that have been held
        /// </summary>
        /// <returns></returns>
        public IList<DateTime> GetSessionDates(DateTime? DateToInclude)
        {
            List<DateTime> dates = this.MemberAttendances
                .Select(ma => ma.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
            if (DateToInclude != null && !dates.Contains(DateToInclude.Value))
            {
                dates.Add(DateToInclude.Value);
            }
            return dates;
        }

        public int AttendanceSet(Guid memberId, bool present, DateTime sessionDate)
        {
            MemberAttendance attendance = this.MemberAttendances
                .Where(ma => ma.MemberId == memberId && ma.Date == sessionDate)
                .FirstOrDefault();
            bool hasAttendance = (attendance != null);
            if (present == true && hasAttendance == false)
            {
                attendance = new MemberAttendance
                {
                    Id = Guid.NewGuid(),
                    MemberId = memberId,
                    Date = sessionDate
                };
                this.MemberAttendances.Add(attendance);
                this.SaveChanges();
            }
            else if (present == false && hasAttendance == true)
            {
                this.MemberAttendances.Remove(attendance);
                this.SaveChanges();
            }
            return this.MemberAttendances.Count(ma => ma.MemberId == memberId);
        }

    }
}
