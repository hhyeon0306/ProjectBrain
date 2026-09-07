using System;

namespace UnityAbilityKit
{
    public interface IAbilityClock { double Now { get; } }

    /// <summary>게임 시간과 테스트 시간을 실행 규칙에서 분리한다.</summary>
    public sealed class ManualAbilityClock : IAbilityClock
    {
        public double Now { get; private set; }
        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || double.IsInfinity(Now + seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            Now += seconds;
        }
    }
}
