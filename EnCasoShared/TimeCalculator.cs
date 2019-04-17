using System;
using System.Collections.Generic;

namespace EnCasoShared
{
    public class TimeCalculator
    {
        public static int GetSecondsUntilNextCheck(DateTime next)
        {
            DateTime now = DateTime.Now;
            DateTime nextInstance;
            now = now.AddSeconds(now.Second * -1);
            DateTime todayAtTime = now.Date.AddHours(next.Hour).AddMinutes(next.Minute);
            nextInstance = now < todayAtTime ? todayAtTime : todayAtTime.AddDays(1);
            //if (nextInstance.DayOfWeek == DayOfWeek.Saturday)
            //{
            //    nextInstance = nextInstance.AddDays(1);
            //}
            //if (nextInstance.DayOfWeek == DayOfWeek.Sunday)
            //{
            //    nextInstance = nextInstance.AddDays(1);
            //}
            TimeSpan span = nextInstance - now;

            return (int)span.TotalSeconds;
        }
    }
}
