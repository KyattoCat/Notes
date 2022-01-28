using System;

public class TimeTask
{
    public int taskID;
    public float delay;
    public float destTime;

    public Action callback;
    public int callCount;

    public TimeTask(int taskID, float delay, float destTime, Action callback, int count)
    {
        this.taskID = taskID;
        this.delay = delay;
        this.destTime = destTime;
        this.callback = callback;
        this.callCount = count;
    }
}
public class FrameTask
{
    public int taskID;
    public int delayFrame;
    public int destFrame;

    public Action callback;
    public int callCount;

    public FrameTask(int taskID, int delay, int destTime, Action callback, int count)
    {
        this.taskID = taskID;
        this.delayFrame = delay;
        this.destFrame = destTime;
        this.callback = callback;
        this.callCount = count;
    }
}

public enum TimeUnit
{
    Millisecond,
    Second,
    Minute,
    Hour,
    Day
}