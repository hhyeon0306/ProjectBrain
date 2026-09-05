using System;
using UnityEngine;

namespace ProjectBrain.Demo.Gameplay
{
    /// <summary>체력 값과 변경 알림을 소유한다.</summary>
    public sealed class DemoHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximum = 100;
        public int Current { get; private set; }
        public int Maximum => maximum;
        public event Action<int, int> Changed;

        public void RestoreFull() => Set(maximum);
        public void Damage(int amount) => Set(Current - Mathf.Max(0, amount));
        private void Set(int value)
        {
            Current = Mathf.Clamp(value, 0, maximum);
            Changed?.Invoke(Current, maximum);
        }
    }
}
