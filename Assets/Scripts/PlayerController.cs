using UnityEngine;

namespace AntonDoors
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 4.4f;
        public float SprintSpeed = 6.6f;
        public float Gravity = -22f;
        public float MouseSensitivity = 1.5f;
        public Camera ViewCamera;

        private CharacterController controller;
        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private float knockbackTimer;
        private Vector3 knockbackDir;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ApplyKnockback(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.back;
            knockbackDir = dir.normalized;
            knockbackTimer = 0.25f;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameManager.RunState.Playing)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }

            yaw += Input.GetAxis("Mouse X") * MouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * MouseSensitivity;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (ViewCamera != null) ViewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = transform.forward * v + transform.right * h;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : WalkSpeed;
            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.deltaTime;
                move += knockbackDir * 6f;
            }

            verticalVelocity += Gravity * Time.deltaTime;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

        }
    }
}
