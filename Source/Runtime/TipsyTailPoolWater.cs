using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Timberborn.AssetSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BlueprintSystem;
using Timberborn.EntitySystem;
using Timberborn.MapStateSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kyler.TipsyTail
{
    public record TipsyTailPoolWaterSpec : ComponentSpec { }

    // The pool surface is drawn with the game's own map water material (PhysicalWater_Opaque, the material the
    // lake is drawn with), on a mesh in the exact layout the game's water renderer uses, fed by a tiny private copy
    // of the data textures the water shader reads (see TipsyTailWaterGrid). Depth shading, the see-through
    // transparency, the ripples, glints and reflections are then the game's, not an imitation, and the pool water
    // looks like still lake water of the same depth under the same light. Nothing is shared with the real water
    // simulation: the private textures are bound per renderer, so the map's water is untouched.
    //
    // If that cannot be set up (a missing material in a future game version, for example), or `water.cfg` says
    // `legacy`, the pool falls back to the v0.2.7 look: the fountain material on a quad, tinted and animated here.
    public sealed class TipsyTailPoolWater : BaseComponent, IAwakableComponent,
        IInitializablePreview, IPostInitializableEntity, IDeletableEntity, IPostPlacementChangeListener
    {
        private const string SurfaceNodeName = "#PoolWater";
        // The basin floor is two blocks below ground (UndergroundModelDepth in the blueprint), and the exported
        // surface sits a fraction of a block above ground, so flooring the surface height gives the ground level.
        internal const int BasinDepthBlocks = 2;

        // Legacy tint: a slate teal-blue, a little darker and bluer than the fountain's own teal.
        internal static readonly Color WaterColor = new Color(0.15f, 0.38f, 0.46f, 1f);
        internal static readonly Color FoamColor = new Color(0.30f, 0.62f, 0.72f, 1f);

        private readonly IAssetLoader _assetLoader;
        private readonly MapSize _mapSize;
        private BlockObject _block;
        private bool _isEntity;
        private bool _deleted;
        private static bool _reportedFailure;

        public TipsyTailPoolWater(IAssetLoader assetLoader, MapSize mapSize)
        {
            _assetLoader = assetLoader;
            _mapSize = mapSize;
        }

        public void Awake()
        {
            _block = GetComponent<BlockObject>();
            TipsyTailWaterTuner.Register(this);
            Apply();
        }

        public void InitializePreview() { Apply(); }
        public void PostInitializeEntity() { _isEntity = true; Apply(); }
        public void OnPostPlacementChanged() { Apply(); }

        public void DeleteEntity()
        {
            _deleted = true;
            DestroyPhysical();
        }

        internal bool IsAlive => !_deleted && GameObject != null;
        internal bool UsesGameWater => _physical.Count > 0;

        private readonly Dictionary<MeshFilter, Mesh> _originalMeshes = new Dictionary<MeshFilter, Mesh>();
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<TipsyTailPhysicalWater> _physical = new List<TipsyTailPhysicalWater>();

        internal void Apply()
        {
            if (!IsAlive) return;
            // Previews keep the fountain quad: the game restyles preview renderers itself.
            bool wantGameWater = _isEntity && !TipsyTailWaterTuner.Legacy && (_block == null || !_block.IsPreview);
            if (wantGameWater && ApplyGameWater()) return;
            DestroyPhysical();
            ApplyLegacy();
        }

        private List<Transform> SurfaceNodes()
        {
            var nodes = new List<Transform>();
            foreach (var node in GameObject.GetComponentsInChildren<Transform>(true))
                if (node.name == SurfaceNodeName) nodes.Add(node);
            return nodes;
        }

        private Mesh OriginalMesh(MeshFilter filter)
        {
            if (!_originalMeshes.ContainsKey(filter)) _originalMeshes[filter] = filter.sharedMesh;
            return _originalMeshes[filter];
        }

        private bool IsOwnWater(Renderer renderer)
        {
            foreach (var physical in _physical)
                if (physical.Renderer == renderer) return true;
            return false;
        }

        // Game water: one physical-water block per #PoolWater node, covering the exported surface's rectangle.
        private bool ApplyGameWater()
        {
            try
            {
                var nodes = SurfaceNodes();
                if (nodes.Count == 0) return false;
                int built = 0;
                foreach (var node in nodes)
                {
                    var filter = node.GetComponentInChildren<MeshFilter>(true);
                    if (filter == null) continue;
                    var original = OriginalMesh(filter);
                    if (original == null) continue;
                    filter.sharedMesh = original;
                    var grid = BuildGrid(filter.transform, original.bounds);
                    var physical = _physical.Find(p => p.Parent == node);
                    if (physical == null)
                    {
                        physical = TipsyTailPhysicalWater.Create(_assetLoader, node);
                        _physical.Add(physical);
                    }
                    physical.Rebuild(grid);
                    // The exported surface only served as a placeholder; the game water replaces it.
                    foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                        if (!IsOwnWater(renderer)) renderer.enabled = false;
                    built++;
                }
                return built > 0;
            }
            catch (Exception e)
            {
                if (!_reportedFailure)
                {
                    _reportedFailure = true;
                    Debug.LogWarning("[TipsyTail] The pool cannot use the game's water material and falls back to the fountain water: " + e);
                }
                DestroyPhysical();
                return false;
            }
        }

        // The surface rectangle and height in world space, from the exported placeholder's bounds.
        private TipsyTailWaterGrid BuildGrid(Transform surface, Bounds bounds)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x, bounds.max.y, (i & 2) == 0 ? bounds.min.z : bounds.max.z);
                var world = surface.TransformPoint(corner);
                minX = Mathf.Min(minX, world.x); maxX = Mathf.Max(maxX, world.x);
                minZ = Mathf.Min(minZ, world.z); maxZ = Mathf.Max(maxZ, world.z);
            }
            float surfaceHeight = surface.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)).y;
            int floorLevel = Mathf.FloorToInt(surfaceHeight) - BasinDepthBlocks;
            int ceilingLevel = Mathf.Max(floorLevel + 1, _mapSize != null ? _mapSize.TotalSize.z : floorLevel + 1);
            return TipsyTailWaterGrid.Build(minX, maxX, minZ, maxZ, surfaceHeight, floorLevel, ceilingLevel);
        }

        private void DestroyPhysical()
        {
            foreach (var physical in _physical) physical.Destroy();
            _physical.Clear();
        }

        internal void FrameUpdate()
        {
            foreach (var physical in _physical) physical.FrameUpdate();
        }

        // Legacy look (v0.2.5 to v0.2.7): FountainWater on a quad with a proper UV1, tinted, with the shader's own
        // clocks switched off and the ripples driven by sliding UV1 here (see TipsyTailWaterMesh.Animate).
        private void ApplyLegacyMesh(Transform node)
        {
            foreach (var filter in node.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null) continue;
                var original = OriginalMesh(filter);
                if (original == null) continue;
                filter.sharedMesh = TipsyTailWaterTuner.FixUv1 ? TipsyTailWaterMesh.For(original) : original;
            }
        }

        // True while any of this pool's legacy renderers is on screen. Used to skip the per-frame ripple update.
        internal bool AnyVisible()
        {
            for (int i = 0; i < _renderers.Count; i++)
                if (_renderers[i] != null && _renderers[i].isVisible) return true;
            return false;
        }

        private void ApplyLegacy()
        {
            _renderers.Clear();
            foreach (var node in SurfaceNodes())
            {
                ApplyLegacyMesh(node);
                foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = true;
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (material == null || !material.HasProperty("_WaterRippleSpeed")) continue;
                        var block = new MaterialPropertyBlock();
                        block.Clear();
                        if (!TipsyTailWaterTuner.UseNativeMaterial)
                        {
                            block.SetColor("_Color", WaterColor);
                            block.SetColor("_FoamColor", FoamColor);
                            if (TipsyTailWaterTuner.FixUv1)
                            {
                                // Every shader clock is off: raw Time jitters and _NonlinearTime is a hidden game global.
                                block.SetFloat("_WaterRippleSpeed", 0f);
                                block.SetVector("_Albedo_Speed", Vector4.zero);
                                block.SetVector("_Albedo_Speed2", Vector4.zero);
                                block.SetVector("_BumpMap1Speed", Vector4.zero);
                                block.SetVector("_BumpMap2Speed", Vector4.zero);
                                // Lake tilings; the second layer's is negative so it drifts the opposite way.
                                block.SetFloat("_BumpMap1Tiling", 0.1f / TipsyTailWaterTuner.DefaultUv1Scale);
                                block.SetFloat("_BumpMap2Tiling", -0.14f / TipsyTailWaterTuner.DefaultUv1Scale);
                                block.SetFloat("_BumpMap1Strength", 0.5f);
                                block.SetFloat("_BumpMap2Strength", 0.75f);
                                block.SetFloat("_GlossMapScale", 2.2f);
                            }
                        }
                        TipsyTailWaterTuner.ApplyOverrides(block, material);
                        renderer.SetPropertyBlock(block, i);
                        if (!_renderers.Contains(renderer)) _renderers.Add(renderer);
                    }
                }
            }
        }

        private static string Describe(Mesh mesh)
        {
            if (mesh == null) return "<none>";
            var b = mesh.bounds;
            return $"'{mesh.name}' vertices={mesh.vertexCount} readable={mesh.isReadable} " +
                   $"uv0={mesh.HasVertexAttribute(VertexAttribute.TexCoord0)} uv1={mesh.HasVertexAttribute(VertexAttribute.TexCoord1)} " +
                   $"boundsCenter={b.center.ToString("F3")} boundsSize={b.size.ToString("F3")}";
        }

        internal static void Describe(StringBuilder text, Material material)
        {
            if (material == null) { text.AppendLine("    material <null>"); return; }
            var shader = material.shader;
            text.AppendLine($"    material '{material.name}' shader '{shader.name}' queue={material.renderQueue} keywords=[{string.Join(",", material.shaderKeywords)}]");
            for (int p = 0; p < shader.GetPropertyCount(); p++)
            {
                var name = shader.GetPropertyName(p);
                switch (shader.GetPropertyType(p))
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        text.AppendLine($"      {name} = {material.GetFloat(name).ToString("0.####", CultureInfo.InvariantCulture)}"); break;
                    case ShaderPropertyType.Color:
                    case ShaderPropertyType.Vector:
                        var v = material.GetVector(name);
                        text.AppendLine($"      {name} = ({v.x.ToString("0.####", CultureInfo.InvariantCulture)}, {v.y.ToString("0.####", CultureInfo.InvariantCulture)}, {v.z.ToString("0.####", CultureInfo.InvariantCulture)}, {v.w.ToString("0.####", CultureInfo.InvariantCulture)})"); break;
                    case ShaderPropertyType.Texture:
                        var t = material.GetTexture(name);
                        text.AppendLine($"      {name} = {(t == null ? "<none>" : t.name + " " + t.width + "x" + t.height)}"); break;
                }
            }
        }

        internal void Describe(StringBuilder text)
        {
            if (!IsAlive) return;
            text.AppendLine($"  pool at {GameObject.transform.position.ToString("F2")} gameWater={UsesGameWater} entity={_isEntity} preview={(_block != null && _block.IsPreview)}");
            foreach (var physical in _physical) physical.Describe(text);
            foreach (var node in SurfaceNodes())
            {
                foreach (var filter in node.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null) continue;
                    _originalMeshes.TryGetValue(filter, out var original);
                    text.AppendLine($"  mesh now: {Describe(filter.sharedMesh)}");
                    text.AppendLine($"  mesh originally: {Describe(original)}");
                }
                foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                {
                    text.AppendLine($"  renderer {renderer.GetType().Name} on '{renderer.gameObject.name}' enabled={renderer.enabled} " +
                                    $"receiveShadows={renderer.receiveShadows} shadowCasting={renderer.shadowCastingMode} " +
                                    $"materials={renderer.sharedMaterials.Length} layer={renderer.gameObject.layer}");
                    foreach (var material in renderer.sharedMaterials) Describe(text, material);
                }
            }
        }
    }

    // The game's water on the pool: its material, its mesh layout and its data textures, all private to one pool.
    internal sealed class TipsyTailPhysicalWater
    {
        // The paths the game's own WaterMesh loads (Configurations/WaterMesh.blueprint.json).
        internal const string OpaqueMaterialPath = "Environment/Water/Materials/PhysicalWater_Opaque";
        internal const string TransparentMaterialPath = "Environment/Water/Materials/PhysicalWater_Transparent";
        internal const string ObjectName = "TipsyTail.GameWater";
        // The game's water tiles live on the Water layer; a render pass draws that layer's back faces into depth.
        internal const string LayerName = "Water";

        private static readonly int MapSizeId = Shader.PropertyToID("_MapSize");
        private static readonly int WaterOpacityId = Shader.PropertyToID("_WaterOpacity");

        private readonly GameObject _object;
        private readonly MeshFilter _filter;
        private readonly MeshRenderer _renderer;
        private readonly Material _opaque;
        private readonly Material _transparent;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly List<Texture> _textures = new List<Texture>();
        private Mesh _mesh;
        private bool _isTransparent;

        internal TipsyTailWaterGrid Grid { get; private set; }
        internal Transform Parent => _object.transform.parent;
        internal Renderer Renderer => _renderer;

        private TipsyTailPhysicalWater(GameObject obj, Material opaque, Material transparent)
        {
            _object = obj;
            _opaque = opaque;
            _transparent = transparent;
            _filter = obj.AddComponent<MeshFilter>();
            _renderer = obj.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = true;
            _renderer.sharedMaterial = _opaque;
        }

        internal static TipsyTailPhysicalWater Create(IAssetLoader loader, Transform parent)
        {
            var opaqueAsset = loader.Load<Material>(OpaqueMaterialPath);
            var transparentAsset = loader.Load<Material>(TransparentMaterialPath);
            if (opaqueAsset == null || transparentAsset == null) throw new InvalidOperationException("The game's water materials were not found.");
            var obj = new GameObject(ObjectName);
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer >= 0) obj.layer = layer;
            obj.transform.SetParent(parent, false);
            var opaque = new Material(opaqueAsset) { name = ObjectName + ".Opaque", hideFlags = HideFlags.HideAndDontSave };
            var transparent = new Material(transparentAsset) { name = ObjectName + ".Transparent", hideFlags = HideFlags.HideAndDontSave };
            var water = new TipsyTailPhysicalWater(obj, opaque, transparent);
            water.KeepWorldIdentity();
            return water;
        }

        internal void Rebuild(TipsyTailWaterGrid grid)
        {
            Grid = grid;
            ReleaseGpuData();
            KeepWorldIdentity();
            int w = grid.Width, h = grid.Height, hw = w * TipsyTailWaterGrid.HeightsPerCell, hh = h * TipsyTailWaterGrid.HeightsPerCell;
            var waterData = Array("WaterData", w, h, TextureFormat.RGBAFloat, grid.WaterData);
            var edgeLinks = Array("EdgeLinks", w, h, TextureFormat.RGBAFloat, grid.EdgeLinks);
            var cornerLinks = Array("CornerLinks", w, h, TextureFormat.RGBAFloat, grid.CornerLinks);
            var skirts = Array("Skirts", w, h, TextureFormat.RGBA32, grid.Skirts);
            var heights = Array("Heights", hw, hh, TextureFormat.RFloat, grid.Heights);
            var outflows = Array("Outflows", w, h, TextureFormat.RGFloat, grid.Outflows);
            var contaminations = Array("Contaminations", w, h, TextureFormat.R8, grid.Contaminations);
            var waterfalls = Array("Waterfalls", w, h, TextureFormat.RFloat, grid.Waterfalls);
            var sourceMask = Array("SourceMask", w, h, TextureFormat.R8, grid.SourceMask);
            // The shader interpolates between the previous and the current tick; still water has one state.
            Bind("_OldWaterData", waterData); Bind("_NewWaterData", waterData);
            Bind("_OldEdgeLinks", edgeLinks); Bind("_NewEdgeLinks", edgeLinks);
            Bind("_NewCornerLinks", cornerLinks);
            Bind("_OldBaseCornerLinks", cornerLinks); Bind("_NewBaseCornerLinks", cornerLinks);
            Bind("_OldSkirts", skirts); Bind("_NewSkirts", skirts);
            Bind("_OldWaterHeights", heights); Bind("_NewWaterHeights", heights);
            Bind("_OldOutflows", outflows); Bind("_NewOutflows", outflows);
            Bind("_OldContaminations", contaminations); Bind("_NewContaminations", contaminations);
            Bind("_OldWaterfalls", waterfalls); Bind("_NewWaterfalls", waterfalls);
            Bind("_WaterSourceMask", sourceMask);
            var mapSize = new Vector4(w, h, 0f, 0f);
            _block.SetVector(MapSizeId, mapSize);
            _opaque.SetVector(MapSizeId, mapSize);
            _transparent.SetVector(MapSizeId, mapSize);

            _mesh = new Mesh { name = ObjectName + ".Mesh", hideFlags = HideFlags.HideAndDontSave };
            int vertexCount = grid.Vertices.Length / 3;
            _mesh.indexFormat = vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            var vertices = new Vector3[vertexCount];
            var uv0 = new Vector4[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = new Vector3(grid.Vertices[i * 3], grid.Vertices[i * 3 + 1], grid.Vertices[i * 3 + 2]);
                uv0[i] = new Vector4(grid.Uv0[i * 4], grid.Uv0[i * 4 + 1], grid.Uv0[i * 4 + 2], grid.Uv0[i * 4 + 3]);
            }
            _mesh.SetVertices(vertices);
            _mesh.SetUVs(0, uv0);
            _mesh.SetTriangles(grid.Triangles, 0, false);
            // The vertex shader lifts the surface from y = 0 to the water height, so the bounds must cover both.
            float yMin = Mathf.Min(0f, grid.FloorLevel - 1f), yMax = Mathf.Max(grid.CeilingLevel, grid.SurfaceHeight) + 1f;
            _mesh.bounds = new Bounds(new Vector3((grid.MinX + grid.MaxX) / 2f, (yMin + yMax) / 2f, (grid.MinZ + grid.MaxZ) / 2f),
                new Vector3(grid.MaxX - grid.MinX, yMax - yMin, grid.MaxZ - grid.MinZ));
            _mesh.UploadMeshData(true);
            _filter.sharedMesh = _mesh;
            ApplyOverrides();
        }

        private Texture2DArray Array<T>(string name, int width, int height, TextureFormat format, T[] data) where T : struct
        {
            var texture = new Texture2DArray(width, height, TipsyTailWaterGrid.Layers, format, false, true)
            {
                name = ObjectName + "." + name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            int perLayer = data.Length / TipsyTailWaterGrid.Layers;
            for (int layer = 0; layer < TipsyTailWaterGrid.Layers; layer++)
                texture.SetPixelData(data, 0, layer, layer * perLayer);
            texture.Apply(false, true);
            _textures.Add(texture);
            return texture;
        }

        // Bound on the renderer's property block and on the two private material copies, so the pool's data wins
        // over the game's global water textures whichever way the pipeline resolves the shader's inputs.
        private void Bind(string property, Texture texture)
        {
            int id = Shader.PropertyToID(property);
            _block.SetTexture(id, texture);
            _opaque.SetTexture(id, texture);
            _transparent.SetTexture(id, texture);
        }

        internal void ApplyOverrides()
        {
            TipsyTailWaterTuner.ApplyOverrides(_block, _renderer.sharedMaterial);
            _renderer.SetPropertyBlock(_block);
        }

        internal void FrameUpdate()
        {
            KeepWorldIdentity();
            SyncOpacity();
        }

        // The mesh is in world space (the water shader needs absolute positions), so the object must stay at the
        // world origin however its parent, the building, is placed or rotated.
        private void KeepWorldIdentity()
        {
            var t = _object.transform;
            if (t.position == Vector3.zero && t.rotation == Quaternion.identity && t.lossyScale == Vector3.one) return;
            t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var p = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(Inverse(p.x), Inverse(p.y), Inverse(p.z));
            t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private static float Inverse(float scale) { return Mathf.Abs(scale) < 1e-6f ? 1f : 1f / scale; }

        // The game swaps the map water to its transparent material while water is hidden (a 0.4 opacity); follow it.
        private void SyncOpacity()
        {
            float opacity = Shader.GetGlobalFloat(WaterOpacityId);
            bool transparent = opacity > 0f && opacity < 0.999f;
            if (transparent == _isTransparent) return;
            _isTransparent = transparent;
            _renderer.sharedMaterial = transparent ? _transparent : _opaque;
            _renderer.SetPropertyBlock(_block);
        }

        private void ReleaseGpuData()
        {
            foreach (var texture in _textures) if (texture != null) UnityEngine.Object.Destroy(texture);
            _textures.Clear();
            if (_mesh != null) { UnityEngine.Object.Destroy(_mesh); _mesh = null; }
        }

        internal void Destroy()
        {
            ReleaseGpuData();
            if (_opaque != null) UnityEngine.Object.Destroy(_opaque);
            if (_transparent != null) UnityEngine.Object.Destroy(_transparent);
            if (_object != null) UnityEngine.Object.Destroy(_object);
        }

        internal void Describe(StringBuilder text)
        {
            var g = Grid;
            text.AppendLine($"  game water '{_object.name}' layer={_object.gameObject.layer} enabled={_renderer.enabled} active={_object.activeInHierarchy} " +
                            $"transparent={_isTransparent} worldPos={_object.transform.position.ToString("F3")}");
            if (g != null)
                text.AppendLine($"    surface x=[{g.MinX:F3},{g.MaxX:F3}] z=[{g.MinZ:F3},{g.MaxZ:F3}] height={g.SurfaceHeight:F4} floor={g.FloorLevel} ceiling={g.CeilingLevel} " +
                                $"cells={g.CellsX}x{g.CellsZ} map={g.Width}x{g.Height} vertices={g.Vertices.Length / 3}");
            text.AppendLine($"    game globals: _WaterOpacity={Shader.GetGlobalFloat(WaterOpacityId):0.###} _MapSize={Shader.GetGlobalVector(MapSizeId).ToString("F0")} " +
                            $"_MaxVisibleLevel={Shader.GetGlobalFloat("_MaxVisibleLevel"):0.##} _NonlinearTime={Shader.GetGlobalFloat("_NonlinearTime"):0.###}");
            TipsyTailPoolWater.Describe(text, _renderer.sharedMaterial);
        }
    }

    // Live tuning for the pool surface. If a `water.cfg` text file sits in the mod folder it is re-read every few
    // seconds and applied to every pool without a restart. It only ever adds renderer-local overrides, so it
    // cannot change gameplay or multiplayer state. Lines (# starts a comment):
    //   legacy                       use the v0.2.7 look (tinted, animated fountain water) instead of the game's water
    //   native                       legacy only: the game's untouched fountain material (no tint)
    //   uv1 on|off                   legacy only: give the quad a proper UV1 (default on)
    //   speed k                      legacy only: ripple drift in blocks per second (default 0.02); 0 stops it
    //   uv1scale k                   legacy only: scale the UV1 coordinates (default 0.14 = lake scale)
    //   color NAME r g b [a]         set a colour property on the pool's water material
    //   float NAME value             set a float property
    //   vector NAME x y z w          set a vector property
    //   mul NAME k                   multiply a float/vector property by k
    //   tex NAME white|black|gray|flatnormal   replace a texture with a constant
    //   log                          write the pool's real renderer/material/property values to Player.log
    public sealed class TipsyTailWaterTuner : MonoBehaviour
    {
        private delegate void Op(MaterialPropertyBlock block, Material material);

        private static readonly List<TipsyTailPoolWater> Pools = new List<TipsyTailPoolWater>();
        private static readonly List<Op> Ops = new List<Op>();
        private static GameObject _host;
        private static Texture2D _white, _black, _gray, _flatNormal;
        private static bool _wantLog;
        private static readonly string[] NewLines = { Environment.NewLine, "\n", "\r" };

        internal static bool Legacy { get; private set; }
        internal static bool UseNativeMaterial { get; private set; }
        internal static bool FixUv1 { get; private set; } = true;
        // The lake material's tilings are about 0.14x the fountain's, so this scale gives the legacy quad the lake's ripple size.
        internal const float DefaultUv1Scale = 0.14f;
        internal static float Uv1Scale { get; private set; } = DefaultUv1Scale;

        // Legacy ripple drift in blocks per second, integrated every frame from unscaled time.
        internal const float DefaultMotionSpeed = 0.02f;
        internal static float MotionSpeed { get; private set; } = DefaultMotionSpeed;
        internal static float OffsetX { get; private set; }
        internal static float OffsetZ { get; private set; }
        internal static bool Animates { get { return Legacy && !UseNativeMaterial && FixUv1 && MotionSpeed > 0f; } }
        private static float _wander;

        private float _nextPoll, _nextContentCheck;
        private string _configPath, _lastSignature;
        private string _lastText;
        private bool _hadFile;
        private bool _reportedError;

        internal static void Register(TipsyTailPoolWater pool)
        {
            Pools.RemoveAll(p => p == null || !p.IsAlive);
            Pools.Add(pool);
            if (_host != null) return;
            _host = new GameObject("TipsyTailWaterTuner") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(_host);
            _host.AddComponent<TipsyTailWaterTuner>();
        }

        internal static void ApplyOverrides(MaterialPropertyBlock block, Material material)
        {
            foreach (var op in Ops) op(block, material);
        }

        private void Update()
        {
            for (int i = Pools.Count - 1; i >= 0; i--)
            {
                var pool = Pools[i];
                if (pool == null || !pool.IsAlive) { Pools.RemoveAt(i); continue; }
                pool.FrameUpdate();
            }

            // Legacy ripples: slide the quad's UV1 every frame while a pool is on screen.
            if (Animates && AnyPoolVisible())
            {
                var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                _wander = (_wander + dt) % 90f;
                var heading = 0.6f + 0.6f * Mathf.Sin(_wander * (2f * Mathf.PI / 90f));
                OffsetX = (OffsetX + Mathf.Cos(heading) * MotionSpeed * dt) % 4096f;
                OffsetZ = (OffsetZ + Mathf.Sin(heading) * MotionSpeed * dt) % 4096f;
                TipsyTailWaterMesh.Animate();
            }

            // Poll rarely and cheaply: this runs on the main thread, so file access must not add hitches.
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;
            try
            {
                var path = _configPath ?? (_configPath = ConfigPath());
                var info = path == null ? null : new FileInfo(path);
                if (info == null || !info.Exists)
                {
                    if (_hadFile) { _hadFile = false; _lastSignature = null; Reset(); ReapplyAll(); }
                    return;
                }
                // Size and timestamp are the fast check. Files copied from one zip can share a timestamp, so the
                // contents are also compared every 10 seconds and whenever the fast check changes.
                var signature = info.Length + "|" + info.LastWriteTimeUtc.Ticks;
                if (_hadFile && signature == _lastSignature && Time.unscaledTime < _nextContentCheck) return;
                _nextContentCheck = Time.unscaledTime + 10f;
                _lastSignature = signature;
                var text = File.ReadAllText(path);
                if (_hadFile && text == _lastText) return;
                _hadFile = true; _lastText = text;
                Parse(text.Split(NewLines, StringSplitOptions.None));
                ReapplyAll();
                if (_wantLog) { _wantLog = false; LogPools(); }
                _reportedError = false;
            }
            catch (Exception e)
            {
                if (_reportedError) return;
                _reportedError = true;
                Debug.LogWarning("[TipsyTail] water.cfg could not be applied: " + e.Message);
            }
        }

        private static void Reset()
        {
            Ops.Clear(); Legacy = false; UseNativeMaterial = false; FixUv1 = true; Uv1Scale = DefaultUv1Scale; MotionSpeed = DefaultMotionSpeed; _wantLog = false;
        }

        private static bool AnyPoolVisible()
        {
            for (int i = 0; i < Pools.Count; i++)
                if (Pools[i] != null && Pools[i].IsAlive && Pools[i].AnyVisible()) return true;
            return false;
        }

        private static void ReapplyAll()
        {
            Pools.RemoveAll(p => p == null || !p.IsAlive);
            foreach (var pool in Pools) pool.Apply();
        }

        private static void LogPools()
        {
            var text = new StringBuilder("[TipsyTail] pool water report (" + Pools.Count + " pool(s))\n");
            text.AppendLine("  legacy=" + Legacy + " | legacy drift offset = (" + OffsetX.ToString("0.###", CultureInfo.InvariantCulture) + ", " + OffsetZ.ToString("0.###", CultureInfo.InvariantCulture) + ") blocks" +
                            " animates=" + Animates + " speed=" + MotionSpeed.ToString("0.###", CultureInfo.InvariantCulture) + " blocks/s" +
                            " | unscaledDeltaTime=" + Time.unscaledDeltaTime.ToString("0.#####", CultureInfo.InvariantCulture) +
                            " timeScale=" + Time.timeScale.ToString("0.##", CultureInfo.InvariantCulture));
            foreach (var pool in Pools) pool.Describe(text);
            Debug.Log(text.ToString());
        }

        private static string ConfigPath()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(location))
            {
                var scripts = Path.GetDirectoryName(location);
                var root = scripts == null ? null : Path.GetDirectoryName(scripts);
                if (root != null) return Path.Combine(root, "water.cfg");
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Timberborn", "Mods", "TipsyTail", "water.cfg");
        }

        private static float F(string s) { return float.Parse(s, CultureInfo.InvariantCulture); }

        private static void Parse(string[] lines)
        {
            Reset();
            foreach (var raw in lines)
            {
                var line = raw; var hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                var t = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length == 0) continue;
                switch (t[0].ToLowerInvariant())
                {
                    case "legacy": Legacy = true; break;
                    case "native": UseNativeMaterial = true; break;
                    case "uv1": FixUv1 = t.Length < 2 || t[1].ToLowerInvariant() != "off"; break;
                    case "uv1scale": Uv1Scale = Mathf.Max(0.001f, F(t[1])); break;
                    case "speed": MotionSpeed = Mathf.Max(0f, F(t[1])); break;
                    case "log": _wantLog = true; break;
                    case "color":
                        { var n = t[1]; var c = new Color(F(t[2]), F(t[3]), F(t[4]), t.Length > 5 ? F(t[5]) : 1f); Ops.Add((b, m) => b.SetColor(n, c)); break; }
                    case "float":
                        { var n = t[1]; var v = F(t[2]); Ops.Add((b, m) => b.SetFloat(n, v)); break; }
                    case "vector":
                        { var n = t[1]; var v = new Vector4(F(t[2]), F(t[3]), F(t[4]), F(t[5])); Ops.Add((b, m) => b.SetVector(n, v)); break; }
                    case "mul":
                        {
                            var n = t[1]; var k = F(t[2]);
                            Ops.Add((b, m) =>
                            {
                                if (m == null) return;
                                var index = m.shader.FindPropertyIndex(n);
                                if (index < 0) return;
                                var type = m.shader.GetPropertyType(index);
                                if (type == ShaderPropertyType.Float || type == ShaderPropertyType.Range) b.SetFloat(n, m.GetFloat(n) * k);
                                else if (type == ShaderPropertyType.Vector) b.SetVector(n, m.GetVector(n) * k);
                            });
                            break;
                        }
                    case "tex":
                        { var n = t[1]; var kind = t[2].ToLowerInvariant(); Ops.Add((b, m) => b.SetTexture(n, Constant(kind))); break; }
                }
            }
        }

        private static Texture2D Constant(string kind)
        {
            switch (kind)
            {
                case "black": return _black != null ? _black : _black = Make(new Color32(0, 0, 0, 255));
                case "gray": return _gray != null ? _gray : _gray = Make(new Color32(128, 128, 128, 255));
                case "flatnormal": return _flatNormal != null ? _flatNormal : _flatNormal = Make(new Color32(128, 128, 255, 255));
                default: return _white != null ? _white : _white = Make(new Color32(255, 255, 255, 255));
            }
        }

        private static Texture2D Make(Color32 value)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
            {
                name = "TipsyTail.Constant",
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(new[] { value });
            texture.Apply(false, true);
            return texture;
        }
    }

    // Legacy surface: a single upward-facing quad covering the pool, with UV1 in block units (scaled by the tuner).
    // FountainWater reads UV1 for every animated layer, and the exported surface only has UV0.
    internal static class TipsyTailWaterMesh
    {
        private sealed class Quad
        {
            public Mesh Mesh;
            public float Width, Depth, Scale;
            public readonly List<Vector2> Uv = new List<Vector2> { default(Vector2), default(Vector2), default(Vector2), default(Vector2) };
        }

        private static readonly Dictionary<string, Quad> Cache = new Dictionary<string, Quad>();

        // UV1 is the surface position in blocks plus the current drift, times the UV scale.
        private static void Fill(Quad q)
        {
            float ox = TipsyTailWaterTuner.OffsetX * q.Scale, oz = TipsyTailWaterTuner.OffsetZ * q.Scale;
            q.Uv[0] = new Vector2(ox, oz);
            q.Uv[1] = new Vector2(ox, oz + q.Depth);
            q.Uv[2] = new Vector2(ox + q.Width, oz + q.Depth);
            q.Uv[3] = new Vector2(ox + q.Width, oz);
        }

        // Called every frame while a legacy pool is on screen. One shared quad serves every pool.
        internal static void Animate()
        {
            foreach (var q in Cache.Values)
            {
                if (q.Mesh == null) continue;
                Fill(q);
                q.Mesh.SetUVs(1, q.Uv);
            }
        }

        internal static Mesh For(Mesh source)
        {
            var b = source.bounds;
            var scale = TipsyTailWaterTuner.Uv1Scale;
            var key = string.Format(CultureInfo.InvariantCulture, "{0:F4}|{1:F4}|{2:F4}|{3:F4}|{4:F4}|{5:F4}|{6:F4}",
                b.min.x, b.max.x, b.min.z, b.max.z, b.max.y, scale, 0f);
            if (Cache.TryGetValue(key, out var cached) && cached.Mesh != null) return cached.Mesh;

            float width = b.size.x * scale, depth = b.size.z * scale, y = b.max.y;
            var mesh = new Mesh { name = "TipsyTail.PoolSurface", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[]
            {
                new Vector3(b.min.x, y, b.min.z), new Vector3(b.min.x, y, b.max.z),
                new Vector3(b.max.x, y, b.max.z), new Vector3(b.max.x, y, b.min.z)
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.tangents = new[]
            {
                new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1)
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            mesh.MarkDynamic();
            var quad = new Quad { Mesh = mesh, Width = width, Depth = depth, Scale = scale };
            Fill(quad);
            mesh.SetUVs(1, quad.Uv);
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.bounds = new Bounds(b.center, b.size);
            Cache[key] = quad;
            return mesh;
        }
    }
}
