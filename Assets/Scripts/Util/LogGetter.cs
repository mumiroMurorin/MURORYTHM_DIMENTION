using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LogGetter : MonoBehaviour
{
    public Action<LogData> OnReceiveLogListener;

    float timeCount;

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        OnReceiveLogListener?.Invoke(new LogData
        {
            Time = timeCount,
            Logtext = type == LogType.Log ? logString : logString + "\n\n" + stackTrace ,
            LogType = type,
        });
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        timeCount = 0;
    }

    private void Update()
    {
        timeCount += Time.deltaTime;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
}

/// <summary>
/// Debug.LogÇ∆àÍèèÇ…égÇ®Ç§
/// </summary>
public class LogData
{
    public float Time { get; set; }

    public LogType LogType { get; set; }

    public string Logtext { get; set; }
}