using System.Collections;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Flow
{
    /// <summary>Small scripted player motions (climb into bed, lie down, wake stretch). Gameplay stays frozen meanwhile.</summary>
    public static class PlayerCutscene
    {
        public static Transform Visual(PlayerMotor3D m)
        {
            var v = m.transform.Find("Visual");
            return v != null ? v : m.transform;
        }

        /// <summary>Hop in an arc onto <paramref name="pose"/>, then tip over onto the back.</summary>
        public static IEnumerator HopAndLieDown(PlayerMotor3D m, Transform pose, float hopTime, float lieTime)
        {
            m.AutoStep = false;
            m.StopImmediately();
            var cc = m.Controller;
            cc.enabled = false;
            Vector3 a = m.transform.position, b = pose.position;
            Quaternion ra = m.transform.rotation, rb = Quaternion.Euler(0f, pose.eulerAngles.y, 0f);
            float apex = Mathf.Max(a.y, b.y) + 0.8f;
            for (float t = 0; t < hopTime; t += Time.unscaledDeltaTime)
            {
                float k = t / hopTime;
                Vector3 p = Vector3.Lerp(a, b, k);
                p.y += 4f * (apex - Mathf.Max(a.y, b.y)) * k * (1f - k); // parabolic hop
                m.transform.SetPositionAndRotation(p, Quaternion.Slerp(ra, rb, k));
                yield return null;
            }
            m.transform.SetPositionAndRotation(b, rb);

            var vis = Visual(m);
            Quaternion v0 = vis.localRotation;
            Quaternion v1 = Quaternion.Euler(-90f, 0f, 0f); // face toward the ceiling
            Vector3 p0 = vis.localPosition, p1 = new Vector3(0f, 0.42f, 0.6f); // centred on the mattress, head toward the pillow (-Z of pose)
            var ss = vis.GetComponent<PlayerSquashStretch>();
            if (ss != null) ss.enabled = false;
            var anim = m.GetComponentInChildren<AppleProceduralAnimator>();
            if (anim != null) anim.sleeping = true; // relax the clasp so the face stays visible
            for (float t = 0; t < lieTime; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / lieTime);
                vis.localRotation = Quaternion.Slerp(v0, v1, k);
                vis.localPosition = Vector3.Lerp(p0, p1, k);
                yield return null;
            }
            vis.localRotation = v1;
            vis.localPosition = p1;
        }

        /// <summary>Restore the visual after a sleep and do a small wake-up stretch.</summary>
        public static IEnumerator WakeStretch(PlayerMotor3D m)
        {
            var vis = Visual(m);
            vis.localRotation = Quaternion.identity;
            vis.localPosition = Vector3.zero;
            var ss = vis.GetComponent<PlayerSquashStretch>();
            if (ss != null) ss.enabled = true;
            m.Controller.enabled = true;
            m.AutoStep = true;
            Vector3 s = vis.localScale;
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / 0.6f * Mathf.PI);
                vis.localScale = new Vector3(s.x * (1f - 0.08f * k), s.y * (1f + 0.15f * k), s.z * (1f - 0.08f * k));
                yield return null;
            }
            vis.localScale = s;
        }
    }
}
