  using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleetController : MonoBehaviour
{
    [SerializeField] private List<BoatController> boats = new List<BoatController>();
    [SerializeField] private bool autoAssignChildren = true;
    [SerializeField] private List<Vector3> waypoints = new List<Vector3>();
    [SerializeField] private int number;

	[Header("Fleet Speed Settings")]
	[SerializeField] private float fleetMaxSpeed = 100f;
	[SerializeField] private float fleetCruiseSpeed = 80f;

	[Header("Fleet Speed Control")]
    [SerializeField] private float fleetStartDelay = 0f;
    [SerializeField] private AnimationCurve fleetAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Segment Speed Control")]
    [SerializeField] private List<SpeedSegment> speedSegments = new List<SpeedSegment>();
    private int currentSegmentIndex = 0;
    private float fleetStartTimer = 0f;
    private bool isFleetStarted = false;
    private Coroutine speedControlCoroutine = null;

    [System.Serializable]
    public class SpeedSegment
    {
        public string segmentName = "Segment";
        public float targetSpeed = 100f;
        public float transitionDuration = 2f;
        public bool waitForAllBoats = true;
        public float minDistanceToNext = 50f;
        [TextArea] public string description = "";
    }

    private int currentWaypointIndex = 0;

    private List<Vector3> formationPositions = new List<Vector3>();
    private Vector3 destination;
    private Vector3 finalDestination;
    private bool hasDestination = false;

    void Start()
    {
        if (autoAssignChildren)
        {
            boats.Clear();
            foreach (Transform child in transform)
            {
                var boat = child.GetComponent<BoatController>();
                if (boat != null) boats.Add(boat);
            }
        }

        InitializeSpeedControl();

        StartPatrol();
    }

    void Update()
    {
        if (!isFleetStarted)
        {
            fleetStartTimer += Time.deltaTime;
            if (fleetStartTimer >= fleetStartDelay)
            {
                StartFleet();
            }
            return;

        }

        if (hasDestination)
        {
            UpdateFleetNavigation();
        }

        UpdateSpeedSegments();
    }

    private void InitializeSpeedControl()
    {
        fleetStartTimer = 0f;
        isFleetStarted = (fleetStartDelay <= 0f);

        if (!isFleetStarted)
        {
            foreach (var boat in boats)
            {
                if (boat != null)
                {
                    boat.SetTargetSpeed(0f, 0f);
                    boat.setInputs(0, 0);
                }
            }

            //Debug.Log($"舰队 {number}: 等待启动，延迟 {fleetStartDelay:F1}秒");
        }
    }

	private void CreateDefaultSpeedSegments()
	{
		// 创建默认的分段
		SpeedSegment startSegment = new SpeedSegment
		{
			segmentName = "启动加速",
			targetSpeed = fleetCruiseSpeed * 0.4f,
			transitionDuration = 2f,
			waitForAllBoats = true,
			minDistanceToNext = 100f,
			description = "舰队启动初始加速阶段"
		};

		SpeedSegment cruiseSegment = new SpeedSegment
		{
			segmentName = "巡航",
			targetSpeed = fleetCruiseSpeed,
			transitionDuration = 3f,
			waitForAllBoats = false,
			minDistanceToNext = 50f,
			description = "正常巡航阶段"
		};

		SpeedSegment approachSegment = new SpeedSegment
		{
			segmentName = "接近减速",
			targetSpeed = fleetCruiseSpeed * 0.3f,
			transitionDuration = 2f,
			waitForAllBoats = true,
			minDistanceToNext = 30f,
			description = "接近目标减速阶段"
		};

		speedSegments.Add(startSegment);
		speedSegments.Add(cruiseSegment);
		speedSegments.Add(approachSegment);

		//Debug.Log($"舰队 {number}: 创建了 {speedSegments.Count} 个默认速度分段");
	}

	private void StartFleet()
	{
		if (isFleetStarted) return;

		isFleetStarted = true;

		// 应用第一个速度分段
		if (speedSegments.Count > 0)
		{
			ApplySpeedSegment(0);
		}
		else
		{
			// 如果没有定义速度分段，使用默认速度
			SetFleetTargetSpeed(fleetCruiseSpeed, 3f);
		}

		//Debug.Log($"舰队 {number}: 已启动，开始航行");
	}

	// 新增：更新速度分段控制
	private void UpdateSpeedSegments()
	{
		if (speedSegments.Count == 0 || currentSegmentIndex >= speedSegments.Count) return;

		var currentSegment = speedSegments[currentSegmentIndex];

		// 检查是否需要切换到下一个分段
		if (ShouldMoveToNextSegment())
		{
			MoveToNextSegment();
		}
	}

	// 新增：检查是否应该切换到下一个速度分段
	private bool ShouldMoveToNextSegment()
	{
		if (currentSegmentIndex >= speedSegments.Count - 1) return false;

		var currentSegment = speedSegments[currentSegmentIndex];

		// 检查是否等待所有船只
		if (currentSegment.waitForAllBoats)
		{
			foreach (var boat in boats)
			{
				if (boat == null) continue;

				// 检查船只是否接近目标速度
				float speedDifference = Mathf.Abs(boat.GetCurrentSpeed() - currentSegment.targetSpeed);
				if (speedDifference > 5f) // 允许5单位的误差
				{
					return false;
				}
			}
		}

		// 检查距离条件
		if (hasDestination)
		{
			float distanceToDestination = 0f;

			if (currentWaypointIndex < waypoints.Count)
			{
				// 计算到当前航点的平均距离
				float totalDistance = 0f;
				int validBoats = 0;

				foreach (var boat in boats)
				{
					if (boat == null) continue;

					totalDistance += Vector3.Distance(boat.transform.position, waypoints[currentWaypointIndex]);
					validBoats++;
				}

				if (validBoats > 0)
				{
					distanceToDestination = totalDistance / validBoats;
				}
			}

			return distanceToDestination <= currentSegment.minDistanceToNext;
		}

		return false;
	}

	// 新增：切换到下一个速度分段
	private void MoveToNextSegment()
	{
		currentSegmentIndex++;

		if (currentSegmentIndex < speedSegments.Count)
		{
			ApplySpeedSegment(currentSegmentIndex);
		}
		else
		{
			//Debug.Log($"舰队 {number}: 已完成所有速度分段");
		}
	}

	// 新增：应用速度分段
	public void ApplySpeedSegment(int segmentIndex)
	{
		if (segmentIndex < 0 || segmentIndex >= speedSegments.Count) return;

		var segment = speedSegments[segmentIndex];
		currentSegmentIndex = segmentIndex;

		SetFleetTargetSpeed(segment.targetSpeed, segment.transitionDuration);

		//Debug.Log($"舰队 {number}: 应用速度分段 [{segmentIndex}] {segment.segmentName} - 目标速度: {segment.targetSpeed:F1}, 过渡时间: {segment.transitionDuration:F1}秒");
	}

	// 新增：设置舰队目标速度
	public void SetFleetTargetSpeed(float targetSpeed, float transitionDuration = 2f)
	{
		if (speedControlCoroutine != null)
		{
			StopCoroutine(speedControlCoroutine);
		}

		speedControlCoroutine = StartCoroutine(SetFleetTargetSpeedCoroutine(targetSpeed, transitionDuration));
	}

	// 新增：设置舰队目标速度的协程
	private IEnumerator SetFleetTargetSpeedCoroutine(float targetSpeed, float transitionDuration)
	{
		List<float> startSpeeds = new List<float>();

		// 记录每艘船的起始速度
		foreach (var boat in boats)
		{
			if (boat != null)
			{
				startSpeeds.Add(boat.GetCurrentSpeed());
			}
			else
			{
				startSpeeds.Add(0f);
			}
		}

		float elapsedTime = 0f;

		while (elapsedTime < transitionDuration)
		{
			elapsedTime += Time.deltaTime;
			float t = Mathf.Clamp01(elapsedTime / transitionDuration);

			// 使用曲线计算插值
			float curveValue = fleetAccelerationCurve.Evaluate(t);

			// 为每艘船设置速度
			for (int i = 0; i < boats.Count; i++)
			{
				if (boats[i] == null) continue;

				float currentTargetSpeed = Mathf.Lerp(startSpeeds[i], targetSpeed, curveValue);
				boats[i].SetTargetSpeed(currentTargetSpeed, 0.1f); // 每艘船使用快速过渡
			}

			yield return null;
		}

		// 确保最终速度准确
		foreach (var boat in boats)
		{
			if (boat != null)
			{
				boat.SetTargetSpeed(targetSpeed, 0f);
			}
		}

		//Debug.Log($"舰队 {number}: 已完成速度变化到 {targetSpeed:F1}");
		speedControlCoroutine = null;
	}

	// 新增：紧急停止舰队
	public void EmergencyStop()
	{
		foreach (var boat in boats)
		{
			if (boat != null)
			{
				boat.StopImmediately();
			}
		}

		//Debug.Log($"舰队 {number}: 紧急停止");
	}

	// 新增：逐渐停止舰队
	public void StopFleetGradually(float stopDuration = 5f)
	{
		SetFleetTargetSpeed(0f, stopDuration);
		//Debug.Log($"舰队 {number}: 开始逐渐停止，持续时间: {stopDuration:F1}秒");
	}

	// 新增：恢复舰队巡航速度
	public void ResumeFleetCruise(float transitionDuration = 3f)
	{
		float cruiseSpeed = fleetCruiseSpeed;
		if (speedSegments.Count > 0 && currentSegmentIndex < speedSegments.Count)
		{
			cruiseSpeed = speedSegments[currentSegmentIndex].targetSpeed;
		}

		SetFleetTargetSpeed(cruiseSpeed, transitionDuration);
		//Debug.Log($"舰队 {number}: 恢复巡航速度 {cruiseSpeed:F1}, 持续时间: {transitionDuration:F1}秒");
	}

	// 新增：添加速度分段
	public void AddSpeedSegment(SpeedSegment newSegment)
	{
		speedSegments.Add(newSegment);
		//Debug.Log($"舰队 {number}: 添加速度分段 [{speedSegments.Count - 1}] {newSegment.segmentName}");
	}

	// 新增：插入速度分段
	public void InsertSpeedSegment(int index, SpeedSegment newSegment)
	{
		if (index >= 0 && index <= speedSegments.Count)
		{
			speedSegments.Insert(index, newSegment);
			//Debug.Log($"舰队 {number}: 插入速度分段 [{index}] {newSegment.segmentName}");
		}
	}

	// 新增：移除速度分段
	public void RemoveSpeedSegment(int index)
	{
		if (index >= 0 && index < speedSegments.Count)
		{
			speedSegments.RemoveAt(index);
			if (currentSegmentIndex >= speedSegments.Count)
			{
				currentSegmentIndex = Mathf.Max(0, speedSegments.Count - 1);
			}
			//Debug.Log($"舰队 {number}: 移除速度分段 [{index}]");
		}
	}

	// 新增：获取当前速度分段信息
	public SpeedSegment GetCurrentSpeedSegment()
	{
		if (currentSegmentIndex >= 0 && currentSegmentIndex < speedSegments.Count)
		{
			return speedSegments[currentSegmentIndex];
		}
		return null;
	}

	// 新增：检查舰队是否已启动
	public bool IsFleetStarted()
	{
		return isFleetStarted;
	}

	// 新增：获取舰队启动剩余时间
	public float GetStartTimeRemaining()
	{
		if (isFleetStarted) return 0f;
		return Mathf.Max(0f, fleetStartDelay - fleetStartTimer);
	}
	public void SetDestination(Vector3 newDestination)
    {
        destination = newDestination;
        hasDestination = true;
    }

    public void SetFinalDestination(Vector3 newDestination)
    {
        finalDestination = newDestination;
        hasDestination = true;
    }

    private void UpdateFleetNavigation()
    {
        for (int i = 0; i < boats.Count; i++)
        {
            if (boats[i] == null) continue;
            Vector3 targetPos;
            if (currentWaypointIndex < waypoints.Count)
            {
                targetPos = waypoints[currentWaypointIndex];
            } else
            {
                targetPos = finalDestination;
            }
            //Debug.Log($" Boat {number} : {targetPos}");
            boats[i].NavigateTo(targetPos);
        }
    }

    private void AddBoat(BoatController newBoat)
    {
        if (!boats.Contains(newBoat))
        {
            boats.Add(newBoat);
        }
    }

    public void RemoveBoat(BoatController boatToRemove)
    {
        if (boats.Contains(boatToRemove))
        {
            boats.Remove(boatToRemove);
        }
    }

    public void AddWaypoint(Vector3 newWaypoint)
    {
        waypoints.Add(newWaypoint);
    }

    public void StartPatrol()
    {
        if (waypoints.Count > 0)
        {
            currentWaypointIndex = 0;
            SetDestination(waypoints[0]);
        }
    }

    public bool WaypointsRemain()
    {
        return currentWaypointIndex < waypoints.Count - 1;
    }

    public bool isFinalArrived(Vector3 localposition)
    {
        Vector3 toDestination = finalDestination - localposition;
        toDestination.y = 0;

        float distance = toDestination.magnitude;
        return distance < 50;
    }

    public void NextWaypoint()
    {
        currentWaypointIndex += 1;
        return;
    }
}
