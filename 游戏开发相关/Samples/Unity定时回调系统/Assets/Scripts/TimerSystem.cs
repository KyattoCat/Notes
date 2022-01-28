using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TimerSystem : MonoBehaviour
{
    public static TimerSystem Instance;
    private readonly object taskIDLock = new object();
    private int taskID = 0;
    private List<int> taskIDList = new List<int>();
    private List<TimeTask> tempTimeTaskList = new List<TimeTask>();
    private List<TimeTask> timeTaskList = new List<TimeTask>();
    private int currentFrame = 0;
    private List<FrameTask> tempFrameTaskList = new List<FrameTask>();
    private List<FrameTask> frameTaskList = new List<FrameTask>();

    public void Init()
    {
        Instance = this;
    }

    private void Update()
    {
        UpdateCurrentFrame();
        CheckFrameTask();
        CheckTimeTask();
    }
    
    private void CheckTimeTask()
    {
        // 取出缓存
        for (int index = 0; index < tempTimeTaskList.Count; index++)
        {
            timeTaskList.Add(tempTimeTaskList[index]);
        }
        tempTimeTaskList.Clear();

        // 需要移除的任务id列表
        List<int> needToRemoveTaskID = new List<int>();
        // 遍历检测任务是否达到条件
        for (int index = 0; index < timeTaskList.Count; index++)
        {
            TimeTask timeTask = timeTaskList[index];
            if (timeTask.destTime > Time.realtimeSinceStartup * 1000)
            {
                continue;
            }
            else
            {
                Action callback = timeTask.callback;
                try
                {
                    if (callback != null)
                    {
                        Debug.Log(string.Format("TimeTask[{0}] is running", timeTask.taskID));
                        // 但是如果callback很费时间怎么办
                        callback();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + " " + e.StackTrace);
                }
                // 不是无限循环
                if (timeTask.callCount != -1)
                {
                    timeTask.callCount--;
                }
                // 执行一定次数后移除
                if (timeTask.callCount == 0)
                {
                    needToRemoveTaskID.Add(taskID);
                }
                else
                {
                    timeTask.destTime = Time.realtimeSinceStartup * 1000 + timeTask.delay;
                }
            }
        }
        // 统一移除
        for (int index = 0; index < needToRemoveTaskID.Count; index++)
        {
            // 这种用for循环来移除 效率好吗
            RemoveTimeTaskByTaskID(needToRemoveTaskID[index]);
            RemoveTaskID(needToRemoveTaskID[index]);
        }
    }
    private void CheckFrameTask()
    {
        // 取出缓存
        for (int index = 0; index < tempFrameTaskList.Count; index++)
        {
            frameTaskList.Add(tempFrameTaskList[index]);
        }
        tempFrameTaskList.Clear();

        // 需要移除的任务id列表
        List<int> needToRemoveTaskID = new List<int>();
        // 遍历检测任务是否达到条件
        for (int index = 0; index < frameTaskList.Count; index++)
        {
            FrameTask frameTask = frameTaskList[index];
            if (frameTask.destFrame > currentFrame)
            {
                continue;
            }
            else
            {
                Action callback = frameTask.callback;
                try
                {
                    if (callback != null)
                    {
                        Debug.Log(string.Format("FrameTask[{0}] is running", frameTask.taskID));
                        // 但是如果callback很费时间怎么办
                        callback();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + " " + e.StackTrace);
                }
                // 不是无限循环
                if (frameTask.callCount != -1)
                {
                    frameTask.callCount--;
                }
                // 执行一定次数后移除
                if (frameTask.callCount == 0)
                {
                    needToRemoveTaskID.Add(taskID);
                }
                else
                {
                    frameTask.destFrame += frameTask.delayFrame;
                }
            }
        }
        // 统一移除
        for (int index = 0; index < needToRemoveTaskID.Count; index++)
        {
            // 这种用for循环来移除 效率好吗
            RemoveFrameTaskByTaskID(needToRemoveTaskID[index]);
            RemoveTaskID(needToRemoveTaskID[index]);
        }
    }
    private void UpdateCurrentFrame()
    {
        currentFrame += 1;
        if (currentFrame == int.MaxValue)
        {
            currentFrame = 0;
        }
    }
    
    #region TimeTask
    /// <summary>
    /// 添加一个定时任务
    /// </summary>
    /// <param name="callback">回调函数</param>
    /// <param name="delay">延时时间</param>
    /// <param name="timeUnit">时间单位 默认毫秒</param>
    /// <param name="count">执行次数 默认为1</param>
    public int AddTimeTask(Action callback, float delay, TimeUnit timeUnit = TimeUnit.Millisecond, int count = 1)
    {
        delay = ConvertToMillisecond(timeUnit, delay);
        // 计算什么时候执行任务
        float destTime = GetDestinationTime(delay);
        int taskID = GetID();
        TimeTask timeTask = new TimeTask(taskID, delay, destTime, callback, count);
        // 添加进入任务缓存列表
        // 确保新任务在下一帧才被加入检测列表
        // 保证检测时不会出现某些奇妙的问题
        tempTimeTaskList.Add(timeTask);
        taskIDList.Add(taskID);
        return taskID;
    }
    /// <summary>
    /// 对外开放 通过任务ID移除定时任务
    /// </summary>
    /// <param name="taskID">任务ID</param>
    /// <returns>移除是否成功</returns>
    public bool DeleteTimeTask(int taskID)
    {
        if (!CheckTaskIDIsValid(taskID))
        {
            return RemoveTimeTaskByTaskID(taskID) && RemoveTaskID(taskID);
        }
        return false;
    }
    
    /// <summary>
    /// 替换一个定时任务
    /// </summary>
    /// <param name="taskID">被替换的任务ID</param>
    /// <param name="callback">回调函数</param>
    /// <param name="delay">延时时间</param>
    /// <param name="timeUnit">时间单位 默认毫秒</param>
    /// <param name="count">执行次数 默认为1</param>
    public bool ReplaceTimeTask(int taskID, Action callback, float delay, TimeUnit timeUnit = TimeUnit.Millisecond, int count = 1)
    {
        if (DeleteTimeTask(taskID) || DeleteFrameTask(taskID))
        {
            delay = ConvertToMillisecond(timeUnit, delay);
            float destTime = GetDestinationTime(delay);
            TimeTask timeTask = new TimeTask(taskID, delay, destTime, callback, count);

            tempTimeTaskList.Add(timeTask);
            taskIDList.Add(taskID);
            return true;
        }
        return false;
    }
    #endregion
    
    #region FrameTask
    /// <summary>
    /// 添加一个帧定时任务
    /// </summary>
    /// <param name="callback">回调函数</param>
    /// <param name="delayFrame">延时帧数</param>
    /// <param name="count">执行次数 默认为1</param>
    public int AddFrameTask(Action callback, int delayFrame, int count = 1)
    {
        // 计算什么时候执行任务
        int destFrame = GetDestinationFrame(delayFrame);
        int taskID = GetID();
        FrameTask frameTask = new FrameTask(taskID, delayFrame, destFrame, callback, count);
        // 添加进入任务缓存列表
        // 确保新任务在下一帧才被加入检测列表
        // 保证检测时不会出现某些奇妙的问题
        tempFrameTaskList.Add(frameTask);
        taskIDList.Add(taskID);
        return taskID;
    }
    /// <summary>
    /// 对外开放 通过任务ID移除帧定时任务
    /// </summary>
    /// <param name="taskID">任务ID</param>
    /// <returns>移除是否成功</returns>
    public bool DeleteFrameTask(int taskID)
    {
        if (!CheckTaskIDIsValid(taskID))
        {
            return RemoveFrameTaskByTaskID(taskID) && RemoveTaskID(taskID);
        }
        return false;
    }
    /// <summary>
    /// 替换一个帧定时任务
    /// </summary>
    /// <param name="taskID">被替换的任务ID</param>
    /// <param name="callback">回调函数</param>
    /// <param name="delayFrame">延时帧数</param>
    /// <param name="count">执行次数 默认为1</param>
    public bool ReplaceFrameTask(int taskID, Action callback, int delayFrame, int count = 1)
    {
        if (DeleteTimeTask(taskID) || DeleteFrameTask(taskID))
        {
            // 计算什么时候执行任务
            int destFrame = GetDestinationFrame(delayFrame);
            FrameTask frameTask = new FrameTask(taskID, delayFrame, destFrame, callback, count);
            // 添加进入任务缓存列表
            // 确保新任务在下一帧才被加入检测列表
            // 保证检测时不会出现某些奇妙的问题
            tempFrameTaskList.Add(frameTask);
            taskIDList.Add(taskID);
            return true;
        }
        return false;
    }
    #endregion
    
    #region ToolMethod
    /// <summary>
    /// 生成ID
    /// </summary>
    /// <returns>生成的ID</returns>
    private int GetID()
    {
        lock (taskIDLock)
        {
            taskID += 1;
            while (!CheckTaskIDIsValid(taskID))
            {
                if (taskID == int.MaxValue)
                {
                    taskID = 0;
                }
                taskID += 1;
            }
        }
        return taskID;
    }
    /// <summary> 
    /// 检查一个ID是否可用
    /// </summary>
    /// <param name="taskID"></param>
    /// <returns>ID是否可用</returns>
    private bool CheckTaskIDIsValid(int taskID)
    {
        foreach (int id in taskIDList)
        {
            if (id == taskID)
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// 通过任务ID移除任务
    /// </summary>
    /// <param name="taskID">任务ID</param>
    /// <returns>移除是否成功</returns>
    private bool RemoveTimeTaskByTaskID(int taskID)
    {
        for (int index = 0; index < timeTaskList.Count; index++)
        {
            TimeTask timeTask = timeTaskList[index];
            if (timeTask.taskID == taskID)
            {
                timeTaskList.RemoveAt(index);
                return true;
            }
        }
        for (int index = 0; index < tempTimeTaskList.Count; index++)
        {
            TimeTask timeTask = tempTimeTaskList[index];
            if (timeTask.taskID == taskID)
            {
                tempTimeTaskList.RemoveAt(index);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 移除任务ID
    /// </summary>
    /// <param name="taskID">任务ID</param>
    /// <returns>移除是否成功</returns>
    private bool RemoveTaskID(int taskID)
    {
        for (int index = 0; index < taskIDList.Count; index++)
        {
            if (taskID == taskIDList[index])
            {
                taskIDList.RemoveAt(index);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 转换时间到毫秒单位
    /// </summary>
    /// <param name="timeUnit">需要转换的时间单位</param>
    /// <param name="time">需要转换的时间</param>
    /// <returns>转换后的时间 单位毫秒</returns>
    private float ConvertToMillisecond(TimeUnit timeUnit, float time)
    {
        // 统一换算成毫秒进行计算
        switch (timeUnit)
        {
            case TimeUnit.Millisecond:
                break;
            case TimeUnit.Second:
                time = time * 1000;
                break;
            case TimeUnit.Minute:
                time = time * 1000 * 60;
                break;
            case TimeUnit.Hour:
                time = time * 1000 * 60 * 60;
                break;
            case TimeUnit.Day:
                time = time * 1000 * 60 * 60 * 24;
                break;
        }
        return time;
    }
    /// <summary>
    /// 输入一个延时时间（毫秒），返回一个最终的实际时间
    /// </summary>
    /// <param name="delay">延时时间 单位毫秒</param>
    /// <returns>目标实际时间</returns>
    private float GetDestinationTime(float delay)
    {
        return Time.realtimeSinceStartup * 1000 + delay;
    }
    /// <summary>
    /// 通过任务ID移除帧任务
    /// </summary>
    /// <param name="taskID">任务ID</param>
    /// <returns>移除是否成功</returns>
    private bool RemoveFrameTaskByTaskID(int taskID)
    {
        for (int index = 0; index < frameTaskList.Count; index++)
        {
            FrameTask frameTask = frameTaskList[index];
            if (frameTask.taskID == taskID)
            {
                frameTaskList.RemoveAt(index);
                return true;
            }
        }
        for (int index = 0; index < tempTimeTaskList.Count; index++)
        {
            FrameTask frameTask = tempFrameTaskList[index];
            if (frameTask.taskID == taskID)
            {
                tempFrameTaskList.RemoveAt(index);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 获取延时一定帧数后的帧数
    /// </summary>
    /// <param name="delayFrame">延时帧数</param>
    /// <returns>实际帧数</returns>
    private int GetDestinationFrame(int delayFrame)
    {
        return (currentFrame + delayFrame) % int.MaxValue;
    }
    #endregion
}
