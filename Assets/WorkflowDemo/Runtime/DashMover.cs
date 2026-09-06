using UnityEngine;

namespace WorkflowDemo
{
    /// <summary>Workflow example: immediate world-space dash; collision handling is out of scope.</summary>
    public sealed class DashMover : MonoBehaviour
    {
        public const float Distance = 3f;
        public const float CooldownSeconds = 2f;
        private readonly DashCooldown cooldown = new DashCooldown(CooldownSeconds);

        public bool TryDash(Vector3 direction, float now)
        {
            if (direction.sqrMagnitude < 0.000001f) return false;
            if (!cooldown.TryUse(now)) return false;
            transform.position += direction.normalized * Distance;
            return true;
        }
    }
}
