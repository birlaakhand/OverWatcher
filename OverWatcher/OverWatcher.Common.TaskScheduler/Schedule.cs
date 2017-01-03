using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.Common.Scheduler
{
    enum Frequency
    {
        NONE, REPEATLY, MINUTELY, HOURLY, DAILY, MONTHLY
    }
    enum Skip
    {
        DAYOFWEEK
    }
    public class Schedule
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Frequency freq;
        private Skip skip;
        private DateTime frequencyValue;
        private List<DateTime> skipValue;

        public DateTime NextRun { get; set; }
        static string format = "MM/dd/hh:mm:ss.fff";
        public bool isOnTime(DateTime dt)
        {
            if (isSkip(dt) || dt < NextRun) return false;
            CalculateNextTime();
            CalculateSkip();
            return true;
        }
        public bool isSingleRun()
        {
            return freq == Frequency.NONE ? true : false;
        }
        public Schedule(string frequency, string frequencyValue,
                                                string skip, string skipValue)
        {
            this.skipValue = new List<DateTime>();
            try
            {
                freq = (Frequency)Enum.Parse(typeof(Frequency), frequency.ToUpper());
                this.skip = (Skip)Enum.Parse(typeof(Skip), skip.ToUpper());
                if (this.skip == Skip.DAYOFWEEK)
                {
                    this.skipValue = skipValue.Split(new char[]{';'}).Select(s => DateTime.MinValue.AddDays((int)ParseDayofweek(s))).ToList();
                }
                else
                {
                    this.frequencyValue = ParseAmbiguousDateString(frequencyValue);
                }
                NextRun = DateTime.Now;
            }
            catch(Exception ex)
            {
                log.Warn(ex.Message + ", Disable Scheduler");
            }
        }

        private bool isSkip(DateTime dt)
        {
            if (this.skip == Skip.DAYOFWEEK)
            {
                return skipValue.Any(s => s.DayOfWeek == dt.DayOfWeek);
            }
            return false;
        }
        private void CalculateSkip()
        {

        }
        private void CalculateNextTime()
        {
            if (freq == Frequency.REPEATLY)
            {
                NextRun = NextRun.AddMilliseconds(frequencyValue.Subtract(DateTime.MinValue).TotalMilliseconds);
            }
        }
        private DateTime ParseAmbiguousDateString(string date)
        {
            if (date.Length != format.Length) throw new ArgumentException("Scheduler:frequency value parse error");
                char[] formatArr = format.ToCharArray();
            char[] dateArr = date.ToCharArray();
            for (int i = 0; i < format.ToCharArray().Length; ++i)
            {
                if (formatArr[i] != dateArr[i]) return DateTime.ParseExact(date, format.Substring(i), System.Globalization.CultureInfo.InvariantCulture);
            }
            throw new ArgumentException("Scheduler:frequency value parse error");
        }
        private DayOfWeek ParseDayofweek(string dow)
        {
            foreach(DayOfWeek d in Enum.GetValues(typeof(DayOfWeek)))
            {
                if(d.ToString().Substring(0, 3) == dow)
                {
                    return d;
                }
            }
            throw new ArgumentException("Scheduler:Day of week parse error");
        }
    }
}
