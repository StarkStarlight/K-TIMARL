using UnityEngine;
using System.IO;
using System;

public static class FileLogger
{
    private static string logFilePath = Path.Combine(Application.dataPath, "..", "GameLogs", "MyCustomLog.txt");

    static FileLogger()
    {
        string directory = Path.GetDirectoryName(logFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void Log(string message)
    {
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";

        Debug.Log(message);

        try
        {
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }
        catch (System.Exception e)
        {
            Debug.LogError("写入日志文件失败: " + e.Message);
        }
    }

    public static void LogWarning(string message)
    {
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] [WARNING] {message}";
        Debug.LogWarning(message); // 控制台显示黄色
        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
    }
}