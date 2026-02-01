using System;
using UnityEngine;
using VSL.VFX; // DamagePopupUIPool 사용

namespace VSL
{
    public class Health : MonoBehaviour
    {
        public int MaxHP { get; private set; } = 100;
        public int CurrentHP { get; private set; } = 100;
        public bool IsDead => _dead;

        // ✅ God Mode
        public bool IsInvincible => invincible;

        public event Action<int, int> OnChanged; // current, max
        public event Action OnDied;

        [Header("Dev / God Mode")]
        [Tooltip("true면 어떤 데미지도 받지 않음(개발용 무적)")]
        [SerializeField] private bool invincible = false;

        [Header("Damage Popup")]
        [Tooltip("피격 시 데미지 숫자 표시")]
        [SerializeField] private bool showDamagePopup = true;

        [Tooltip("머리 위로 띄우기 오프셋")]
        [SerializeField] private Vector3 popupOffset = new Vector3(0f, 0.6f, 0f);

        [Header("Death Behaviour")]
        [Tooltip("죽을 때 오브젝트를 Destroy 할지 (적=ON, 플레이어=OFF 추천)")]
        [SerializeField] private bool destroyOnDeath = true;

        [Tooltip("죽을 때 콜라이더를 꺼서 더 이상 접촉/피격 안되게")]
        [SerializeField] private bool disableCollidersOnDeath = true;

        [SerializeField] private float destroyDelay = 0f;

        private bool _dead;
        private Collider2D[] _cols;

        private void Awake()
        {
            if (disableCollidersOnDeath)
                _cols = GetComponentsInChildren<Collider2D>(true);

            // 안전장치: 인스펙터에서 Init 안 해도 최소 동작
            if (MaxHP <= 0)
            {
                MaxHP = 100;
                CurrentHP = 100;
            }
        }

        /// <summary>
        /// 외부(스탯/직업)에서 체력 초기화할 때 호출
        /// </summary>
        public void Init(int maxHP)
        {
            MaxHP = Mathf.Max(1, maxHP);
            CurrentHP = MaxHP;
            _dead = false;

            // 죽음 처리로 꺼졌던 콜라이더가 있으면 다시 켜기
            if (disableCollidersOnDeath && _cols != null)
            {
                for (int i = 0; i < _cols.Length; i++)
                    if (_cols[i] != null) _cols[i].enabled = true;
            }

            OnChanged?.Invoke(CurrentHP, MaxHP);
        }

        /// <summary>
        /// ✅ 개발용 무적 토글
        /// </summary>
        public void SetInvincible(bool value)
        {
            invincible = value;
        }

        /// <summary>
        /// ✅ 개발용: 즉시 풀피 회복
        /// </summary>
        public void HealToFull()
        {
            if (_dead) return;
            CurrentHP = MaxHP;
            OnChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void TakeDamage(int amount)
        {
            if (_dead) return;

            // ✅ GOD MODE: 데미지 무시
            if (invincible) return;

            amount = Mathf.Max(0, amount);
            if (amount == 0) return;

            // 체력 감소
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnChanged?.Invoke(CurrentHP, MaxHP);

            // ✅ 데미지 팝업(UI) 표시
            if (showDamagePopup && DamagePopupUIPool.I != null)
            {
                DamagePopupUIPool.I.Show(amount, transform.position + popupOffset);
            }

            if (CurrentHP <= 0)
                Die();
        }

        public void Heal(int amount)
        {
            if (_dead) return;

            amount = Mathf.Max(0, amount);
            if (amount == 0) return;

            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;

            if (disableCollidersOnDeath && _cols != null)
            {
                for (int i = 0; i < _cols.Length; i++)
                    if (_cols[i] != null) _cols[i].enabled = false;
            }

            OnDied?.Invoke();

            if (destroyOnDeath)
            {
                if (destroyDelay <= 0f) Destroy(gameObject);
                else Destroy(gameObject, destroyDelay);
            }
        }
    }
}
