using System.Collections;
using System.Linq;
using DreamRush.Boss;
using DreamRush.Cards;
using DreamRush.DreamLogic;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>M6: every starter card through the real effect pipeline against the real chair (GDD §16, §17, §30 Cards).</summary>
    public class CardSystemPlayModeTests
    {
        const float Dt = 1f / 60f;
        PlayerMotor3D _m;
        BossChairController _chair;
        PlayerCardController _cards;
        CardLibrary _lib;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            SceneManager.LoadScene(GameFlowManager.DreamScene);
            yield return null;
            var enc = Object.FindFirstObjectByType<BossEncounter>();
            enc.skipReveal = true;
            yield return null; yield return null; yield return null;
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _chair = Object.FindFirstObjectByType<BossChairController>();
            _cards = _m.GetComponent<PlayerCardController>();
            _lib = CardLibrary.Load();
            Assert.IsNotNull(_lib, "CardLibrary in Resources");
            Assert.IsNotNull(_cards);
            _m.AutoStep = false; _m.CameraTransform = null;
            _chair.AutoStep = false;
            Object.Destroy(enc);
            var laws = Object.FindFirstObjectByType<DreamLawDirector>();
            if (laws != null) laws.Stop();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
        }

        void Setup(string cardId, Vector3 player, Vector3 chair, bool faceChair = true)
        {
            _m.Teleport(player, Quaternion.identity);
            for (int i = 0; i < 15; i++) _m.Step(Dt, default);
            _chair.ResetForSeed(3, chair);
            if (faceChair)
            {
                Vector3 to = chair - player; to.y = 0;
                _m.transform.rotation = Quaternion.LookRotation(to);
            }
            else _m.transform.rotation = Quaternion.LookRotation(player - chair);
            _cards.Equip(_lib.Find(cardId));
        }

        string SitAndWin()
        {
            string method = null;
            _chair.Defeated += m => method = m;
            // walk up to the chair while it is open, then sit
            string firstWhy = null; float firstDist = 0f; ChairState firstState = _chair.State;
            for (int i = 0; i < 240 && !_chair.CanSit(_m, out var w0); i++)
            {
                if (firstWhy == null) { firstWhy = w0; firstDist = Vector3.Distance(_m.transform.position, _chair.transform.position); }
                Vector3 to = _chair.transform.position - _m.transform.position; to.y = 0;
                _m.Step(Dt, new PlayerCommand { Move = new Vector2(to.x, to.z).normalized });
                _chair.Step(Dt);
            }
            Assert.IsTrue(_chair.CanSit(_m, out var why), $"could reach and sit during the opening: {why} (first: {firstWhy} @ {firstDist:F2} m, state {firstState}, now {_chair.State}, p={_m.transform.position}, c={_chair.transform.position})");
            Assert.IsTrue(_chair.BeginSitting(_m));
            for (int i = 0; i < 130; i++) _chair.Step(Dt);
            return method;
        }

        [Test]
        public void Library_HasSixStarterCards_AndThreeLaws_WithIcons()
        {
            Assert.That(_lib.starterPool.Count, Is.EqualTo(6));
            Assert.That(_lib.dreamLaws.Count, Is.EqualTo(3));
            Assert.IsTrue(_lib.starterPool.All(c => c.icon != null && !string.IsNullOrEmpty(c.description)));
        }

        [UnityTest] // §16.2 Super Glue → Rooted → still must walk up and sit
        public IEnumerator SuperGlue_RootsChair_ThenSitWins()
        {
            Setup("SuperGlue", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 80.5f));
            var r = _cards.TryUse();
            Assert.IsTrue(r.Used, r.Message);
            Assert.That(_chair.State, Is.EqualTo(ChairState.Rooted));
            Assert.That(_cards.CooldownRemaining, Is.GreaterThan(0f), "cooldown started");
            Assert.That(SitAndWin(), Is.EqualTo("Rooted"));
            yield return null;
        }

        [UnityTest] // §30: invalid targets do not trigger effects or cooldown
        public IEnumerator InvalidTarget_DoesNothing()
        {
            Setup("SuperGlue", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 80.5f), faceChair: false);
            var r = _cards.TryUse();
            Assert.IsFalse(r.Used);
            Assert.That(_chair.State, Is.Not.EqualTo(ChairState.Rooted));
            Assert.That(_cards.CooldownRemaining, Is.EqualTo(0f));
            yield return null;
        }

        [UnityTest] // §30: cooldown blocks a second use
        public IEnumerator Cooldown_BlocksImmediateReuse()
        {
            Setup("CameraFlash", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 81f));
            Assert.IsTrue(_cards.TryUse().Used);
            Assert.IsFalse(_cards.TryUse().Used);
            yield return null;
        }

        [UnityTest] // §16.3 Camera Flash → Frozen
        public IEnumerator CameraFlash_FreezesChair_ThenSitWins()
        {
            Setup("CameraFlash", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 81f));
            Assert.IsTrue(_cards.TryUse().Used);
            Assert.That(_chair.State, Is.EqualTo(ChairState.Frozen));
            Assert.That(SitAndWin(), Is.EqualTo("Frozen"));
            yield return null;
        }

        [UnityTest] // §16.4 Fishing Rod → pulled close → recovery window
        public IEnumerator FishingRod_PullsChairClose_ThenSitWins()
        {
            Setup("FishingRod", new Vector3(0, 0.1f, 73f), new Vector3(0, 0, 83f));
            float d0 = Vector3.Distance(_m.transform.position, _chair.transform.position);
            Assert.IsTrue(_cards.TryUse().Used);
            Assert.That(_chair.State, Is.EqualTo(ChairState.Pulled));
            for (int i = 0; i < 150 && _chair.State == ChairState.Pulled; i++) _chair.Step(Dt);
            float d1 = Vector3.Distance(_m.transform.position, _chair.transform.position);
            Assert.That(d1, Is.LessThan(d0 - 5f), "reeled in");
            Assert.That(_chair.State, Is.EqualTo(ChairState.Stumble));
            Assert.That(SitAndWin(), Is.EqualTo("Pulled"));
            yield return null;
        }

        [UnityTest] // §16.5 Be a Grandpa → chair offers its seat
        public IEnumerator BeAGrandpa_ChairOffersSeat_ThenSitWins()
        {
            Setup("BeAGrandpa", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 81f));
            Assert.IsTrue(_cards.TryUse().Used);
            Assert.IsTrue(_m.GetComponent<PlayerTransformation>().IsGrandpa);
            _chair.Step(Dt);
            Assert.That(_chair.State, Is.EqualTo(ChairState.OfferSeat));
            Assert.That(SitAndWin(), Is.EqualTo("Grandpa"));
            yield return null;
        }

        [UnityTest] // M7: every successful card use gives visible + audible feedback; a failed use gives the fail sound only
        public IEnumerator CardFeedback_VisualAndAudio()
        {
            Audio.AudioManager.ResetForTests();
            Setup("CameraFlash", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 81f));
            Assert.IsTrue(_cards.TryUse().Used);
            Assert.AreEqual(Audio.Sfx.CardFlash, Audio.AudioManager.LastPlayed);
            var flash = UIRoot.Get().Overlay.Find("ScreenFlash");
            Assert.IsNotNull(flash, "screen flash created");
            Assert.IsTrue(flash.gameObject.activeSelf, "screen flash visible");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsFalse(flash.gameObject.activeSelf, "screen flash fades out (never sticks)");

            Setup("SuperGlue", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 80.5f));
            Assert.IsTrue(_cards.TryUse().Used);
            Assert.IsNotNull(Object.FindFirstObjectByType<Presentation.GroundDecal>(), "glue puddle under the chair");

            Setup("FishingRod", new Vector3(0, 0.1f, 73f), new Vector3(3f, 0, 83f));
            Assert.IsTrue(_cards.TryUse().Used);
            Assert.IsNotNull(Object.FindFirstObjectByType<FishingLineFX>(), "fishing line drawn");

            int before = Audio.AudioManager.PlayedCount;
            Setup("SuperGlue", new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 80.5f), faceChair: false);
            Assert.IsFalse(_cards.TryUse().Used);
            Assert.AreEqual(Audio.Sfx.CardFail, Audio.AudioManager.LastPlayed);
            Assert.That(Audio.AudioManager.PlayedCount, Is.EqualTo(before + 1));
            yield return null;
        }

        [UnityTest] // Decoy: the chair flees from the decoy instead of the player
        public IEnumerator DecoyApple_ChairFleesFromDecoy()
        {
            Setup("DecoyApple", new Vector3(0, 0.1f, 74f), new Vector3(0, 0, 80f));
            Assert.IsTrue(_cards.TryUse().Used);
            var decoy = Object.FindFirstObjectByType<DecoyApple>();
            Assert.IsNotNull(decoy);
            // move the player far away: a normal chair would rest; this one keeps fleeing the decoy
            _m.Teleport(new Vector3(-14f, 0.1f, 91f), Quaternion.identity);
            float d0 = Vector3.Distance(decoy.transform.position, _chair.transform.position);
            for (int i = 0; i < 90; i++) _chair.Step(Dt);
            float d1 = Vector3.Distance(decoy.transform.position, _chair.transform.position);
            Assert.That(d1, Is.GreaterThan(d0 + 1f), "ran away from the decoy");
            yield return null;
        }

        [UnityTest] // Bouncy Cushion: launches the apple high (balcony / shortcut route)
        public IEnumerator BouncyCushion_LaunchesPlayer()
        {
            Setup("BouncyCushion", new Vector3(-6f, 0.1f, 76f), new Vector3(8, 0, 88f));
            _m.transform.rotation = Quaternion.identity;
            Assert.IsTrue(_cards.TryUse().Used);
            var cushion = Object.FindFirstObjectByType<BouncyCushion>();
            Assert.IsNotNull(cushion);
            yield return null;
            // hop onto it
            _m.Teleport(cushion.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            float maxVy = 0f;
            for (int i = 0; i < 90; i++) { _m.Step(Dt, default); yield return null; maxVy = Mathf.Max(maxVy, _m.VerticalVelocity); if (maxVy > 10f) break; }
            Assert.That(maxVy, Is.GreaterThan(10f), "bounced");
        }

        [UnityTest] // pitch: Dream Laws forbid an action for a while, then restore it
        public IEnumerator DreamLaw_ForbidsThenRestores()
        {
            var laws = Object.FindFirstObjectByType<DreamLawDirector>();
            laws.firstLawAfter = 1f; laws.warning = 0.5f;
            laws.Begin(7);
            for (int i = 0; i < 80; i++) laws.Tick(Dt);
            Assert.IsNotNull(laws.ActiveLaw, "a law started");
            var rule = laws.ActiveLaw.lawRule;
            bool restricted = rule == DreamLawRule.NoJump ? !_m.AllowJump : rule == DreamLawRule.NoSprint ? !_m.AllowSprint : _m.GroundGripScale < 1f;
            Assert.IsTrue(restricted);
            for (int i = 0; i < 60 * 16; i++) laws.Tick(Dt);
            Assert.IsNull(laws.ActiveLaw);
            Assert.IsTrue(_m.AllowJump && _m.AllowSprint && _m.GroundGripScale >= 1f, "rules restored");
            laws.Stop();
            yield return null;
        }

        [UnityTest] // §17.1 three-card choice UI stores the pick on the run
        public IEnumerator CardSelect_StoresChosenCard()
        {
            var run = new RunState(1234);
            var co = CardSelectPanel.Run(run);
            Assert.IsTrue(co.MoveNext()); // shows the panel
            var panel = Object.FindFirstObjectByType<CardSelectPanel>();
            Assert.IsNotNull(panel);
            var expected = CardDrawManager.DrawThree(_lib.starterPool, 1234)[1];
            panel.Pick(1);
            float t = 0f;
            while (co.MoveNext() && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.That(run.ChosenCardId, Is.EqualTo(expected.cardId));
        }
    }
}
