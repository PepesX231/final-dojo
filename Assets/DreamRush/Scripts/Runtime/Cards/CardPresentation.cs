using DreamRush.DreamLogic;
using DreamRush.Player;
using DreamRush.Presentation;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Cards
{
    /// <summary>
    /// Feedback for a successful card use (GDD §17.5 step 5, §20.5): every card gets a readable 2D paper effect,
    /// a sound and, where it fits, a small camera/screen response. Pure presentation — gameplay already happened
    /// in <see cref="CardEffectResolver"/>, so missing FX assets never change outcomes.
    /// </summary>
    public static class CardPresentation
    {
        public static void Play(CardDefinition card, CardUseResult res, PlayerMotor3D player)
        {
            if (card == null || !res.Used || player == null) return;
            var fx = FXLibrary.Instance;
            Vector3 p = player.transform.position;
            Transform target = res.Target != null ? res.Target.Transform : null;
            Audio.AudioManager.PlayCard(card.action, target != null ? target.position : p);

            switch (card.action)
            {
                case EffectAction.Freeze: // CAMERA FLASH
                    if (UIRoot.Exists) UIRoot.Get().ScreenFlash(new Color(1f, 0.98f, 0.9f, 0.85f), 0.35f);
                    if (fx != null)
                    {
                        WorldFX.Spawn(fx.flash, p + Vector3.up * 1.0f + player.transform.forward * 0.5f, 0.9f, 0.3f);
                        VFX.Play(VFXKind.CardBurst, p + Vector3.up * 1.0f + player.transform.forward * 0.5f);
                        if (target != null) WorldFX.Spawn(fx.flash, target.position + Vector3.up * 1.6f, 1.4f, 0.5f, target);
                    }
                    CameraShake.Add(0.15f);
                    break;

                case EffectAction.Root: // SUPER GLUE
                    if (fx != null && target != null)
                    {
                        GroundDecal.Spawn(fx.glueSplat, Ground(target.position), 2.4f, card.duration);
                        WorldFX.Spawn(fx.poof, target.position + Vector3.up * 0.3f, 1.2f, 0.4f);
                    }
                    break;

                case EffectAction.Pull: // FISHING ROD
                    if (target != null) FishingLineFX.Create(player.transform, target, card.duration + 4f);
                    break;

                case EffectAction.ChangeBehavior: // BE A GRANDPA
                    VFX.Play(VFXKind.Poof, p + Vector3.up * 0.7f, 1.4f);
                    if (fx != null) WorldFX.Spawn(fx.poof, p + Vector3.up * 0.7f, 1.8f, 0.5f);
                    CameraShake.Add(0.1f);
                    break;

                case EffectAction.SpawnDecoy:
                    if (fx != null) WorldFX.Spawn(fx.poof, p + player.transform.forward * 1.6f + Vector3.up * 0.6f, 1.6f, 0.5f);
                    break;

                case EffectAction.CreatePlatform:
                    if (fx != null) WorldFX.Spawn(fx.dust, p + player.transform.forward * 1.3f + Vector3.up * 0.3f, 1.4f, 0.4f);
                    break;
            }
        }

        static Vector3 Ground(Vector3 at)
        {
            return Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : at;
        }
    }
}
