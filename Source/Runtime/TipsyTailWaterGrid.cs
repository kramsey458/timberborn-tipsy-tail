using System;

namespace Kyler.TipsyTail
{
    // One pool's private copy of everything the game's water shader reads, laid out exactly the way Timberborn's
    // own water renderer lays it out (Timberborn.WaterSystemRendering.WaterMesh and Shaders/CalculateWaterVertex.hlsl).
    //
    // The game draws map water as one 48-vertex block per water cell: a 4x4 grid of top-surface vertices (nine
    // quads) plus a strip of skirt vertices along each side that the shader can drop to the floor. Nothing about the
    // surface is stored in the mesh itself. Each vertex carries its cell coordinates, its index within the block and
    // a flag mask in UV0, and the vertex shader looks up the cell in a set of map-sized texture arrays (depth, floor,
    // ceiling, links to the four neighbours and the four diagonals, skirts, and a 4x4-per-cell height map) to decide
    // where the vertex goes. The fragment shader then reads more of those textures through the same cell coordinates.
    //
    // This grid describes a still, clean, deep body of water covering the pool's surface rectangle and nothing else,
    // on a tiny map of its own with one empty cell of padding, so the pool needs none of the real map's water data.
    // Pure C# so the tests can check the layout without Unity.
    public sealed class TipsyTailWaterGrid
    {
        public const int Layers = 2;             // the shader also reads the column above (columnIndex + 1)
        public const int Padding = 1;
        public const int VerticesPerCell = 48;
        public const int HeightsPerCell = 4;     // the height map has 4x4 texels per cell (GetVertexUv in the HLSL)
        public const float NoLink = -1f;

        // Vertex flag bits, from WaterUtils.cginc (EDGE_VERTEX_BIT 0 ... FLOOR_SKIRT_BIT 7).
        public const int EdgeVertexBit = 1 << 0, CornerVertexBit = 1 << 1, SkirtBit = 1 << 2, LeftSkirtBit = 1 << 3,
            RightSkirtBit = 1 << 4, TopSkirtBit = 1 << 5, BottomSkirtBit = 1 << 6, FloorSkirtBit = 1 << 7;

        // Edge link channels (CalculateWaterVertex.hlsl): x links the +z neighbour, y the -x, z the -z and w the +x.
        public const int LinkPlusZ = 0, LinkMinusX = 1, LinkMinusZ = 2, LinkPlusX = 3;
        // Corner link channels (GetSamplingParameters.hlsl): x is (-x,+z), y is (+x,+z), z is (-x,-z) and w is (+x,-z).
        public const int CornerMinusXPlusZ = 0, CornerPlusXPlusZ = 1, CornerMinusXMinusZ = 2, CornerPlusXMinusZ = 3;

        // The game's index buffer for one cell: nine top quads, then the four skirt strips (WaterMesh.GetWaterMesh).
        public static readonly int[] CellTriangles =
        {
            1, 0, 4, 1, 4, 5, 5, 6, 1, 6, 2, 1, 6, 7, 2, 7, 3, 2, 8, 9, 4, 9, 5, 4, 9, 10, 5, 10, 6, 5,
            10, 11, 6, 11, 7, 6, 12, 13, 8, 13, 9, 8, 13, 14, 9, 14, 10, 9, 11, 10, 14, 11, 14, 15,
            32, 16, 17, 32, 17, 33, 33, 17, 18, 33, 18, 34, 34, 18, 35, 35, 18, 19,
            36, 21, 20, 36, 37, 21, 37, 22, 21, 37, 38, 22, 38, 39, 22, 39, 23, 22,
            40, 24, 25, 40, 25, 41, 41, 25, 42, 42, 25, 26, 42, 26, 43, 43, 26, 27,
            47, 31, 30, 47, 30, 46, 46, 30, 29, 46, 29, 45, 45, 29, 44, 44, 29, 28
        };

