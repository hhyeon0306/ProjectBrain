using System;
using System.Linq;
using NUnit.Framework;

namespace UnityAbilityKit.Tests
{
    public sealed class AbilitySystemTests
    {
        private ManualAbilityClock clock;
        private AbilitySystem hero, target;
        private static GameplayEffectDefinition Damage(float amount = 25) => new GameplayEffectDefinition("Effect.Damage", "Damage", 0, new[] { new AttributeModifier(AttributeId.Health, ModifierOperation.Add, -amount) });
        private static GameplayEffectDefinition Haste(double duration = 2) => new GameplayEffectDefinition("Effect.Haste", "Haste", duration, new[] { new AttributeModifier(AttributeId.MoveSpeed, ModifierOperation.Multiply, 1.5f) }, new[] { "State.Hasted" });
        private static GameplayEffectDefinition Stun(double duration = 2) => new GameplayEffectDefinition("Effect.Stun", "Stun", duration, new[] { new AttributeModifier(AttributeId.MoveSpeed, ModifierOperation.Multiply, 0) }, new[] { "State.Stunned" });
        private static GameplayAbilityDefinition Fire(float cost = 20, double cooldown = 2) => new GameplayAbilityDefinition("Ability.Fire", "Fire", cost, cooldown, AbilityTarget.Other, new[] { Damage() }, blockedTags: new[] { "State.Stunned" });
        [SetUp] public void Setup() { clock = new ManualAbilityClock(); hero = new AbilitySystem("Hero", AttributeSet.CreateDefault(), clock); target = new AbilitySystem("Target", AttributeSet.CreateDefault(), clock); hero.Grant(Fire()); }

