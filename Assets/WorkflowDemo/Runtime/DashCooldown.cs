using System;

namespace WorkflowDemo
{
    /// <summary>Caller supplies a monotonic clock, making cooldown rules deterministic.</summary>
    public sealed class DashCooldown
    {
        private readonly float duration;
        private float readyAt = float.NegativeInfinity;

        public DashCooldown(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));
            this.duration = duration;
        }

        public bool TryUse(float now)
        {
            if (float.IsNaN(now) || float.IsInfinity(now))
                throw new ArgumentOutOfRangeException(nameof(now));
            if (now < readyAt) return false;
            readyAt = now + duration;
            return true;
        }

        public float Remaining(float now) => Math.Max(0f, readyAt - now);
    }
}
