using System;
using UnityEngine;

namespace VSL
{
    public static class WeaponTriggerHub
    {
        // player, target, baseDamage
        public static event Action<GameObject, Transform, float> OnAttackTriggered;

        public static void RaiseAttackTriggered(GameObject player, Transform target, float baseDamage)
        {
            OnAttackTriggered?.Invoke(player, target, baseDamage);
        }
    }
}
