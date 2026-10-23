using MamasComing;
using UnityEditor;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// Writes Resources/MamaKidCatalog.asset: the fruit kid prefabs KidLook swaps in at runtime.
    public static class MamaKidCatalogBuilder
    {
        const string Path = "Assets/MamasComing/Resources/MamaKidCatalog.asset";
        const string Dir = "Assets/MamasComing/Prefabs/Characters/";

        [MenuItem("Mama's Coming/Build Kid Catalog")]
        public static void Build()
        {
            var cat = AssetDatabase.LoadAssetAtPath<KidCatalog>(Path);
            if (!cat) { cat = ScriptableObject.CreateInstance<KidCatalog>(); AssetDatabase.CreateAsset(cat, Path); }
            cat.kids = new GameObject[KidLook.Count];
            for (int i = 1; i < KidLook.Count; i++)
            {
                cat.kids[i] = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "PF_Kid_" + KidLook.Names[i] + ".prefab");
                if (!cat.kids[i]) Debug.LogWarning("[Mama's Coming] missing PF_Kid_" + KidLook.Names[i]);
            }
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mama's Coming] kid catalog written to " + Path);
        }
    }
}
