using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LogInfo;

public class BoatController : MonoBehaviour {

	[SerializeField] private List<GameObject> m_motors;

	[SerializeField] private bool m_enableAudio = true;
	[SerializeField] private AudioSource m_boatAudioSource;
	[SerializeField] private float m_boatAudioMinPitch = 0.4F;
	[SerializeField] private float m_boatAudioMaxPitch = 1.2F;

	[SerializeField] public float m_FinalSpeed = 100F;
	[SerializeField] public float m_InertiaFactor = 0.005F;
	[SerializeField] public float m_turningFactor = 2.0F;
	[SerializeField] public float m_accelerationTorqueFactor = 35F;
	[SerializeField] public float m_turningTorqueFactor = 35F;

	[SerializeField] private bool m_enableSpeedLog = true;
	[SerializeField] private float m_logInterval = 1f;

	private float m_logTimer = 0f;

	private float m_verticalInput = 0F;
	private float m_horizontalInput = 0F;
	private Rigidbody m_rigidbody;
	private Vector2 m_androidInputInit;

	//新增控件自动航行
	[Header("Autopilot Settings")]
	[SerializeField] private float navigationUpdateInterval = 1f;
	[SerializeField] private float arrivalDistance = 50;
	[SerializeField] private float maxTurnAngle = 45;

	private Vector3 currentWaypoint;
	public bool hasWaypoint = false;
	public bool hasArrived = false;
	private float navigationTimer = 0f;

	[Header("PID Controller Settings")]
	[SerializeField] private float pid_Kp = 2.0f;
	[SerializeField] private float pid_Ki = 0.05f;
	[SerializeField] private float pid_Kd = 1.0f;
	[SerializeField] private float pid_IntegralLimit = 5.0f;
	[SerializeField] private float pid_OutputLimit = 1.0f;
	[SerializeField] private float pid_AngleDeadZone = 2.0f;

	private float pid_Integral = 0f;
	private float pid_PreviousError = 0f;
	private float pid_LastOutput = 0f;
	private float pid_SmoothingFactor = 0.2f;

	[Header("Speed Control Settings")]
	[SerializeField] private float m_currentTargetSpeed = 100F;
	[SerializeField] private float m_speedChangeRate = 5F;
	[SerializeField] private AnimationCurve m_speedChangeCurve = AnimationCurve.Linear(0, 0, 1, 1);

	private float m_speedChangeProgress = 0f;
	private float m_startSpeed = 0f;
	private float m_endSpeed = 0f;

	private float accel=0;
	private float accelBreak;
	[SerializeField] public string BoatNumber;

	private ShipStabilizer stabilizer;

     void Start() {
		// base.Start();
		stabilizer = GetComponent<ShipStabilizer>();
		m_rigidbody = GetComponent<Rigidbody>();
		// m_rigidbody.drag = 1;
		//  m_rigidbody.angularDrag = 1;
		accelBreak = m_FinalSpeed*0.3f;

		m_logTimer = m_logInterval;
		
		initPosition ();

		initPIDControl();
	}

	public void initPIDControl()
    {
		pid_Integral = 0f;
		pid_PreviousError = 0f;
		pid_LastOutput = 0f;
		//Debug.Log($"Boat {BoatNumber}: PID控制器已初始化 (Kp={pid_Kp}, Ki={pid_Ki}, Kd={pid_Kd})");
		//LoggerManager.Instance.Write($"Boat {BoatNumber}: PID控制器已初始化 (Kp={pid_Kp}, Ki={pid_Ki}, Kd={pid_Kd})", LogType.Log);
	}

