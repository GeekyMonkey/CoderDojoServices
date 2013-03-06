using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace CoderDojo
{

    public class AttendanceHub : Hub
    {
        public void OnAttendanceChange(DateTime attendanceDate, Guid memberId, bool present, int sessionCount)
        {
            // Call te broadcastMessage method to update clients.
            Clients.Others.OnAttendanceChange(attendanceDate.ToString("yyyy-MM-dd"), memberId.ToString("N"), present.ToString().ToLower(), sessionCount);
        }
    }
}
