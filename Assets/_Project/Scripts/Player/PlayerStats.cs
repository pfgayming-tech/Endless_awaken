using System;
using UnityEngine;

namespace VSL
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Projectile Size (Final)")]
        public float projectileSizeMult = 1f;

        [Header("Selected (Runtime)")]
        public JobType job;
        public WeaponType weaponType;

        [Header("Final Stats (Runtime)")]
        public float damage;
        public float fireRate;     // shots/sec
        public float moveSpeed;
        public int maxHp;

        [Header("Weapon Extras (Final)")]
        public float projectileSpeed = 9f;
        public int pierce = 0;
        public int extraProjectiles = 0;     // 분열(추가탄)
        public float meleeRadius = 1.2f;

        [Header("Spirit (Final)")]
        public int spiritCount = 0;
        public float spiritDamageMult = 1f;

        [Header("Mage Splash (Final)")]
        public float splashRadius = 0f;

        // ---------------------------
        // ✅ 레벨업(런타임) 누적 업그레이드
        // ---------------------------
        private int _lvPierceBonus = 0;
        private int _lvSplitBonus = 0;
        private float _lvDamageMult = 1f;
        private float _lvMoveSpeedMult = 1f;
        private float _lvShotIntervalMult = 1f;       // 작아질수록 더 빠름(간격 감소)
        private float _lvProjectileSizeMult = 1f;     // 발사체 크기 배율

        // ---------------------------
        // 기존 런(인런) 업그레이드
        // ---------------------------
        private float _runDamageMult = 1f;
        private float _runFireRateMult = 1f;
        private float _runMoveSpeedMult = 1f;
        private int _runMaxHpBonus = 0;

        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        public void BuildFromSelectedJob(JobType selected)
        {
            job = selected;

            // 런 시작 시 리셋
            _runDamageMult = 1f;
            _runFireRateMult = 1f;
            _runMoveSpeedMult = 1f;
            _runMaxHpBonus = 0;

            // 레벨업 보너스 리셋
            _lvPierceBonus = 0;
            _lvSplitBonus = 0;
            _lvDamageMult = 1f;
            _lvMoveSpeedMult = 1f;
            _lvShotIntervalMult = 1f;
            _lvProjectileSizeMult = 1f;

            RecalculateFinal();
        }

        /// <summary>
        /// ✅ LevelUpPanel은 무조건 이걸 호출해야 함 (final을 직접 건드리면 재계산에서 덮임)
        /// intervalMult: 0.70 이면 "발사간격 0.7배" (더 빠름)
        /// sizeMult: 1.5면 "크기 1.5배"
        /// </summary>
        public void ApplyLevelUpUpgrade(
            LevelUpUpgradeType type,
            int pierceUp,
            int splitUp,
            float dmgMult,
            float intervalMult,
            float moveSpeedMult,
            float sizeMult = 1.5f
        )
        {
            switch (type)
            {
                case LevelUpUpgradeType.Pierce:
                    _lvPierceBonus += pierceUp;
                    break;

                case LevelUpUpgradeType.Split:
                    _lvSplitBonus += splitUp;
                    break;

                case LevelUpUpgradeType.DamageUp:
                    _lvDamageMult *= Mathf.Max(0.01f, dmgMult);
                    break;

                case LevelUpUpgradeType.ShotIntervalDown:
                    // intervalMult이 0.7이면 “간격 0.7배” = 더 빠름
                    _lvShotIntervalMult *= Mathf.Clamp(intervalMult, 0.05f, 5f);
                    _lvShotIntervalMult = Mathf.Clamp(_lvShotIntervalMult, 0.20f, 10f);
                    break;

                case LevelUpUpgradeType.MoveSpeedUp:
                    _lvMoveSpeedMult *= Mathf.Clamp(moveSpeedMult, 0.05f, 10f);
                    _lvMoveSpeedMult = Mathf.Clamp(_lvMoveSpeedMult, 0.30f, 20f);
                    break;

                case LevelUpUpgradeType.SizeUp:
                    _lvProjectileSizeMult *= Mathf.Clamp(sizeMult, 0.2f, 10f);
                    _lvProjectileSizeMult = Mathf.Clamp(_lvProjectileSizeMult, 0.2f, 6f);
                    break;
            }

            RecalculateFinal();
        }

        // ------------------------------------------
        // ✅ 최종 계산은 항상 여기서 한 번에!
        // ------------------------------------------
        private void RecalculateFinal()
        {
            var baseCfg = JobConfig.Get(job);
            weaponType = baseCfg.weaponType;

            // base
            float baseDamage = baseCfg.baseDamage;
            float baseFireRate = Mathf.Max(0.2f, baseCfg.baseFireRate);
            float baseMove = Mathf.Max(1f, baseCfg.baseMoveSpeed);
            int baseHp = Mathf.Max(1, baseCfg.baseMaxHp);

            projectileSpeed = baseCfg.projectileSpeed;
            meleeRadius = baseCfg.meleeRadius;

            int basePierce = baseCfg.basePierce;
            int baseSplit = 0; // 기본 추가탄은 0
            int baseSpirits = baseCfg.baseSpirits;

            spiritCount = baseSpirits;
            spiritDamageMult = 1f;
            splashRadius = 0f;

            // transcend/meta
            var jp = SaveService.GetJob(job);

            float tDmgMult = 1f + jp.spentDamage * 0.05f;
            float tFRMult = 1f + jp.spentFireRate * 0.05f;
            float tMSMult = 1f + jp.spentMoveSpeed * 0.03f;

            // 직업 특수
            ApplyJobSpecials(job, jp, ref basePierce, ref baseSplit);

            // ✅ 최종(레벨업 포함)
            damage = baseDamage * tDmgMult * _runDamageMult * _lvDamageMult;

            // 발사 간격 감소 = intervalMult가 작아질수록 fireRate가 커짐
            fireRate = (baseFireRate * tFRMult * _runFireRateMult) / _lvShotIntervalMult;
            fireRate = Mathf.Max(0.2f, fireRate);

            moveSpeed = baseMove * tMSMult * _runMoveSpeedMult * _lvMoveSpeedMult;

            pierce = basePierce + _lvPierceBonus;
            extraProjectiles = baseSplit + _lvSplitBonus;

            projectileSizeMult = _lvProjectileSizeMult;

            maxHp = baseHp + _runMaxHpBonus + GetJobHpBonus(job, jp);

            if (_health != null) _health.Init(maxHp);
        }

        private void ApplyJobSpecials(JobType j, JobProgress jp, ref int basePierce, ref int baseSplit)
        {
            switch (j)
            {
                case JobType.Archer:
                    projectileSpeed += jp.spentSpecialA * 0.6f;
                    basePierce += jp.spentSpecialB;
                    break;

                case JobType.Mage:
                    baseSplit += jp.spentSpecialA / 2;
                    splashRadius += jp.spentSpecialB * 0.2f;
                    break;

                case JobType.Fighter:
                    meleeRadius += jp.spentSpecialA * 0.08f;
                    _runFireRateMult *= 1f + jp.spentSpecialB * 0.02f;
                    break;

                case JobType.Knight:
                    meleeRadius += jp.spentSpecialB * 0.12f;
                    break;

                case JobType.SpiritMaster:
                default:
                    spiritCount += jp.spentSpecialA / 2;
                    spiritDamageMult *= 1f + jp.spentSpecialB * 0.10f;
                    break;
            }
        }

        private int GetJobHpBonus(JobType j, JobProgress jp)
        {
            if (j == JobType.Knight) return jp.spentSpecialA * 2;
            return 0;
        }

       

        public void AddPierceBonus(int amount)
        {
            amount = Mathf.Max(0, amount);
            _lvPierceBonus += amount;
            RecalculateFinal();

            Debug.Log($"[PlayerStats] AddPierceBonus +{amount} => pierce(final)={pierce} (lvBonus={_lvPierceBonus})");
        }

        public void AddSplitBonus(int amount)
        {
            amount = Mathf.Max(0, amount);
            _lvSplitBonus += amount;
            RecalculateFinal();

            Debug.Log($"[PlayerStats] AddSplitBonus +{amount} => extraProjectiles(final)={extraProjectiles} (lvBonus={_lvSplitBonus})");
        }

    }
}
