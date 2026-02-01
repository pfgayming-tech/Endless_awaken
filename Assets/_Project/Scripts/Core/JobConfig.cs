using UnityEngine;

namespace VSL
{
    public struct JobBase
    {
        public WeaponType weaponType;
        public float baseDamage;
        public float baseFireRate;   // shots per second
        public float baseMoveSpeed;
        public int baseMaxHp;
        public float meleeRadius;
        public float projectileSpeed;
        public int basePierce;
        public int baseSpirits;
    }

    public static class JobConfig
    {
        // "모험가(공통)" 기반 위에 직업 색만 주는 느낌으로 값 설정
        public static JobBase Get(JobType job)
        {
            // Adventurer baseline
            float advDmg = 9f;
            float advFR = 1.2f;
            float advMS = 4.0f;
            int advHP = 100;

            switch (job)
            {
                case JobType.Knight:
                    return new JobBase
                    {
                        weaponType = WeaponType.Melee,
                        baseDamage = advDmg + 3f,
                        baseFireRate = advFR - 0.2f,
                        baseMoveSpeed = advMS - 0.2f,
                        baseMaxHp = advHP + 30,
                        meleeRadius = 1.25f,
                        projectileSpeed = 0f,
                        basePierce = 0,
                        baseSpirits = 0
                    };

                case JobType.Archer:
                    return new JobBase
                    {
                        weaponType = WeaponType.Ranged,
                        baseDamage = advDmg + 1f,
                        baseFireRate = advFR + 0.4f,
                        baseMoveSpeed = advMS + 0.2f,
                        baseMaxHp = advHP,
                        meleeRadius = 0f,
                        projectileSpeed = 10f,
                        basePierce = 0,
                        baseSpirits = 0
                    };

                case JobType.Mage:
                    return new JobBase
                    {
                        weaponType = WeaponType.Ranged,
                        baseDamage = advDmg + 5f,
                        baseFireRate = advFR - 0.1f,
                        baseMoveSpeed = advMS,
                        baseMaxHp = advHP - 5,
                        meleeRadius = 0f,
                        projectileSpeed = 8.5f,
                        basePierce = 1,
                        baseSpirits = 0
                    };

                case JobType.Fighter:
                    return new JobBase
                    {
                        weaponType = WeaponType.Melee,
                        baseDamage = advDmg + 2f,
                        baseFireRate = advFR + 0.6f,
                        baseMoveSpeed = advMS + 0.4f,
                        baseMaxHp = advHP + 10,
                        meleeRadius = 1.05f,
                        projectileSpeed = 0f,
                        basePierce = 0,
                        baseSpirits = 0
                    };

                case JobType.SpiritMaster:
                default:
                    return new JobBase
                    {
                        weaponType = WeaponType.Spirits,
                        baseDamage = advDmg,
                        baseFireRate = advFR,
                        baseMoveSpeed = advMS + 0.1f,
                        baseMaxHp = advHP,
                        meleeRadius = 0f,
                        projectileSpeed = 9f,
                        basePierce = 0,
                        baseSpirits = 2
                    };
            }
        }
    }
}
