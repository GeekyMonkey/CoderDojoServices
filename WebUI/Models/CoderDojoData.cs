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
            byte[] bytes = encoding.GetBytes(password);
            string passwordHash = Encoding.UTF8.GetString(encryptor.ComputeHash(bytes));
            return passwordHash;
        }

        /// <summary>
        /// Get a list of all of the coder dojo sessions that have been held
        /// </summary>
        /// <returns></returns>
        public IQueryable<DateTime> GetSessionDates()
        {
            IQueryable<DateTime> dates = this.MemberAttendances
                .Select(ma => ma.Date)
                .Distinct()
                .OrderBy(d => d);
            return dates;
        }
    }
}
