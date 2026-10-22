using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M16: per-instance colour for shared materials whose name contains <see cref="keyword"/> (car paint, pyjamas, kiosks).</summary>
    public class RandomTint : MonoBehaviour
    {
        public string keyword = "CarPaint";
        public Color[] palette = { Color.white };
        public int seed;

        void Start()
        {
            var rnd = new System.Random(seed != 0 ? seed : GetInstanceID());
            var c = palette[rnd.Next(palette.Length)];
            var mpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.Contains(keyword)) { mpb.Clear(); mpb.SetColor("_BaseColor", c); r.SetPropertyBlock(mpb, i); }
            }
        }
    }
}
