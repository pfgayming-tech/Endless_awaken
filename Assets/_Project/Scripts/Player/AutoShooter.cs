using System.Collections;
using UnityEngine;
using VSL.VFX;

namespace VSL
{
    public class AutoShooter : MonoBehaviour
    {
        // -----------------------
        // Ultimate hooks
        // -----------------------
        [Header("Ultimate Hooks")]
        [Tooltip("궁수 '표식'용 강제 타겟(보스/엘리트 등). UltimateSystem이 넣어줌")]
        public Transform forcedTarget;

        [Tooltip("궁극기 버프 배율(없으면 자동으로 GetComponent)")]
        public PlayerBuffs buffs;

        // -----------------------
        // Split spacing
        // -----------------------
        [Header("Split Spawn Offset")]
        [Tooltip("분열 탄이 서로 겹치지 않게 좌/우로 벌리는 거리(월드 단위)")]
        public float splitLateralSpacing = 0.25f;

        // -----------------------
        // Prefabs
        // -----------------------
        [Header("Prefabs")]
        public Projectile projectilePrefab;
        public SpiritOrb spiritOrbPrefab;

        // -----------------------
        // Spawn point
        // -----------------------
        [Header("Spawn Point (optional)")]
        public Transform muzzle;
        public float spawnForwardOffset = 0.6f;

        // -----------------------
        // Targeting
        // -----------------------
        [Header("Targeting")]
        public LayerMask enemyLayer;
        public float targetRange = 12f;

        // -----------------------
        // Homing
        // -----------------------
        [Header("Homing")]
        [Tooltip("속도가 빠르면(예: 20~30) sharpness도 25~60으로 올려야 휘는 게 보임")]
        public float homingSharpness = 25f;

        // -----------------------
        // Split spread
        // -----------------------
        [Header("Split (spread)")]
        public float splitSpreadAngle = 8f;

        // -----------------------
        // Movement safety
        // -----------------------
        [Header("Movement Safety")]
        [Tooltip("Stats의 projectileSpeed가 0~낮게 들어와도 멈춘 것처럼 보이지 않게")]
        public float minProjectileSpeed = 6f;

        // -----------------------
        // VFX
        // -----------------------
        [Header("VFX")]
        public bool enableMuzzleFlash = true;
        public float muzzleFlashDuration = 0.10f;
        public float muzzleFlashFromScale = 0.35f;
        public float muzzleFlashToScale = 0.90f;
        public int muzzleFlashSortingOrder = 200;

        // -----------------------
        // Internals
        // -----------------------
        private PlayerStats _stats;
        private float _cooldown;

        private Transform[] _spirits;
        private float _spiritAngle;

        private Vector3 FirePos => muzzle ? muzzle.position : transform.position;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            if (buffs == null) buffs = GetComponent<PlayerBuffs>();
        }

        private void Start()
        {
            SetupSpiritsIfNeeded();
        }

