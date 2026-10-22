using DreamRush.Player;
using UnityEngine;

namespace DreamRush.OpenWorld
{
    /// <summary>Pip can't swim (yet): sinking under the lake surface returns you to the last pillow fire (−1 moon via Fell).</summary>
    public class WaterRescue : MonoBehaviour
    {
        public float depth = 1.1f;
        PlayerMotor3D _p;
        float _under;
        void Start() => _p = FindFirstObjectByType<PlayerMotor3D>();
        void Update()
        {
            if (_p == null) return;
            Vector3 pos = _p.transform.position;
            Vector2 d = new Vector2(pos.x, pos.z) - DreamlandTerrain.Lake;
            bool under = d.magnitude < 100f && pos.y < DreamlandTerrain.WaterLevel - depth;
            _under = under ? _under + Time.deltaTime : 0f;
            if (_under > 0.25f)
            {
                _under = 0f;
                Presentation.VFX.Play(Presentation.VFXKind.Poof, new Vector3(pos.x, DreamlandTerrain.WaterLevel + 0.3f, pos.z), 2f, Presentation.VFX.Cream);
                UI.UIRoot.Get().Toast("บลุ๊บ… Pip ว่ายน้ำไม่เป็น!", 1.5f);
                Dream.DreamLevelManager.Current?.Respawn();
            }
        }
    }
}
