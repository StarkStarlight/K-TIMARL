using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LogInfo
{
    public class LoggerManager : MonoBehaviour, IDisposable
    {
        private List<string> m_LogArray;

        private string m_LogPath = null;

        private int m_LogMaxCapacity = 500;

        private int m_CurrLogCount = 0;

        private int m_LogBufferMaxNumber = 10;

        private bool m_IsInitialized = false;

        private static LoggerManager m_Instance;
        public static LoggerManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    GameObject go = new GameObject("LoggerManager");
                    m_Instance = go.AddComponent<LoggerManager>();
                    DontDestroyOnLoad(go);
                }
                return m_Instance;
            }
        }

        public void Init(string customPath = null)
        {
            if (m_IsInitialized) return;

            m_LogArray = new List<string>();
            if (string.IsNullOrEmpty(customPath))
            {
                string logDir = Path.Combine(Application.persistentDataPath, "GameLogs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                m_LogPath = Path.Combine(logDir,
                    "MYH_LOG_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + "-Start.txt");
            }
            else
            {
                m_LogPath = customPath;
            }

            Debug.Log($"LoggerManager.Init LogPath: {m_LogPath}");
            m_IsInitialized = true;
        }

        private void CreateFile(string pathAndName, string info)
        {
            try
            {
                StreamWriter sw;
                FileInfo fileInfo = new FileInfo(pathAndName);

                string directory = Path.GetDirectoryName(pathAndName);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!fileInfo.Exists)
                {
                    sw = fileInfo.CreateText();
                }
                else
                {
                    sw = fileInfo.AppendText();
                }

                sw.WriteLine(info);
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入日志文件失败: {ex.Message}");
            }
        }

        public void SyncLog()
        {
            if (!string.IsNullOrEmpty(m_LogPath) && m_LogArray.Count > 0)
            {
                for (int i = 0; i < m_LogArray.Count; ++i)
                {
                    CreateFile(m_LogPath, m_LogArray[i]);
                }
                ClearLogArray();
            }
        }

        private void AppendDataToFile(string writerFileData)
        {
            if (!string.IsNullOrEmpty(writerFileData))
            {
                m_LogArray.Add(writerFileData);
            }

            if (m_LogArray.Count >= m_LogBufferMaxNumber)
            {
                SyncLog();
            }
        }

        public void Write(string strWriteFileData, LogType logType)
        {
            if (!m_IsInitialized)
            {
                Init(); 
            }

            if (m_CurrLogCount >= m_LogMaxCapacity)
            {
                string directory = Path.GetDirectoryName(m_LogPath);
                string fileName = "MYH_LOG_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + ".txt";
                m_LogPath = Path.Combine(directory, fileName);
                m_CurrLogCount = 0;
            }
            ++m_CurrLogCount;

            if (!string.IsNullOrEmpty(strWriteFileData))
            {
                strWriteFileData = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                                  "] [" + logType + "] " + strWriteFileData;
                AppendDataToFile(strWriteFileData);
            }
        }

        private void ClearLogArray()
        {
            if (null != m_LogArray)
            {
                m_LogArray.Clear();
            }
        }

        public void Dispose()
        {
            SyncLog();

            if (m_LogArray != null)
            {
                m_LogArray.Clear();
                m_LogArray = null;
            }

            Debug.Log("LoggerManager disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnApplicationQuit()
        {
            SyncLog();
        }
    }
}