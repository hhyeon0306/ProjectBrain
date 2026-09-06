using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorkflowDemo.Tests
{
    public sealed class DashCooldownTests
    {
        [Test] public void FirstUseIsImmediatelyAvailable()
            => Assert.That(new DashCooldown(2f).TryUse(0f), Is.True);

        [Test] public void RepeatedInputDoesNotExtendCooldown()
        {
            var cooldown = new DashCooldown(2f);
            Assert.That(cooldown.TryUse(0f), Is.True);
            Assert.That(cooldown.TryUse(1f), Is.False);
            Assert.That(cooldown.Remaining(1f), Is.EqualTo(1f));
        }

        [Test] public void DashIsReadyAtExactBoundary()
        {
            var cooldown = new DashCooldown(2f);
            cooldown.TryUse(0f);
            Assert.That(cooldown.TryUse(2f), Is.True, "Dash must recover at exactly two seconds.");
        }

        [Test] public void RemainingNeverBecomesNegative()
        {
            var cooldown = new DashCooldown(2f);
            cooldown.TryUse(0f);
            Assert.That(cooldown.Remaining(3f), Is.Zero);
        }

        [Test] public void InvalidDurationIsRejected()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new DashCooldown(-1f));

        [Test] public void InvalidClockIsRejected()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new DashCooldown(2f).TryUse(float.NaN));
    }

    public sealed class DashMoverTests
    {
        private Scene preview;
        private GameObject player;
        private DashMover mover;

        [SetUp] public void SetUp()
        {
            preview = EditorSceneManager.NewPreviewScene();
            player = new GameObject("WorkflowDemoPlayer");
            SceneManager.MoveGameObjectToScene(player, preview);
            mover = player.AddComponent<DashMover>();
        }

        [TearDown] public void TearDown()
        {
            if (player != null) UnityEngine.Object.DestroyImmediate(player);
            EditorSceneManager.ClosePreviewScene(preview);
        }

        [Test] public void DirectionMagnitudeDoesNotChangeDistance()
        {
            Assert.That(mover.TryDash(new Vector3(10f, 0f, 0f), 0f), Is.True);
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(3f, 0f, 0f)));
        }

        [Test] public void CooldownRejectsMovementThenAllowsNextDash()
        {
            mover.TryDash(Vector3.right, 0f);
            Assert.That(mover.TryDash(Vector3.right, 1f), Is.False);
            Assert.That(player.transform.position.x, Is.EqualTo(3f));
            Assert.That(mover.TryDash(Vector3.right, 2f), Is.True);
            Assert.That(player.transform.position.x, Is.EqualTo(6f));
        }

        [Test] public void ZeroDirectionDoesNotConsumeCooldown()
        {
            Assert.That(mover.TryDash(Vector3.zero, 0f), Is.False);
            Assert.That(mover.TryDash(Vector3.right, 0f), Is.True);
        }
    }
}
