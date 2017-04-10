using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using OverWatcher.Common.Logging;
using System.Timers;

namespace OverWatcher.Common.Scheduler
{
    public class TaskScheduler
    {
        private readonly System.Timers.Timer _timer;
        public delegate void TaskDelegate();
        private readonly Dictionary<TaskDelegate, Schedule> _task;
        private readonly ConcurrentDictionary<TaskDelegate, Thread> _threadMonitor;
        public TaskScheduler(int interval)
        {
            _timer = new System.Timers.Timer(interval);
            _task = new Dictionary<TaskDelegate, Schedule>();
        }
        public void Start()
        {
            ElapsedEventHandler handler = TaskEventHandler;
            _timer.Elapsed += handler;
            _timer.Start();
            handler.BeginInvoke(this, null, r => {
                ElapsedEventHandler h = r.AsyncState as ElapsedEventHandler;
                h?.EndInvoke(r);
            }, handler);
        }

        private void TaskEventHandler(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (KeyValuePair<TaskDelegate, Schedule> pair in _task)
                {
                    System.DateTime now = System.DateTime.Now;
                    if (!pair.Value.IsOnTime(now)) continue;
                    Logger.Info("Task Start at " + now.ToString("MM/dd/yyyy hh:mm"));
                    pair.Key.Invoke();
                    Logger.Info("Task Run Finished, Next Run at " + pair.Value.NextRun.ToString(CultureInfo.CurrentCulture));
                }
            }
            catch (Exception exception)
            {
                Logger.Fatal(exception, "Scheduler Fatal Error");
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    throw new Exception("Scheduler Fatal Error", exception);
                });
            }

        }
        public void Stop()
        {
            _timer.Stop();
        }
        public void AddTask(TaskDelegate d, Schedule s)
        {
            _task[d] = s;
        }
        public void RemoveTask(TaskDelegate d)
        {
            _task.Remove(d);
        }
    }
}
