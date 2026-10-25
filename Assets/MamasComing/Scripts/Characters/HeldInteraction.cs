namespace MamasComing
{
    /// The prompt and host validation use the same compatibility rule.
    public static class HeldInteraction
    {
        public static bool Allows(PlayerController p, Interactable target)
        {
            if(!p || !target)return false;
            var h=p.Held; if(!h)return true;
            if(target==h)return false;
            if(target is TrashBin)return TrashBin.IsTrash(h);
            if(target is Cabinet)return !h.dragOnFloor; // open a hiding spot and stash what you carry (the old rule blocked hiding anything but the pot)
            if(target is WaterDump)return h is Bucket || h is Mop || h is Towel;
            if(target is CookPot)return h is Lid || h is Towel;
            if(target is Bucket)return h is Mop m && m.load>.05f;
            if(target is SmokeAlarm)return h is Mop || h is Squeegee;
            if(target is AquariumHazard)return h is Fish;
            if(target is SinkPipeHazard)return h is ToolItem t && t.kind==ToolItem.Kind.Wrench;
            if(target is ToiletHazard)return h is ToolItem t && t.kind==ToolItem.Kind.Plunger;
            return false;
        }
    }
}
