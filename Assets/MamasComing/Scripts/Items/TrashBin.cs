using System.Collections.Generic;
using UnityEngine;
namespace MamasComing
{
    /// <summary>A disposal destination with an animated lid, retaining Cabinet synchronization.
    /// Holding trash near it: E or Q pops the lid open, drops it in and closes the lid again.</summary>
    public class TrashBin : Cabinet
    {
        public int capacity = 4, stored;
        public static readonly List<TrashBin> Bins = new List<TrashBin>();
        protected override void OnEnable() { base.OnEnable(); if (!Bins.Contains(this)) Bins.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Bins.Remove(this); }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// Trash bags, pizza boxes, and anything whose home spot is on/in a bin.
        public static bool IsTrash(Pickup item)
        {
            if (!(item is MessyProp mp) || mp.dragOnFloor) return false;
            if (mp is RottenFood || mp.displayName == "Trash bag" || mp.displayName == "Pizza box") return true;
            if (!mp.homeSet) return false;
            foreach (var b in Bins) if (b && Vector3.Distance(Flat(b.transform.position), Flat(mp.homePosition)) < 0.9f) return true;
            return false;
        }

        /// The bin this player would throw their held trash into right now (null = none in reach).
        public static TrashBin For(PlayerController p)
        {
            if (!p || p.Held == null || !IsTrash(p.Held)) return null;
            Vector3 a = Flat(p.transform.position), f = Flat(p.transform.position + p.transform.forward * 0.75f);
            TrashBin best = null; float bd = float.MaxValue;
            foreach (var b in Bins)
            {
                if (!b || !b.isActiveAndEnabled || b.stored >= b.capacity) continue;
                Vector3 c = Flat(b.transform.position);
                float d = Mathf.Min(Vector3.Distance(a, c), Vector3.Distance(f, c) - 0.2f);
                if (d < 1.35f && d < bd) { bd = d; best = b; }
            }
            return best;
        }

        bool Trash(PlayerController p) => p != null && IsTrash(p.Held);
        public override bool CanInteract(PlayerController p) => true;
        public override float Priority(PlayerController p) => Trash(p) ? 1.5f : 0f;
        public override string GetPrompt(PlayerController p)
        {
            if (Trash(p) && stored >= capacity) return MamaReadability.Text("Bin full — try another bin", "ถังเต็มแล้ว ลองถังข้าง ๆ");
            if (Trash(p)) return MamaReadability.Text("Throw the " + p.Held.displayName.ToLower() + " in the bin", "ทิ้ง" + Loc.T(p.Held.displayName) + "ลงถัง");
            return MamaReadability.Text(IsOpen ? "Close the bin lid" : "Open the bin lid", IsOpen ? "ปิดฝาถังขยะ" : "เปิดฝาถังขยะ");
        }
        public override void Interact(PlayerController p)
        {
            if (Trash(p) && stored >= capacity) { if (GameManager.I) GameManager.I.ui.Confirm(MamaReadability.Text("This bin is full.", "ถังนี้เต็มแล้ว")); return; }
            if (Trash(p)) { Dispose(p); return; }
            SetOpen(!IsOpen); MamaAudio.Play("bin_lid", transform.position, .55f);
        }

        void Dispose(PlayerController p)
        {
            bool wasOpen = IsOpen;
            if (!wasOpen) { SetOpen(true); MamaAudio.Play("bin_lid", transform.position, .55f); }
            var item = p.ReleaseHeld() as MessyProp;
            Vector3 basePos = stashPoint ? stashPoint.position : transform.position + Vector3.up * 0.3f;
            item.homePosition = basePos + Vector3.up * stored * .11f;
            item.homeRotation = Quaternion.Euler(0f, transform.eulerAngles.y + Random.Range(-12f, 12f), 0f); item.homeSet = true;
            // squash big things (pizza box) so they sit inside the bin instead of poking through its walls
            var rs = item.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float wide = Mathf.Max(b.size.x, b.size.z), fit = 0.46f;
                if (wide > fit) item.transform.localScale *= fit / wide;
            }
            // Base drop skips MessyProp's old-home auto-settle and its duplicate pop.
            item.OnDropped(null, Vector3.zero);
            item.Settle(); stored++;
            MamaRound.Award(p, item, item.displayName, 50);
            if (item is RottenFood food) food.DisposeFood(p);
            MamaAudio.Play(item.displayName == "Trash bag" ? "bag_drop" : "place", transform.position, .7f);
            if (GameManager.I) GameManager.I.ui.Confirm(MamaReadability.Text(item.displayName + " thrown in the bin.", "ทิ้ง" + Loc.T(item.displayName) + "ลงถังแล้ว"));
            if (!wasOpen) { CancelInvoke(nameof(CloseLid)); Invoke(nameof(CloseLid), 0.7f); }
        }
        void CloseLid() { SetOpen(false); MamaAudio.Play("bin_lid", transform.position, .45f, 1.1f); }
    }
}
