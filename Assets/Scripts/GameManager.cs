using UnityEngine;
using LogInfo;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        LoggerManager.Instance.Init();
    }
}