	public void initPosition()	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		m_androidInputInit.x = Input.acceleration.y;
		m_androidInputInit.y = Input.acceleration.x;
		#endif
	}


	void Update()	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		Vector2 touchInput = Vector2.zero;
		touchInput.x =  -(Input.acceleration.y - m_androidInputInit.y);
		touchInput.y =  Input.acceleration.x - m_androidInputInit.x;

		if (touchInput.sqrMagnitude > 1)
			touchInput.Normalize();

		setInputs (touchInput.x, touchInput.y);
		#else
		if(m_enableSpeedLog) {
            m_logTimer -= Time.deltaTime;
            if(m_logTimer <= 0f) {
                LogSpeedAndDirection();
                m_logTimer = m_logInterval;
            }
        }
		//setInputs(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));
		#endif
	}

	public void setInputs(float iVerticalInput, float iHorizontalInput)	{
		m_verticalInput = iVerticalInput;
		m_horizontalInput = iHorizontalInput;
	}

	 void FixedUpdate()	{
		//base.FixedUpdate();
		UpdateAutopilot();

		UpdateSpeedChange();

		if(m_verticalInput > 0) {
			if(accel < m_currentTargetSpeed)
			{ 
				accel += (m_currentTargetSpeed * m_InertiaFactor); 
				accel *= m_verticalInput;
			}
		} else if(m_verticalInput == 0)
		{
			if(accel > 0) { accel -= m_currentTargetSpeed * m_InertiaFactor; }
			if(accel < 0) { accel += m_currentTargetSpeed * m_InertiaFactor; }
		}else if(m_verticalInput < 0)
		{
			if(accel > -accelBreak) { accel -= m_currentTargetSpeed * m_InertiaFactor*2;  }
		}
		
		m_rigidbody.AddRelativeForce(Vector3.forward  * accel);

        m_rigidbody.AddRelativeTorque(
			m_verticalInput * -m_accelerationTorqueFactor,
			m_horizontalInput * m_turningFactor,
			m_horizontalInput * -m_turningTorqueFactor
        );

        if(m_motors.Count > 0) {

            float motorRotationAngle = 0F;
			float motorMaxRotationAngle = 70;

			motorRotationAngle = - m_horizontalInput * motorMaxRotationAngle;

			for(int i=0; i<m_motors.Count; i++) {
				float currentAngleY = m_motors[i].transform.localEulerAngles.y;
				if (currentAngleY > 180.0f)
					currentAngleY -= 360.0f;

				float localEulerAngleY = Lerp(currentAngleY, motorRotationAngle, Time.deltaTime * 10);
				m_motors[i].transform.localEulerAngles = new Vector3(
					m_motors[i].transform.localEulerAngles.x,
					localEulerAngleY,
					m_motors[i].transform.localEulerAngles.z
				);
            }
        }
		
		if (m_enableAudio && m_boatAudioSource != null) 
		{
			
			float pitchLevel =  m_boatAudioMaxPitch*Mathf.Abs(m_verticalInput);
			if(m_verticalInput<0) pitchLevel*=0.7f;

			if (pitchLevel < m_boatAudioMinPitch) pitchLevel = m_boatAudioMinPitch;


			float smoothPitchLevel = Lerp(m_boatAudioSource.pitch, pitchLevel, Time.deltaTime*0.5f);

			m_boatAudioSource.pitch = smoothPitchLevel;
		}
    }

	static float Lerp (float from, float to, float value) {
		if (value < 0.0f) return from;
		else if (value > 1.0f) return to;
		return (to - from) * value + from;
	}

	private void LogSpeedAndDirection()
	{
		if (m_rigidbody == null) return;

		float speed = m_rigidbody.velocity.magnitude;

		Vector3 direction = m_rigidbody.velocity.normalized;
		float angle = Vector3.Angle(Vector3.forward, direction);

		string directionText = "Forward";
		if (direction.x > 0)
		{
			directionText = angle <= 45 ? "Forward-Right" : "Right";
		}
		else if (direction.x < 0)
		{
			directionText = angle <= 45 ? "Forward-Left" : "Left";
		}

		if (Vector3.Dot(direction, Vector3.forward) < 0)
		{
			directionText = "Reverse" + directionText.Replace("Forward", "");
		}

		FileLogger.Log($"Boat {BoatNumber} Status - Speed: {speed:F2} m/s, Location: ({transform.position.x:F2}, {transform.position.z:F2})");
		//LoggerManager.Instance.Write($"Boat {BoatNumber} Status - Speed: {speed:F2} m/s, Direction: {directionText}, Angle: {angle:F1}°", LogType.Log);
	}

	public void NavigateTo(Vector3 destination)
    {
		if (hasWaypoint && Vector3.Distance(currentWaypoint, destination) < 1f)
		{
			currentWaypoint = destination;
			hasWaypoint = true;
			hasArrived = false;

			ResetPIDControl();
			return;
		}

		currentWaypoint = destination;
		hasWaypoint = true;
		hasArrived = false;

		ResetPIDControl();
		//Debug.Log($"Boat {BoatNumber}: 设置新航点 {destination}, 重置PID控制器");
		//LoggerManager.Instance.Write($"Boat {BoatNumber}: 设置新航点 {destination}, 重置PID控制器", LogType.Log);
	}

	public void ResetPIDControl()
    {
		pid_Integral = 0f;
		pid_PreviousError = 0f;
		pid_LastOutput = 0f;
    }

	private float CalcPIDOutput(float currentError, float deltaTime)
    {
		float proportional = pid_Kp * currentError;
		pid_Integral += currentError * deltaTime;

		pid_Integral = Mathf.Clamp(pid_Integral, -pid_IntegralLimit, pid_IntegralLimit);
		float integral = pid_Ki * pid_Integral;

		float derivative = 0f;
		if (deltaTime > 0)
        {
			float errorChange = (currentError - pid_PreviousError) / deltaTime;
			derivative = pid_Kd * errorChange;
        }
		pid_PreviousError = currentError;

		float rawOutput = proportional + integral + derivative;
		float clampedOutput = Mathf.Clamp(rawOutput, -pid_OutputLimit, pid_OutputLimit);

		float smoothOutput = Mathf.Lerp(pid_LastOutput, clampedOutput, pid_SmoothingFactor);
		pid_LastOutput = smoothOutput;

		return smoothOutput;
    }

	private void UpdateAutopilot()
    {
		if (hasArrived) return;

		navigationTimer -= Time.deltaTime;
		if (navigationTimer <= 0f)
        {
			UpdateNavigation();
			navigationTimer = navigationUpdateInterval;
        }
    }

	private void UpdateNavigation()
    {
		Vector3 toWaypoint = currentWaypoint - transform.position;
		toWaypoint.y = 0;

		float distance = toWaypoint.magnitude;

		float desiredSpeedMultiplier = 1f;

		if (distance <= arrivalDistance * 2f)
        {
			desiredSpeedMultiplier = Mathf.Clamp(distance / (arrivalDistance * 2f), 0.3f, 1f); 
			float desiredSpeed = m_FinalSpeed * desiredSpeedMultiplier;
			SetTargetSpeed(desiredSpeed, 1f);

			if (transform.parent.GetComponent<FleetController>().WaypointsRemain())
			{
				transform.parent.GetComponent<FleetController>().NextWaypoint();
				//Debug.Log($"Boat {BoatNumber}: 到达航点，切换到下一个");
			}
			else
			{
				hasWaypoint = false;
				//Debug.Log($"Boat {BoatNumber}: 完成所有航点");
			}
			if (transform.parent.GetComponent<FleetController>().isFinalArrived(transform.position))
            {
				hasArrived = true;
				//Debug.Log($"Boat {BoatNumber}: 已到达最终目的地");
			}
			setInputs(0, 0);
			return;
        }

		float forwardInput = Mathf.Clamp01(distance / (arrivalDistance * 2f));
		float targetAngle = Vector3.SignedAngle(transform.forward, toWaypoint, Vector3.up);

		float turnInput = 0f;
		if (Mathf.Abs(targetAngle) > pid_AngleDeadZone)
        {
			float normalizedError = targetAngle / maxTurnAngle;
			turnInput = CalcPIDOutput(normalizedError, navigationUpdateInterval);
			turnInput = Mathf.Clamp(turnInput, -1f, 1f);
			if (m_enableSpeedLog && Time.frameCount % 60 == 0)
            {
				//Debug.Log($"Boat {BoatNumber} PID调试 - 角度误差: {targetAngle:F1}°, 归一化误差: {normalizedError:F2}, PID输出: {turnInput:F2}");
				//LoggerManager.Instance.Write($"Boat {BoatNumber} PID调试 - 角度误差: {targetAngle:F1}°, 归一化误差: {normalizedError:F2}, PID输出: {turnInput:F2}", LogType.Log);
			}
		} else
		{
			pid_Integral *= 0.9f;
			turnInput = 0f;
		}

		setInputs(forwardInput, turnInput);
    }

	void OnValidate()
	{
		if (Application.isPlaying && m_rigidbody != null)
		{
			pid_Kp = Mathf.Max(0, pid_Kp);
			pid_Ki = Mathf.Max(0, pid_Ki);
			pid_Kd = Mathf.Max(0, pid_Kd);
			pid_IntegralLimit = Mathf.Max(0, pid_IntegralLimit);
			pid_OutputLimit = Mathf.Max(0.1f, pid_OutputLimit);

			ResetPIDControl();
		}
	}

	public void SetTargetSpeed(float targetSpeed, float changeDuration = 0f)
    {
		if (changeDuration <= 0)
        {
			m_currentTargetSpeed = Mathf.Clamp(targetSpeed, 0, m_FinalSpeed);
			accel = m_currentTargetSpeed;
        }
        else
        {
			m_startSpeed = GetCurrentSpeed();
			m_endSpeed = Mathf.Clamp(targetSpeed, 0, m_FinalSpeed);
			m_speedChangeProgress = 0f;
			m_speedChangeRate = 1f / changeDuration;
			//Debug.Log($"Boat {BoatNumber}: 开始速度变化 {m_startSpeed:F1} -> {m_endSpeed:F1}, 持续时间: {changeDuration:F1}秒");
        }
    }

	public float GetCurrentSpeed()
    {
		return accel;
    }

	public float GetTargetSpeed()
    {
		return m_currentTargetSpeed;
    }

	public void UpdateSpeedChange()
    {
		if (m_speedChangeProgress < 1f)
        {
			m_speedChangeProgress += m_speedChangeRate * Time.deltaTime;
			m_speedChangeProgress = Mathf.Clamp01(m_speedChangeProgress);

			float curveValue = m_speedChangeCurve.Evaluate(m_speedChangeProgress);
			m_currentTargetSpeed = Mathf.Lerp(m_startSpeed, m_endSpeed, curveValue);

			float speedDifference = m_currentTargetSpeed - accel;
			float acceleration = Mathf.Sign(speedDifference) * m_FinalSpeed * m_InertiaFactor * 0.5f;

			if (Mathf.Abs(speedDifference) > 1f)
            {
				accel += acceleration * Time.deltaTime;
            }
			else
            {
				accel = m_currentTargetSpeed;
            }

			if (m_enableSpeedLog && Time.frameCount % 30 == 0)
            {
				//Debug.Log($"Boat {BoatNumber}: 速度变化中 - 进度: {m_speedChangeProgress:F2}, 当前速度: {accel:F1}, 目标: {m_currentTargetSpeed:F1}");
			}
        }
    }

	public void StopImmediately()
	{
		SetTargetSpeed(0f, 0f);
		accel = 0f;
		setInputs(0, 0);
		//Debug.Log($"Boat {BoatNumber}: 立即停止");
	}

	public void StopGradually(float stopDuration = 3f)
    {
		SetTargetSpeed(0f, stopDuration);
		//Debug.Log($"Boat {BoatNumber}: 开始逐渐停止，持续时间: {stopDuration:F1}秒");
	}

	public void ResumeCruisesSpeed(float changeDuration = 2f)
    {
		SetTargetSpeed(m_FinalSpeed, changeDuration);
		//Debug.Log($"Boat {BoatNumber}: 恢复巡航速度 {m_FinalSpeed:F1}, 持续时间: {changeDuration:F1}秒");
	}

	/*void OnDrawGizmosSelected()
	{
		if (hasWaypoint)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(currentWaypoint, 10f);

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.position, currentWaypoint);

			Gizmos.color = Color.blue;
			Gizmos.DrawRay(transform.position, transform.forward * 50f);

			Vector3 toWaypoint = (currentWaypoint - transform.position).normalized * 50f;
			Gizmos.color = Color.red;
			Gizmos.DrawRay(transform.position, toWaypoint);

			#if UNITY_EDITOR
			UnityEditor.Handles.Label(transform.position + Vector3.up * 20, 
				$"PID: P={pid_Kp:F2}, I={pid_Ki:F2}, D={pid_Kd:F2}\n" +
				$"积分: {pid_Integral:F2}, 输出: {pid_LastOutput:F2}");
			#endif
		}
	}*/
}
