

// Quake 2 IBSP v38 parsing code
// References used:
// https://www.flipcode.com/archives/Quake_2_BSP_File_Format.shtml
// http://jheriko-rtw.blogspot.com/2010/11/dissecting-quake-2-bsp-format.html

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace map2jms
{
    public class BspHeader
    {
        readonly string correctHeader = "IBSP";
        readonly int correctVersion = 38;

        public bool isValid;

        public int version;
        public int[] lumpOffsets;
        public int[] lumpSizes;

        // construct a header from the reader
        public BspHeader(BinaryReader reader)
        {
            // read and convert file header to a string
            string fileHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));

            if (correctHeader != fileHeader)
            {
                throw new Exception("Incorrect BSP header! Template: " + correctHeader + " Response: " + fileHeader);
            }

            int fileVersion = reader.ReadInt32();

            if (correctVersion != fileVersion)
            {
                throw new Exception("Incorrect BSP version! expected " + correctVersion + ", got " + fileVersion);
            }

            lumpOffsets = new int[(int)Q2BspLump.TOTAL_LUMPS];
            lumpSizes = new int[(int)Q2BspLump.TOTAL_LUMPS];

            for (int i = 0; i < (int)Q2BspLump.TOTAL_LUMPS; i++)
            {
                lumpOffsets[i] = reader.ReadInt32();
                lumpSizes[i] = reader.ReadInt32();
            }

            isValid = true;
        }
    }

    public class BspFile
    {
        BspHeader header;
        FileStream filestream;
        BinaryReader reader;

        public Vector3[] vertices;
        public BspEdge[] edges;
        public int[] surfEdges;
        public Plane[] planes;

        public BspModel[] models;

        //public int[] indices;

        public BspTexInfo[] texInfos;
        public List<string> uniqueTextureNames = new List<string>();

        public BspSurface[] surfaces;
        //public BspPrimitive[] primitives;

        public BspFile(string filePath)
        {
            filestream = File.OpenRead(filePath);
            reader = new BinaryReader(filestream);

            header = new BspHeader(reader);

            LoadVertices();
            LoadEdges();
            LoadSurfaceEdges();
            LoadPlanes();
            LoadTextures();
            LoadSurfaces();
            LoadModels();
            Close();
        }

        public int SetBSPReaderPos(Q2BspLump lump)
        {
            filestream.Position = header.lumpOffsets[(int)lump];
            return header.lumpSizes[(int)lump];
        }

        void LoadVertices()
        {
            // vertices
            int lumpSize = SetBSPReaderPos(Q2BspLump.Vertices);
            //byte[] data = bspBReader.ReadBytes(lumpSize);
            int totalVerts = lumpSize / 12; // vertices are 12 bytes in size

            BinaryReader r = reader;

            vertices = new Vector3[totalVerts];
            for (int i = 0; i < totalVerts; i++)
            {
                vertices[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            }

            Console.WriteLine(totalVerts + " vertices");
        }

        void LoadEdges()
        {
            int lumpSize = SetBSPReaderPos(Q2BspLump.Edges);
            int totalEdges = lumpSize / 4;

            BinaryReader r = reader;

            edges = new BspEdge[totalEdges];
            for (int i = 0; i < totalEdges; i++)
            {
                edges[i] = new BspEdge();
                edges[i].v1 = r.ReadUInt16();
                edges[i].v2 = r.ReadUInt16();
            }

            Console.WriteLine(totalEdges + " edges");
        }

        void LoadSurfaceEdges()
        {
            int lumpSize = SetBSPReaderPos(Q2BspLump.SurfaceEdges);
            int totalFaceEdges = lumpSize / 4;

            BinaryReader r = reader;

            surfEdges = new int[totalFaceEdges];
            for (int i = 0; i < totalFaceEdges; i++)
            {
                surfEdges[i] = r.ReadInt32();
            }

            Console.WriteLine(totalFaceEdges + " face edges");
        }

        void LoadPlanes()
        {
            int lumpSize = SetBSPReaderPos(Q2BspLump.Planes);
            int totalPlanes = lumpSize / 20;

            BinaryReader r = reader;

            planes = new Plane[totalPlanes];
            for (int i = 0; i < totalPlanes; i++)
            {
                planes[i].Normal = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                planes[i].D = r.ReadSingle();
                int type = r.ReadInt32();
            }

            Console.WriteLine(totalPlanes + " planes");
        }

        // https://c20.reclaimers.net/general/source-data/materials/h1-materials/
        string GetMaterialSymbols(SurfaceFlags flags)
        {
            string symbols = "";

            //if (flags != BspFlag.None)
            //    Console.WriteLine(flags.ToString());

            if (flags.HasFlag(SurfaceFlags.TwoSided))
                symbols += "%";
            if (flags.HasFlag(SurfaceFlags.Transparent))
                symbols += "#";
            if (flags.HasFlag(SurfaceFlags.RenderOnly))
                symbols += "!";
            if (flags.HasFlag(SurfaceFlags.UnitCollision))
                symbols += "*";
            if (flags.HasFlag(SurfaceFlags.FullCollision))
                symbols += "@";
            if (flags.HasFlag(SurfaceFlags.FogPlane))
                symbols += "$";
            if (flags.HasFlag(SurfaceFlags.Climable))
                symbols += "^";
            if (flags.HasFlag(SurfaceFlags.Breakable))
                symbols += "-";
            if (flags.HasFlag(SurfaceFlags.AIDeafening))
                symbols += "&";
            if (flags.HasFlag(SurfaceFlags.ExactPortal))
                symbols += ".";

            return symbols;
        }

        void LoadTextures()
        {
            int lumpSize = SetBSPReaderPos(Q2BspLump.TextureInfo);
            int entrySize = 76;

            int totalTextures = lumpSize / entrySize;

            texInfos = new BspTexInfo[totalTextures];

            BinaryReader r = reader;

            for (int i = 0; i < totalTextures; i++)
            {
                BspTexInfo info = new BspTexInfo();

                info.uAxis = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                info.uOffset = r.ReadSingle();

                info.vAxis = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                info.vOffset = r.ReadSingle();

                info.flags = (SurfaceFlags)r.ReadInt32();
                info.value = r.ReadInt32();

                string texName = Encoding.ASCII.GetString(reader.ReadBytes(32));
                texName = texName.Substring(0, texName.IndexOf((char)0));
                texName.Trim();

                string texPngPath = "halo/textures/" + texName + ".png";
                if (File.Exists(texPngPath))
                {
                    // this is bad and I should probably allow tif files instead
                    Bitmap bitmap = new Bitmap(texPngPath);
                    info.fileTexWidth = bitmap.Width;
                    info.fileTexHeight = bitmap.Height;
                    bitmap.Dispose();
                }
                else
                {
                    Console.WriteLine("Warning: PNG file for " + texName + " does not exist. Using default texture scale.");
                }

                texName += GetMaterialSymbols((SurfaceFlags)info.flags);

                info.textureNameIndex = uniqueTextureNames.IndexOf(texName);

                // name deduplication for easy material writing
                if (info.textureNameIndex == -1 && texName != "tools/skip")
                {
                    if (Program.materialRemap.ContainsKey(texName))
                        texName = Program.materialRemap[texName];

                    uniqueTextureNames.Add(texName);
                    info.textureNameIndex = uniqueTextureNames.Count - 1;
                }

                info.nextTexInfo = r.ReadInt32();

                texInfos[i] = info;
            }

            Console.WriteLine(totalTextures + " texture infos");
        }

        void LoadSurfaces()
        {
            int lumpSize = SetBSPReaderPos(Q2BspLump.Surfaces);
            int surfaceSize = 20;
            int totalSurfaces = lumpSize / surfaceSize;

            surfaces = new BspSurface[totalSurfaces];
            BspSurface[] s = surfaces;

            BinaryReader r = reader;

            for (int i = 0; i < totalSurfaces; i++)
            {
                s[i] = new BspSurface();

                s[i].plane = r.ReadUInt16();
                s[i].planeSide = r.ReadUInt16();

                s[i].firstEdge = r.ReadInt32();
                s[i].numEdges = r.ReadUInt16();

                s[i].textureInfo = r.ReadUInt16();

                s[i].lightmapStyles = r.ReadInt32();
                s[i].lightmapOffset = r.ReadInt32();
            }

            Console.WriteLine(totalSurfaces + " surfaces");
        }

        void LoadModels()
        {
            int lumpSize = SetBSPReaderPos(Q2BspLump.Models);
            int modelSize = 48;
            int totalModels = lumpSize / modelSize;

            models = new BspModel[totalModels];
            BspModel[] m = models;

            BinaryReader r = reader;

            for (int i = 0; i < totalModels; i++)
            {
                m[i] = new BspModel();

                m[i].bbmin = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                m[i].bbmax = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                m[i].origin = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

                m[i].headNode = r.ReadInt32();
                m[i].firstFace = r.ReadInt32();
                m[i].numFaces = r.ReadInt32();
            }

            Console.WriteLine(totalModels + " models");
        }

        void Close()
        {
            reader.Close();
            filestream.Close();
        }
    }
}
