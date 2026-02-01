using System.Collections;
using System.Reflection;
using UnityEngine;

// (선택) 데미지 팝업을 궁극기 중 스타일로 바꾸는 기능을 쓰고 싶으면 유지
// 없다면 이 using과 아래 SetUltimateActive() 안의 DamagePopupUIPool 부분을 지워도 됨.
using VSL.VFX;

namespace VSL
{
    public class UltimateSystem : MonoBehaviour
    {
        // ✅ 궁극기 활성 여부(외부에서 보기용)
        public bool IsActive => _isActive;

        [Header("Refs")]
        public PlayerStats stats;
        public AutoShooter shooter;
        public PlayerBuffs buffs;

        [Header("Gauge")]
        public float gaugeMax = 100f;
        public float gaugePerSecond = 4f;   // 시간당 충전
        public float gaugePerKill = 8f;     // 처치당 충전

        [Header("Input")]
        public KeyCode activateKey = KeyCode.Space;

        [Header("Feedback (Visual)")]
        [Tooltip("궁극기 실행 중 켜둘 오브젝트(플레이어 자식 UltAura 등)")]
        public GameObject activeVfxObject;

        [Tooltip("궁극기 시작 시 1회 재생 파티클(선택)")]
        public ParticleSystem startFx;

        [Tooltip("궁극기 종료 시 1회 재생 파티클(선택)")]
        public ParticleSystem endFx;

        [Header("ArcaneStorm VFX (Mage B)")]
        [Tooltip("바닥 원형 표식 프리팹(선택). 2D Sprite/Particle 아무거나")]
        public GameObject stormTelegraphPrefab;

        [Tooltip("위에서 떨어지는 운석/빛 프리팹(추천). 2D Sprite/Particle 아무거나")]
        public GameObject stormMeteorPrefab;

        [Tooltip("착탄 순간 이펙트 프리팹(선택)")]
        public GameObject stormImpactPrefab;

        [Tooltip("표식 보여주고 실제 낙하 시작까지 지연")]
        public float stormTelegraphDelay = 0.25f;

        [Tooltip("운석 시작 높이(월드 단위)")]
        public float stormFallHeight = 8f;

        [Tooltip("운석 낙하 시간(짧게 주면 타격감 좋음)")]
        public float stormFallTime = 0.15f;

        [Tooltip("VFX 자동 삭제 시간")]
        public float stormVfxAutoDestroy = 1.2f;

        [Header("Runtime (debug)")]
        [SerializeField] private float _gauge;
        public bool IsReady => _gauge >= gaugeMax;

        private bool _isActive;
        private Coroutine _runningCo;

        // 궁극기 실행 중 킬 카운트(연장/효과에 사용)
        private int _killsDuringUltimate;

        private void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (shooter == null) shooter = GetComponent<AutoShooter>();
            if (buffs == null) buffs = GetComponent<PlayerBuffs>();
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= OnEnemyKilled;

            if (_runningCo != null)
            {
                StopCoroutine(_runningCo);
                _runningCo = null;
            }

            SetUltimateActive(false);
        }

        private void Update()
        {
            // 시간 충전
            if (!IsReady)
            {
                _gauge += gaugePerSecond * Time.deltaTime;
                if (_gauge > gaugeMax) _gauge = gaugeMax;
            }

            // 키 발동
            if (IsReady && Input.GetKeyDown(activateKey))
                TryActivate();
        }

        private void OnEnemyKilled(Vector3 pos)
        {
            // 처치 충전
            if (!IsReady)
            {
                _gauge += gaugePerKill;
                if (_gauge > gaugeMax) _gauge = gaugeMax;
            }

            // 궁극기 중 킬 카운트
            if (_runningCo != null) _killsDuringUltimate++;
        }

        public float GetGauge01() => gaugeMax <= 0 ? 0 : Mathf.Clamp01(_gauge / gaugeMax);

        public void TryActivate()
        {
            if (!IsReady) return;
            if (_runningCo != null) return;

            if (stats == null) stats = GetComponent<PlayerStats>();

            _gauge = 0f;
            _killsDuringUltimate = 0;

            var slot = SaveService.GetUltSelectedSlot(stats.job);
            int lv = Mathf.Clamp(SaveService.GetUltSelectedLevel(stats.job), 0, 5);

            _runningCo = StartCoroutine(RunUltimate(stats.job, slot, lv));
        }

