using UnityEngine;
using UnityEngine.InputSystem;

namespace ItTakesTwo
{
    public class PlayerInputManager: MonoBehaviour
    {
        public InputActionAsset actions;

        protected InputAction m_movement;
        protected InputAction m_look;

        protected Camera m_camera;

        protected float m_movementDirectionUnlockTime;

        protected const string k_mouseDeviceName = "Mouse";
        protected bool m_inputsEnabled = true;

        protected virtual void CacheActions()
        {
            m_movement = actions["Move"];
            m_look = actions["Look"];
            
        }

        protected virtual void Awake()
        {
            if (actions != null)
            {
                actions = Instantiate(actions); // 每个玩家独立一份
            }
            CacheActions();
        }

        protected virtual void Start()
        {
            m_camera = Camera.main;
            if (m_inputsEnabled)
            {
                actions?.Enable();
            }
        }

        protected virtual void OnEnable()
        {
            if (m_inputsEnabled)
            {
                actions?.Enable();
            }
        }

        protected virtual void OnDisable() => actions?.Disable();

        public virtual Vector3 GetMovementDirection()
        {
            if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;

            var value = m_movement.ReadValue<Vector2>();
            return GetAxisWithCrossDeadZone(value);
        }

        public virtual Vector3 GetMovementCameraDirection()
        {
            var direction = GetMovementDirection();

            if (direction.sqrMagnitude > 0)
            {
                var rotation = Quaternion.AngleAxis(m_camera.transform.eulerAngles.y, Vector3.up);
                direction = rotation * direction;
                direction = direction.normalized;
            }

            return direction;
        }
        public virtual Vector3 GetLookDirection()
        {
            var value = m_look.ReadValue<Vector2>();

            if (IsLookingWithMouse())
            {
                return new Vector3(value.x, 0, value.y);
            }

            return GetAxisWithCrossDeadZone(value);
        }

        /// <summary>
        /// 死区处理
        /// </summary>
        /// <param name="axis">死区大小</param>
        /// <returns></returns>
        public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis)
        {
            var deadzone = InputSystem.settings.defaultDeadzoneMin;
            axis.x = Mathf.Abs(axis.x) > deadzone ? RemapToDeadzone(axis.x, deadzone) : 0;
            axis.y = Mathf.Abs(axis.y) > deadzone ? RemapToDeadzone(axis.y, deadzone) : 0;
            return new Vector3(axis.x, 0, axis.y);
        }
        public virtual bool IsLookingWithMouse()
        {
            if (m_look.activeControl == null)
            {
                return false;
            }

            return m_look.activeControl.device.name.Equals(k_mouseDeviceName);
        }
        public void SetInputsEnabled(bool enabled)
        {
            m_inputsEnabled = enabled;
            if (enabled)
            {
                actions?.Enable();
            }
            else
            {
                actions?.Disable();
            }
        }

        protected float RemapToDeadzone(float value, float deadzone) => (value - deadzone) / (1 - deadzone);
        
    }
}