using DreamRush.Core;
using DreamRush.DreamLogic;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Cards
{
    public struct CardUseResult
    {
        public bool Used;
        public string Message;
        public IEffectReceiver Target;
    }

    /// <summary>
    /// Card activation pipeline (GDD §17.5 / §18.4): select target → range → validate property → apply effect →
    /// feedback. Invalid targets never trigger effects or consume the cooldown. No card-vs-boss special cases.
    /// </summary>
    public static class CardEffectResolver
    {
        public static CardUseResult Use(CardDefinition card, PlayerMotor3D player)
        {
            var res = new CardUseResult();
            if (card == null || player == null) { res.Message = "No card"; return res; }
            Vector3 pos = player.transform.position, fwd = player.transform.forward;
            var ctx = new EffectContext { Action = card.action, Source = player, Duration = card.duration, CardId = card.cardId };

            switch (card.action)
            {
                case EffectAction.Root:
                case EffectAction.Freeze:
                case EffectAction.Pull:
                    {
                        var target = EffectReceiverRegistry.FindTarget(pos, fwd, card.range, card.coneDegrees, card.SupportedProperty);
                        if (target == null) { res.Message = $"{card.displayName}: nothing to aim at — face it and get closer"; return res; }
                        if (!target.CanReceive(ctx)) { res.Message = $"{card.displayName} doesn't work on that right now"; return res; }
                        target.ApplyEffect(ctx);
                        res.Used = true; res.Target = target;
                        res.Message = card.action switch
                        {
                            EffectAction.Root => "Splat! Stuck fast.",
                            EffectAction.Freeze => "FLASH!",
                            _ => "Reeling in!"
                        };
                        return res;
                    }
                case EffectAction.ChangeBehavior:
                    {
                        var tf = player.GetComponent<PlayerTransformation>(); if (tf == null) tf = player.gameObject.AddComponent<PlayerTransformation>();
                        tf.BecomeGrandpa(card.duration);
                        foreach (var r in EffectReceiverRegistry.All)
                            if (r != null && (r.Properties & DreamProperty.SocialResponsive) != 0 && r.CanReceive(ctx)) r.ApplyEffect(ctx);
                        res.Used = true; res.Message = "Oof, my back... (you feel very grandpa)";
                        return res;
                    }
                case EffectAction.SpawnDecoy:
                    {
                        Vector3 p = pos + fwd * 1.6f;
                        var decoy = SpawnDecoy(p, card.duration);
                        foreach (var r in EffectReceiverRegistry.All)
                            if (r != null && (r.Properties & DreamProperty.Distractible) != 0 && Vector3.Distance(r.Transform.position, p) < 14f && r.CanReceive(ctx))
                                r.ApplyEffect(ctx);
                        res.Used = true; res.Message = "A decoy apple! Who's the real one?";
                        _ = decoy;
                        return res;
                    }
                case EffectAction.CreatePlatform:
                    {
                        Vector3 p = pos + fwd * 1.3f + Vector3.up;
                        if (!Physics.Raycast(p, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)) { res.Message = "No floor for the cushion"; return res; }
                        SpawnCushion(hit.point, card.duration);
                        res.Used = true; res.Message = "Boing! Hop on.";
                        return res;
                    }
            }
            res.Message = "Unknown card";
            return res;
        }

        static GameObject SpawnDecoy(Vector3 at, float life)
        {
            var root = new GameObject("DecoyApple");
            root.transform.position = at;
            var fx = Presentation.FXLibrary.Instance;
            if (fx != null && fx.decoyStandee != null)
            {
                // "cardboard you": flat painted standee on a stick (2D Type B in a 3D space, faces the stage camera)
                var board = GameObject.CreatePrimitive(PrimitiveType.Quad);
                board.name = "Standee";
                Object.DestroyImmediate(board.GetComponent<Collider>());
                board.transform.SetParent(root.transform, false);
                board.transform.localPosition = new Vector3(0, 0.78f, 0);
                board.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                board.GetComponent<Renderer>().sharedMaterial = fx.decoyStandee;
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0, 0.6f, 0);
                body.transform.localScale = new Vector3(0.85f, 0.8f, 0.75f);
                body.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.CreateLit(Color.Lerp(Palette.AppleRed, Palette.WarmCream, 0.45f), 0.1f, "M_Decoy");
            }
            var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(stick.GetComponent<Collider>());
            stick.transform.SetParent(root.transform, false);
            stick.transform.localPosition = new Vector3(0, 0.15f, 0.02f);
            stick.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
            stick.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.CreateLit(new Color(0.75f, 0.58f, 0.4f), 0.1f, "M_DecoyStick");
            root.AddComponent<DecoyApple>().life = life;
            return root;
        }

        static GameObject SpawnCushion(Vector3 at, float life)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere); // puffy pillow, not a box
            c.name = "BouncyCushion";
            Object.DestroyImmediate(c.GetComponent<Collider>()); // bounce is handled by proximity, so it never blocks movement
            c.transform.position = at + Vector3.up * 0.2f;
            c.transform.localScale = new Vector3(1.7f, 0.45f, 1.7f);
            c.GetComponent<Renderer>().sharedMaterial = RuntimeMaterials.CreateLit(Palette.SkyBlue, 0.15f, "M_BouncyCushion");
            c.AddComponent<BouncyCushion>().life = life;
            return c;
        }
    }
}
