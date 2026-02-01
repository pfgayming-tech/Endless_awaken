using System.Collections.Generic;
using UnityEngine;

namespace VSL
{
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        // -------------------------------
        // Pierce Behavior
        // -------------------------------
        [Header("Pierce Behavior")]
        [Tooltip("관통(pierce>0)일 때: 첫 타격 이후엔 추적/리타겟 없이 직진하게 함")]
        public bool pierceGoStraightAfterHit = true;

        // 관통 직진 모드가 활성화될 수 있는지(관통이 있는 투사체일 때만 true)
        private bool _straightModeAfterHit;

        // -------------------------------
        // Offscreen Despawn
        // -------------------------------
        [Header("Offscreen Despawn")]
        public bool destroyWhenOffscreen = true;

        [Tooltip("화면 가장자리 여유(뷰포트 기준). 0.05면 5% 밖으로 나가야 삭제 판정")]
        public float offscreenMargin = 0.08f;

        [Tooltip("잠깐 나갔다가 들어오는 경우 방지용. 이 시간 이상 화면 밖이면 삭제")]
        public float offscreenGraceTime = 0.15f;

        private float _offscreenTimer = 0f;

        // -------------------------------
        // Visual / Facing
        // -------------------------------
        public enum FacingAxis { Right, Up }

        [Header("Visual")]
        public FacingAxis facingAxis = FacingAxis.Up; // 삼각형 꼭짓점이 위면 Up
        public float spriteAngleOffset = 0f;          // 필요하면 180 등으로 미세 조정

        // -------------------------------
        // Lifetime / Movement
        // -------------------------------
        [Header("Lifetime")]
        public float lifeTime = 4f;

        [Header("Movement")]
        [Tooltip("projectileSpeed가 너무 낮아 멈춘 것처럼 보일 때를 방지")]
        public float minSpeed = 6f;

        // -------------------------------
        // Homing / Acquire
        // -------------------------------
        [Header("Homing Auto Acquire")]
        public bool autoAcquireTarget = true;
        public float acquireInterval = 0.08f;

        // -------------------------------
        // Debug
        // -------------------------------
        [Header("Debug")]
        public bool debugLog = false;

        // runtime stats
        private float _damage;
        private float _speed;
        private LayerMask _enemyLayer;

        // movement
        private Vector2 _dir = Vector2.right;

        // target
        private Transform _target;
        private Rigidbody2D _targetRb;
        private Collider2D _targetCol;
        private float _homingSharpness;

        // pierce
        private int _remainingHits = 1;      // pierce=0이면 1번 맞고 종료
        private float _retargetRange = 0f;   // 다음 타겟 탐색 범위
        private readonly HashSet<int> _hitSet = new HashSet<int>(64);

        // physics
        private Collider2D _col;
        private Rigidbody2D _rb;

        // timers
        private float _life;
        private float _acquireTimer;

        private void Awake()
        {
            EnsurePhysics();
        }

        private void EnsurePhysics()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;

            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();

            // ✅ "확실하게 움직이게" Dynamic + velocity 사용
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.simulated = true;
            _rb.gravityScale = 0f;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // 절대 잠들지 않게(가끔 슬립 걸리면 멈춘 것처럼 보임)
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        private void ResetRuntimeState()
        {
            _life = lifeTime;
            _acquireTimer = 0f;

            _damage = 0f;
            _speed = minSpeed;
            _enemyLayer = 0;

            _target = null;
            _targetRb = null;
            _targetCol = null;

            _homingSharpness = 0f;

            _remainingHits = 1;
            _retargetRange = 0f;

            _hitSet.Clear();
            _dir = Vector2.right;

            _offscreenTimer = 0f;

            // ✅ 관통 직진 모드 초기화
            _straightModeAfterHit = false;

            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }

        // -------------------------------
        // 직진
        // -------------------------------
        public void Init(float damage, float speed, Vector2 direction, LayerMask enemyLayer)
        {
            ResetRuntimeState();

            _damage = damage;
            _speed = Mathf.Max(minSpeed, speed);
            _enemyLayer = enemyLayer;

            _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            ApplyRotationFromDir();

            if (debugLog)
                Debug.Log($"[Projectile] Init straight speed={_speed} dir={_dir}", this);
        }

