using UnityEngine;

namespace VSL
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        public float moveSpeed = 2.5f;
        public int contactDamage = 8;
        public float contactInterval = 0.6f;
        public int xpValue = 6;

        private Rigidbody2D _rb;
        private Transform _target;
        private float _contactTimer;
        private Health _health;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _target = p.transform;

            // 난이도 체력 배율 적용
            int baseHp = 25;
            float mult = GameManager.Instance != null ? GameManager.Instance.EnemyHpMultiplier : 1f;
            _health.Init(Mathf.RoundToInt(baseHp * mult));

            _health.OnDied += OnDied;
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            Vector2 dir = (_target.position - transform.position).normalized;
            _rb.linearVelocity = dir * moveSpeed;
        }

        private void Update()
        {
            _contactTimer -= Time.deltaTime;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_contactTimer > 0f) return;
            if (!collision.collider.CompareTag("Player")) return;

            var h = collision.collider.GetComponent<Health>();
            if (h != null)
            {
                h.TakeDamage(contactDamage);
                _contactTimer = contactInterval;
            }
        }

        private void OnDied()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddXP(xpValue);

            Destroy(gameObject);
        }
    }
}
