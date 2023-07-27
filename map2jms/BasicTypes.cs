using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace map2jms
{
    public struct Vector3
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #region FNA imported code
        // These math functions were imported from the FNA-XNA project.
        // https://github.com/FNA-XNA/FNA

        public static float Distance(Vector3 vector1, Vector3 vector2)
        {
            float result;
            DistanceSquared(ref vector1, ref vector2, out result);
            return (float)Math.Sqrt(result);
        }

        public static void Distance(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            DistanceSquared(ref value1, ref value2, out result);
            result = (float)Math.Sqrt(result);
        }

        public static float DistanceSquared(Vector3 value1, Vector3 value2)
        {
            return (
                (value1.X - value2.X) * (value1.X - value2.X) +
                (value1.Y - value2.Y) * (value1.Y - value2.Y) +
                (value1.Z - value2.Z) * (value1.Z - value2.Z)
            );
        }

        public static void DistanceSquared(
            ref Vector3 value1,
            ref Vector3 value2,
            out float result
        )
        {
            result = (
                (value1.X - value2.X) * (value1.X - value2.X) +
                (value1.Y - value2.Y) * (value1.Y - value2.Y) +
                (value1.Z - value2.Z) * (value1.Z - value2.Z)
            );
        }
        #endregion FNA imported code
    }

    public struct Plane
    {
        public Vector3 Normal;
        public float D;

        public Plane(Vector3 normal, float distance)
        {
            Normal = normal;
            D = distance;
        }
    }

    public struct Vector2
    {
        public float X, Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Color
    {
        public byte R, G, B, A;

        public Color(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    public struct JmsVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector3 Normal;

        public JmsVertex(
            Vector3 position,
            Vector2 textureCoordinate1,
            Vector3 normal)
        {
            Position = position;
            TextureCoordinate = textureCoordinate1;
            Normal = normal;
        }

    }

    public struct JmsTriangle
    {
        public int materialIndex;
        public int v1, v2, v3;

        public JmsTriangle(int material, int v1, int v2, int v3)
        {
            this.materialIndex = material;
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }


    //public enum Q3BspLump
    //{
    //    Entities,
    //    Textures,
    //    Planes,
    //    Nodes,
    //    Leaves,
    //    LeafPrimitiveIndices,
    //    LeafBrushIndices,
    //    Models,
    //    Brushes,
    //    BrushSides,
    //    Vertices,
    //    Indices,
    //    Effects,
    //    Primitives,
    //    Lightmaps,
    //    LightVolumes,
    //    Visibility,
    //    TOTAL_LUMPS
    //}

    public enum Q2BspLump
    {
        Entities,
        Planes,
        Vertices,
        Visibility,
        Nodes,
        TextureInfo,
        Surfaces,
        Lighting,
        Leaves,
        LeafFaceTable,
        LeafBrushTable,
        Edges,
        SurfaceEdges,
        Models,
        Brushes,
        BrushSides,
        Pop,
        Areas,
        AreaPortals,
        TOTAL_LUMPS
    }

    public class BspSurface
    {
        public int plane;
        public int planeSide;

        public int firstEdge;
        public int numEdges;

        public int textureInfo;

        // both unused for our purposes
        public int lightmapStyles;
        public int lightmapOffset;
    }

    public class BspTexInfo
    {
        public Vector3 uAxis;
        public float uOffset;

        public Vector3 vAxis;
        public float vOffset;

        public SurfaceFlags flags;
        public int value; // ??

        public int textureNameIndex;

        public int nextTexInfo;

        public int fileTexWidth = 256;
        public int fileTexHeight = 256;

        public Vector2 GetUVFromVertex(Vector3 vertex, int textureWidth, int textureHeight)
        {
            return new Vector2(
                (vertex.X * uAxis.X + vertex.Y * uAxis.Y + vertex.Z * uAxis.Z + uOffset) / textureWidth,
                -(vertex.X * vAxis.X + vertex.Y * vAxis.Y + vertex.Z * vAxis.Z + vOffset) / textureHeight);
        }
    }

    public class BspEdge
    {
        // 16 bit numbers
        public int v1;
        public int v2;
    }

    public class BspModel
    {
        public Vector3 bbmin, bbmax;
        public Vector3 origin;
        public int headNode;
        public int firstFace;
        public int numFaces;
    }

    [Flags]
    public enum SurfaceFlags
    {
        None = 0,
        // halo flags
        TwoSided = 1 << 10,
        Transparent = 1 << 11,
        RenderOnly = 1 << 12,
        UnitCollision = 1 << 13,
        FullCollision = 1 << 14,
        FogPlane = 1 << 15,
        Climable = 1 << 16,
        Breakable = 1 << 17,
        AIDeafening = 1 << 18,
        ExactPortal = 1 << 19,
        // map2jms flags
        AddMidpoint = 1 << 20,
    }


    //public enum BspPrimitiveType
    //{
    //    NULL,
    //    Polygon,
    //    Patch,
    //    Mesh,
    //    Billboard
    //}

    //public class BspPrimitive
    //{
    //    public int textureID;           // 0-4
    //    public int effectID;            // 4-8
    //    public BspPrimitiveType type;      // 8-12
    //    public int firstVertex;         // 12-16
    //    public int vertexCount;         // 16-20
    //    public int firstIndex;          // 20-24
    //    public int indexCount;          // 24-28

    //    public int lightmapIndex;       // 28-32
    //    public int lightmapX;           // 32-36
    //    public int lightmapY;           // 36-40
    //    public int lightmapW;           // 40-44
    //    public int lightmapH;           // 44-48

    //    public Vector3 lightmapOrigin;  // 48-52-56-60

    //    // idk
    //    public Vector2 lightmapST1;     // 60-64-68
    //    public Vector2 lightmapST2;     // 68-72-76
    //    public Vector2 lightmapST3;     // 76-80-84

    //    public Vector3 normal;          // 84-88-92-96
    //    public int patchW;              // 96-100
    //    public int patchH;              // 100-104
    //}
}
