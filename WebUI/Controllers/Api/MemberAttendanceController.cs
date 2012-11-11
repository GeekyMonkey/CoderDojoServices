using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace CoderDojo.Controllers.Api
{
    public class MemberAttendanceController : ApiController
    {
        private CoderDojoData db = new CoderDojoData();

        // GET api/MemberAttendance
        public IEnumerable<MemberAttendance> GetMemberAttendances()
        {
            var memberattendances = db.MemberAttendances.Include(m => m.Member);
            return memberattendances.AsEnumerable();
        }

        // GET api/MemberAttendance/5
        public MemberAttendance GetMemberAttendance(Guid id)
        {
            MemberAttendance memberattendance = db.MemberAttendances.Find(id);
            if (memberattendance == null)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }

            return memberattendance;
        }

        // PUT api/MemberAttendance/5
        public HttpResponseMessage PutMemberAttendance(Guid id, MemberAttendance memberattendance)
        {
            if (ModelState.IsValid && id == memberattendance.Id)
            {
                db.Entry(memberattendance).State = EntityState.Modified;

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        // POST api/MemberAttendance
        public HttpResponseMessage PostMemberAttendance(MemberAttendance memberattendance)
        {
            if (ModelState.IsValid)
            {
                db.MemberAttendances.Add(memberattendance);
                db.SaveChanges();

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created, memberattendance);
                response.Headers.Location = new Uri(Url.Link("DefaultApi", new { id = memberattendance.Id }));
                return response;
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        // DELETE api/MemberAttendance/5
        public HttpResponseMessage DeleteMemberAttendance(Guid id)
        {
            MemberAttendance memberattendance = db.MemberAttendances.Find(id);
            if (memberattendance == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            db.MemberAttendances.Remove(memberattendance);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return Request.CreateResponse(HttpStatusCode.OK, memberattendance);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}