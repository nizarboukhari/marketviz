using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Workers.Common
{
    public static class DateExtension
    {
        public static DateTime ToDateTime(this long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp);
            return dateTime;
        }
    }
}
