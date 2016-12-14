using Microsoft.AspNet.SignalR;
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

        // POST api/MemberAttendance/Fingerprint?id=3&testing=false
        [HttpGet, ActionName("Fingerprint")]
        public HttpResponseMessage GetFingerprint(int id, bool testing = false)
        {
            var member = db.Members.FirstOrDefault(m => m.FingerprintId == id);
            if (member != null)
            {
                member.SetLoginDate();
                db.SaveChanges();

                DateTime sessionDate = DateTime.Today;
                int sessionCount;
                if (testing)
                {
                    // Testing fingerprint feature only - don't do actual sign-in
                    sessionCount = db.MemberAttendances.Count(ma => ma.MemberId == member.Id);
                }
                else
                {
                    // Do a real sign-in
                    sessionCount = db.AttendanceSet(member.Id, true, sessionDate);
                    int dojoAttendanceCount = db.MemberAttendances.Count(ma => ma.Date == sessionDate);
                    // Notify other members looking at this screen
                    IHubContext context = GlobalHost.ConnectionManager.GetHubContext<AttendanceHub>();
                    context.Clients.All.OnAttendanceChange(sessionDate.ToString("dd-MMM-yyyy"), member.Id.ToString("N"), member.MemberName, (member.TeamId ?? Guid.Empty).ToString("N"), true.ToString().ToLower(), sessionCount, dojoAttendanceCount, "", member.ImageUrl);
                }
                string message = member.GetLoginMessage();

                var responseObject = new SignInResponse
                {
                    memberId = member.Id.ToString("N"),
                    memberName = member.MemberName,
                    memberSessionCount = sessionCount,
                    memberMessage = message
                };

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, responseObject);
                return response;
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Clean up
        /// </summary>
        /// <param name="disposing">Is Disposing</param>
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}