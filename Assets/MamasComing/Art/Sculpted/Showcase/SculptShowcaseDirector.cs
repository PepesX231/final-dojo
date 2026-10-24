using UnityEngine;

namespace MamasComing
{
    /// Showcase only (Scenes/MamaSculptShowcase): cycles each sculpted kid through its moves and fires the comic VFX,
    /// so the models, animation and effects can be reviewed together without the game systems.
    public class SculptShowcaseDirector : MonoBehaviour
    {
        public Animator[] kids;
        public string[] cycle = { "Idle", "Walk", "Punch", "Jump", "Taunt", "Run", "Hit", "Cheer" };
        public float every = 1.6f;
        float t; int step;

        void Update()
        {
            t += Time.deltaTime;
            if (t < every) return;
            t = 0; step++;
            for (int i = 0; i < kids.Length; i++)
            {
                if (!kids[i]) continue;
                string s = cycle[(step + i) % cycle.Length];
                kids[i].CrossFadeInFixedTime(s, .12f);
                Vector3 p = kids[i].transform.position;
                if (s == "Punch") ComicFx.Pow(p + kids[i].transform.forward * .55f + Vector3.up * .6f, new Color(1f, .4f, .3f));
                if (s == "Jump") ComicFx.Puff(p, .7f, 5);
                if (s == "Hit") ComicFx.Dizzy(kids[i].transform, 1.4f, .3f, 1.2f);
                if (s == "Cheer") ComicFx.Splash(p + kids[i].transform.right * .6f, .8f);
            }
        }
    }
}
