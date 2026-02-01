using UnityEngine;

namespace VSL
{
    public class PlayerBuffs : MonoBehaviour
    {
        // -----------------------------
        // 공격 계수들 (궁극기에서 만지고, AutoShooter가 읽어야 적용됨)
        // -----------------------------
        [Header("Multipliers (Ultimate)")]
        public float damageMult = 1f;           // 데미지 배율
        public float fireRateMult = 1f;         // 공격속도 배율
        public float projectileSpeedMult = 1f;  // 탄속 배율
        public float moveSpeedMult = 1f;        // 이동속도 배율

        // -----------------------------
        // 추가 옵션 (궁극기/표식 등)
        // -----------------------------
        [Header("Additive (Ultimate)")]
        public int pierceBonus = 0;             // 관통 보너스
        public int extraProjectilesBonus = 0;   // 분열(추가 탄) 보너스

        [Header("Archer Support Shots")]
        public int supportShots = 0;            // 공격 1회당 추가로 쏘는 지원화살 수

        // -----------------------------
        // 마법사 에코
        // -----------------------------
        [Header("Mage Echo")]
        public bool echoEnabled = false;        // 에코 기능 사용 여부
        [Range(0f, 1f)] public float echoChance = 0f;
        public float echoDelay = 0.25f;
        public float echoDamageMult = 0.35f;

        // -----------------------------
        // 방어/생존 계열
        // -----------------------------
        [Header("Defense (Ultimate)")]
        [Tooltip("받는 피해 배율 (0.75 = 25% 감소)")]
        public float damageTakenMult = 1f;

        [Tooltip("넉백/경직 면역 등(네 피격/이동 시스템에서 참고)")]
        public bool superArmor = false;

        // -----------------------------
        // Reset
        // -----------------------------
        public void ResetAll()
        {
            damageMult = 1f;
            fireRateMult = 1f;
            projectileSpeedMult = 1f;
            moveSpeedMult = 1f;

            pierceBonus = 0;
            extraProjectilesBonus = 0;
            supportShots = 0;

            echoEnabled = false;
            echoChance = 0f;
            echoDelay = 0.25f;
            echoDamageMult = 0.35f;

            damageTakenMult = 1f;
            superArmor = false;
        }
    }
}
