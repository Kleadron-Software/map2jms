using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace map2jms
{
    class Program
    {
        //static string q3map2Path = "q3map2\\q3map2.exe";
        const string FloatFormat = "0.000000";
        //float scaleFactor = 100f / 100f;

        // I'm just gonna have them compile the map first, seperately 
        //string q2toolPath = "q2tool\\q2tool.exe";
        //string halotoolPath = "";

        public static Dictionary<string, string> materialRemap = new Dictionary<string, string>();
        //public static Dictionary<string, string> textureToPng = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            Console.WriteLine("map2jms by Kleadron Software (c) 2023");

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: map2jms.exe (path-to-quake2-bsp) (path-to-export-jms)");
                Console.WriteLine("Example: map2jms bumbus.bsp bumbus.jms");
                //Console.WriteLine("You should probably set up a batch file to do this automatically with full file paths.");

                if (Debugger.IsAttached)
                    Console.ReadLine();

                return;
            }

            if (File.Exists("name_remap.txt"))
            {
                Console.WriteLine("Parsing name_remap.txt");
                string[] remapFile = File.ReadAllLines("name_remap.txt");

                foreach(string line in remapFile)
                {
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] split = line.Split('=');
                    if (split.Length < 2)
                        continue;

                    string nameFrom = split[0].Trim();
                    string nameTo = split[1].Trim();

                    materialRemap[nameFrom] = nameTo;
                }
            }

            //Console.WriteLine("Discovering Textures");

            //string[] pngFiles = Directory.GetFiles(Path.GetFullPath("halo/textures/"), "*.png", SearchOption.AllDirectories);

            //foreach(string pngPath in pngFiles)
            //{
            //    string name = Path.GetFileNameWithoutExtension(pngPath);
            //    textureToPng[name] = pngPath;
            //    //Console.WriteLine(name + " = " + pngPath);
            //}

            string bspPath = Path.GetFullPath(args[0]);
            string jmsPath = Path.GetFullPath(args[1]);
            string jmsTempPath = jmsPath + ".tmp";

            if (!File.Exists(bspPath))
            {
                Console.WriteLine("Quake 2 BSP file at " + bspPath + " does not exist.");
                return;
            }

            string folder = Path.GetDirectoryName(jmsPath);
            
            if (!Directory.Exists(folder))
            {
                Console.WriteLine("The folder to export to at " + folder + " does not exist.");
            }

            Console.WriteLine("Starting conversion process");
            Console.WriteLine("Input File: " + bspPath);
            Console.WriteLine("Output File: " + jmsPath);

            try
            {
                

                Console.WriteLine("Reading Quake 2 BSP File");

                FileStream jmsfs = File.Create(jmsTempPath);
                BspFile bspFile = new BspFile(bspPath);

                Console.WriteLine("Writing JMS File");

                StreamWriter writer = new StreamWriter(jmsfs);

                writer.WriteLine("8200");   // JMS version
                writer.WriteLine("0");      // node checksum

                writer.WriteLine("1"); // node count
                writer.WriteLine("frame_root"); // node name
                writer.WriteLine("-1"); // node first child
                writer.WriteLine("-1"); // node next sibling
                writer.WriteLine("0.000000\t0.000000\t0.000000\t1.000000"); // rotation
                writer.WriteLine("0.000000\t0.000000\t0.000000"); // translation


                // write textures
                writer.WriteLine(bspFile.uniqueTextureNames.Count); // texture name count
                for (int i = 0; i < bspFile.uniqueTextureNames.Count; i++)
                {
                    // remove any path associated with the texture name
                    string[] split = bspFile.uniqueTextureNames[i].Split('/');
                    writer.WriteLine(split[split.Length - 1]); // texture name
                    writer.WriteLine("<none>"); // path or something, just gonna have tool make the materials
                }

                writer.WriteLine("0"); // no markers

                writer.WriteLine("1"); // one region
                writer.WriteLine("unnamed"); // region name

                // create vertex and triangle data! fun!

                List<JmsVertex> vertices = new List<JmsVertex>();
                List<JmsTriangle> triangles = new List<JmsTriangle>();

                CreateVertsAndTris(bspFile, bspFile.models[0], vertices, triangles, null);

                //Console.WriteLine("Generated " + vertices.Count + " vertices and " + triangles.Count + " triangles");

                for (int i = 1; i < bspFile.models.Length; i++)
                {
                    CreateVertsAndTris(bspFile, bspFile.models[i], vertices, triangles, null);
                    //CreateVertsAndTris(bspFile, bspFile.models[i], vertices, triangles, "+portal");
                    //CreateVertsAndTris(bspFile, bspFile.models[i], vertices, triangles, "playerclip*");
                    //CreateVertsAndTris(bspFile, bspFile.models[i], vertices, triangles, "placeholder_digsite");
                }

                Console.WriteLine("Generated " + vertices.Count + " vertices and " + triangles.Count + " triangles");

                // write vertices
                writer.WriteLine(vertices.Count);
                for (int i = 0; i < vertices.Count; i++)
                {
                    JmsVertex vert = vertices[i];
                    //vert.Position.X *= scaleFactor;
                    //vert.Position.Y *= scaleFactor;
                    //vert.Position.Z *= scaleFactor;
                    writer.WriteLine("0"); // parent node
                    writer.WriteLine(
                        vert.Position.X.ToString(FloatFormat) + "\t" +
                        vert.Position.Y.ToString(FloatFormat) + "\t" +
                        vert.Position.Z.ToString(FloatFormat)); // position
                    writer.WriteLine(
                        vert.Normal.X.ToString(FloatFormat) + "\t" +
                        vert.Normal.Y.ToString(FloatFormat) + "\t" +
                        vert.Normal.Z.ToString(FloatFormat)); // normal
                    writer.WriteLine("-1"); // node 1 index for blending
                    writer.WriteLine("0.000000"); // node 1 weight
                    writer.WriteLine(vert.TextureCoordinate.X.ToString(FloatFormat)); // texture u
                    writer.WriteLine(vert.TextureCoordinate.Y.ToString(FloatFormat)); // texture v
                    writer.WriteLine("0"); // some unused flag
                }

                // THIS MIGHT BE VERY WRONG - write triangles
                //int totalTriangles = 0;
                //for (int i = 0; i < bspFile.primitives.Length; i++)
                //{
                //    BspPrimitive prim = bspFile.primitives[i];
                //    totalTriangles += prim.indexCount / 3;
                //}

                //// triangles
                writer.WriteLine(triangles.Count);
                for (int i = 0; i < triangles.Count; i++)
                {
                    // writes out polygonal primitives as triangles
                    JmsTriangle triangle = triangles[i];

                    writer.WriteLine("0"); // region 0
                    writer.WriteLine(triangle.materialIndex); // texture index

                    writer.WriteLine(
                        triangle.v1 + "\t" +
                        triangle.v2 + "\t" +
                        triangle.v3);
                }

                //ProcessStartInfo q3map2StartInfo = new ProcessStartInfo(q3map2Path);

                //Console.WriteLine("STARTING Q3MAP2");
                //Process process = Process.Start(q3map2StartInfo);
                //Console.WriteLine("Q3MAP2 DONE");

                writer.Close();

                if (File.Exists(jmsPath))
                    File.Delete(jmsPath);

                File.Copy(jmsTempPath, jmsPath);

                // temp file gets deleted a few lines lower regardless if this succeeded or not

                Console.WriteLine("Done!");
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                File.WriteAllText("map2jms_exception.txt", e.ToString());

                Console.WriteLine("An exception occurred while converting the BSP file.");
                Console.WriteLine(e.ToString());
                Console.WriteLine("Exception saved to map2jms_exception.txt.");
            }

            if (File.Exists(jmsTempPath))
                File.Delete(jmsTempPath);

            if (Debugger.IsAttached)
                Console.ReadLine();
        }

        private static void CreateVertsAndTris(BspFile bspFile, BspModel bspModel, List<JmsVertex> vertices, List<JmsTriangle> triangles, string filter)
        {
            int firstFace = bspModel.firstFace;
            int numFaces = bspModel.numFaces;

            int totalDegenerates = 0;
            int totalMidpoints = 0;

            for (int faceI = 0; faceI < numFaces; faceI++)
            {
                BspSurface surface = bspFile.surfaces[firstFace + faceI];

                if (bspFile.texInfos[surface.textureInfo].textureNameIndex == -1)
                    continue;

                string texName = bspFile.uniqueTextureNames[bspFile.texInfos[surface.textureInfo].textureNameIndex];
                if (filter != null)
                {
                    if (texName != filter)
                    {
                        continue;
                    }
                }

                List<JmsVertex> newVerts = new List<JmsVertex>();
                List<JmsTriangle> newTris = new List<JmsTriangle>();

                int baseVertex = vertices.Count;
                int firstEdge = surface.firstEdge;

                BspTexInfo texInfo = bspFile.texInfos[surface.textureInfo];
                Vector3 normal = bspFile.planes[surface.plane].Normal;

                Vector3 midpoint = new Vector3(0, 0, 0);

                if (surface.planeSide != 0)
                {
                    normal.X *= -1f;
                    normal.Y *= -1f;
                    normal.Z *= -1f;
                }

                // make verts
                for (int i = 0; i < surface.numEdges; i++)
                {
                    int surfEdge = bspFile.surfEdges[firstEdge + i];
                    BspEdge edge = bspFile.edges[Math.Abs(surfEdge)];

                    int vertIndex = surfEdge >= 0 ? edge.v1 : edge.v2;
                    Vector3 vertex = bspFile.vertices[vertIndex];
                    Vector2 uv = texInfo.GetUVFromVertex(vertex, texInfo.fileTexWidth, texInfo.fileTexHeight);

                    midpoint.X += vertex.X;
                    midpoint.Y += vertex.Y;
                    midpoint.Z += vertex.Z;

                    newVerts.Add(new JmsVertex(vertex, uv, normal));
                }

                midpoint.X /= surface.numEdges;
                midpoint.Y /= surface.numEdges;
                midpoint.Z /= surface.numEdges;

                Vector2 miduv = texInfo.GetUVFromVertex(midpoint, texInfo.fileTexWidth, texInfo.fileTexHeight);

                int retries = 0;
                bool finished = false;
                bool addedMidPoint = false;

                if (texInfo.flags.HasFlag(SurfaceFlags.AddMidpoint))
                {
                    newVerts.Insert(0, new JmsVertex(midpoint, miduv, normal));
                    addedMidPoint = true;
                    totalMidpoints++;
                }

                // make triangles
                while (!finished && retries <= surface.numEdges)
                {
                    finished = true;

                    for (int i = 2; i < newVerts.Count; i++)
                    {
                        JmsTriangle triangle = new JmsTriangle(
                            texInfo.textureNameIndex,
                            baseVertex,
                            baseVertex + (i),
                            baseVertex + (i - 1));

                        List<float> lengths = new List<float>();
                        lengths.Add(Vector3.Distance(newVerts[triangle.v1 - baseVertex].Position, newVerts[triangle.v2 - baseVertex].Position));
                        lengths.Add(Vector3.Distance(newVerts[triangle.v2 - baseVertex].Position, newVerts[triangle.v3 - baseVertex].Position));
                        lengths.Add(Vector3.Distance(newVerts[triangle.v3 - baseVertex].Position, newVerts[triangle.v1 - baseVertex].Position));

                        lengths.Sort();
                        //lengths.Reverse();

                        if (lengths[0] + lengths[1] <= lengths[2] + 0.001f)
                        {
                            newTris.Clear();
                            newVerts.Insert(0, newVerts[newVerts.Count - 1]);
                            newVerts.RemoveAt(newVerts.Count - 1);
                            if (retries == 0)
                                totalDegenerates++;
                            retries++;
                            if (!addedMidPoint)
                            {
                                if (retries == surface.numEdges - 1)
                                {
                                    //Console.WriteLine("Adding midpoint to surface.");
                                    newVerts.Insert(0, new JmsVertex(midpoint, miduv, normal));
                                    addedMidPoint = true;
                                    totalMidpoints++;
                                }
                            }
                            finished = false;
                            break;
                        }
                        else
                        {
                            newTris.Add(triangle);
                        }
                    }
                }

                if (addedMidPoint)
                {
                    JmsTriangle triangle = new JmsTriangle(
                            texInfo.textureNameIndex,
                            baseVertex,
                            baseVertex + 1,
                            baseVertex + newVerts.Count - 1);
                    newTris.Add(triangle);
                }

                if (retries > 0)
                {
                   // Console.WriteLine("Surface had degenerate triangles, rotated vertices " + retries + " times");
                }

                if (!finished)
                {
                    Console.WriteLine("gave up fixing surface :(");
                    throw new Exception("Surface with degenerate triangles could not be fixed.");
                }

                vertices.AddRange(newVerts);
                triangles.AddRange(newTris);
            }

            if (totalDegenerates > 0)
                Console.WriteLine(totalDegenerates + " surfaces with degenerate triangles fixed");
            if (totalMidpoints > 0)
                Console.WriteLine(totalMidpoints + " surfaces with center points added");
        }
    }
}
