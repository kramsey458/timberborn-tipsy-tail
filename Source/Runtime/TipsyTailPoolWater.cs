using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BlueprintSystem;
using Timberborn.EntitySystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kyler.TipsyTail
{
    public record TipsyTailPoolWaterSpec : ComponentSpec { }

    // Override this renderer only; never mutate the shared FountainWater material.
    //
    // FountainWater already uses the lake's own albedo, normal and gloss textures and animates them
    // slowly, so the pool keeps that native motion and texture. Only the tint changes: the fountain's
    // teal becomes the lake's deep blue. The albedo texture is dark, so the tint is deliberately a
    // dark blue; a bright tint blows out (v0.2.3 did exactly that).
    public sealed class TipsyTailPoolWater : BaseComponent, IAwakableComponent,
        IInitializablePreview, IPostInitializableEntity
    {
        // Lake blue, calibrated so the lit pool matches the lake's deep water rather than the fountain's teal.
        internal static readonly Color WaterColor = new Color(0.09f, 0.27f, 0.61f, 1f);
        internal static readonly Color FoamColor = new Color(0.16f, 0.40f, 0.72f, 1f);

        public void Awake() { TipsyTailWaterTuner.Register(this); Apply(); }
        public void InitializePreview() { Apply(); }
        public void PostInitializeEntity() { Apply(); }

        internal bool IsAlive => GameObject != null;

        private readonly Dictionary<MeshFilter, Mesh> _originalMeshes = new Dictionary<MeshFilter, Mesh>();

        // FountainWater reads UV1 for every animated layer (albedo scroll, both normal maps, gloss and the
        // ripple offset), but the exported pool surface only has UV0. With no UV1 every pixel samples one
        // fixed point, so the whole surface pulses in sync instead of showing moving ripples. Give the surface
        // a clean quad whose UV1 is in block units, which is the scale the material's tilings expect.
        private void ApplyMesh(Transform node)
        {
            foreach (var filter in node.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null) continue;
                if (!_originalMeshes.ContainsKey(filter)) _originalMeshes[filter] = filter.sharedMesh;
                var original = _originalMeshes[filter];
                if (original == null) continue;
                filter.sharedMesh = TipsyTailWaterTuner.FixUv1 ? TipsyTailWaterMesh.For(original) : original;
            }
        }

        internal void Apply()
        {
            if (!IsAlive) return;
            foreach (var node in GameObject.GetComponentsInChildren<Transform>(true))
            {
                if (node.name != "#PoolWater") continue;
                ApplyMesh(node);
                foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                {
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
                                // The ripple offset is added in UV1 space, so scale it with the UV to keep
                                // the same drift in blocks per second; bump speeds are the lake's own.
                                block.SetFloat("_WaterRippleSpeed", material.GetFloat("_WaterRippleSpeed") * TipsyTailWaterTuner.Uv1Scale);
                                block.SetVector("_BumpMap1Speed", new Vector4(0.01f, 0.008f, 0f, 0f));
                                block.SetVector("_BumpMap2Speed", new Vector4(-0.008f, -0.01f, 0f, 0f));
                            }
                        }
                        TipsyTailWaterTuner.ApplyOverrides(block, material);
                        renderer.SetPropertyBlock(block, i);
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

        internal void Describe(StringBuilder text)
        {
            if (!IsAlive) return;
            foreach (var node in GameObject.GetComponentsInChildren<Transform>(true))
            {
                if (node.name != "#PoolWater") continue;
                foreach (var filter in node.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null) continue;
                    _originalMeshes.TryGetValue(filter, out var original);
                    text.AppendLine($"  mesh now: {Describe(filter.sharedMesh)}");
                    text.AppendLine($"  mesh originally: {Describe(original)}");
                }
                foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                {
                    text.AppendLine($"  renderer {renderer.GetType().Name} on '{node.name}' enabled={renderer.enabled} " +
                                    $"receiveShadows={renderer.receiveShadows} shadowCasting={renderer.shadowCastingMode} " +
                                    $"materials={renderer.sharedMaterials.Length} layer={node.gameObject.layer}");
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null) { text.AppendLine("    material <null>"); continue; }
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
                }
            }
        }
    }

    // Live tuning for the pool surface. If a `water.cfg` text file sits in the mod folder it is re-read
    // twice a second and applied to every pool without a restart. It only ever adds renderer-local
    // property overrides, so it cannot change gameplay or multiplayer state. Lines (# starts a comment):
    //   native                       use the game's untouched material (no tint)
    //   uv1 on|off                   give the surface a proper UV1 (default on); off reproduces the old behaviour
    //   uv1scale k                   scale the UV1 coordinates (default 0.14 = lake scale; 1 = the fountain's own scale);
    //                                larger = finer ripples
    //   color NAME r g b [a]         set a colour property
    //   float NAME value             set a float property
    //   vector NAME x y z w          set a vector property
    //   mul NAME k                   multiply a native float/vector property by k
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

        internal static bool UseNativeMaterial { get; private set; }
        internal static bool FixUv1 { get; private set; } = true;
        // The lake material's tilings are about 0.14x the fountain's (bump 0.1/0.14 vs 1/2, albedo 0.06 vs 0.5,
        // gloss 0.01 vs 0.0625), so this scale gives the pool the lake's ripple size.
        internal const float DefaultUv1Scale = 0.14f;
        internal static float Uv1Scale { get; private set; } = DefaultUv1Scale;

        private float _nextPoll;
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
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 0.5f;
            try
            {
                var path = ConfigPath();
                var exists = path != null && File.Exists(path);
                if (!exists)
                {
                    if (_hadFile) { _hadFile = false; Ops.Clear(); UseNativeMaterial = false; FixUv1 = true; Uv1Scale = DefaultUv1Scale; ReapplyAll(); }
                    return;
                }
                // Compare contents, not timestamps: files copied from a zip can share one timestamp.
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

        private static void ReapplyAll()
        {
            Pools.RemoveAll(p => p == null || !p.IsAlive);
            foreach (var pool in Pools) pool.Apply();
        }

        private static void LogPools()
        {
            var text = new StringBuilder("[TipsyTail] pool water report (" + Pools.Count + " pool(s))\n");
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
            Ops.Clear(); UseNativeMaterial = false; FixUv1 = true; Uv1Scale = DefaultUv1Scale; _wantLog = false;
            foreach (var raw in lines)
            {
                var line = raw; var hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                var t = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length == 0) continue;
                switch (t[0].ToLowerInvariant())
                {
                    case "native": UseNativeMaterial = true; break;
                    case "uv1": FixUv1 = t.Length < 2 || t[1].ToLowerInvariant() != "off"; break;
                    case "uv1scale": Uv1Scale = Mathf.Max(0.001f, F(t[1])); break;
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

    // A single upward-facing quad covering the pool surface, with UV1 in block units (scaled by the tuner).
    // FountainWater is double sided, so one quad is enough; the exported box only ever showed its top face.
    internal static class TipsyTailWaterMesh
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        internal static Mesh For(Mesh source)
        {
            var b = source.bounds;
            var scale = TipsyTailWaterTuner.Uv1Scale;
            var key = string.Format(CultureInfo.InvariantCulture, "{0:F4}|{1:F4}|{2:F4}|{3:F4}|{4:F4}|{5:F4}|{6:F4}",
                b.min.x, b.max.x, b.min.z, b.max.z, b.max.y, scale, 0f);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

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
            mesh.SetUVs(1, new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(0, depth), new Vector2(width, depth), new Vector2(width, 0)
            });
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.bounds = new Bounds(b.center, b.size);
            Cache[key] = mesh;
            return mesh;
        }
    }
}
