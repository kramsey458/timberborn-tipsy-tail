using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BlueprintSystem;
using Timberborn.EntitySystem;
using UnityEngine;

namespace Kyler.TipsyTail
{
    public record TipsyTailPoolWaterSpec : ComponentSpec { }

    // Override this renderer only; never mutate the shared FountainWater material.
    //
    // FountainWaterURP is built to animate: its albedo blends scrolling texture layers through a
    // noise map, multiplies a detail layer, and mixes in depth-based foam; specular and normals are
    // also texture driven. Freezing the scroll speeds leaves those layers as static blotches, dark
    // patches and sparkle specks. Every texture input is therefore replaced with a constant so the
    // surface resolves to one flat, glossy, still-water colour.
    public sealed class TipsyTailPoolWater : BaseComponent, IAwakableComponent,
        IInitializablePreview, IPostInitializableEntity
    {
        // Timberborn's own water tint (WaterOutputParticleColors), so the pool matches map water.
        private static readonly Color WaterColor = new Color(0.197f, 0.573f, 0.708f, 1f);
        private const float Smoothness = 0.7f;

        private static Texture2D _flatDetail;
        private static Texture2D _flatGloss;

        public void Awake() { Apply(); }
        public void InitializePreview() { Apply(); }
        public void PostInitializeEntity() { Apply(); }

        // The shader multiplies the detail layer by 2, so linear 0.5 is neutral.
        private static Texture2D FlatDetail =>
            _flatDetail != null ? _flatDetail : _flatDetail = ConstantTexture(new Color32(128, 128, 128, 255));

        // Red is metallic, alpha is smoothness; both are scaled by properties below.
        private static Texture2D FlatGloss =>
            _flatGloss != null ? _flatGloss : _flatGloss = ConstantTexture(new Color32(0, 0, 0, 255));

        private static Texture2D ConstantTexture(Color32 value)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
            {
                name = "TipsyTail.FlatWater",
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(new[] { value });
            texture.Apply(false, true);
            return texture;
        }

        private void Apply()
        {
            foreach (var node in GameObject.GetComponentsInChildren<Transform>(true))
            {
                if (node.name != "#PoolWater") continue;
                foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (material == null || !material.HasProperty("_WaterRippleSpeed")) continue;
                        var block = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(block, i);

                        // No motion anywhere.
                        block.SetVector("_Albedo_Speed", Vector4.zero);
                        block.SetVector("_Albedo_Speed2", Vector4.zero);
                        block.SetVector("_BumpMap1Speed", Vector4.zero);
                        block.SetVector("_BumpMap2Speed", Vector4.zero);
                        block.SetFloat("_WaterRippleSpeed", 0f);

                        // Flat colour: both albedo layers become the tint, and the detail layer is neutral.
                        block.SetColor("_Color", WaterColor);
                        block.SetTexture("_MainTex", Texture2D.whiteTexture);
                        block.SetTexture("_DetailAlbedoTex", FlatDetail);

                        // Foam is |sceneDepth - surfaceDepth| against [offset, offset + depth] * intensity.
                        // A negative offset keeps every depth outside that band (no foam, no divide by zero);
                        // the foam colour matches the water in case a driver ever evaluates it anyway.
                        block.SetFloat("_FoamIntensity", 1f);
                        block.SetFloat("_FoamOffset", -1f);
                        block.SetFloat("_FoamDepth", 0.05f);
                        block.SetColor("_FoamColor", WaterColor);
                        block.SetFloat("_Grayscale", 0f);

                        // A perfectly level surface with a soft, uniform gloss and no metal.
                        block.SetFloat("_BumpMap1Strength", 0f);
                        block.SetFloat("_BumpMap2Strength", 0f);
                        block.SetTexture("_MetallicGlossMap", FlatGloss);
                        block.SetFloat("_MetallicMapScale", 0f);
                        block.SetFloat("_GlossMapScale", Smoothness);

                        renderer.SetPropertyBlock(block, i);
                    }
                }
            }
        }
    }
}
