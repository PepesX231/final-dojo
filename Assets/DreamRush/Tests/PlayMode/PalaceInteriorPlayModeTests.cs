using System.Collections;
using System.Linq;
using DreamRush.Combat;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Palace;
using DreamRush.Player;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>M15 level 2 — enterable buildings, the minute economy, the city edge and the fall rescue.</summary>
    public class PalaceInteriorPlayModeTests
    {
        PlayerMotor3D _m; Vector2 _move;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false; Time.timeScale = 1f;
            TickMinion.ResetCounters();
            SceneManager.LoadScene("Dream_MidnightPalace");
            yield return null; yield return null;
            if(Object.FindFirstObjectByType<DreamRush.Campaign.ExistingWorldArrival>()!=null&&!DreamRush.Campaign.DreamCinematic.SkipForGameplayProbe)yield return new WaitForSeconds(5.5f);
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _m.CameraTransform = null; _move = Vector2.zero;
            _m.CommandSource = new FuncCommandSource(() => new PlayerCommand { Move = _move });
            float t = 0; while (!_m.IsGrounded && t < 10f) { t += Time.deltaTime; yield return null; }
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
        }

        static IEnumerator Until(System.Func<bool> c, float timeout, string what)
        {
            float t = 0; while (!c()) { t += Time.deltaTime; if (t > timeout) Assert.Fail("timed out: " + what); yield return null; }
        }

        static BuildingDoor[] Doors() => Object.FindObjectsByType<BuildingDoor>(FindObjectsSortMode.InstanceID);

        [UnityTest] // PI-01 every city building has a door; all six kinds exist; nobody steps out into a wall
        public IEnumerator Doors_EveryKind_AndClearOutside()
        {
            var doors = Doors();
            Assert.That(doors.Length, Is.GreaterThanOrEqualTo(40), "doors on the city blocks");
            foreach (RoomType t in System.Enum.GetValues(typeof(RoomType)))
                Assert.That(doors.Count(d => d.type == t), Is.GreaterThanOrEqualTo(1), "a door of type " + t);
            Assert.That(doors.Select(d => d.id).Distinct().Count(), Is.EqualTo(doors.Length), "unique ids");
            int mask = ~(1 << _m.gameObject.layer);
            foreach (var d in doors)
            {
                Assert.IsNotNull(d.outside, d.id);
                bool blocked = Physics.CheckCapsule(d.outside.position + Vector3.up * 0.45f, d.outside.position + Vector3.up * 1.0f, 0.3f, mask, QueryTriggerInteraction.Ignore);
                Assert.IsFalse(blocked, $"door {d.id} at {d.outside.position} steps out into geometry");
            }
            Assert.IsNotNull(InteriorDirector.Current); Assert.That(InteriorDirector.Current.rooms.Count, Is.EqualTo(doors.Length), "every door opens onto a real room");
            foreach (var r in InteriorDirector.Current.rooms) { Assert.IsNotNull(r.door); Assert.IsTrue(r.area.Contains(r.spawn.position + Vector3.up * 0.5f), "room volume holds its doorway spawn " + r.door.id); }
            yield return null;
        }

        [UnityTest] // PI-02 walk into a flat: a resident, minutes on the furniture; take one; leave → back outside that door
        public IEnumerator Flat_EnterCollectLeave()
        {
            var id = InteriorDirector.Current; var pd = PalaceDirector.Current;
            var door = Doors().First(d => d.type == RoomType.Flat);
            _m.Teleport(door.outside.position, door.outside.rotation);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(id.Inside, "standing on the street");
            // walk in through the doorway (no button, no teleport)
            Vector3 inDir = -door.outside.forward;
            _move = new Vector2(inDir.x, inDir.z);
            yield return Until(() => id.Inside, 4f, "walked inside");
            _move = Vector2.zero;
            Assert.That(id.Door, Is.EqualTo(door), "inside this building's own room");
            Assert.IsNotNull(id.Room.dynamicRoot.GetComponentInChildren<NpcTalker>(), "a resident lives here");
            var coins = id.Room.dynamicRoot.GetComponentsInChildren<LostMinute>();
            Assert.That(coins.Length, Is.GreaterThanOrEqualTo(2), "minutes on the furniture");
            int before = pd.Minutes;
            _m.Teleport(coins[0].transform.position + Vector3.down * 0.3f, Quaternion.identity);
            yield return Until(() => pd.Minutes > before, 3f, "minute collected");
            yield return new WaitForSeconds(0.5f);
            Assert.That(_m.transform.position.y, Is.GreaterThan(-0.5f), "standing in the room, not falling");
            int taken = pd.Minutes - before;   // the magnet may grab a neighbour too
            id.WalkOut(door);
            yield return Until(() => !id.Inside, 2f, "outside");
            id.WalkInto(door); yield return Until(() => id.Inside, 2f, "inside again"); yield return null;
            Assert.That(id.Room.dynamicRoot.GetComponentsInChildren<LostMinute>().Length, Is.EqualTo(coins.Length - taken), "collected minutes stay collected (room is not re-dressed)");
            id.WalkOut(door); yield return Until(() => !id.Inside, 2f, "out");
        }

        [UnityTest] // PI-03 Watchmaker: minutes buy upgrades (extra moon raises max HP), can't overspend
        public IEnumerator Shop_BuysUpgrades()
        {
            var pd = PalaceDirector.Current; var st = SleepStability.Current;
            int max = st.Max;
            Assert.IsFalse(ShopPanel.BuyById("moon"), "can't afford with 0 minutes");
            pd.AddMinutes(20);
            Assert.IsTrue(ShopPanel.BuyById("moon"));
            Assert.That(pd.Minutes, Is.EqualTo(5)); Assert.That(st.Max, Is.EqualTo(max + 1));
            Assert.IsFalse(ShopPanel.BuyById("moon"), "5 minutes is not enough");
            yield return null;
        }

        [UnityTest] // PI-04 the city edge holds, and falling under the city brings Pip back to the last safe ground
        public IEnumerator Edge_Holds_And_FallRescues()
        {
            float edge = 170f + 16f;   // MidnightPalaceBuilder.Half + 16
            _m.Teleport(new Vector3(edge - 6f, 0.3f, 4f), Quaternion.Euler(0, 90, 0));
            _move = new Vector2(1, 0);
            yield return new WaitForSeconds(2.5f);
            _move = Vector2.zero;
            Assert.That(_m.transform.position.x, Is.LessThan(edge), "the invisible wall stops Pip");
            Assert.That(_m.transform.position.y, Is.GreaterThan(-1f));
            yield return new WaitForSeconds(0.6f);
            _m.Teleport(new Vector3(0, -12f, -150f), Quaternion.identity);
            yield return Until(() => _m.transform.position.y > -2f, 3f, "rescued");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Dream_MidnightPalace"), "no game over for falling");
        }

        [UnityTest] // PI-05 a dojo: strike the gong, beat 3 waves, get 15 minutes; the gong goes silent
        public IEnumerator Dojo_ThreeWaves_Reward()
        {
            var id = InteriorDirector.Current; var pd = PalaceDirector.Current;
            var door = Doors().First(d => d.type == RoomType.Dojo);
            id.WalkInto(door); yield return Until(() => id.Inside, 2f, "inside");
            var trial = id.Room.GetComponentInChildren<DojoTrial>();
            Assert.IsNotNull(trial);
            trial.StartTrial();
            float t = 0; int killed = 0;
            while (trial.Running && t < 40f)
            {
                foreach (var e in EnemyBase.All.ToArray())
                    if (e != null && !e.Dead && !e.Dropping && Vector3.Distance(e.transform.position, trial.arenaCenter.position) < 16f)
                    { e.TakeHit(new HitInfo { damage = 9999f, posture = 999f, point = e.transform.position, direction = Vector3.forward, heavy = true }); killed++; }
                t += Time.deltaTime; yield return null;
            }
            Assert.IsTrue(trial.Won, "trial won"); Assert.That(killed, Is.GreaterThanOrEqualTo(6));
            _m.Teleport(trial.arenaCenter.position + Vector3.up * 0.2f, Quaternion.identity);   // walk to the prize
            yield return new WaitForSeconds(2.5f);   // the minute fountain homes in
            Assert.That(pd.Minutes, Is.GreaterThanOrEqualTo(10));
            Assert.IsTrue(door.Cleared);
            id.WalkOut(door); yield return Until(() => !id.Inside, 2f, "out");
        }

        [UnityTest] // PI-06 the library draws the hour compass; vending machines pay out when slashed
        public IEnumerator Library_Compass_And_Vending()
        {
            var pd = PalaceDirector.Current;
            Assert.IsFalse(pd.HourCompass);
            var id = InteriorDirector.Current;
            var door = Doors().First(d => d.type == RoomType.Library);
            id.WalkInto(door); yield return Until(() => id.Inside, 2f, "inside"); yield return null;
            var npc = id.Room.dynamicRoot.GetComponentInChildren<NpcTalker>();
            Assert.That(npc.role, Is.EqualTo(RoomType.Library));
            npc.Interact(new Interaction.InteractionContext { Player = _m.gameObject, Motor = _m });
            yield return new WaitForSeconds(0.2f);
            var dlg = Object.FindFirstObjectByType<DialoguePanel>(); Assert.IsNotNull(dlg, "talk box opened"); dlg.Skip(); yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(pd.HourCompass, "librarian draws the compass");
            id.WalkOut(door); yield return Until(() => !id.Inside, 2f, "out");
            var vend = Object.FindObjectsByType<VendingMachine>(FindObjectsSortMode.None);
            Assert.That(vend.Length, Is.GreaterThanOrEqualTo(3), "vending machines on the streets");
            Assert.IsTrue(vend[0].TakeHit(new HitInfo { damage = 10, point = vend[0].transform.position }));
            Assert.That(LostMinuteCount(vend[0].transform.position), Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest] // PI-07 skill VFX + the M15 sound set: a swing spawns a crescent slash; every new clip and all 8 music loops load
        public IEnumerator SlashFx_And_AudioAssets()
        {
            int before = Presentation.SlashArc.Spawned;
            var pc = PlayerCombat.Current; Assert.IsNotNull(pc);
            pc.DevDo("light");
            yield return new WaitForSeconds(0.3f);
            Assert.That(Presentation.SlashArc.Spawned, Is.GreaterThan(before), "crescent slash spawned");
            foreach (Audio.Music m in System.Enum.GetValues(typeof(Audio.Music)))
                if (m != Audio.Music.None) Assert.IsNotNull(Resources.Load<AudioClip>("DreamRush/Audio/MUS_" + m), "music " + m);
            string[] sfx = { "Swing_0", "SwingHeavy_0", "Hit_0", "HitHeavy_0", "Whoosh_0", "Clank_0", "Burst", "Coin_0", "Door", "Grind", "Reflect", "EnemyAlert", "Shock", "UiHover", "UiBack", "Pickup" };
            foreach (var n in sfx) Assert.IsNotNull(Resources.Load<AudioClip>("DreamRush/Audio/SFX_" + n), "sfx " + n);
        }

        static int LostMinuteCount(Vector3 near) => Object.FindObjectsByType<LostMinute>(FindObjectsSortMode.None).Count(l => Vector3.Distance(l.transform.position, near) < 4f);
    }
}
