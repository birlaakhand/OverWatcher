using System;
using System.Collections.Generic;
using System.Linq;
using OverWatcher.Common.Logging;

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
        private readonly Frequency _freq;
        private readonly Skip _skip;
        private readonly System.DateTime _frequencyValue;
        private List<System.DateTime> _skipValue;

        public DateTime NextRun { get; set; }
        public bool IsOnTime(System.DateTime dt)
        {
            if (IsSkip(dt) || dt < NextRun) return false;
            CalculateNextTime();
            CalculateNextSkip();
            return true;
        }
        public bool IsSingleRun()
        {
            return _freq == Frequency.NONE ? true : false;
        }
        public Schedule(string frequency, string frequencyValue,
                                                string skip, string skipValue)
        {
            this._skipValue = new List<System.DateTime>();
            try
            {
                _freq = (Frequency)Enum.Parse(typeof(Frequency), frequency.ToUpper());
                this._skip = (Skip)Enum.Parse(typeof(Skip), skip.ToUpper());
                CalculateSkip(skipValue);
                this._frequencyValue = ParseAmbiguousDateString(frequencyValue);
                NextRun = DateTimeHelper.DateTimeHelper.ZoneNow;
            }
            catch(Exception ex)
            {
                Logger.Warn(ex.Message + ", Disable Scheduler");
            }
        }

        private bool IsSkip(System.DateTime dt)
        {
            if (this._skip == Skip.DAYOFWEEK)
            {
                return _skipValue.Any(s => s.DayOfWeek == dt.DayOfWeek);
            }
            return false;
        }
        private void CalculateNextSkip()
        {
            // To Be Implemented
        }
        private void CalculateSkip(string skipValue)
        {
            if (this._skip == Skip.DAYOFWEEK)
            {
                this._skipValue = skipValue
                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => DateTime.MinValue.AddDays((int)ParseDayofweek(s) + 6)).ToList();
            }

        }

        private void CalculateNextTime()
        {
            if (_freq == Frequency.REPEATLY)
            {
                NextRun = NextRun.AddMilliseconds(_frequencyValue.Subtract(DateTime.MinValue).TotalMilliseconds);
            }
            while(IsSkip(NextRun))
            {
                if (this._skip == Skip.DAYOFWEEK)
                {
                    NextRun = NextRun.AddDays(1);
                }
            }
        }

        private static DateTime ParseAmbiguousDateString(string date)
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
                return System.DateTime.ParseExact(date, "yyyy/MM/dd/hh:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch(Exception ex)
            {
                throw new ArgumentException("Scheduler:frequency value parse error - " + ex.Message);
            }
            
        }
        private static DayOfWeek ParseDayofweek(string dow)
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
