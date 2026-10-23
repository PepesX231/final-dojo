using UnityEngine;

namespace MamasComing.WitchTest
{
    public class WitchHome : MonoBehaviour
    {
        public string acceptsId, label;
        public Vector2 halfArea = new Vector2(.07f, .07f);
        public float verticalTolerance = .09f;
        public SpriteRenderer cue;

        public bool Accepts(WitchProp prop, Vector3 pivot)
        {
            if (!prop || prop.id != acceptsId) return false;
            Vector3 p = transform.InverseTransformPoint(pivot);
            return Mathf.Abs(p.x) <= halfArea.x && Mathf.Abs(p.z) <= halfArea.y && Mathf.Abs(p.y) <= verticalTolerance;
        }

        public void Show(bool visible, bool valid)
        {
            if (!cue) return;
            cue.gameObject.SetActive(visible);
            cue.color = valid ? new Color(.65f, 1f, .77f, .75f) : new Color(1f, .77f, .52f, .4f);
        }
    }
}
