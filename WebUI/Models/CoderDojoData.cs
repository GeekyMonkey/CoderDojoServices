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
        public string GeneratePasswordHash(string password)
        {
            MD5 encryptor = MD5.Create();
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] bytes = encoding.GetBytes(password);
            string passwordHash = Encoding.UTF8.GetString(encryptor.ComputeHash(bytes));
            return passwordHash;
        }
    }
}