        private void Update()
        {
            if (_stats == null) return;

            // Spirits 무기는 기존 유지
            if (_stats.weaponType == WeaponType.Spirits)
            {
                EnsureSpiritCount();
                UpdateSpiritOrbit();
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            // ✅ 궁극기 버프(공속) 반영
            float frMult = (buffs != null) ? buffs.fireRateMult : 1f;
            float rate = Mathf.Max(0.2f, _stats.fireRate * Mathf.Max(0.01f, frMult));
            _cooldown = 1f / rate;

            DoHomingShots_WithUltimates();
        }

        private void SpawnMuzzleFlash(Vector3 pos)
        {
            if (!enableMuzzleFlash) return;
            SimpleFlashVfx.Spawn(pos, muzzleFlashDuration, muzzleFlashFromScale, muzzleFlashToScale, muzzleFlashSortingOrder);
        }

        // ============================================================
        // ✅ 궁극기 적용 발사
        // - forcedTarget(표식)이 있으면 그 적 우선
        // - buffs: damage/fireRate/speed/pierce/split/support/echo 적용
        // ============================================================
        private void DoHomingShots_WithUltimates()
        {
            if (projectilePrefab == null) return;

            Transform target = SelectTarget();
            if (target == null) return;

            Vector3 origin = FirePos;

            // ✅ 목표 좌표는 콜라이더 중심(피벗/자식 콜라이더 문제 방지)
            Vector2 aimPos = GetAimPoint(target);
            Vector2 to = aimPos - (Vector2)origin;
            if (to.sqrMagnitude < 0.0001f) to = Vector2.right;

            Vector2 baseDir = to.normalized;

            // --------- Ultimate buff values ----------
            float dmgMult = (buffs != null) ? buffs.damageMult : 1f;
            float spdMult = (buffs != null) ? buffs.projectileSpeedMult : 1f;
            int pierceBonus = (buffs != null) ? buffs.pierceBonus : 0;
            int splitBonus = (buffs != null) ? buffs.extraProjectilesBonus : 0;

            // ✅ 최종 스탯
            float damage = _stats.damage * Mathf.Max(0.01f, dmgMult);
            float speed = Mathf.Max(minProjectileSpeed, _stats.projectileSpeed * Mathf.Max(0.01f, spdMult));

            int pierce = Mathf.Max(0, _stats.pierce + pierceBonus);
            int shots = 1 + Mathf.Max(0, _stats.extraProjectiles + splitBonus);

            float spread = Mathf.Max(0f, splitSpreadAngle);

            // 1) 기본 탄(분열 포함)
            for (int i = 0; i < shots; i++)
            {
                SpawnOneHomingPierceProjectile(
                    origin: origin,
                    target: target,
                    baseDir: baseDir,
                    shotIndex: i,
                    shotCount: shots,
                    spread: spread,
                    damage: damage,
                    speed: speed,
                    pierce: pierce
                );
            }

            // 2) 궁수 궁극기 같은 “지원 화살(추가 탄)” — attack당 몇 발 추가
            int support = (buffs != null) ? Mathf.Max(0, buffs.supportShots) : 0;
            if (support > 0)
            {
                for (int k = 0; k < support; k++)
                {
                    // 지원탄은 퍼짐을 조금만(또는 0) 주는 게 깔끔
                    float tinySpread = Mathf.Min(6f, spread * 0.5f);
                    SpawnOneHomingPierceProjectile(
                        origin: origin,
                        target: target,
                        baseDir: baseDir,
                        shotIndex: k,
                        shotCount: Mathf.Max(1, support),
                        spread: tinySpread,
                        damage: damage,
                        speed: speed,
                        pierce: pierce
                    );
                }
            }

            // ✅ 공격 트리거 이벤트(궁극기 시스템 등)
            WeaponTriggerHub.RaiseAttackTriggered(gameObject, target, damage);

            // 3) 마법사 궁극기 “에코(지연 1회 추가 발사)” 느낌을 AutoShooter에서도 구현 가능
            //    (버프가 켜져 있으면, 일정 확률로 같은 타겟에게 딜 약한 탄을 지연 발사)
            if (buffs != null && buffs.echoEnabled && buffs.echoChance > 0f)
            {
                float roll = Random.value;
                if (roll <= buffs.echoChance)
                {
                    float echoDelay = Mathf.Max(0f, buffs.echoDelay);
                    float echoDmg = damage * Mathf.Max(0.01f, buffs.echoDamageMult);

                    // 에코는 "1발만" 추가(원하면 support처럼 여러 발로도 바꿀 수 있음)
                    StartCoroutine(Co_EchoShot(echoDelay, origin, target, baseDir, echoDmg, speed, pierce));
                }
            }
        }

        private IEnumerator Co_EchoShot(float delay, Vector3 origin, Transform target, Vector2 baseDir, float damage, float speed, int pierce)
        {
            // TimeScale=0에서도 UI/연출이 안 멈추게 하고 싶으면 Realtime 사용
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            if (this == null || !gameObject.activeInHierarchy) yield break;
            if (projectilePrefab == null) yield break;

            // 타겟이 죽었으면 가장 가까운 적으로 재선정
            Transform t = IsValidEnemyTarget(target) ? target : FindNearestEnemyRoot();

            if (t == null) yield break;

            // 에코는 스폰 위치를 살짝만 앞으로
            Vector3 spawnPos = origin + (Vector3)(baseDir.normalized * spawnForwardOffset);
            SpawnMuzzleFlash(spawnPos);

            var p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // ✅ Projectile이 InitHomingPierce를 지원해야 함!
            p.InitHomingPierce(
                target: t,
                damage: damage,
                speed: speed,
                enemyLayer: enemyLayer,
                homingSharpness: homingSharpness,
                pierce: pierce,
                retargetRange: targetRange
            );
        }

        private void SpawnOneHomingPierceProjectile(
            Vector3 origin,
            Transform target,
            Vector2 baseDir,
            int shotIndex,
            int shotCount,
            float spread,
            float damage,
            float speed,
            int pierce
        )
        {
            // 퍼짐 각 계산
            float angle = 0f;
            if (shotCount > 1 && spread > 0f)
            {
                float t = shotIndex / (shotCount - 1f);
                angle = Mathf.Lerp(-spread, spread, t);
            }

            Vector2 shotDir = (Quaternion.Euler(0, 0, angle) * baseDir).normalized;

            // 좌/우 벌리기(수직 벡터)
            Vector2 perp = new Vector2(-shotDir.y, shotDir.x);

            float center = (shotCount - 1) * 0.5f;
            float lateral = (shotIndex - center) * splitLateralSpacing;

            Vector3 spawnPos = origin
                               + (Vector3)(shotDir * spawnForwardOffset)
                               + (Vector3)(perp * lateral);

            SpawnMuzzleFlash(spawnPos);

            var p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // ✅ Projectile이 InitHomingPierce를 지원해야 함!
            p.InitHomingPierce(
                target: target,
                damage: damage,
                speed: speed,
                enemyLayer: enemyLayer,
                homingSharpness: homingSharpness,
                pierce: pierce,
                retargetRange: targetRange
            );
        }

        // ============================================================
        // Target selection
        // ============================================================
        private Transform SelectTarget()
        {
            // 1) forcedTarget가 유효하면 최우선 (궁수 표식)
            if (IsValidEnemyTarget(forcedTarget))
            {
                // forcedTarget가 자식 콜라이더일 수 있으니 Health 루트로 올림
                var h = forcedTarget.GetComponentInParent<Health>();
                if (h != null) return h.transform;

                // 그래도 없으면 forcedTarget 자체 사용
                return forcedTarget;
            }

            // 2) 아니면 가장 가까운 적
            return FindNearestEnemyRoot();
        }

        private bool IsValidEnemyTarget(Transform t)
        {
            if (t == null) return false;
            if (!t.gameObject.activeInHierarchy) return false;

            // 레이어 체크(혹시 forcedTarget가 적이 아닌데 들어온 경우 방지)
            int mask = 1 << t.gameObject.layer;
            if ((mask & enemyLayer.value) == 0)
            {
                // 부모/Health 루트가 적 레이어일 수도 있음
                var hh = t.GetComponentInParent<Health>();
                if (hh == null) return false;

                int pmask = 1 << hh.gameObject.layer;
                if ((pmask & enemyLayer.value) == 0) return false;
            }

            // 거리 체크
            Vector3 origin = FirePos;
            float d2 = (t.position - origin).sqrMagnitude;
            return d2 <= (targetRange * targetRange);
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

                Transform t = h.transform; // Health 루트
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

        // ============================================================
        // Spirits (원본 유지)
        // ============================================================
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

        internal void FireExtraHomingBursts(Transform target, int totalExtra, float extraDmg, float homingSharpness, float spreadAngle)
        {
            if (projectilePrefab == null) return;
            if (_stats == null) _stats = GetComponent<PlayerStats>();
            if (_stats == null) return;

            // 타겟이 죽었거나 범위 밖이면 가장 가까운 적으로 재선정
            Transform t = IsValidEnemyTarget(target) ? target : FindNearestEnemyRoot();
            if (t == null) return;

            Vector3 origin = FirePos;

            // 목표 좌표는 콜라이더 중심
            Vector2 aimPos = GetAimPoint(t);
            Vector2 to = aimPos - (Vector2)origin;
            if (to.sqrMagnitude < 0.0001f) to = Vector2.right;

            Vector2 baseDir = to.normalized;

            // 속도/관통은 현재 스탯 + 버프 기준으로
            float spdMult = (buffs != null) ? buffs.projectileSpeedMult : 1f;
            int pierceBonus = (buffs != null) ? buffs.pierceBonus : 0;

            float speed = Mathf.Max(minProjectileSpeed, _stats.projectileSpeed * Mathf.Max(0.01f, spdMult));
            int pierce = Mathf.Max(0, _stats.pierce + pierceBonus);

            int count = Mathf.Max(1, totalExtra);
            float spread = Mathf.Max(0f, spreadAngle);

            for (int i = 0; i < count; i++)
            {
                // 퍼짐 각
                float angle = 0f;
                if (count > 1 && spread > 0f)
                {
                    float tt = i / (count - 1f);
                    angle = Mathf.Lerp(-spread, spread, tt);
                }

                Vector2 dir = (Quaternion.Euler(0, 0, angle) * baseDir).normalized;

                Vector3 spawnPos = origin + (Vector3)(dir * spawnForwardOffset);
                SpawnMuzzleFlash(spawnPos);

                var p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

                // extraDmg는 UltimateSystem이 이미 계산해서 넘겨줌(여기서 또 배율 적용 X)
                p.InitHomingPierce(
                    target: t,
                    damage: extraDmg,
                    speed: speed,
                    enemyLayer: enemyLayer,
                    homingSharpness: Mathf.Max(0f, homingSharpness),
                    pierce: pierce,
                    retargetRange: targetRange
                );
            }
        }
    }
}