        // The top-surface vertex each skirt vertex duplicates: vertices 16..31 are the edge skirts and 32..47 the
        // floor skirts, in the order -z side, -x side, +x side, +z side (WaterMesh.UpdateWaterMesh).
        public static readonly int[] SkirtSource = { 0, 1, 2, 3, 0, 4, 8, 12, 3, 7, 11, 15, 12, 13, 14, 15 };
        private static readonly int[] SkirtSideBits = { BottomSkirtBit, LeftSkirtBit, RightSkirtBit, TopSkirtBit };
        private static readonly int[] CornerVertices =
            { 0, 3, 12, 15, 20, 16, 19, 20, 23, 24, 27, 28, 31, 32, 35, 36, 39, 40, 43, 44, 47 };

        public int Width { get; private set; }       // private map size in cells, padding included
        public int Height { get; private set; }
        public int CellsX { get; private set; }      // cells covered by the pool
        public int CellsZ { get; private set; }
        public int FirstTileX { get; private set; }  // world tile of the first pool cell
        public int FirstTileZ { get; private set; }
        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MinZ { get; private set; }
        public float MaxZ { get; private set; }
        public float SurfaceHeight { get; private set; }
        public int FloorLevel { get; private set; }
        public int CeilingLevel { get; private set; }
        public float Depth => SurfaceHeight - FloorLevel;

        // Mesh, in world space: the shader replaces y, which must be the column index (0), with the surface height.
        public float[] Vertices { get; private set; }   // x, y, z per vertex
        public float[] Uv0 { get; private set; }        // cell x, cell z, vertex index, flag mask per vertex
        public int[] Triangles { get; private set; }

        // Texture arrays, layer-major, texel-major, channel-minor. Layer 1 is empty.
        public float[] WaterData { get; private set; }      // RGBAFloat: depth, floor, ceiling, unused
        public float[] EdgeLinks { get; private set; }      // RGBAFloat: column index of the linked neighbour or -1
        public float[] CornerLinks { get; private set; }    // RGBAFloat: same for the diagonals; also the base corner links
        public byte[] Skirts { get; private set; }          // RGBA32: no skirt flags, the unlinked sides drop by themselves
        public float[] Heights { get; private set; }        // RFloat, 4x4 per cell
        public float[] Outflows { get; private set; }       // RGFloat: still water
        public byte[] Contaminations { get; private set; }  // R8: clean water
        public float[] Waterfalls { get; private set; }     // RFloat: none
        public byte[] SourceMask { get; private set; }      // R8: no water source

        public int Texels => Width * Height;
        public int HeightTexels => Width * HeightsPerCell * Height * HeightsPerCell;

        public static TipsyTailWaterGrid Build(float minX, float maxX, float minZ, float maxZ,
            float surfaceHeight, int floorLevel, int ceilingLevel)
        {
            if (!(maxX > minX) || !(maxZ > minZ)) throw new ArgumentException("The pool surface must have a positive size.");
            if (!(surfaceHeight > floorLevel)) throw new ArgumentException("The water surface must be above the basin floor.");
            if (ceilingLevel <= floorLevel) throw new ArgumentException("The ceiling must be above the floor.");
            var g = new TipsyTailWaterGrid
            {
                MinX = minX, MaxX = maxX, MinZ = minZ, MaxZ = maxZ,
                SurfaceHeight = surfaceHeight, FloorLevel = floorLevel, CeilingLevel = ceilingLevel,
                FirstTileX = (int)Math.Floor(minX), FirstTileZ = (int)Math.Floor(minZ)
            };
            g.CellsX = (int)Math.Ceiling(maxX) - g.FirstTileX;
            g.CellsZ = (int)Math.Ceiling(maxZ) - g.FirstTileZ;
            g.Width = g.CellsX + 2 * Padding;
            g.Height = g.CellsZ + 2 * Padding;
            g.FillTextures();
            g.FillMesh();
            return g;
        }