        // -------------------------------
        // 호밍 + 관통 + 리타겟
        // -------------------------------
        public void InitHomingPierce(
            Transform target,
            float damage,
            float speed,
            LayerMask enemyLayer,
            float homingSharpness,
            int pierce,
            float retargetRange)
        {
            ResetRuntimeState();

            _damage = damage;
            _speed = Mathf.Max(minSpeed, speed);
            _enemyLayer = enemyLayer;

            _homingSharpness = Mathf.Max(0f, homingSharpness);
            _remainingHits = Mathf.Max(1, pierce + 1);
            _retargetRange = Mathf.Max(0f, retargetRange);

            // ✅ 관통이 있는 투사체면 "첫 타격 이후 직진" 모드 활성 가능
            _straightModeAfterHit = pierceGoStraightAfterHit && (pierce > 0);

            // ✅ target이 콜라이더 자식이어도 Health 루트로 정규화
            _target = NormalizeToHealthRoot(target);
            CacheTargetRefs();

            // ✅ target이 null이면 자동 탐색
            if (_target == null && autoAcquireTarget && _retargetRange > 0f)
            {
                _target = FindNextTarget(transform.position, _retargetRange);
                CacheTargetRefs();
            }

            // 초기 방향
            if (_target != null)
            {
                Vector2 to = GetTargetPos2D() - (Vector2)transform.position;
                _dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
            }
            else
            {
                _dir = Vector2.right;
            }

            ApplyRotationFromDir();

            if (debugLog)
                Debug.Log($"[Projectile] InitHomingPierce speed={_speed}, sharp={_homingSharpness}, range={_retargetRange}, hits={_remainingHits}, straightAfterHit={_straightModeAfterHit}, target={(_target ? _target.name : "null")}", this);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            _life -= dt;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 타겟 비활성/파괴 처리
            if (_target != null && !_target.gameObject.activeInHierarchy)
            {
                _target = null;
                _targetRb = null;
                _targetCol = null;
            }

            // ✅ 타겟 없으면 주기적으로 자동 탐색
            if (_target == null && autoAcquireTarget && _retargetRange > 0f)
            {
                _acquireTimer -= dt;
                if (_acquireTimer <= 0f)
                {
                    _acquireTimer = acquireInterval;
                    _target = FindNextTarget(transform.position, _retargetRange);
                    CacheTargetRefs();

                    if (debugLog && _target != null)
                        Debug.Log($"[Projectile] AutoAcquire -> {_target.name}", this);
                }
            }

            // ✅ 호밍(방향 업데이트)
            if (_target != null)
            {
                Vector2 desired = GetTargetPos2D() - (Vector2)transform.position;
                if (desired.sqrMagnitude > 0.0001f)
                {
                    desired.Normalize();

                    if (_homingSharpness <= 0f)
                    {
                        _dir = desired;
                    }
                    else
                    {
                        float t = 1f - Mathf.Exp(-_homingSharpness * dt);
                        _dir = Vector2.Lerp(_dir, desired, t).normalized;
                    }

                    ApplyRotationFromDir();
                }
            }

            // ✅ “무조건 이동” (velocity)
            if (_rb != null)
                _rb.linearVelocity = _dir * _speed;
            else
                transform.position += (Vector3)(_dir * _speed * dt);

            // ✅ 화면 밖이면 삭제
            if (destroyWhenOffscreen)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 vp = cam.WorldToViewportPoint(transform.position);

                    bool outside =
                        vp.z < 0f ||
                        vp.x < -offscreenMargin || vp.x > 1f + offscreenMargin ||
                        vp.y < -offscreenMargin || vp.y > 1f + offscreenMargin;

                    if (outside)
                    {
                        _offscreenTimer += dt;
                        if (_offscreenTimer >= offscreenGraceTime)
                        {
                            Destroy(gameObject);
                            return;
                        }
                    }
                    else
                    {
                        _offscreenTimer = 0f;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _enemyLayer) == 0) return;

            var h = other.GetComponentInParent<Health>();
            if (h == null) return;

            int id = h.GetInstanceID();
            if (_hitSet.Contains(id)) return;
            _hitSet.Add(id);

            h.TakeDamage(Mathf.RoundToInt(_damage));
            _remainingHits--;

            if (debugLog)
                Debug.Log($"[Projectile] HIT {h.name} remainingHits={_remainingHits}", this);

            if (_remainingHits <= 0)
            {
                Destroy(gameObject);
                return;
            }

            // ✅ 관통 직진 모드:
            // 첫 타격 이후엔 추적/리타겟/자동탐색을 끄고 "현재 방향 그대로" 직진하게 한다.
            if (_straightModeAfterHit)
            {
                _target = null;
                _targetRb = null;
                _targetCol = null;

                autoAcquireTarget = false;
                _retargetRange = 0f;

                if (debugLog)
                    Debug.Log("[Projectile] StraightAfterHit: disable retarget/autoAcquire", this);

                return; // ✅ 리타겟하지 않고 끝 (남은 hit은 직진하며 다른 적에 trigger로 맞음)
            }

            // ✅ 기존: 체인 호밍/리타겟
            if (_retargetRange > 0f)
            {
                _target = FindNextTarget(transform.position, _retargetRange);
                CacheTargetRefs();
            }
        }

        private Transform FindNextTarget(Vector3 from, float range)
        {
            var hits = Physics2D.OverlapCircleAll(from, range, _enemyLayer);
            if (hits == null || hits.Length == 0) return null;

            float best = float.MaxValue;
            Transform bestT = null;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i].GetComponentInParent<Health>();
                if (h == null) continue;

                int hid = h.GetInstanceID();
                if (_hitSet.Contains(hid)) continue;

                Transform t = h.transform;
                float d = (t.position - from).sqrMagnitude;

                if (d < best)
                {
                    best = d;
                    bestT = t;
                }
            }

            return bestT;
        }

        private Transform NormalizeToHealthRoot(Transform t)
        {
            if (t == null) return null;
            var h = t.GetComponentInParent<Health>();
            return h != null ? h.transform : t;
        }

        private void CacheTargetRefs()
        {
            _targetRb = null;
            _targetCol = null;

            if (_target == null) return;

            _targetRb = _target.GetComponent<Rigidbody2D>();

            // ✅ 좌표는 히트박스 중심(피벗 문제 방지)
            _targetCol = _target.GetComponent<Collider2D>();
            if (_targetCol == null)
                _targetCol = _target.GetComponentInChildren<Collider2D>();
        }

        private Vector2 GetTargetPos2D()
        {
            if (_targetCol != null) return (Vector2)_targetCol.bounds.center;
            if (_targetRb != null) return _targetRb.position;
            return _target != null ? (Vector2)_target.position : (Vector2)transform.position;
        }

        private void ApplyRotationFromDir()
        {
            float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;

            // ✅ 스프라이트가 "위(+Y)"를 앞방향으로 그려졌다면 90도 보정
            if (facingAxis == FacingAxis.Up) angle -= 90f;

            angle += spriteAngleOffset;

            if (_rb != null) _rb.MoveRotation(angle);
            else transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
