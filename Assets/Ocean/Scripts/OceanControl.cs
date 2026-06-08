using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ocean))]
public class OceanControl : MonoBehaviour
{
    [Header("潮汐设置")]
    [Tooltip("潮汐周期（秒）")]
    public float tideCycleDuration = 720f; // 12分钟周期（现实世界潮汐约12小时）

    [Tooltip("最小波浪高度")]
    public float minWaveHeight = 0.5f;

    [Tooltip("最大波浪高度")]
    public float maxWaveHeight = 2f;

    [Tooltip("最小流速")]
    public float minFlowSpeed = 0.5f;

    [Tooltip("最大流速")]
    public float maxFlowSpeed = 1.5f;

    [Tooltip("潮汐相位偏移 (0-1)")]
    [Range(0f, 1f)]
    public float tidePhaseOffset = 0f;

    [Header("波浪设置")]
    [Tooltip("波浪缩放")]
    public float waveScale = 15f;

    [Tooltip("波浪密度")]
    public float waveDensity = 1f;

    private Ocean ocean;
    private float currentCycleTime = 0f;

    void Start()
    {
        ocean = GetComponent<Ocean>();
        if (ocean == null)
        {
            Debug.LogError("TideSimulator requires Ocean component!");
            enabled = false;
            return;
        }

        // 初始化波浪设置
        ocean.waveScale = waveScale;
        ocean.waveDistanceFactor = waveDensity;
    }

    void Update()
    {
        // 更新潮汐周期时间
        currentCycleTime = (currentCycleTime + Time.deltaTime) % tideCycleDuration;

        // 计算潮汐相位 (0-1)
        float tidePhase = (currentCycleTime / tideCycleDuration + tidePhaseOffset) % 1f;

        // 使用正弦函数模拟潮汐起伏
        float tideFactor = Mathf.Sin(tidePhase * Mathf.PI * 2f) * 0.5f + 0.5f;

        // 计算当前波浪高度和流速
        float currentWaveHeight = Mathf.Lerp(minWaveHeight, maxWaveHeight, tideFactor);
        float currentFlowSpeed = Mathf.Lerp(minFlowSpeed, maxFlowSpeed, tideFactor);

        // 更新海洋参数
        ocean.scale = currentWaveHeight;
        ocean.speed = currentFlowSpeed;
    }

    // 提供API手动设置潮汐相位
    public void SetTidePhase(float phase)
    {
        phase = Mathf.Clamp01(phase);
        currentCycleTime = phase * tideCycleDuration;
    }

    // 提供API设置潮汐参数
    public void SetTideParameters(float duration, float minHeight, float maxHeight, float minSpeed, float maxSpeed)
    {
        tideCycleDuration = Mathf.Max(0.1f, duration);
        minWaveHeight = minHeight;
        maxWaveHeight = maxHeight;
        minFlowSpeed = minSpeed;
        maxFlowSpeed = maxSpeed;
    }
}
