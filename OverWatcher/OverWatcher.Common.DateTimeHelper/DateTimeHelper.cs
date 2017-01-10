using System;
using System.Configuration;
using OverWatcher.Common.Logging;

namespace OverWatcher.Common
{
    public class DateTimeHelper
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

        public static DateTime DateTimeLocalToSpecifiedZone(System.DateTime dt)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(TimeZoneInfo.ConvertTimeToUtc(dt), SpecifiedTimeZone);
        }
        public static DateTime ZoneNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.UtcNow, SpecifiedTimeZone);
        }
    }
}
