using UnityEngine;

namespace MamasComing
{
    public class Window : Interactable
    {
        public bool open;
        public Transform sash;
        public Vector3 closedLocal;
        public Vector3 openLocal;

        void Update()
        {
            if (sash) sash.localPosition = Vector3.Lerp(sash.localPosition, open ? openLocal : closedLocal, Time.deltaTime * 6f);
        }

        public override string GetPrompt(PlayerController p) => open ? "Close the window" : "Open the window";

        public override void Interact(PlayerController p)
        {
            open = !open;
            MamaAudio.Play("creak", transform.position, 0.7f, open ? 1.1f : 0.9f); if (open) MamaAudio.Play("whoosh", transform.position, 0.5f, 0.8f);
            if (open && GameManager.I) GameManager.I.Toast("Window open - smoke will drift out. A fan would help...");
        }
    }
}
