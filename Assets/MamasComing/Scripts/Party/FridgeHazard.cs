using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- KITCHEN: fridge door left open after the party
    public class FridgeHazard : Hazard
    {
        public Transform door;
        public ParticleSystem stink;
        public float openAngle = -75f;
        bool spoiled;
        public void Spoil()
        {
            if (spoiled) return; spoiled = true; Failed = true; Active = true; RefreshVisuals();
            if (QuestDirector.I) QuestDirector.I.Alarm(this, "The fridge has been warm too long... the food is SPOILING.", "ตู้เย็นไม่เย็นนานเกินไป... อาหารเริ่มบูด");
        }
        protected override void OnFailed() { spoiled = true; }
        protected override void VisualTick(float dt, bool running)
        {
            if (door) door.localRotation = Quaternion.Slerp(door.localRotation, Quaternion.Euler(0, Active && !Fixed ? openAngle : 0f, 0), dt * 8f);
            if (stink) { var em = stink.emission; em.rateOverTime = spoiled ? 8f : 0f; }
        }
        public override bool IsProblem => spoiled || (Active && !Fixed);
    }
}
