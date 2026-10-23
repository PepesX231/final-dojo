using UnityEngine;

namespace MamasComing
{
    /// <summary>Floor drain. Starts clogged with hair; hold E to unclog.</summary>
    public class Drain : Interactable
    {
        public float radius = 0.7f;
        public float clearRate = 4.5f;
        public float cloggedRate = 0.06f;
        public bool clogged = true;
        public GameObject hairClump;
        [HideInInspector] public float drained;

        public float CurrentRate => clogged ? cloggedRate : clearRate;

        void Start() { if (FloodSim.I) FloodSim.I.Register(this); }

        void Update() { if (hairClump) hairClump.SetActive(clogged); }

        public override bool CanInteract(PlayerController p) => clogged;
        public override string GetPrompt(PlayerController p) => "Hold [E]  Pull the hair out of the drain (ew!)";
        public override float HoldTime(PlayerController p) => 2.2f;
        public override void Interact(PlayerController p)
        {
            clogged = false;
            MamaAudio.Play("gurgle", transform.position, 1f); MamaAudio.Play("squish", transform.position, 0.6f, 0.8f);
            if (GameManager.I) GameManager.I.Toast("Ew... but it's draining!");
        }
    }
}
