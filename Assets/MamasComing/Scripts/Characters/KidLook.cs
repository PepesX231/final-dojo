using UnityEngine;

namespace MamasComing
{
    /// Which fruit a player is (Apple, Banana, Orange, Watermelon, Wax apple) and swapping a kid's model at runtime.
    /// The menu / lobby sets <see cref="Selected"/>; the scene kid wears it on spawn and online play sends the id
    /// with every pose so friends see the right fruit. Models come from the fruit kid prefabs listed in
    /// Resources/MamaKidCatalog (built by "Mama's Coming/Build Kid Catalog"); the apple is the scene kid's own model.
    public static class KidLook
    {
        public const int Apple = 0, Banana = 1, Orange = 2, Watermelon = 3, WaxApple = 4, Count = 5;
        public static readonly string[] Names = { "Apple", "Banana", "Orange", "Watermelon", "WaxApple" };
        public static readonly string[] NamesTh = { "แอปเปิล", "กล้วย", "ส้ม", "แตงโม", "ชมพู่" };
        const string Pref = "mama.kid";

        static int selected = -1;
        /// This machine's chosen fruit (saved between sessions).
        public static int Selected
        {
            get { if (selected < 0) selected = Mathf.Clamp(PlayerPrefs.GetInt(Pref, Apple), 0, Count - 1); return selected; }
            set { selected = Mathf.Clamp(value, 0, Count - 1); PlayerPrefs.SetInt(Pref, selected); }
        }

        static KidCatalog catalog; static bool loaded;
        static KidCatalog Catalog { get { if (!loaded) { loaded = true; catalog = Resources.Load<KidCatalog>("MamaKidCatalog"); } return catalog; } }

        static GameObject appleTemplate;   // inactive copy of the scene kid's original apple model
        public static GameObject AppleTemplate => appleTemplate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { selected = -1; loaded = false; catalog = null; appleTemplate = null; }

        public static int Of(PlayerController pc) { var t = Chr(pc); return t ? IdOf(t.name) : Apple; }

        public static int IdOf(string chrName)
        {
            for (int i = Count - 1; i > 0; i--) if (chrName.Contains(Names[i])) return i;   // "WaxApple" before "Apple"
            return Apple;
        }

        static Transform Visual(PlayerController pc) => pc && pc.fallPivot ? (pc.fallPivot.Find("Visual") ?? pc.fallPivot) : null;

        static Transform Chr(PlayerController pc)
        {
            var v = Visual(pc); if (!v) return null;
            foreach (Transform c in v) if (c.name.StartsWith("CHR_")) return c;
            return null;
        }

        static string appleTuning;

        /// Keep a rest-pose copy of the scene kid's apple model, so a kid that changed fruit can change back
        /// (and remote apples can be shown while we play a banana). Call before the model animates (Awake).
        public static void RememberApple(PlayerController pc)
        {
            if (appleTemplate) return;
            var old = Chr(pc); if (!old || IdOf(old.name) != Apple) return;
            old.gameObject.SetActive(false);   // copy it inactive: the template never runs its animators
            appleTemplate = Object.Instantiate(old.gameObject);
            old.gameObject.SetActive(true);
            appleTemplate.name = old.name;
            Object.DontDestroyOnLoad(appleTemplate);
            if (pc.actions) appleTuning = JsonUtility.ToJson(pc.actions);
        }

        /// Dress this kid as fruit <paramref name="id"/>. Returns false (and leaves the kid alone) when that model is not available.
        public static bool Apply(PlayerController pc, int id)
        {
            var visual = Visual(pc); var old = Chr(pc);
            if (!visual || !old) return false;
            id = Mathf.Clamp(id, 0, Count - 1);
            if (IdOf(old.name) == id) return true;

            RememberApple(pc);
            GameObject src = null; Transform srcRoot = null;
            if (id == Apple) src = appleTemplate;
            else if (Catalog && Catalog.Prefab(id)) { srcRoot = Catalog.Prefab(id).transform; foreach (var t in srcRoot.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("CHR_")) { src = t.gameObject; break; } }
            if (!src) return false;

            Vector3 lp = src == appleTemplate ? old.localPosition : src.transform.localPosition;
            Quaternion lr = src == appleTemplate ? old.localRotation : src.transform.localRotation;
            Vector3 ls = src == appleTemplate ? old.localScale : src.transform.localScale;
            old.gameObject.SetActive(false); old.name = "_old"; Object.Destroy(old.gameObject);
            var go = Object.Instantiate(src, visual, false);
            go.name = src.name; go.transform.localPosition = lp; go.transform.localRotation = lr; go.transform.localScale = ls;
            go.SetActive(true);
            var chr = go.transform;

            // limb / body animators follow the new model
            pc.anim = go.GetComponentInChildren<DreamRush.Player.AppleProceduralAnimator>(true);
            foreach (var fa in go.GetComponentsInChildren<FruitAnimator>(true)) { fa.player = pc; fa.enabled = true; }
            var act = pc.actions;
            if (act)
            {
                // copy the fruit's tuning (pivot, carry depth...) from its prefab, then point it at this kid
                var srcAct = srcRoot ? srcRoot.GetComponent<KidActionAnimator>() : null;
                if (srcAct) JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(srcAct), act);
                else if (appleTuning != null) JsonUtility.FromJsonOverwrite(appleTuning, act);
                act.player = pc;
                act.Rebind(chr, act.pivot);
            }
            if (pc.Body) pc.Body.Rebind();
            if (pc.anim && !pc.isLocal) pc.WakeUp();
            return true;
        }
    }
}
