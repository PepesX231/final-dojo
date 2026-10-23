# Mama Recast prop library

Open `MamaRecast_PropLibrary.blend` in Blender 4.5 or later. The teddy is visible initially; each other named `PROP_*` / `PET_*` is hidden in the Outliner because all assets share a clean export origin. Hide the current asset and unhide another to inspect it. Enable Material Preview to see the palette atlas.

Original meshes were authored for this project by `../Scripts/build_mama_recast.py`. Rebuild from repository root:

```powershell
& '.tools/blender/blender-4.5.0-windows-x64/blender.exe' -b --python ArtSource/Blender/Scripts/build_mama_recast.py
```

This rewrites generated runtime FBXs and the palette atlas, so stop Unity Play mode first. Exact dimensions, triangles and material counts are in `manifest.json`. Blender uses metres, Z up and -Y front; exported FBX uses Unity Y up. Bin lid pivot is authored separately for hinging. Scene integration is by `MamasComing.Editor.MamaRecastBuilder.Apply`, not the full level generator.

The family characters in `../ProvidedFamily/` are user-supplied originals and are not generated or overwritten by this script. Their compatibility and animation limitations are documented in `docs/MAMA_RECAST_IMPLEMENTATION.md` at repository root.
