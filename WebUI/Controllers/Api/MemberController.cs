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
    public class MemberController : ApiController
    {
        private CoderDojoData db = new CoderDojoData();

        // GET api/Member
        public IEnumerable<Member> GetMembers()
        {
            return db.Members.ToList();
        }

        // GET api/Member/5
        public Member GetMember(Guid id)
        {
            Member member = db.Members.Find(id);
            if (member == null)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }

            return member;
        }

        // PUT api/Member/5
        public HttpResponseMessage PutMember(Guid id, Member member)
        {
            if (ModelState.IsValid && id == member.Id)
            {
                db.Entry(member).State = EntityState.Modified;

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

        // POST api/Member
        public HttpResponseMessage PostMember(Member member)
        {
            if (ModelState.IsValid)
            {
                db.Members.Add(member);
                db.SaveChanges();

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created, member);
                response.Headers.Location = new Uri(Url.Link("DefaultApi", new { id = member.Id }));
                return response;
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        // DELETE api/Member/5
        public HttpResponseMessage DeleteMember(Guid id)
        {
            Member member = db.Members.Find(id);
            if (member == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            db.Members.Remove(member);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return Request.CreateResponse(HttpStatusCode.OK, member);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}