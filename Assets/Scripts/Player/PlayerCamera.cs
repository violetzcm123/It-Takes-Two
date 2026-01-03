using Cinemachine;
using UnityEngine;

namespace ItTakesTwo
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class PlayerCamera: MonoBehaviour
    {
        [Header("Camera Settings")]
		public Player player;
		public float maxDistance = 15f;
		public float initialAngle = 20f;
		public float heightOffset = 1f;

		[Header("Following Settings")]
		public float verticalUpDeadZone = 0.15f;
		public float verticalDownDeadZone = 0.15f;
		public float verticalAirUpDeadZone = 4f;
		public float verticalAirDownDeadZone = 0;
		public float maxVerticalSpeed = 10f;
		public float maxAirVerticalSpeed = 100f;

		[Header("Orbit Settings")]
		public bool canOrbit = true;
		public bool canOrbitWithVelocity = true;
		public float orbitVelocityMultiplier = 5;

		[Range(0, 90)]
		public float verticalMaxRotation = 80;

		[Range(-90, 0)]
		public float verticalMinRotation = -20;

		protected float m_cameraDistance;
		protected float m_cameraTargetYaw;
		protected float m_cameraTargetPitch;

		protected Vector3 m_cameraTargetPosition;

		protected CinemachineVirtualCamera m_camera;
		protected Cinemachine3rdPersonFollow m_cameraBody;
		protected CinemachineBrain m_brain;

		protected Transform m_target;

		protected string k_targetName = "Player Follower Camera Target";

		protected virtual void InitializeComponents()
		{
			if (!player)
			{
				player = FindObjectOfType<Player>();
			}

			m_camera = GetComponent<CinemachineVirtualCamera>();
			m_cameraBody = m_camera.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
			m_brain = Camera.main.GetComponent<CinemachineBrain>();
		}

		protected virtual void InitializeFollower()
		{
			m_target = new GameObject(k_targetName).transform;
			m_target.position = player.transform.position;
		}

		protected virtual void InitializeCamera()
		{
			m_camera.Follow = m_target.transform;
			m_camera.LookAt = player.transform;

			Reset();
		}

		protected virtual bool VerticalFollowingStates()
		{
			return true;
		}

		public virtual void Reset()
		{
			m_cameraDistance = maxDistance;
			m_cameraTargetPitch = initialAngle;
			m_cameraTargetYaw = player.transform.rotation.eulerAngles.y;
			m_cameraTargetPosition = player.unsizedPosition + Vector3.up * heightOffset;
			MoveTarget();
			m_brain.ManualUpdate();//强制立即更新摄像机逻辑
		}

		protected virtual void HandleOffset()//摄像机在垂直方向上的滞后跟随效果
		{
			var target = player.unsizedPosition + Vector3.up * heightOffset;
			var previousPosition = m_cameraTargetPosition;
			var targetHeight = previousPosition.y;

			if (player.isGrounded || VerticalFollowingStates())
			{
				if (target.y > previousPosition.y + verticalUpDeadZone)
				{
					var offset = target.y - previousPosition.y - verticalUpDeadZone;
					targetHeight += Mathf.Min(offset, maxVerticalSpeed * Time.deltaTime);
				}
				else if (target.y < previousPosition.y - verticalDownDeadZone)
				{
					var offset = target.y - previousPosition.y + verticalDownDeadZone;
					targetHeight += Mathf.Max(offset, -maxVerticalSpeed * Time.deltaTime);
				}
			}
			else if (target.y > previousPosition.y + verticalAirUpDeadZone)
			{
				var offset = target.y - previousPosition.y - verticalAirUpDeadZone;
				targetHeight += Mathf.Min(offset, maxAirVerticalSpeed * Time.deltaTime);
			}
			else if (target.y < previousPosition.y - verticalAirDownDeadZone)
			{
				var offset = target.y - previousPosition.y + verticalAirDownDeadZone;
				targetHeight += Mathf.Max(offset, -maxAirVerticalSpeed * Time.deltaTime);
			}

			m_cameraTargetPosition = new Vector3(target.x, targetHeight, target.z);
		}

		protected virtual void HandleOrbit()
		{
			if (!canOrbit || player == null || player.inputs == null) return;
			if (canOrbit)// 是否允许旋转
			{
				var direction = player.inputs.GetLookDirection();

				if (direction.sqrMagnitude > 0)
				{
					var usingMouse = player.inputs.IsLookingWithMouse();
					float deltaTimeMultiplier = usingMouse ? Time.timeScale : Time.deltaTime;

					m_cameraTargetYaw += direction.x * deltaTimeMultiplier;//绕 Y 轴旋转，对应水平方向（左右转头）
					m_cameraTargetPitch -= direction.z * deltaTimeMultiplier;
					m_cameraTargetPitch = ClampAngle(m_cameraTargetPitch, verticalMinRotation, verticalMaxRotation);//对应垂直方向（抬头/低头）
				}
			}
		}

		protected virtual void HandleVelocityOrbit()
		{
			if (canOrbitWithVelocity && player.isGrounded)
			{
				var localVelocity = m_target.InverseTransformVector(player.velocity);
				m_cameraTargetYaw += localVelocity.x * orbitVelocityMultiplier * Time.deltaTime;
			}
		}

		protected virtual void MoveTarget()
		{
			m_target.position = m_cameraTargetPosition;
			m_target.rotation = Quaternion.Euler(m_cameraTargetPitch, m_cameraTargetYaw, 0.0f);
			m_cameraBody.CameraDistance = m_cameraDistance;
		}
		private void Awake()
		{
			enabled = false;
		}

		// 重写Start方法
		protected virtual void Start()
		{
			// 初始化组件
			InitializeComponents();
			// 初始化跟随者
			InitializeFollower();
			// 初始化相机
			InitializeCamera();
		}

		protected virtual void LateUpdate()
		{
			if (player == null || m_target == null || m_camera == null) return;

			HandleOrbit();
			HandleVelocityOrbit();
			HandleOffset();
			MoveTarget();
		}

		protected virtual float ClampAngle(float angle, float min, float max)
		{
			if (angle < -360)
			{
				angle += 360;
			}

			if (angle > 360)
			{
				angle -= 360;
			}

			return Mathf.Clamp(angle, min, max);
		}
		public void BindPlayer(Player target)
		{
			if (target == null) return;

			player = target;

			if (m_camera == null || m_cameraBody == null || m_brain == null)
			{
				InitializeComponents();
			}

			if (m_target == null)
			{
				InitializeFollower();
			}

			InitializeCamera();
			enabled = true;
		}

    }
}