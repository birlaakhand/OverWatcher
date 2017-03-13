using System;
using System.Configuration;
using OverWatcher.Common.Logging;

namespace OverWatcher.Common
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo SpecifiedTimeZone;
        static DateTimeHelper()
        {
            try
            {
                if (ConfigurationManager.AppSettings["TimeZone"] != null)
                {
                    SpecifiedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ConfigurationManager.AppSettings["TimeZone"]);
                    Logger.Info("TimeZone Set to " + SpecifiedTimeZone.Id);
                }

                else
                {
                    Logger.Info("TimeZone Set to Local");
                }

            }
            catch (Exception ex)
            {
                Logger.Warn("TimeZone Format is Wrong, Set to Local Detail: " + ex.Message);
            }
            SpecifiedTimeZone = TimeZoneInfo.Local;
        }

        public static DateTime DateTimeLocalToSpecifiedZone(this System.DateTime dt)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(TimeZoneInfo.ConvertTimeToUtc(dt), SpecifiedTimeZone);
        }
        public static DateTime ZoneNow
        {
            get
            {
                return TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.UtcNow, SpecifiedTimeZone);
            }
            
        }

        public static DateTime AddWorkingDays(this DateTime dt, double value)
        {
            dt = dt.AddDays(value);
            if(dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
            {
                if(value < 0)
                {
                    dt.AddDays(Math.Sign(value) * ((int)dt.DayOfWeek - 5) % 7);
                }
                else
                {
                    dt.AddDays(7 - ((int)dt.DayOfWeek - 1) % 7);
                }
            }
            return dt;
        }
    }
}
