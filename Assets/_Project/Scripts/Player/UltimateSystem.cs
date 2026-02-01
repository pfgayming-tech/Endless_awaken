using System.Collections;
using UnityEngine;

namespace VSL
{
    public class UltimateSystem : MonoBehaviour
    {
        [Header("Refs")]
        public PlayerStats stats;
        public AutoShooter shooter;
        public PlayerBuffs buffs;

        [Header("Gauge")]
        public float gaugeMax = 100f;
        public float gaugePerSecond = 4f;   // ✅ 시간당 충전
        public float gaugePerKill = 8f;     // ✅ 처치당 충전

        [Header("Input")]
        public KeyCode activateKey = KeyCode.Space;

        [Header("Runtime (debug)")]
        [SerializeField] private float _gauge;
        public bool IsReady => _gauge >= gaugeMax;

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

            _gauge = 0f;
            _killsDuringUltimate = 0;

            var slot = UltimateMetaStore.GetSelectedSlot(stats.job);
            int lv = Mathf.Clamp(UltimateMetaStore.GetSelectedLevel(stats.job), 0, 5);

            _runningCo = StartCoroutine(RunUltimate(stats.job, slot, lv));
        }

        private IEnumerator RunUltimate(JobType job, UltimateSlot slot, int lv)
        {
            // 궁극기 시작 전에 버프 리셋
            if (buffs != null) buffs.ResetAll();

            // ✅ 직업/슬롯별 실행
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
                    // 다른 직업은 일단 마법사 A처럼 기본 동작만
                    yield return Mage_A_Overclock(lv);
                    break;
            }