        [Test] public void SuccessfulCastCommitsCostCooldownAndDamage()
        { Assert.That(hero.TryActivate("Ability.Fire", target).Success); Assert.That(hero.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(80)); Assert.That(target.Attributes.Get(AttributeId.Health), Is.EqualTo(75)); Assert.That(hero.CooldownRemaining("Ability.Fire"), Is.EqualTo(2)); }
        [Test] public void CooldownRejectionDoesNotSpendOrApplyAgain()
        { hero.TryActivate("Ability.Fire", target); Assert.That(hero.TryActivate("Ability.Fire", target).Failure, Is.EqualTo(AbilityFailure.OnCooldown)); Assert.That(hero.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(80)); Assert.That(target.Attributes.Get(AttributeId.Health), Is.EqualTo(75)); }
        [Test] public void InsufficientResourceDoesNotStartCooldown()
        { hero.Attributes.SetBase(AttributeId.Mana, 19); Assert.That(hero.TryActivate("Ability.Fire", target).Failure, Is.EqualTo(AbilityFailure.InsufficientResource)); Assert.That(hero.CooldownRemaining("Ability.Fire"), Is.Zero); Assert.That(target.Attributes.Get(AttributeId.Health), Is.EqualTo(100)); }
        [TestCase(false)] [TestCase(true)] public void OtherTargetMustExistAndDifferFromOwner(bool self)
        { Assert.That(hero.TryActivate("Ability.Fire", self ? hero : null).Failure, Is.EqualTo(AbilityFailure.InvalidTarget)); Assert.That(hero.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(100)); }
        [Test] public void CooldownEndsExactlyAtDeadline()
        { hero.TryActivate("Ability.Fire", target); clock.Advance(2); Assert.That(hero.TryActivate("Ability.Fire", target).Success); }
        [Test] public void SharedDefinitionsHaveIndependentActorState()
        { target.Grant(Fire()); hero.TryActivate("Ability.Fire", target); Assert.That(target.CooldownRemaining("Ability.Fire"), Is.Zero); Assert.That(target.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(100)); Assert.That(target.TryActivate("Ability.Fire", hero).Success); }
        [Test] public void QueryingAvailabilityDoesNotCommit()
        { for (int i = 0; i < 10; i++) Assert.That(hero.CanActivate("Ability.Fire", target).Success); Assert.That(hero.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(100)); Assert.That(hero.Events.Count, Is.Zero); }
        [Test] public void StunBlocksUntilExpiryWithoutResourceLoss()
        { hero.TryApplyEffect(Stun(), out _); Assert.That(hero.TryActivate("Ability.Fire", target).Failure, Is.EqualTo(AbilityFailure.BlockedByTag)); Assert.That(hero.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(100)); clock.Advance(2); Assert.That(hero.TryActivate("Ability.Fire", target).Success); Assert.That(hero.HasTag("State.Stunned"), Is.False); }
        [Test] public void TemporaryModifierDoesNotRestoreAnOutdatedBase()
        { hero.TryApplyEffect(Haste(), out _); Assert.That(hero.Attributes.Get(AttributeId.MoveSpeed), Is.EqualTo(7.5f)); hero.Attributes.SetBase(AttributeId.MoveSpeed, 8); Assert.That(hero.Attributes.Get(AttributeId.MoveSpeed), Is.EqualTo(12)); clock.Advance(2); hero.Tick(); Assert.That(hero.Attributes.Get(AttributeId.MoveSpeed), Is.EqualTo(8)); }
        [Test] public void OverlappingEffectsRetainSharedTagUntilLastOwnerExpires()
        { hero.TryApplyEffect(Stun(1), out _); hero.TryApplyEffect(Stun(3), out _); clock.Advance(1); hero.Tick(); Assert.That(hero.HasTag("State.Stunned")); clock.Advance(2); hero.Tick(); Assert.That(hero.HasTag("State.Stunned"), Is.False); Assert.That(hero.Attributes.Get(AttributeId.MoveSpeed), Is.EqualTo(5)); }
        [Test] public void TimedEffectCanBeRemovedEarlyOnlyOnce()
        { hero.TryApplyEffect(Haste(), out long handle); Assert.That(hero.RemoveEffect(handle)); Assert.That(hero.RemoveEffect(handle), Is.False); Assert.That(hero.Attributes.Get(AttributeId.MoveSpeed), Is.EqualTo(5)); Assert.That(hero.HasTag("State.Hasted"), Is.False); }
        [Test] public void SkippedFramesExpireAllDueEffects()
        { hero.TryApplyEffect(Haste(), out _); hero.TryApplyEffect(Stun(), out _); clock.Advance(100); hero.Tick(); Assert.That(hero.ActiveEffects, Is.Empty); Assert.That(hero.Tags.Snapshot(), Is.Empty); }
        [Test] public void InstantEffectsClampHealthToBounds()
        { target.TryApplyEffect(Damage(200), out _); Assert.That(target.Attributes.Get(AttributeId.Health), Is.Zero); target.TryApplyEffect(Damage(-200), out _); Assert.That(target.Attributes.Get(AttributeId.Health), Is.EqualTo(100)); }
        [Test] public void DeadOwnerCannotActivate()
        { hero.Attributes.SetBase(AttributeId.Health, 0); Assert.That(hero.TryActivate("Ability.Fire", target).Failure, Is.EqualTo(AbilityFailure.ActorDead)); Assert.That(hero.HasTag("State.Dead")); }
        [Test] public void RequiredTagUsesParentHierarchy()
        { hero.Grant(new GameplayAbilityDefinition("Ability.Require", "Require", 0, 0, AbilityTarget.Self, new[] { Haste() }, new[] { "Equipment.Staff" })); Assert.That(hero.TryActivate("Ability.Require").Failure, Is.EqualTo(AbilityFailure.MissingRequiredTag)); hero.Tags.Add("Equipment.Staff.Fire"); Assert.That(hero.TryActivate("Ability.Require").Success); }
        [TestCase("State", true)] [TestCase("State.Stunned", true)] [TestCase("State.Stun", false)] [TestCase("state", false)]
        public void TagMatchingRespectsSegmentsAndCase(string query, bool expected)
        { hero.Tags.Add("State.Stunned"); Assert.That(hero.HasTag(query), Is.EqualTo(expected)); }
        [Test] public void AttributePreflightPreventsPartialMultiEffectCommit()
        { var bare = new AbilitySystem("Bare", new AttributeSet(new[] { new AttributeDefinition(AttributeId.Health, 100, 0, 100) }), clock); hero.Grant(new GameplayAbilityDefinition("Ability.Mixed", "Mixed", 20, 3, AbilityTarget.Other, new[] { Damage(), Haste() })); Assert.That(hero.TryActivate("Ability.Mixed", bare).Failure, Is.EqualTo(AbilityFailure.MissingAttribute)); Assert.That(bare.Attributes.Get(AttributeId.Health), Is.EqualTo(100)); Assert.That(hero.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(100)); Assert.That(hero.CooldownRemaining("Ability.Mixed"), Is.Zero); }
        [Test] public void EffectCapacityPreflightPreventsCostCommit()
        { for (int i = 0; i < AbilitySystem.MaximumActiveEffects; i++) Assert.That(target.TryApplyEffect(Haste(), out _)); hero.Grant(new GameplayAbilityDefinition("Ability.Buff", "Buff", 10, 2, AbilityTarget.Other, new[] { Haste() })); Assert.That(hero.TryActivate("Ability.Buff", target).Failure, Is.EqualTo(AbilityFailure.EffectCapacity)); Assert.That(hero.Attributes.GetBase(AttributeId.Mana), Is.EqualTo(100)); }
        [Test] public void RevokedAbilityCannotActivate()
        { Assert.That(hero.Revoke("Ability.Fire")); Assert.That(hero.TryActivate("Ability.Fire", target).Failure, Is.EqualTo(AbilityFailure.NotGranted)); }
        [Test] public void DefinitionCopiesCallerOwnedCollections()
        { var values = new[] { Damage() }; var blocked = new[] { "State.Stunned" }; var ability = new GameplayAbilityDefinition("Ability.Copy", "Copy", 0, 0, AbilityTarget.Other, values, blockedTags: blocked); values[0] = Haste(); blocked[0] = "State.Other"; Assert.That(ability.Effects[0].Id, Is.EqualTo("Effect.Damage")); Assert.That(ability.BlockedTags[0], Is.EqualTo("State.Stunned")); }
        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] public void ClockRejectsInvalidAdvance(double seconds)
        { Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(seconds)); }
        [TestCase(-1f)] [TestCase(float.NaN)] public void AbilityRejectsInvalidCost(float cost)
        { Assert.Throws<ArgumentException>(() => Fire(cost)); }
        [Test] public void InstantEffectCannotLeakPermanentTag()
        { Assert.Throws<ArgumentException>(() => new GameplayEffectDefinition("Bad", "Bad", 0, null, new[] { "State.Stunned" })); }
        [Test] public void EventHistoryIsBounded()
        { for (int i = 0; i < 100; i++) hero.TryActivate("Unknown", target); Assert.That(hero.Events.Count, Is.EqualTo(64)); }
    }
}
