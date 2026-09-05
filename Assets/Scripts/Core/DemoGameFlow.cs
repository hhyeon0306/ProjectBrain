using ProjectBrain.Demo.Gameplay;
using ProjectBrain.Demo.Infrastructure;
using ProjectBrain.Demo.UI;
using UnityEngine;

namespace ProjectBrain.Demo.Core
{
    /// <summary>데모 시스템의 시작과 종료를 조정하는 중심 구성 요소.</summary>
    public sealed class DemoGameFlow : MonoBehaviour
    {
        [SerializeField] private DemoPlayerMovement movement;
        [SerializeField] private DemoHealth health;
        [SerializeField] private DemoHudPresenter hud;
        [SerializeField] private DemoSaveService saveService;

        public void StartSession()
        {
            health.RestoreFull();
            hud.ShowHealth(health.Current, health.Maximum);
        }

        public void FinishSession() => saveService.Save(health.Current, movement.transform.position);
    }
}