        private void FillTextures()
        {
            int n = Texels;
            WaterData = new float[Layers * n * 4];
            EdgeLinks = Filled(Layers * n * 4, NoLink);
            CornerLinks = Filled(Layers * n * 4, NoLink);
            Skirts = new byte[Layers * n * 4];
            Heights = new float[Layers * HeightTexels];
            Outflows = new float[Layers * n * 2];
            Contaminations = new byte[Layers * n];
            Waterfalls = new float[Layers * n];
            SourceMask = new byte[Layers * n];
            int heightWidth = Width * HeightsPerCell;
            for (int cz = 0; cz < CellsZ; cz++)
            for (int cx = 0; cx < CellsX; cx++)
            {
                int vx = cx + Padding, vz = cz + Padding, i = vz * Width + vx;
                WaterData[i * 4] = Depth;
                WaterData[i * 4 + 1] = FloorLevel;
                WaterData[i * 4 + 2] = CeilingLevel;
                EdgeLinks[i * 4 + LinkPlusZ] = cz + 1 < CellsZ ? 0f : NoLink;
                EdgeLinks[i * 4 + LinkMinusX] = cx > 0 ? 0f : NoLink;
                EdgeLinks[i * 4 + LinkMinusZ] = cz > 0 ? 0f : NoLink;
                EdgeLinks[i * 4 + LinkPlusX] = cx + 1 < CellsX ? 0f : NoLink;
                CornerLinks[i * 4 + CornerMinusXPlusZ] = cx > 0 && cz + 1 < CellsZ ? 0f : NoLink;
                CornerLinks[i * 4 + CornerPlusXPlusZ] = cx + 1 < CellsX && cz + 1 < CellsZ ? 0f : NoLink;
                CornerLinks[i * 4 + CornerMinusXMinusZ] = cx > 0 && cz > 0 ? 0f : NoLink;
                CornerLinks[i * 4 + CornerPlusXMinusZ] = cx + 1 < CellsX && cz > 0 ? 0f : NoLink;
                for (int oz = 0; oz < HeightsPerCell; oz++)
                for (int ox = 0; ox < HeightsPerCell; ox++)
                    Heights[(vz * HeightsPerCell + oz) * heightWidth + vx * HeightsPerCell + ox] = SurfaceHeight;
            }
        }

        private void FillMesh()
        {
            int cells = CellsX * CellsZ;
            Vertices = new float[cells * VerticesPerCell * 3];
            Uv0 = new float[cells * VerticesPerCell * 4];
            Triangles = new int[cells * CellTriangles.Length];
            int cell = 0;
            for (int cz = 0; cz < CellsZ; cz++)
            for (int cx = 0; cx < CellsX; cx++, cell++)
            {
                // Cells follow the world's tiles, clipped to the surface rectangle, so the water stops at the basin walls.
                float x0 = Math.Max(FirstTileX + cx, MinX), x1 = Math.Min(FirstTileX + cx + 1, MaxX);
                float z0 = Math.Max(FirstTileZ + cz, MinZ), z1 = Math.Min(FirstTileZ + cz + 1, MaxZ);
                int baseVertex = cell * VerticesPerCell;
                for (int i = 0; i < VerticesPerCell; i++)
                {
                    int top = i < 16 ? i : SkirtSource[(i - 16) % 16];
                    int v = baseVertex + i;
                    Vertices[v * 3] = x0 + (top % 4) / 3f * (x1 - x0);
                    Vertices[v * 3 + 1] = 0f;
                    Vertices[v * 3 + 2] = z0 + (top / 4) / 3f * (z1 - z0);
                    Uv0[v * 4] = cx + Padding;
                    Uv0[v * 4 + 1] = cz + Padding;
                    Uv0[v * 4 + 2] = i;
                    Uv0[v * 4 + 3] = Mask(i);
                }
                for (int t = 0; t < CellTriangles.Length; t++)
                    Triangles[cell * CellTriangles.Length + t] = baseVertex + CellTriangles[t];
            }
        }

        // The flag mask the game gives each vertex of a cell (WaterMesh.UpdateWaterMesh).
        public static int Mask(int vertex)
        {
            int mask;
            if (vertex < 16)
                mask = vertex == 5 || vertex == 6 || vertex == 9 || vertex == 10 ? 0 : EdgeVertexBit;
            else
            {
                mask = EdgeVertexBit | SkirtBit | SkirtSideBits[(vertex - 16) % 16 / 4];
                if (vertex >= 32) mask |= FloorSkirtBit;
            }
            if (Array.IndexOf(CornerVertices, vertex) >= 0) mask |= CornerVertexBit;
            return mask;
        }

        private static float[] Filled(int length, float value)
        {
            var array = new float[length];
            for (int i = 0; i < length; i++) array[i] = value;
            return array;
        }
    }
}
