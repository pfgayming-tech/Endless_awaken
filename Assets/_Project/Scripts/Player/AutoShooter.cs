using UnityEngine;
using VSL.VFX;

namespace VSL
{
    public class AutoShooter : MonoBehaviour
    {
        [Header("Split Spawn Offset")]
        [Tooltip("분열 탄이 서로 겹치지 않게 좌/우로 벌리는 거리(월드 단위)")]
        public float splitLateralSpacing = 0.25f;

        [Header("Prefabs")]
        public Projectile projectilePrefab;
        public SpiritOrb spiritOrbPrefab;

        [Header("Spawn Point (optional)")]
        public Transform muzzle;
        public float spawnForwardOffset = 0.6f;

        [Header("Targeting")]
        public LayerMask enemyLayer;
        public float targetRange = 12f;

        [Header("Homing")]
        [Tooltip("속도가 빠르면(예: 20~30) sharpness도 25~60으로 올려야 휘는 게 보임")]
        public float homingSharpness = 25f;

        [Header("Split (spread)")]
        public float splitSpreadAngle = 8f;

        [Header("Movement Safety")]
        [Tooltip("Stats의 projectileSpeed가 0~낮게 들어와도 멈춘 것처럼 보이지 않게")]
        public float minProjectileSpeed = 6f;

        [Header("VFX")]
        public bool enableMuzzleFlash = true;
        public float muzzleFlashDuration = 0.10f;
        public float muzzleFlashFromScale = 0.35f;
        public float muzzleFlashToScale = 0.90f;
        public int muzzleFlashSortingOrder = 200;

        private PlayerStats _stats;
        private float _cooldown;

        private Transform[] _spirits;
        private float _spiritAngle;

        private Vector3 FirePos => muzzle ? muzzle.position : transform.position;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            SetupSpiritsIfNeeded();
        }

        private void Update()
        {
            if (_stats == null) return;

            if (_stats.weaponType == WeaponType.Spirits)
            {
                EnsureSpiritCount();
                UpdateSpiritOrbit();
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            float rate = Mathf.Max(0.2f, _stats.fireRate);
            _cooldown = 1f / rate;

            DoHomingShots();
        }

        private void SpawnMuzzleFlash(Vector3 pos)
        {
            if (!enableMuzzleFlash) return;
            SimpleFlashVfx.Spawn(pos, muzzleFlashDuration, muzzleFlashFromScale, muzzleFlashToScale, muzzleFlashSortingOrder);
        }

        private void DoHomingShots()
        {
            if (projectilePrefab == null) return;

            Transform target = FindNearestEnemyRoot();
            if (target == null) return;

            Vector3 origin = FirePos;

            // ✅ 목표 좌표는 콜라이더 중심(피벗/자식 콜라이더 문제 방지)
            Vector2 aimPos = GetAimPoint(target);
            Vector2 to = aimPos - (Vector2)origin;
            if (to.sqrMagnitude < 0.0001f) to = Vector2.right;

            Vector2 baseDir = to.normalized;

            int shots = 1 + Mathf.Max(0, _stats.extraProjectiles);
            float spread = Mathf.Max(0f, splitSpreadAngle);

            float speed = Mathf.Max(minProjectileSpeed, _stats.projectileSpeed);

            for (int i = 0; i < shots; i++)
            {
                float angle = 0f;
                if (shots > 1 && spread > 0f)
                {
                    float t = i / (shots - 1f);
                    angle = Mathf.Lerp(-spread, spread, t);
                }

                Vector2 shotDir = (Quaternion.Euler(0, 0, angle) * baseDir).normalized;

                // ✅ 좌/우로 벌리는 방향(shotDir의 수직 벡터)
                Vector2 perp = new Vector2(-shotDir.y, shotDir.x);

                // ✅ i를 -..0..+ 로 센터링
                float center = (shots - 1) * 0.5f;
                float lateral = (i - center) * splitLateralSpacing;

                Vector3 spawnPos = origin
                    + (Vector3)(shotDir * spawnForwardOffset)
                    + (Vector3)(perp * lateral);

                SpawnMuzzleFlash(spawnPos);

                var p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

                p.InitHomingPierce(
                    target: target,
                    damage: _stats.damage,
                    speed: speed,
                    enemyLayer: enemyLayer,
                    homingSharpness: homingSharpness,
                    pierce: _stats.pierce,
                    retargetRange: targetRange
                );
            }

        }

        private Transform FindNearestEnemyRoot()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, targetRange, enemyLayer);
            if (hits == null || hits.Length == 0) return null;

            float best = float.MaxValue;
            Transform bestT = null;

            Vector3 origin = FirePos;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i].GetComponentInParent<Health>();
                if (h == null) continue;

                Transform t = h.transform; // ✅ Health 루트
                Vector2 center = GetAimPoint(t);

                float d = ((Vector2)origin - center).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestT = t;
                }
            }

            return bestT;
        }

        private Vector2 GetAimPoint(Transform targetRoot)
        {
            if (targetRoot == null) return Vector2.zero;

            var col = targetRoot.GetComponent<Collider2D>();
            if (col == null) col = targetRoot.GetComponentInChildren<Collider2D>();
            if (col != null) return (Vector2)col.bounds.center;

            return (Vector2)targetRoot.position;
        }

        // -------------------------------
        // Spirits (원본 유지)
        // -------------------------------
        private void SetupSpiritsIfNeeded()
        {
            if (_stats == null) return;
            if (_stats.weaponType != WeaponType.Spirits) return;
            if (spiritOrbPrefab == null) return;

            int count = Mathf.Max(0, _stats.spiritCount);
            _spirits = new Transform[count];

            for (int i = 0; i < count; i++)
            {
                var orb = Instantiate(spiritOrbPrefab, transform.position, Quaternion.identity);
                orb.Bind(transform, i, count, _stats.damage * _stats.spiritDamageMult, enemyLayer);
                _spirits[i] = orb.transform;
            }
        }

        private void EnsureSpiritCount()
        {
            if (_stats.weaponType != WeaponType.Spirits) return;
            if (spiritOrbPrefab == null) return;

            int desired = Mathf.Max(0, _stats.spiritCount);
            int current = _spirits == null ? 0 : _spirits.Length;
            if (desired == current) return;

            if (_spirits != null)
            {
                for (int i = 0; i < _spirits.Length; i++)
                    if (_spirits[i] != null) Destroy(_spirits[i].gameObject);
            }

            _spirits = new Transform[desired];
            for (int i = 0; i < desired; i++)
            {
                var orb = Instantiate(spiritOrbPrefab, transform.position, Quaternion.identity);
                orb.Bind(transform, i, desired, _stats.damage * _stats.spiritDamageMult, enemyLayer);
                _spirits[i] = orb.transform;
            }
        }

        private void UpdateSpiritOrbit()
        {
            if (_spirits == null || _spirits.Length == 0) return;

            _spiritAngle += Time.deltaTime * 120f;
            for (int i = 0; i < _spirits.Length; i++)
            {
                var orb = _spirits[i] != null ? _spirits[i].GetComponent<SpiritOrb>() : null;
                if (orb != null) orb.SetOrbitAngle(_spiritAngle);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, targetRange);
        }
    }
}
