using System;
using System.Collections.Generic;
using UnityEngine;

public class Scheduler:MonoSingleton<Scheduler>,IManager
{
    private LinkedList<Timer> timers=new LinkedList<Timer>();

    public event System.Action UpdateEvent;

    public Timer Wait(float delay, short repeatTimes, Action task)
    {
        Timer timer = new Timer(delay, repeatTimes, task);
        timer.ListNode = this.timers.AddLast(timer);
        return timer;
    }

    public Timer Wait(float delay, Action task)
    {
        Timer timer = new Timer(delay, 1, task);
        timer.ListNode = this.timers.AddLast(timer);
        return timer;
    }

    /// <remarks>无限循环</remarks>
    public Timer Repeat(float delay, Action task)
    {
        Timer timer = new Timer(delay, -1, task);
        timer.ListNode = this.timers.AddLast(timer);
        return timer;
    }

    public void Stop(Timer timer)
    {
        if (timer != null && timer.ListNode != null && this.timers.Contains(timer))
        {
            this.timers.Remove(timer.ListNode);
            timer.ListNode = null;
        }
    }

    /// <remarks>重启定时器</remarks>
    public void ReStart(Timer timer)
    {
        timer.ResetTime();
        if (timer.ListNode == null)
        {
            timer.ListNode = this.timers.AddLast(timer);
        }
    }

    public void Update()
    {
        if (UpdateEvent != null)
        {
            this.UpdateEvent();
        }
        updateTimers();
    }

    /// <summary>
    /// 定时器刷新
    /// </summary>
    private void updateTimers()
    {
        if (this.timers == null || this.timers.Count <= 0)
        {
            return;
        }
        var i = this.timers.First;
        while (null != i)
        {
            var next = i.Next;
            var value = i.Value;
            if (value.Update(Time.deltaTime))
            {
                if (value.ListNode != null)
                {
                    this.timers.Remove(value.ListNode);
                    value.ListNode = null;
                }
            }
            i = next;
        }
    }


}