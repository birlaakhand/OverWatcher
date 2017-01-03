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
        public bool isOnTime(DateTime dt)
        {
            if (isSkip(dt) || dt < NextRun) return false;
            CalculateNextTime();
            CalculateNextSkip();
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
                CalculateSkip(skipValue);
                this.frequencyValue = ParseAmbiguousDateString(frequencyValue);
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
        private void CalculateNextSkip()
        {

        }
        private void CalculateSkip(string skipValue)
        {
            if (this.skip == Skip.DAYOFWEEK)
            {
                this.skipValue = skipValue.Split(new char[] { ';' }).Select(s => DateTime.MinValue.AddDays((int)ParseDayofweek(s))).ToList();
            }

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
            try
            {               
                date = date.Replace("yyyy", "0001");
                date = date.Replace("MM", "01");
                date = date.Replace("dd", "01");
                date = date.Replace("hh", "00");
                date = date.Replace("mm", "00");
                date = date.Replace("ss", "00");
                date = date.Replace("fff", "000");
                return DateTime.ParseExact(date, "yyyy/MM/dd/hh:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch(Exception ex)
            {
                throw new ArgumentException("Scheduler:frequency value parse error - " + ex.Message);
            }
            
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
