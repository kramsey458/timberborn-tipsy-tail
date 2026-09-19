using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BlueprintSystem;
using Timberborn.EntitySystem;
using UnityEngine;

namespace Kyler.TipsyTail
{
    public record TipsyTailPoolWaterSpec : ComponentSpec { }

    // Override this renderer only; never mutate the shared FountainWater material.
    public sealed class TipsyTailPoolWater : BaseComponent, IAwakableComponent,
        IInitializablePreview, IPostInitializableEntity
    {
        public void Awake() { Apply(); }
        public void InitializePreview() { Apply(); }
        public void PostInitializeEntity() { Apply(); }

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
                        block.SetVector("_Albedo_Speed", Vector4.zero);
                        block.SetVector("_Albedo_Speed2", Vector4.zero);
                        block.SetVector("_BumpMap1Speed", Vector4.zero);
                        block.SetVector("_BumpMap2Speed", Vector4.zero);
                        block.SetFloat("_WaterRippleSpeed", 0f);
                        block.SetFloat("_FoamIntensity", 0f);
                        block.SetFloat("_BumpMap1Strength", 0.08f);
                        block.SetFloat("_BumpMap2Strength", 0.08f);
                        renderer.SetPropertyBlock(block, i);
                    }
                }
            }
        }
    }
}
