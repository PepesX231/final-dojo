using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MamasComing.Editor
{
    /// Play mode: makes each loading-tip problem actually happen in the house and photographs it for Resources/MamaTips.
    /// exec MamasComing.Editor.MamaTipShots.Run   (then stop Play mode and refresh so the PNGs re-import)
    public static class MamaTipShots
    {
        public static void Run() { var go = new GameObject("Tip shots"); go.AddComponent<Runner>(); }

        class Runner : MonoBehaviour
        {
            Camera cam; RenderTexture rt;
            const string Dir = "Assets/MamasComing/Resources/MamaTips/";

            ThirdPersonCamera tpc; PlayerController kid;
            IEnumerator Start()
            {
                var gm = GameManager.I; if (!gm) { Debug.LogWarning("[TipShots] no GameManager"); Destroy(gameObject); yield break; }
                if (gm.state == GameManager.State.Title) gm.StartRound();
                kid = gm.player; tpc = Camera.main.GetComponent<ThirdPersonCamera>();
                if (tpc) { tpc.distance = 4.6f; }
                yield return new WaitForSeconds(.5f);
                rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.ARGB32);
                Debug.Log("[TipShots] pot at " + (gm.pot ? gm.pot.transform.position.ToString("F2") + " active=" + gm.pot.gameObject.activeInHierarchy : "none"));

                // 1) burnt pot: black pot on the stove
                if (gm.pot) { gm.pot.burn = 1f; yield return Shot("pot", gm.pot.transform.position); }
                // 2) oil fire on the stove
                if (gm.pot && gm.pot.Fire) { gm.pot.Fire.Ignite(1f); yield return new WaitForSeconds(4f); yield return Shot("fire", gm.pot.transform.position); gm.pot.Fire.intensity = 0; gm.pot.Fire.burning = false; }
                // 3) rotten food in the open fridge
                var fridge = Object.FindFirstObjectByType<FridgeHazard>();
                if (fridge)
                {
                    fridge.Spoil();
                    var f = typeof(FridgeHazard).GetField("doorOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (f != null) f.SetValue(fridge, true);
                    yield return new WaitForSeconds(1.5f);
                    yield return Shot("fridge", fridge.transform.position);
                }
                // 4) water flooding out of the bathroom
                if (gm.tap)
                {
                    gm.tap.running = true;
                    if (FloodSim.I) for (int i = 0; i < 12; i++) FloodSim.I.AddLiters(gm.tap.transform.position + new Vector3(Random.Range(-1.6f, 1.6f), 0, Random.Range(-2.2f, -.2f)), 260f, .9f);
                    yield return new WaitForSeconds(2f);
                    yield return Shot("water", gm.tap.transform.position);
                }
                // 5) power cut at the breaker panel
                var breaker = Object.FindFirstObjectByType<BreakerPanel>();
                if (breaker) { PowerGrid.Trip("tip photo", "tip photo"); yield return new WaitForSeconds(1.5f); yield return Shot("power", breaker.transform.position); PowerGrid.Restore(); }
                Debug.Log("[TipShots] done");
                rt.Release(); Destroy(gameObject);
            }

            /// Walk the kid next to the subject so the game camera (with its cut-away walls) frames it, then grab that view.
            IEnumerator Shot(string key, Vector3 aim)
            {
                var cam = Camera.main;
                // stand on the open side of the subject (8 directions, longest free line of sight wins, the current view breaks ties)
                Vector3 toCam = cam.transform.position - aim; toCam.y = 0; toCam.Normalize();
                Vector3 best = toCam; float bestScore = -1e9f;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 d = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
                    Vector3 from = aim + Vector3.up * .6f;
                    float free = Physics.Raycast(from + d * .45f, d, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) ? hit.distance : 3f;
                    float score = free + Vector3.Dot(d, toCam) * .4f;
                    if (score > bestScore) { bestScore = score; best = d; }
                }
                Vector3 stand = aim + best * 1.15f; stand.y = kid.transform.position.y;
                if (kid.Motor) kid.Motor.Teleport(stand, Quaternion.LookRotation(-best)); else kid.transform.position = stand;
                if (tpc) tpc.yaw = Quaternion.LookRotation(-best).eulerAngles.y;
                yield return new WaitForSeconds(2.4f);
                float fov = cam.fieldOfView; cam.fieldOfView = fov * .8f;
                var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old; cam.fieldOfView = fov;
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(1024, 576, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Dir + key + ".png", tex.EncodeToPNG()); Destroy(tex);
                Debug.Log("[TipShots] " + key);
            }
        }
    }
}
