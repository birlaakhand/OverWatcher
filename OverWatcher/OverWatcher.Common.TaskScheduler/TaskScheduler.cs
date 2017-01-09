using System;
using System.Collections.Generic;
using OverWatcher.Common.Log;
using System.Timers;

namespace OverWatcher.Common.Scheduler
{
    public class TaskScheduler
    {
        System.Timers.Timer timer;
        public delegate void TaskDelegate();
        private Dictionary<TaskDelegate, Schedule> task;
        public TaskScheduler(int interval)
        {
            timer = new System.Timers.Timer(interval);
            task = new Dictionary<TaskDelegate, Schedule>();
        }
        public void Start()
        {
            ElapsedEventHandler handler = new ElapsedEventHandler(TaskEventHandler);
            timer.Elapsed += handler;
            timer.Start();
            handler.BeginInvoke(this, null, new AsyncCallback(Timer_ElapsedCallback), handler);
        }
        private void Timer_ElapsedCallback(IAsyncResult result)
        {
            ElapsedEventHandler handler = result.AsyncState as ElapsedEventHandler;
            if (handler != null)
            {
                handler.EndInvoke(result);
            }
        }

        private void TaskEventHandler(object sender, ElapsedEventArgs e)
        {
            foreach (KeyValuePair<TaskDelegate, Schedule> pair in task)
            {
                System.DateTime now = System.DateTime.Now;
                if (pair.Value.isOnTime(now))
                {
                    Log.Logger.Info(string.Format("Task Start",
                                now.ToString("MM/dd/yyyy hh:mm")));
                    pair.Key.Invoke();
                    Log.Logger.Info("Task Run Finished, Next Run at " + pair.Value.NextRun.ToString());
                }
            }
        }
        public void Stop()
        {
            timer.Stop();
        }
        public void AddTask(TaskDelegate d, Schedule s)
        {
            task[d] = s;
        }
        public void RemoveTask(TaskDelegate d)
        {
            task.Remove(d);
        }
    }
}
