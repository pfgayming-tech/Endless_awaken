using UnityEngine;

namespace VSL
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Move")]
        public float moveSpeed = 2.5f;

        [Header("Contact Damage")]
        public int contactDamage = 8;
        public float contactInterval = 0.6f;

        [Header("Reward")]
        public int xpValue = 6;

        [Header("Target")]
        public string playerTag = "Player";

        [Header("HP (Base)")]
        public int baseHp = 25;

        private Rigidbody2D _rb;
        private Transform _target;
        private float _contactTimer;
        private Health _health;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();

            // 물리 기본 안정 세팅 (원하면 지워도 됨)
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) _target = p.transform;

            // 체력 초기화
            if (_health != null)
            {
                float mult = (GameManager.Instance != null) ? GameManager.Instance.EnemyHpMultiplier : 1f;
                _health.Init(Mathf.RoundToInt(baseHp * mult));
                _health.OnDied += OnDied;
            }
            else
            {
                Debug.LogWarning($"[EnemyController] Health 컴포넌트가 없음: {name}");
            }
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.0001f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            dir.Normalize();
            _rb.linearVelocity = dir * moveSpeed;
        }

        private void Update()
        {
            _contactTimer -= Time.deltaTime;
        }

        // ----------------------------
        // Contact damage: Collision
        // ----------------------------
        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDealContactDamage(collision.collider);
        }

        // ----------------------------
        // Contact damage: Trigger (옵션)
        // - 만약 적/플레이어 콜라이더가 Trigger면 이쪽이 불림
        // ----------------------------
        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealContactDamage(other);
        }

        private void TryDealContactDamage(Collider2D other)
        {
            if (_contactTimer > 0f) return;
            if (other == null) return;

            // ✅ 콜라이더가 자식이어도 Player 루트 태그로 판정
            var root = other.transform.root;
            if (root == null || !root.CompareTag(playerTag)) return;

            // ✅ 루트(또는 부모 체인)에서 Health를 찾는다
            var h = other.GetComponentInParent<Health>();
            if (h == null) h = root.GetComponentInChildren<Health>();
            if (h == null) return;

            h.TakeDamage(contactDamage);
            _contactTimer = contactInterval;
        }

        private void OnDied()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddXP(xpValue);

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDied -= OnDied;
        }
    }
}
