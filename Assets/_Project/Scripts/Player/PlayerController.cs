using UnityEngine;

namespace VSL
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public VirtualJoystick joystick;
        private Rigidbody2D _rb;
        private PlayerStats _stats;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stats = GetComponent<PlayerStats>();

            // ✅ 카메라/이동 '틱틱' 튐 완화: 보간 켜기
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.freezeRotation = true;
            _rb.gravityScale = 0f;
        }

        private void Start()
        {
            if (joystick == null) joystick = FindObjectOfType<VirtualJoystick>();
        }

        private void FixedUpdate()
        {
            Vector2 input = Vector2.zero;

            if (joystick != null)
                input = joystick.Direction;
            else
            {
                // 에디터 테스트용
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }

            input = Vector2.ClampMagnitude(input, 1f);
            float speed = (_stats != null) ? _stats.moveSpeed : 4f;
            _rb.linearVelocity = input * speed;
        }
    }
}
