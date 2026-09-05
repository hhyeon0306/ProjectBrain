using UnityEngine;

namespace ProjectBrain.Demo.Gameplay
{
    /// <summary>플레이어의 이동 입력을 정규화해 제공한다.</summary>
    public sealed class DemoPlayerInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public void SetMove(Vector2 value) => Move = Vector2.ClampMagnitude(value, 1f);
    }
}