            // 종료 처리
            if (buffs != null) buffs.ResetAll();
            _runningCo = null;
        }

        // -----------------------------
        // 유틸: 레벨 비례 계수(튜닝 포인트)
        // -----------------------------
        private static float LvScale(float baseValue, float perLv, int lv)
        {
            // ✅ “지금은 레벨 비례” 요구 반영
            // 예: base=1.0, perLv=0.05, lv=3 => 1.15
            return baseValue + perLv * Mathf.Clamp(lv, 0, 5);
        }

        private int DevWeaponCount()
        {
            // ✅ 무기 슬롯 시스템이 아직 정식으로 없으면 일단 “임시”로:
            // - 나중에 WeaponManager 만들면 여기만 바꾸면 됨.
            // 예: 장착 무기 수 = 1 + extraProjectiles? 같은 임시로도 OK
            return Mathf.Max(1, 1); // TODO: WeaponManager 연결
        }

        // =========================================================
        // 2) 기사 A: 쌍단 검기난무 (블레이드 + 검영 + 종료 검기)
        // =========================================================
        private IEnumerator Knight_A_Blades(int lv)
        {
            // duration: 6s + (lv>=1 ? +1s : 0) + (lv>=5 ? kill연장 : 0)
            float duration = 6f + (lv >= 1 ? 1f : 0f);

            // 검기 폭(두께) + 관통추가 같은 건 “라인 히트 박스”에서 처리
            float widthMult = (lv >= 2) ? 1.25f : 1f;

            // 피해감소/슈퍼아머
            if (lv >= 4 && buffs != null)
            {
                buffs.damageTakenMult = 0.75f;  // 25% 감소
                buffs.superArmor = true;       // TODO: 실제 넉백 시스템과 연결
            }

            int weaponCount = DevWeaponCount();
            int shadowCount = weaponCount + (lv >= 3 ? 1 : 0);

            // 공격 트리거를 받아서 “검기 라인 + 검영 딜레이” 발생
            void OnAttack(GameObject player, Transform target, float baseDamage)
            {
                if (player != gameObject) return;

                // 1) 기본 검기 라인(전방)
                DoLineSlash(baseDamage * LvScale(0.6f, 0.08f, lv), widthMult);

                // 2) 검영: 0.2초 뒤 동일 방향으로 추가 베기
                for (int i = 0; i < shadowCount; i++)
                {
                    StartCoroutine(DelayAction(0.2f, () =>
                    {
                        DoLineSlash(baseDamage * LvScale(0.35f, 0.05f, lv), widthMult);
                    }));
                }
            }

            WeaponTriggerHub.OnAttackTriggered += OnAttack;

            // 기사 기본 휘두름을 “2연타 느낌”으로 만들고 싶으면:
            // - 너의 근접 공격 루틴에서 AttackTriggered를 2번 쏘도록 바꾸는 게 정석
            // - 지금은 궁극기 쪽에서 “추가 라인딜”로 체감만 구현

            float t = 0f;
            int killsGate = 15;
            int lastKillsCheckpoint = 0;
            float maxExtend = 3f;
            float extended = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;

                // Lv5: 15킬마다 +0.3s (최대 +3s)
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

            // 종료: 초대형 검기 1회
            DoLineSlash(stats.damage * LvScale(1.2f, 0.15f, lv), 2.2f);
        }

        // 기사 “검기(라인)” 공격: 전방으로 BoxCastAll로 적에게 데미지
        private void DoLineSlash(float dmg, float widthMult)
        {
            // TODO: 방향은 네 캐릭터 바라보는 방향으로 연결
            Vector2 dir = Vector2.right;
            float length = 18f;                 // 화면 끝까지 느낌 (튜닝)
            float width = 1.2f * widthMult;     // 검기 폭

            Vector2 center = (Vector2)transform.position + dir * (length * 0.5f);
            Vector2 size = new Vector2(length, width);

            var hits = Physics2D.OverlapBoxAll(center, size, 0f, shooter != null ? shooter.enemyLayer : ~0);
            if (hits == null) return;

            int pierce = (lvCache >= 2) ? 1 : 0; // Lv2 관통 +1 느낌(“추가 타겟 허용”)
            int hitCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i].GetComponentInParent<Health>();
                if (h == null) continue;

                h.TakeDamage(Mathf.RoundToInt(dmg));
                hitCount++;

                // 관통 느낌: 제한을 두고 싶으면 여기서 컷
                if (pierce <= 0 && hitCount >= 1) { /* 단일 */ }
            }
        }

        // (DoLineSlash에서 lv를 쓰고 싶어서 임시 캐시)
        private int lvCache = 0;

        // =========================================================
        // 3) 기사 B: 철벽 진형 (끌어당김 + 무기 발동 가속)
        // =========================================================
        private IEnumerator Knight_B_Formation(int lv)
        {
            float duration = 8f + (lv >= 1 ? 0f : 0f); // 요구상 Lv1은 범위 증가라 duration 고정
            lvCache = lv;

            // 끌어당김 범위
            float radius = 3.5f * (lv >= 1 ? 1.15f : 1f);
            float pullStrength = LvScale(8f, 1.0f, lv); // 레벨 비례(튜닝)

            // 무기 발동 가속(= 공속/발사속도 증가로 대체)
            float accel = (lv >= 3 ? 1.0f : 1.0f); // Lv3은 보호막이지만 일단 자리만
            float fireRateMult = (lv >= 2) ? 1.0f / 0.70f : 1.0f / 0.80f; // 20%→30% 가속 느낌
            if (buffs != null) buffs.fireRateMult = fireRateMult;

            // 피해감소/슈퍼아머는 기사 A Lv4에 있었고, B는 취향
            // 여기서는 진형 안정성을 위해 약하게 줌(튜닝)
            if (buffs != null)
            {
                buffs.damageTakenMult = LvScale(0.90f, -0.02f, lv); // lv 오를수록 조금 더 단단(예시)
                buffs.superArmor = (lv >= 4);
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;

                // 주변 적 끌어당김
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

            // 종료 폭발: 타격횟수 비례는 “공격트리거 카운트”로 대체 가능
            // 지금은 개발단계: 간단 폭발 1회
            DoAOE(transform.position, radius * 0.9f, stats.damage * LvScale(1.0f, 0.15f, lv));
        }

        // =========================================================
        // 4) 궁수 A: 천궁 폭우 (난사 + 분열(대체) + 무기수 지원화살)
        // =========================================================
        private IEnumerator Archer_A_Rain(int lv)
        {
            float duration = 6f + (lv >= 1 ? 1f : 0f);
            lvCache = lv;

            // 난사 느낌: fireRateMult 크게
            if (buffs != null) buffs.fireRateMult = LvScale(2.2f, 0.25f, lv);

            int weaponCount = DevWeaponCount();

            void OnAttack(GameObject player, Transform target, float baseDamage)
            {
                if (player != gameObject) return;
                if (shooter == null) return;

                // “분열” 구현(간단 대체): 공격 트리거마다 작은 화살 2~3개 추가 발사
                int split = (lv >= 2) ? 3 : 2;

                // 지원 화살: 무기 1개당 1개 추가
                int support = weaponCount;

                int totalExtra = split + support;

                // ✅ 작은 추가 화살들은 데미지 낮게
                float extraDmg = baseDamage * LvScale(0.28f, 0.03f, lv);

                shooter.FireExtraHomingBursts(target, totalExtra, extraDmg, homingSharpness: 25f, spreadAngle: 14f);
            }

            WeaponTriggerHub.OnAttackTriggered += OnAttack;

            // Lv4: 이동속도 +20% + 피격무시 1회는 나중에 Health/Shield랑 연결
            if (lv >= 4 && buffs != null)
            {
                buffs.moveSpeedMult = 1.20f;
                // TODO: “피격 무시 1회”는 Shield 컴포넌트 만들면 연결
            }

            // Lv5: 20킬마다 0.8초 난사 파동(최대3회)
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
        // 5) 궁수 B: 사냥꾼의 표식 (강제 타겟 + 피해 증가 + 처치시 게이지 회복)
        // =========================================================
        private IEnumerator Archer_B_Mark(int lv)
        {
            float duration = 8f + (lv >= 1 ? 2f : 0f);
            lvCache = lv;

            Transform mark = FindHighestHpEnemyInRange();
            if (mark == null) yield break;

            // 표식 동안 강제 타겟
            if (shooter != null) shooter.forcedTarget = mark;

            // 표식 피해 증가
            if (buffs != null)
            {
                float dmgUp = (lv >= 3) ? 1.35f : 1.25f;
                buffs.damageMult = dmgUp;
            }

            // Lv2: 첫 5타 치명/추가피해는 나중에 Crit 시스템 연결
            // Lv4: 표식 처치 시 게이지 40% 회복 -> 여기서는 “적 처치 이벤트”로 처리
            void OnKilled(Vector3 pos)
            {
                if (lv >= 4 && mark == null)
                {
                    // 표식이 죽었다고 가정하면 게이지 보너스
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

            // 종료 폭발(누적 타격 수 비례)은 “공격트리거 카운트”로 확장 가능
            // 지금은 개발단계: 약화 폭발 1회
            if (mark != null)
                DoAOE(mark.position, 2.8f, stats.damage * LvScale(0.9f, 0.12f, lv));

            if (shooter != null) shooter.forcedTarget = null;
        }

        private Transform FindHighestHpEnemyInRange()
        {
            if (shooter == null) return null;

            var cols = Physics2D.OverlapCircleAll(transform.position, shooter.targetRange, shooter.enemyLayer);
            if (cols == null || cols.Length == 0) return null;

            float bestHp = -1;
            Transform best = null;

            for (int i = 0; i < cols.Length; i++)
            {
                var h = cols[i].GetComponentInParent<Health>();
                if (h == null) continue;

                // Health가 CurrentHP 접근 불가면, 임시로 거리 기준으로 바꾸면 됨
                float hp = h.CurrentHP; // ✅ 네 Health에 public getter가 없다면 수정 필요
                if (hp > bestHp)
                {
                    bestHp = hp;
                    best = h.transform;
                }
            }
            return best;
        }

        // =========================================================
        // 6) 마법사 A: 차원 과부하 (에코 + 무기 가속)
        // =========================================================
        private IEnumerator Mage_A_Overclock(int lv)
        {
            float duration = 6f + (lv >= 1 ? 1f : 0f);
            lvCache = lv;

            float echoDelay = 0.25f;

            // 에코 위력: 35% -> 45% (레벨 비례로)
            float echoPower = (lv >= 2) ? 0.45f : 0.35f;
            echoPower = LvScale(echoPower, 0.02f, lv); // ✅ 지금은 레벨 비례(나중에 조절)

            // 무기 발동 가속: 20% -> 30%
            float accel = (lv >= 3) ? 1.30f : 1.20f;
            if (buffs != null) buffs.fireRateMult = accel;

            // 안정성: 보호막/넉백 면역은 나중에 연결
            if (lv >= 4 && buffs != null)
            {
                buffs.superArmor = true;
                // TODO: shield 1회는 Shield 컴포넌트로
            }

            int triggerCount = 0;

            void OnAttack(GameObject player, Transform target, float baseDamage)
            {
                if (player != gameObject) return;
                if (shooter == null) return;

                triggerCount++;

                // 0.25초 후 “에코”로 한 번 더 발사(35~45% 위력)
                StartCoroutine(DelayAction(echoDelay, () =>
                {
                    shooter.FireExtraHomingBursts(target, 1, baseDamage * echoPower, homingSharpness: 30f, spreadAngle: 0f);
                }));
            }

            WeaponTriggerHub.OnAttackTriggered += OnAttack;

            // Lv5: 15킬마다 +0.3s (최대 +3s)
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

            // 종료: 차원 붕괴 폭발(에코 포함 발동 횟수 비례)
            float boomDmg = stats.damage * LvScale(0.8f, 0.10f, lv) * Mathf.Clamp(1f + triggerCount * 0.03f, 1f, 3.0f);
            float boomR = 3.2f * Mathf.Clamp(1f + triggerCount * 0.01f, 1f, 2.0f);
            DoAOE(transform.position, boomR, boomDmg);
        }

        // =========================================================
        // 7) 마법사 B: 아케인 스톰 (폭격 + 무기수 연계)
        // =========================================================
        private IEnumerator Mage_B_ArcaneStorm(int lv)
        {
            float duration = 4f; // 기본 4초
            lvCache = lv;

            int weaponCount = DevWeaponCount();

            // Strike 밀도(레벨 비례)
            float strikesPerSecond = LvScale(6f, 1.0f, lv); // ✅ 레벨에 비례 (튜닝)
            float radius = LvScale(4.5f, 0.3f, lv);         // 폭격 반경

            // 폭격 중 기본공격 20% 빠르게
            if (buffs != null) buffs.fireRateMult = 1.20f;

            // 무기수 보너스: 초당 추가 Strike
            float extraStrikesPerSecond = weaponCount * 0.8f;

            // Lv5: 자동추적 비율
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
                    float dmg = stats.damage * LvScale(0.55f, 0.07f, lv);

                    DoAOE(pos, 1.4f, dmg);

                    // Lv3: 잔류 장판 2초 느낌(간단)
                    if (lv >= 3)
                        StartCoroutine(ResidualDot(pos, 2f, stats.damage * 0.10f));
                }

                yield return null;
            }

            // 종료: 붕괴 폭발 1회(간단)
            DoAOE(transform.position, radius * 0.9f, stats.damage * LvScale(1.0f, 0.12f, lv));
        }

        private Vector3 PickStormTargetPos(float radius, float autoAimRatio)
        {
            // autoAimRatio 비율만큼 “가까운 적 근처”를 우선
            if (shooter != null && Random.value < autoAimRatio)
            {
                var t = FindNearestEnemyForStorm();
                if (t != null) return t.position;
            }

            // 랜덤 원형
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
