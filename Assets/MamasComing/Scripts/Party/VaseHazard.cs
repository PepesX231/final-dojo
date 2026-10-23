using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- LIVING ROOM: Mom's vase broken at the party
    public class VaseHazard : Hazard
    {
        public GameObject shards;
        public MessyProp gluedVase;
        protected override void OnFixed(PlayerController p)
        {
            if (shards) shards.SetActive(false);
            if (gluedVase)
            {
                gluedVase.gameObject.SetActive(true);
                if (InteractPoint != Vector3.zero) gluedVase.transform.position = transform.position + Vector3.up * 0.15f;
            }
        }
        public override bool IsProblem => Active && !Fixed;
        public override string MomLine => T("My VASE?! Where's my good vase?!", "แจกันของแม่?! แจกันใบสวยหายไปไหน?!");
    }
}
