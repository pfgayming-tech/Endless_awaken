using System;
using UnityEngine;

namespace VSL
{
    public static class GameEvents
    {
        // 적 1마리 죽을 때마다 호출해줘 (게이지 + 킬연장 등에 사용)
        public static event Action<Vector3> OnEnemyKilled;

        public static void RaiseEnemyKilled(Vector3 pos)
        {
            OnEnemyKilled?.Invoke(pos);
        }
    }
}
