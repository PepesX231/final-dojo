using UnityEngine;

namespace MamasComing
{
    /// <summary>Pot lid. The correct way to kill a pot grease fire.</summary>
    public class Lid : MessyProp
    {
        public override bool CanSettle => false;
        CookPot onPot;

        public void AttachTo(Transform socket)
        {
            transform.SetParent(socket, false);
            MamaAudio.Play("slam", socket.position, 0.6f, 1.4f);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            Body.isKinematic = true;
            SetCols(false);
            onPot = socket.GetComponentInParent<CookPot>();
            homePosition = transform.position; // on the pot is a fine "home"
            homeRotation = transform.rotation;
        }

        public override bool CanInteract(PlayerController p) => onPot == null && base.CanInteract(p);
        public override bool IsProblem => onPot == null && base.IsProblem;
    }
}
