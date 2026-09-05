using UnityEngine;

namespace ProjectBrain.Demo.UI
{
    /// <summary>게임 상태를 화면 표현용 문자열로 바꾼다.</summary>
    public sealed class DemoHudPresenter : MonoBehaviour
    {
        public string HealthText { get; private set; } = "HP --/--";
        public void ShowHealth(int current, int maximum) => HealthText = $"HP {current}/{maximum}";
    }
}
