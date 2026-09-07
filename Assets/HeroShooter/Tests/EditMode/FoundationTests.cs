using NUnit.Framework;

namespace Engchanok.HeroShooter.Tests
{
    public sealed class FoundationTests
    {
        [Test]
        public void Health_ReachesDefeatAndCanResetForRespawn()
        {
            HealthModel health = new(100f);
            Assert.That(health.Damage(25f), Is.EqualTo(25f));
            Assert.That(health.Current, Is.EqualTo(75f));
            health.Damage(200f);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(health.Current, Is.Zero);
            health.Reset(100f);
            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.Current, Is.EqualTo(100f));
        }

        [Test]
        public void Magazine_RequiresDistinctShotsAndCompletesReloadAtDeadline()
        {
            MagazineModel magazine = new(2, 0.2f, 1.5f);
            Assert.That(magazine.TryFire(0f), Is.True);
            Assert.That(magazine.TryFire(0f), Is.False);
            Assert.That(magazine.TryFire(0.2f), Is.True);
            Assert.That(magazine.Ammo, Is.Zero);
            Assert.That(magazine.StartReload(0.2f), Is.True);
            Assert.That(magazine.CompleteReload(1.69f), Is.False);
            Assert.That(magazine.CompleteReload(1.7f), Is.True);
            Assert.That(magazine.Ammo, Is.EqualTo(2));
        }

        [Test]
        public void Cooldown_BlocksReuseUntilElapsed()
        {
            CooldownTimer cooldown = new();
            Assert.That(cooldown.TryUse(1.25f), Is.True);
            Assert.That(cooldown.TryUse(1.25f), Is.False);
            cooldown.Tick(1f);
            Assert.That(cooldown.Remaining, Is.EqualTo(0.25f).Within(0.001f));
            cooldown.Tick(0.25f);
            Assert.That(cooldown.IsReady, Is.True);
            Assert.That(cooldown.TryUse(1.25f), Is.True);
        }
    }
}
