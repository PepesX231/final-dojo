using System.Collections;
using System.Linq;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.OpenWorld;
using DreamRush.Player;
using DreamRush.UI;
using DreamRush.World1;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>Dreamland open world: content, move set, the fleeing chair, THE ALARM.</summary>
    public class OpenWorldPlayModeTests
    {
        PlayerMotor3D _m;
        PlayerAbilities _ab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Dream_Openworld");
            yield return null; yield return null;
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _ab = _m.GetComponent<PlayerAbilities>();
            Assert.IsNotNull(_ab, "player has the open-world move set");
            float t = 0f;
            while (!_m.IsGrounded && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(_m.IsGrounded, "landed in Dreamland");
        }

        [TearDown]
        public void TearDown()
        {
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
        }

        static IEnumerator Until(System.Func<bool> c, float timeout, string what)
        {
            float t = 0; while (!c()) { t += Time.deltaTime; if (t > timeout) Assert.Fail("timed out: " + what); yield return null; }
        }

        [UnityTest] // OW-01
        public IEnumerator World_HasRegionsNavMeshAndContent()
        {
            var chair = Object.FindFirstObjectByType<OpenWorldChair>();
            var alarm = Object.FindFirstObjectByType<NightmareAlarm>();
            Assert.IsNotNull(chair); Assert.IsNotNull(alarm);
            Assert.That(chair.GetComponent<NavMeshAgent>().isOnNavMesh, "chair on NavMesh");
            Assert.That(alarm.GetComponent<NavMeshAgent>().isOnNavMesh, "alarm on NavMesh");
            int reachable = 0;
            var path = new NavMeshPath();
            foreach (var l in chair.landmarks)
                if (NavMesh.CalculatePath(chair.transform.position, l.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) reachable++;
            Debug.Log($"[OW-01] landmarks reachable {reachable}/{chair.landmarks.Count}");
            Assert.That(reachable, Is.GreaterThanOrEqualTo(chair.landmarks.Count * 3 / 4));
            Assert.That(Object.FindObjectsByType<SafeZone>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(Object.FindObjectsByType<DreamStar>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(10));
            Assert.That(Object.FindObjectsByType<SpeedRing>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(5));
            yield return new WaitForSeconds(1f);
            var grass = Object.FindFirstObjectByType<GrassField>();
            Assert.That(grass.DrawnLastFrame, Is.GreaterThan(500), "grass around the player");
        }

        [UnityTest] // OW-02 double jump goes higher than a jump; dash covers ground fast; glide falls slowly
        public IEnumerator MoveSet_DoubleJumpDashGlide()
        {
            // presses last exactly one frame (the motor and the move set both read the source every frame)
            int jumpFrame = -1, dashFrame = -1; bool jumpHeld = false;
            Vector2 move = Vector2.zero;
            _m.CameraTransform = null;
            _m.CommandSource = new FuncCommandSource(() => new PlayerCommand { Move = move, JumpPressed = Time.frameCount == jumpFrame, JumpHeld = jumpHeld, DashPressed = Time.frameCount == dashFrame });
            float y0 = _m.transform.position.y;
            // single jump peak
            jumpFrame = Time.frameCount + 1; jumpHeld = true;
            float peak1 = y0;
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { peak1 = Mathf.Max(peak1, _m.transform.position.y); yield return null; }
            yield return Until(() => _m.IsGrounded, 3f, "land 1");
            // double jump at apex
            y0 = _m.transform.position.y;
            jumpFrame = Time.frameCount + 1; jumpHeld = true;
            yield return new WaitForSeconds(0.4f);
            jumpFrame = Time.frameCount + 1;
            float peak2 = y0;
            for (float t = 0; t < 1.6f; t += Time.deltaTime) { peak2 = Mathf.Max(peak2, _m.transform.position.y); yield return null; }
            Debug.Log($"[OW-02] jump {peak1 - y0:F2} m, double {peak2 - y0:F2} m");
            Assert.That(_ab.UsedDoubleJump || _m.IsGrounded);
            Assert.That(peak2 - y0, Is.GreaterThan((peak1 - y0) + 1.0f), "double jump adds height");
            // glide: keep holding jump while falling
            float minVy = 0f;
            for (float t = 0; t < 0.8f; t += Time.deltaTime) { if (_ab.Gliding) minVy = Mathf.Min(minVy, _m.VerticalVelocity); yield return null; }
            Assert.That(minVy, Is.GreaterThan(-2.6f), "glide keeps the fall slow");
            jumpHeld = false;
            yield return Until(() => _m.IsGrounded, 6f, "land 2");
            // dash on the ground
            Vector3 p0 = _m.transform.position;
            move = new Vector2(0, 1); dashFrame = Time.frameCount + 1;
            yield return new WaitForSeconds(0.35f);   // walking alone covers ≤ 1.9 m in 0.35 s
            float d = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(_m.transform.position.x, 0, _m.transform.position.z));
            Debug.Log($"[OW-02] dash moved {d:F2} m in 0.35 s");
            Assert.That(d, Is.GreaterThan(3.0f));
            move = Vector2.zero;
            _m.CommandSource = null;
        }

        [UnityTest] // OW-03 the chair flees, tires, and gives a key when sat on
        public IEnumerator Chair_FleesThenGivesKeyWhenSatOn()
        {
            var chair = Object.FindFirstObjectByType<OpenWorldChair>();
            var dir = OpenWorldDirector.Current;
            Vector3 c0 = chair.transform.position;
            _m.Teleport(c0 + new Vector3(0, 0.5f, -8f), Quaternion.identity);
            yield return Until(() => chair.Current == OpenWorldChair.State.Flee, 3f, "flee");
            yield return new WaitForSeconds(2.5f);
            Assert.That(Vector3.Distance(chair.transform.position, c0), Is.GreaterThan(5f), "it ran");
            chair.DevExhaust();
            _m.Teleport(chair.transform.position + Vector3.up * 1.0f, Quaternion.identity);
            yield return Until(() => chair.Current == OpenWorldChair.State.Sitting, 2f, "sitting");
            yield return Until(() => chair.KeysDropped == 1, 4f, "key");
            Assert.That(dir.Keys, Is.EqualTo(1));
            yield return new WaitForSeconds(0.5f);
            Assert.That(_m.Controller.enabled && _m.AutoStep, "player released");
        }

        [UnityTest] // OW-04 THE ALARM notices you, chases, and gives up when you reach a pillow fire
        public IEnumerator Alarm_ChasesThenLosesYouInSafeZone()
        {
            var alarm = Object.FindFirstObjectByType<NightmareAlarm>();
            Vector3 front = alarm.transform.position + alarm.transform.forward * 10f;
            front.y = DreamlandTerrain.Height(front.x, front.z) + 0.5f;
            _m.Teleport(front, Quaternion.identity);
            yield return Until(() => alarm.Current == NightmareAlarm.State.Chase || alarm.Current == NightmareAlarm.State.Slam, 4f, "chase");
            var safe = Object.FindObjectsByType<SafeZone>(FindObjectsSortMode.None).OrderBy(z => Vector3.Distance(z.transform.position, alarm.transform.position)).First();
            _m.Teleport(safe.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            yield return Until(() => alarm.Current == NightmareAlarm.State.Search || alarm.Current == NightmareAlarm.State.Roam, 3f, "gave up");
        }

        [UnityTest] // OW-05 the whole loop: 3 chases → 3 Dream Keys → victory; the chair warps far away and gets fitter each time
        public IEnumerator FullLoop_ThreeKeysWinsTheDream()
        {
            var chair = Object.FindFirstObjectByType<OpenWorldChair>();
            var dir = OpenWorldDirector.Current;
            var alarm = Object.FindFirstObjectByType<NightmareAlarm>();
            alarm.gameObject.SetActive(false);   // this test is about the chair loop
            Vector3 lastSpawn = chair.transform.position;
            float lastStamina = chair.staminaMax;
            for (int k = 1; k <= 3; k++)
            {
                yield return Until(() => chair.Current == OpenWorldChair.State.Waiting, 6f, "chair back (key " + k + ")");
                if (k > 1)
                {
                    float hop = Vector3.Distance(chair.transform.position, lastSpawn);
                    Debug.Log($"[OW-05] key {k}: chair reappeared {hop:F0} m away, stamina {chair.staminaMax:F1}, run {chair.runSpeed:F1}");
                    Assert.That(hop, Is.GreaterThan(30f), "the chair warps somewhere else");
                    Assert.That(chair.staminaMax, Is.GreaterThan(lastStamina), "fitter each key");
                }
                lastSpawn = chair.transform.position; lastStamina = chair.staminaMax;
                Vector3 c0 = chair.transform.position;
                Vector3 near = c0 + new Vector3(0, 0, -8f); near.y = DreamlandTerrain.Height(near.x, near.z) + 0.6f;
                _m.Teleport(near, Quaternion.identity);
                yield return Until(() => chair.Current == OpenWorldChair.State.Flee, 4f, "flee " + k);
                yield return new WaitForSeconds(1f);
                chair.DevExhaust();
                _m.Teleport(chair.transform.position + Vector3.up * 1.0f, Quaternion.identity);
                yield return Until(() => chair.Current == OpenWorldChair.State.Sitting, 2f, "sitting " + k);
                yield return Until(() => chair.KeysDropped == k, 4f, "key " + k);
                Assert.That(dir.Keys, Is.EqualTo(k));
            }
            Assert.IsTrue(dir.Won, "3 keys = the dream is won");
        }
    }
}
