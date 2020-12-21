using System;
using System.Collections.Generic;

/// <remarks>定时器组件，设置延迟触发</remarks>
public class Timer
{
    /// <remarks>记录定时器原始延时数据</remarks>
    private float delay;
    /// <remarks>
    /// 默认状态为1
    /// &gt;1时为指定的循环次数，
    /// =-1时为无限次重复
    /// </remarks>
    private short repetTimes;
    /// <remarks>真实的剩余时间</remarks>
    private float residueTime;
    /// <remarks>剩余重复次数</remarks>
    private int residueRepetTimes;

    public LinkedListNode<Timer> ListNode;

    private event System.Action task;

    public Timer(float delay, short repetTimes, System.Action task)
    {
        this.delay = delay;
        this.repetTimes = repetTimes;
        this.task = task;
        this.residueRepetTimes = this.repetTimes;
        this.residueTime = this.delay;
    }

    public void ResetTime()
    {
        this.residueTime = this.delay;
        this.residueRepetTimes = this.repetTimes;
    }

    public bool Update(float deltaTime)
    {
        this.residueTime -= deltaTime;
        if (this.residueTime <= 0f)
        {
            try
            {
                this.task();
            }
            catch (Exception e)
            {
                Helper.LogError(e.ToString());
            }
            if (this.repetTimes > 0)
            {
                this.residueRepetTimes--;
                if (this.residueRepetTimes <= 0)
                {
                    return true;
                }
            }
            else if (this.repetTimes == -1)
            {
                this.residueTime = this.delay;
            }
            else
            {
                return true;
            }
        }
        return false;
    }
}