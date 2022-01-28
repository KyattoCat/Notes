using System;
using UnityEngine;
using UnityEngine.UI;

public class GameRoot : MonoBehaviour
{
    public Text textTaskID;

    private void Start() {
        TimerSystem timerSystem = GetComponent<TimerSystem>();
        timerSystem.Init();
    }

    public void OnButtonAddTimeTaskClick()
    {
        Debug.Log("Add Time Task");
        TimerSystem.Instance.AddTimeTask(FuncA, 1000, TimeUnit.Millisecond, -1);
    }

    public void OnButtonDeleteTimeTaskClick()
    {
        if (textTaskID.text == null || textTaskID.text == "")
        {
            return;
        }

        try
        {
            int taskID = Convert.ToInt32(textTaskID.text);
            bool result = TimerSystem.Instance.DeleteTimeTask(taskID);
            Debug.Log("结果:" + result);
        }
        catch(System.FormatException)
        {
            Debug.Log("ID不正确");
        }
    }

    public void OnButtonReplaceTimeTaskClick()
    {
        if (textTaskID.text == null || textTaskID.text == "")
        {
            return;
        }

        try
        {
            int taskID = Convert.ToInt32(textTaskID.text);
            bool result = TimerSystem.Instance.ReplaceTimeTask(taskID, FuncB, 2, TimeUnit.Second, -1);
            Debug.Log("结果:" + result);
        }
        catch(System.FormatException)
        {
            Debug.Log("ID不正确");
        }
    }
    
    public void OnButtonAddFrameTaskClick()
    {
        Debug.Log("Add Time Task");
        TimerSystem.Instance.AddFrameTask(FuncA, 60, -1);
    }

    public void OnButtonDeleteFrameTaskClick()
    {
        if (textTaskID.text == null || textTaskID.text == "")
        {
            return;
        }

        try
        {
            int taskID = Convert.ToInt32(textTaskID.text);
            bool result = TimerSystem.Instance.DeleteFrameTask(taskID);
            Debug.Log("结果:" + result);
        }
        catch(System.FormatException)
        {
            Debug.Log("ID不正确");
        }
    }

    public void OnButtonReplaceFrameTaskClick()
    {
        if (textTaskID.text == null || textTaskID.text == "")
        {
            return;
        }

        try
        {
            int taskID = Convert.ToInt32(textTaskID.text);
            bool result = TimerSystem.Instance.ReplaceFrameTask(taskID, FuncB, 120, -1);
            Debug.Log("结果:" + result);
        }
        catch(System.FormatException)
        {
            Debug.Log("ID不正确");
        }
    }
    
    void FuncA()
    {
        Debug.Log("A");
    }

    void FuncB()
    {
        Debug.Log("B");
    }
}
