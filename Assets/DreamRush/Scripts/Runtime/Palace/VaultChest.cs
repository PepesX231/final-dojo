using DreamRush.Interaction;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>TIME VAULT: a platforming room (gear steps, a grind rail) with a chest at the top: 12 minutes, once per vault.</summary>
    public class VaultChest : Interactable
    {
        public Transform lid;
        BuildingDoor _door; bool _open;

        protected override void OnEnable() { base.OnEnable(); prompt = "OPEN"; }

        public void Arm(BuildingDoor door)
        {
            _door = door;
            _open = PalaceDirector.Current != null && PalaceDirector.Current.Taken("door:" + door.id);
            interactable = !_open; prompt = _open ? "EMPTY" : "OPEN";
            if (lid != null) lid.localRotation = Quaternion.Euler(_open ? -100f : 0f, 0, 0);
        }

        protected override void OnInteract(InteractionContext context)
        {
            if (_open) return;
            _open = true; interactable = false;
            if (lid != null) lid.localRotation = Quaternion.Euler(-100f, 0, 0);
            if (PalaceDirector.Current != null && _door != null) PalaceDirector.Current.Take("door:" + _door.id);
            LostMinute.Burst(transform.position + Vector3.up * 0.5f, 12);
            Presentation.VFX.Play(Presentation.VFXKind.StarBurst, transform.position + Vector3.up, 2f, Presentation.VFX.Gold);
            Audio.AudioManager.Play(Audio.Sfx.Victory, transform.position, 0.7f);
            UIRoot.Get().Toast("TIME VAULT — LOST MINUTE ×12", 1.6f);
        }
    }
}
