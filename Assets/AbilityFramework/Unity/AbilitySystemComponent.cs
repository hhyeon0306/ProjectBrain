using System;
using UnityEngine;

namespace UnityAbilityKit.Unity
{
    /// <summary>Unity 생명주기와 공유 설정 에셋을 순수 C# 실행기에 연결한다.</summary>
    [DisallowMultipleComponent]
    public sealed class AbilitySystemComponent : MonoBehaviour
    {
        private sealed class UnityClock : IAbilityClock { public double Now => Time.timeAsDouble; }
        public string actorId = "Actor";
        [Range(0, 100)] public float startingHealth = 100;
        [Range(0, 100)] public float startingMana = 100;
        public GameplayAbilityAsset[] initialAbilities = Array.Empty<GameplayAbilityAsset>();
        public AbilitySystem System { get; private set; }
        private void Awake() => RebuildSystem();
        private void Update() => System?.Tick();
        public void RebuildSystem()
        {
            var attributes = AttributeSet.CreateDefault();
            attributes.SetBase(AttributeId.Health, startingHealth); attributes.SetBase(AttributeId.Mana, startingMana);
            var system = new AbilitySystem(actorId, attributes, new UnityClock());
            foreach (var asset in initialAbilities)
                system.Grant(asset != null ? asset.Build() : throw new InvalidOperationException("Missing ability on " + name));
            System = system;
        }
    }
}