        private IEnumerator RunUltimate(JobType job, UltimateSlot slot, int lv)
        {
            SetUltimateActive(true);

            // 시작 전에 버프 리셋
            if (buffs != null) buffs.ResetAll();

            switch (job)
            {
                case JobType.Knight:
                    if (slot == UltimateSlot.A) yield return Knight_A_Blades(lv);
                    else yield return Knight_B_Formation(lv);
                    break;

                case JobType.Archer:
                    if (slot == UltimateSlot.A) yield return Archer_A_Rain(lv);
                    else yield return Archer_B_Mark(lv);
                    break;

                case JobType.Mage:
                    if (slot == UltimateSlot.A) yield return Mage_A_Overclock(lv);
                    else yield return Mage_B_ArcaneStorm(lv);
                    break;

                default:
                    yield return Mage_A_Overclock(lv);
                    break;
            }

            // 종료 처리
            if (buffs != null) buffs.ResetAll();

            _runningCo = null;
            SetUltimateActive(false);
        }

        private void SetUltimateActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;

            // ✅ 오라/이펙트 토글
            if (active)
            {
                if (activeVfxObject) activeVfxObject.SetActive(true);
                if (startFx) startFx.Play(true);
            }
            else
            {
                if (endFx) endFx.Play(true);
                if (activeVfxObject) activeVfxObject.SetActive(false);
            }

