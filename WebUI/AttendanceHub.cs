﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace CoderDojo
{

    public class AttendanceHub : Hub
    {
        public void OnAttendanceChange(DateTime attendanceDate, Guid memberId, string memberName, string teamId, bool present, int memberSessionCount, int dojoAttendanceCount, string memberMessage)
        {
            // Call te broadcastMessage method to update clients.
            Clients.Others.OnAttendanceChange(attendanceDate.ToString("yyyy-MM-dd"), memberId.ToString("N"), memberName,  teamId, present.ToString().ToLower(), memberSessionCount, dojoAttendanceCount, memberMessage);
        }
    }
}
