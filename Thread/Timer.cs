﻿using System;
using System.Timers;

namespace Notus.Threads
{
    public class Timer : IDisposable
    {
        private System.Action? DefinedFunctionObj;
        private System.Timers.Timer? InnerTimerObject;
        private bool TimerStarted = false;
        private int IntervalTimeValue = 5000;
        public int Interval
        {
            get
            {
                return IntervalTimeValue;
            }
            set
            {
                IntervalTimeValue = value;
            }
        }
        public Timer()
        {
        }
        public Timer(int TimerInterval)
        {
            IntervalTimeValue = TimerInterval;
        }
        public void Close()
        {
            Kill();
        }
        public void Kill()
        {
            DefinedFunctionObj = null;
            if (TimerStarted == true)
            {
                if (InnerTimerObject != null)
                {
                    try
                    {
                        InnerTimerObject.Stop();
                        InnerTimerObject.Dispose();
                    }
                    catch { }
                }
                InnerTimerObject = null;
            }
        }
        public void SetInterval(int newInterval)
        {
            if(IntervalTimeValue!= newInterval)
            {
                IntervalTimeValue = newInterval;
                //Console.WriteLine("before : " + InnerTimerObject.Interval.ToString());
                InnerTimerObject.Interval = newInterval;
                //Console.WriteLine("after  : " + InnerTimerObject.Interval.ToString());
            }
        }
        private void SubStart(System.Action incomeAction)
        {
            DefinedFunctionObj = incomeAction;
            InnerTimerObject = new System.Timers.Timer(IntervalTimeValue);
            InnerTimerObject.Elapsed += OnTimedEvent_ForScreen;
            InnerTimerObject.AutoReset = true;
            InnerTimerObject.Enabled = true;
            TimerStarted = true;
            //aTimer.Start();
        }
        public void Start(int interval, System.Action incomeAction, bool executeImmediately)
        {
            IntervalTimeValue = interval;
            Start(incomeAction, executeImmediately);
        }
        public void Start(int interval, System.Action incomeAction)
        {
            IntervalTimeValue = interval;
            Start(incomeAction);
        }
        public void Start(System.Action incomeAction)
        {
            SubStart(incomeAction);
        }
        public void Start(System.Action incomeAction, bool executeImmediately)
        {
            SubStart(incomeAction);
            if (executeImmediately == true)
            {
                DefinedFunctionObj();
            }
        }
        private void OnTimedEvent_ForScreen(Object source, ElapsedEventArgs e)
        {
            if (DefinedFunctionObj != null)
            {
                DefinedFunctionObj();
            }
        }
        public void Dispose()
        {
            Kill();
        }
        ~Timer()
        {
            //Dispose(true);
        }
    }
}