            // ✅ (선택) 데미지 팝업을 궁극기 중 강조 스타일로
            if (DamagePopupUIPool.I != null)
                DamagePopupUIPool.I.SetUltimateActive(active);
        }

        // -----------------------------
        // 유틸: 레벨 비례 계수(튜닝 포인트)
        // -----------------------------
        private static float LvScale(float baseValue, float perLv, int lv)
        {
            return baseValue + perLv * Mathf.Clamp(lv, 0, 5);
        }

        private int DevWeaponCount()
        {
            // 무기 슬롯 시스템이 아직 없으면 임시 1
            return Mathf.Max(1, 1);
        }

        // =========================================================
        // 2) 기사 A: 쌍단 검기난무
        // =========================================================
        private IEnumerator Knight_A_Blades(int lv)
        {
            float duration = 6f + (lv >= 1 ? 1f : 0f);
            float widthMult = (lv >= 2) ? 1.25f : 1f;

            if (lv >= 4 && buffs != null)
            {
                buffs.damageTakenMult = 0.75f;
                buffs.superArmor = true;
            }

            int weaponCount = DevWeaponCount();
            int shadowCount = weaponCount + (lv >= 3 ? 1 : 0);

            void OnAttack(GameObject player, Transform target, float baseDamage)
            {
                if (player != gameObject) return;

                DoLineSlash(baseDamage * LvScale(0.6f, 0.08f, lv), widthMult, lv);

                for (int i = 0; i < shadowCount; i++)
                {
                    StartCoroutine(DelayAction(0.2f, () =>
                    {
                        DoLineSlash(baseDamage * LvScale(0.35f, 0.05f, lv), widthMult, lv);
                    }));
                }
            }

            WeaponTriggerHub.OnAttackTriggered += OnAttack;

            float t = 0f;
            int killsGate = 15;
            int lastKillsCheckpoint = 0;
            float maxExtend = 3f;
            float extended = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;

                if (lv >= 5)
                {
                    int checkpoints = _killsDuringUltimate / killsGate;
                    if (checkpoints > lastKillsCheckpoint)
                    {
                        int diff = checkpoints - lastKillsCheckpoint;
                        lastKillsCheckpoint = checkpoints;

                        float add = diff * 0.3f;
                        float canAdd = Mathf.Max(0f, maxExtend - extended);
                        float realAdd = Mathf.Min(add, canAdd);

                        duration += realAdd;
                        extended += realAdd;
                    }
                }

                yield return null;
            }

            WeaponTriggerHub.OnAttackTriggered -= OnAttack;

            DoLineSlash(stats.damage * LvScale(1.2f, 0.15f, lv), 2.2f, lv);
        }

        private void DoLineSlash(float dmg, float widthMult, int lv)
        {
            float xSign = transform.localScale.x >= 0f ? 1f : -1f;
            Vector2 dir = new Vector2(xSign, 0f);

            float length = 18f;
            float width = 1.2f * widthMult;

            Vector2 center = (Vector2)transform.position + dir * (length * 0.5f);
            Vector2 size = new Vector2(length, width);

            int mask = shooter != null ? shooter.enemyLayer : ~0;
            var hits = Physics2D.OverlapBoxAll(center, size, 0f, mask);
            if (hits == null || hits.Length == 0) return;

            int extraPierce = (lv >= 2) ? 1 : 0;
            int maxHits = 1 + extraPierce;

            int hitCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i].GetComponentInParent<Health>();
                if (h == null) continue;

                h.TakeDamage(Mathf.RoundToInt(dmg));
                hitCount++;

                if (hitCount >= maxHits) break;
            }
        }

        // =========================================================
        // 3) 기사 B: 철벽 진형
        // =========================================================
        private IEnumerator Knight_B_Formation(int lv)
        {
            float duration = 8f;

            float radius = 3.5f * (lv >= 1 ? 1.15f : 1f);
            float pullStrength = LvScale(8f, 1.0f, lv);

            float fireRateMult = (lv >= 2) ? 1.0f / 0.70f : 1.0f / 0.80f;
            if (buffs != null) buffs.fireRateMult = fireRateMult;

            if (buffs != null)
            {
                buffs.damageTakenMult = LvScale(0.90f, -0.02f, lv);
                buffs.superArmor = (lv >= 4);
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;

                var cols = Physics2D.OverlapCircleAll(transform.position, radius, shooter != null ? shooter.enemyLayer : ~0);
                for (int i = 0; i < cols.Length; i++)
                {
                    var rb = cols[i].attachedRigidbody;
                    if (rb == null) continue;

                    Vector2 dir = (Vector2)transform.position - rb.position;
                    if (dir.sqrMagnitude < 0.0001f) continue;

                    rb.AddForce(dir.normalized * pullStrength, ForceMode2D.Force);
                }

                yield return null;
            }

            DoAOE(transform.position, radius * 0.9f, stats.damage * LvScale(1.0f, 0.15f, lv));
        }

        // =========================================================
        // 4) 궁수 A: 천궁 폭우
        // =========================================================
        private IEnumerator Archer_A_Rain(int lv)
        {
            float duration = 6f + (lv >= 1 ? 1f : 0f);

            if (buffs != null) buffs.fireRateMult = LvScale(2.2f, 0.25f, lv);

            int weaponCount = DevWeaponCount();

            void OnAttack(GameObject player, Transform target, float baseDamage)
            {
                if (player != gameObject) return;
                if (shooter == null) return;

                int split = (lv >= 2) ? 3 : 2;
                int support = weaponCount;

                int totalExtra = split + support;
                float extraDmg = baseDamage * LvScale(0.28f, 0.03f, lv);

                shooter.FireExtraHomingBursts(target, totalExtra, extraDmg, homingSharpness: 25f, spreadAngle: 14f);
            }

            WeaponTriggerHub.OnAttackTriggered += OnAttack;

            if (lv >= 4 && buffs != null) buffs.moveSpeedMult = 1.20f;

            int waves = 0;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;

                if (lv >= 5 && waves < 3)
                {
                    if (_killsDuringUltimate >= (waves + 1) * 20)
                    {
                        waves++;
                        StartCoroutine(ArcherBurstWave(0.8f, LvScale(1.0f, 0.1f, lv)));
                    }
                }

                yield return null;
            }

            WeaponTriggerHub.OnAttackTriggered -= OnAttack;
        }

        private IEnumerator ArcherBurstWave(float seconds, float fireRateMult)
        {
            if (buffs == null) yield break;

            float prev = buffs.fireRateMult;
            buffs.fireRateMult *= 1.6f * fireRateMult;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }

            buffs.fireRateMult = prev;
        }

        // =========================================================
        // 5) 궁수 B: 사냥꾼의 표식
        // =========================================================
        private IEnumerator Archer_B_Mark(int lv)
        {
            float duration = 8f + (lv >= 1 ? 2f : 0f);

            Transform mark = FindHighestHpEnemyInRange_Safe();
            if (mark == null) yield break;

            if (shooter != null) shooter.forcedTarget = mark;

            if (buffs != null)
            {
                float dmgUp = (lv >= 3) ? 1.35f : 1.25f;
                buffs.damageMult = dmgUp;
            }

            // Lv4: 표식 처치 시 게이지 회복 (mark가 파괴되면 null로 바뀐다는 전제)
            void OnKilled(Vector3 pos)
            {
                if (lv >= 4 && mark == null)
                {
                    _gauge = Mathf.Min(gaugeMax, _gauge + gaugeMax * 0.40f);
                }
            }
            GameEvents.OnEnemyKilled += OnKilled;

            float t = 0f;
            while (t < duration)
            {
                if (mark == null) break;
                t += Time.deltaTime;
                yield return null;
            }

            GameEvents.OnEnemyKilled -= OnKilled;

            if (mark != null)
                DoAOE(mark.position, 2.8f, stats.damage * LvScale(0.9f, 0.12f, lv));

            if (shooter != null) shooter.forcedTarget = null;
        }

        // ✅ Health.CurrentHP가 없어도 터지지 않게 안전 구현
        private Transform FindHighestHpEnemyInRange_Safe()
        {
            if (shooter == null) return null;

            var cols = Physics2D.OverlapCircleAll(transform.position, shooter.targetRange, shooter.enemyLayer);
            if (cols == null || cols.Length == 0) return null;

            float bestHp = -1f;
            Transform best = null;

            for (int i = 0; i < cols.Length; i++)
            {
                var h = cols[i].GetComponentInParent<Health>();
                if (h == null) continue;

                float hp = GetHpValueSafe(h);
                if (hp > bestHp)
                {
                    bestHp = hp;
                    best = h.transform;
                }
            }

            if (best == null) best = FindNearestEnemyForStorm();
            return best;
        }

        private static float GetHpValueSafe(Health h)
        {
            if (h == null) return 0f;

            var type = h.GetType();

            var prop = type.GetProperty("CurrentHP", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                object v = prop.GetValue(h);
                if (v is int vi) return vi;
                if (v is float vf) return vf;
            }

            string[] fields = { "currentHp", "CurrentHp", "_hp", "hp", "_currentHp" };
            for (int i = 0; i < fields.Length; i++)
            {
                var f = type.GetField(fields[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f == null) continue;

                object v = f.GetValue(h);
                if (v is int vi) return vi;
                if (v is float vf) return vf;
            }

            return 1f;
        }

        // =========================================================
        // 6) 마법사 A: 차원 과부하 (에코 + 무기 가속)
        // =========================================================
        private IEnumerator Mage_A_Overclock(int lv)
        {
            float duration = 6f + (lv >= 1 ? 1f : 0f);
            float echoDelay = 0.25f;

            float echoPower = (lv >= 2) ? 0.45f : 0.35f;
            echoPower = LvScale(echoPower, 0.02f, lv);

            float accel = (lv >= 3) ? 1.30f : 1.20f;
            if (buffs != null) buffs.fireRateMult = accel;

            if (lv >= 4 && buffs != null) buffs.superArmor = true;

            int triggerCount = 0;

            void OnAttack(GameObject player, Transform target, float baseDamage)
            {
                if (player != gameObject) return;
                if (shooter == null) return;

                triggerCount++;

                StartCoroutine(DelayAction(echoDelay, () =>
                {
                    // ✅ 에코샷(추가탄) - 이게 안 보이면 projectile 프리팹/소팅 문제
                    shooter.FireExtraHomingBursts(target, 1, baseDamage * echoPower, homingSharpness: 30f, spreadAngle: 0f);
                }));
            }

            WeaponTriggerHub.OnAttackTriggered += OnAttack;

            float t = 0f;
            int gate = 15;
            int checkpoint = 0;
            float extended = 0f;
            float maxExt = 3f;

            while (t < duration)
            {
                t += Time.deltaTime;

                if (lv >= 5)
                {
                    int cp = _killsDuringUltimate / gate;
                    if (cp > checkpoint)
                    {
                        int diff = cp - checkpoint;
                        checkpoint = cp;

                        float add = diff * 0.3f;
                        float can = Mathf.Max(0f, maxExt - extended);
                        float real = Mathf.Min(add, can);

                        duration += real;
                        extended += real;
                    }
                }

                yield return null;
            }

            WeaponTriggerHub.OnAttackTriggered -= OnAttack;

            float boomDmg = stats.damage * LvScale(0.8f, 0.10f, lv) * Mathf.Clamp(1f + triggerCount * 0.03f, 1f, 3.0f);
            float boomR = 3.2f * Mathf.Clamp(1f + triggerCount * 0.01f, 1f, 2.0f);

            // ✅ 종료 폭발도 VFX 붙이고 싶으면 ImpactPrefab 같은 걸 활용해도 됨
            DoAOE(transform.position, boomR, boomDmg);
        }

        // =========================================================
        // 7) 마법사 B: 아케인 스톰 (폭격 + 무기수 연계)
        //    ✅ 기존 즉시 DoAOE() -> "표식/낙하/착탄"으로 변경 (보이게!)
        // =========================================================
        private IEnumerator Mage_B_ArcaneStorm(int lv)
        {
            float duration = 4f;

            int weaponCount = DevWeaponCount();

            float strikesPerSecond = LvScale(6f, 1.0f, lv);
            float radius = LvScale(4.5f, 0.3f, lv);

            if (buffs != null) buffs.fireRateMult = 1.20f;

            float extraStrikesPerSecond = weaponCount * 0.8f;
            float autoAimRatio = (lv >= 5) ? 0.60f : 0.0f;

            float t = 0f;
            float acc = 0f;

            while (t < duration)
            {
                float dt = Time.deltaTime;
                t += dt;

                float totalSPS = strikesPerSecond + extraStrikesPerSecond;
                acc += dt * totalSPS;

                while (acc >= 1f)
                {
                    acc -= 1f;

                    Vector3 pos = PickStormTargetPos(radius, autoAimRatio);

                    // ✅ 데미지 자체는 원래 낮게 설계(0.55 배)라 31 -> 18 이런 값이 정상임
                    // 더 강하게 보이게 하려면 0.55f를 0.8f ~ 1.0f로 올려도 됨.
                    float dmg = stats.damage * LvScale(0.55f, 0.07f, lv);

                    // ✅ "보이는 폭격" + 마지막에 DoAOE 동기화
                    StartCoroutine(StormStrike(pos, 1.4f, dmg));

                    // Lv3 잔류 DOT도 "보이게" 하고 싶으면 telegraph/impact를 추가로 쓰면 됨
                    if (lv >= 3)
                        StartCoroutine(ResidualDot(pos, 2f, stats.damage * 0.10f));
                }

                yield return null;
            }

            // 종료 폭발(원한다면 impact VFX 한 번 더)
            StartCoroutine(StormStrike(transform.position, radius * 0.9f, stats.damage * LvScale(1.0f, 0.12f, lv)));
        }

        private IEnumerator StormStrike(Vector3 pos, float radius, float dmg)
        {
            // 1) 바닥 표식
            GameObject tele = null;
            if (stormTelegraphPrefab)
            {
                tele = Instantiate(stormTelegraphPrefab, pos, Quaternion.identity);
                Destroy(tele, stormVfxAutoDestroy);
            }

            yield return new WaitForSeconds(stormTelegraphDelay);

            if (tele) Destroy(tele);

            // 2) 운석/빛 낙하
            GameObject meteor = null;
            if (stormMeteorPrefab)
            {
                Vector3 start = pos + Vector3.up * stormFallHeight;
                meteor = Instantiate(stormMeteorPrefab, start, Quaternion.identity);

                float tt = 0f;
                float dur = Mathf.Max(0.01f, stormFallTime);
                while (tt < dur)
                {
                    tt += Time.deltaTime;
                    float k = Mathf.Clamp01(tt / dur);
                    if (meteor) meteor.transform.position = Vector3.Lerp(start, pos, k);
                    yield return null;
                }

                Destroy(meteor, stormVfxAutoDestroy);
            }

            // 3) 착탄 이펙트
            if (stormImpactPrefab)
            {
                var impact = Instantiate(stormImpactPrefab, pos, Quaternion.identity);
                Destroy(impact, stormVfxAutoDestroy);
            }

            // 4) 실제 데미지(연출과 동기화)
            DoAOE(pos, radius, dmg);
        }

        private Vector3 PickStormTargetPos(float radius, float autoAimRatio)
        {
            if (shooter != null && Random.value < autoAimRatio)
            {
                var t = FindNearestEnemyForStorm();
                if (t != null) return t.position;
            }

            Vector2 r = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(r.x, r.y, 0f);
        }

        private Transform FindNearestEnemyForStorm()
        {
            if (shooter == null) return null;

            var cols = Physics2D.OverlapCircleAll(transform.position, shooter.targetRange, shooter.enemyLayer);
            if (cols == null || cols.Length == 0) return null;

            float best = float.MaxValue;
            Transform bestT = null;

            for (int i = 0; i < cols.Length; i++)
            {
                float d = (cols[i].transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestT = cols[i].transform;
                }
            }

            return bestT;
        }

        // -----------------------------
        // 공용: AOE, 잔류DOT, 딜레이
        // -----------------------------
        private void DoAOE(Vector3 pos, float radius, float dmg)
        {
            int layer = shooter != null ? shooter.enemyLayer : ~0;
            var cols = Physics2D.OverlapCircleAll(pos, radius, layer);
            if (cols == null) return;

            for (int i = 0; i < cols.Length; i++)
            {
                var h = cols[i].GetComponentInParent<Health>();
                if (h != null) h.TakeDamage(Mathf.RoundToInt(dmg));
            }
        }

        private IEnumerator ResidualDot(Vector3 pos, float seconds, float tickDmg)
        {
            float t = 0f;
            float tick = 0.25f;
            float acc = 0f;

            while (t < seconds)
            {
                float dt = Time.deltaTime;
                t += dt;
                acc += dt;

                if (acc >= tick)
                {
                    acc -= tick;
                    DoAOE(pos, 1.2f, tickDmg);
                }

                yield return null;
            }
        }

        private IEnumerator DelayAction(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
