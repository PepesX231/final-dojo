using UnityEngine;

namespace MamasComing
{
    /// Fruit kid prefabs indexed by KidLook id (index 0 = apple, the scene kid's own model, may stay empty).
    /// Lives at Resources/MamaKidCatalog so runtime code can find the models without scene references.
    [CreateAssetMenu(menuName = "Mama's Coming/Kid Catalog")]
    public class KidCatalog : ScriptableObject
    {
        public GameObject[] kids = new GameObject[KidLook.Count];

        public GameObject Prefab(int id) => kids != null && id >= 0 && id < kids.Length ? kids[id] : null;
    }
}
