using UnityEngine;

namespace MamasComing
{
    /// Remembers whether a label was bold before the theme swapped its font (added automatically).
    [DisallowMultipleComponent]
    public class MamaThemeWeight : MonoBehaviour
    {
        public bool bold;
    }
}
