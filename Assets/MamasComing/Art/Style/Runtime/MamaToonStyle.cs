using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MamasComing.Style
{
    /// <summary>Per-level "Toybox Comic" knobs. Lives in a level's Global Volume profile; MamaToonStyleFeature pushes it to the shaders.</summary>
    [Serializable, VolumeComponentMenu("MamasComing/Toon Style")]
    public sealed class MamaToonStyle : VolumeComponent
    {
        [Header("Ink lines (screen space)")]
        public ClampedFloatParameter inkStrength = new ClampedFloatParameter(0f, 0f, 1f);
        public ColorParameter inkColor = new ColorParameter(new Color(0.12f, 0.08f, 0.16f), false, false, true);
        [Tooltip("0 = pure ink colour, 1 = darkened surface colour")]
        public ClampedFloatParameter inkKeepsColor = new ClampedFloatParameter(0.25f, 0f, 1f);
        public ClampedFloatParameter inkThickness = new ClampedFloatParameter(1.4f, 0.5f, 4f);
        [Tooltip("Relative depth jump that counts as a silhouette (0.05 = 5%)")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.06f, 0.005f, 0.5f);
        public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.35f, 0.05f, 1f);
        public MinFloatParameter inkFadeStart = new MinFloatParameter(12f, 0f);
        public MinFloatParameter inkFadeEnd = new MinFloatParameter(40f, 0f);
        public ClampedFloatParameter inkFar = new ClampedFloatParameter(0.35f, 0f, 1f);

        [Header("Surfaces (ToyToon)")]
        public ColorParameter shadowTint = new ColorParameter(new Color(0.55f, 0.52f, 0.85f), false, false, true);
        public ClampedFloatParameter shadowTintAmount = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter celSharpness = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter ambientBoost = new ClampedFloatParameter(0f, -0.5f, 1f);
        public ClampedFloatParameter shadowLift = new ClampedFloatParameter(0f, -0.2f, 0.5f);
        public ColorParameter rimColor = new ColorParameter(new Color(1f, 0.92f, 0.82f), false, false, true);
        public ClampedFloatParameter rimStrength = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter halftoneStrength = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter halftoneCell = new ClampedFloatParameter(5f, 2f, 16f);
        public ClampedFloatParameter paintGrain = new ClampedFloatParameter(0f, 0f, 1f);
        [Tooltip("Metres covered by one tile of the paint-grain texture")]
        public MinFloatParameter paintGrainScale = new MinFloatParameter(1.5f, 0.01f);

        public bool IsActive() => inkStrength.value > 0 || shadowTintAmount.value > 0 || halftoneStrength.value > 0 || paintGrain.value > 0 || rimStrength.value > 0;
    }
}
