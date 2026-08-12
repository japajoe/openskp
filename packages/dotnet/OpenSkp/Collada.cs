using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenSkp
{
    public class Collada
    {
        public static void Export(Scene scene, string filePath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<COLLADA xmlns=\"http://www.collada.org/2005/11/COLLADASchema\" version=\"1.4.1\">");
            sb.AppendLine("  <asset>");
            sb.AppendLine("    <contributor>");
            sb.AppendLine("      <authoring_tool>Scene Collada Exporter</authoring_tool>");
            sb.AppendLine("    </contributor>");
            sb.AppendLine("    <created>" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</created>");
            sb.AppendLine("    <modified>" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</modified>");
            sb.AppendLine("    <unit meter=\"1\" name=\"meter\"/>");
            sb.AppendLine("    <up_axis>Y_UP</up_axis>");
            sb.AppendLine("  </asset>");

            sb.AppendLine("  <library_effects>");
            for (int i = 0; i < scene.GltfMaterials.Count; i++)
            {
                sb.AppendLine("    <effect id=\"material_" + i + "-effect\">");
                sb.AppendLine("      <profile_COMMON>");
                sb.AppendLine("        <technique sid=\"common\">");
                sb.AppendLine("          <lambert>");
                sb.AppendLine("            <diffuse>");
                sb.AppendLine("              <color>0.8 0.8 0.8 1</color>");
                sb.AppendLine("            </diffuse>");
                sb.AppendLine("          </lambert>");
                sb.AppendLine("        </technique>");
                sb.AppendLine("      </profile_COMMON>");
                sb.AppendLine("    </effect>");
            }

            if (scene.GltfMaterials.Count == 0)
            {
                sb.AppendLine("    <effect id=\"material_default-effect\">");
                sb.AppendLine("      <profile_COMMON>");
                sb.AppendLine("        <technique sid=\"common\">");
                sb.AppendLine("          <lambert>");
                sb.AppendLine("            <diffuse>");
                sb.AppendLine("              <color>0.8 0.8 0.8 1</color>");
                sb.AppendLine("            </diffuse>");
                sb.AppendLine("          </lambert>");
                sb.AppendLine("        </technique>");
                sb.AppendLine("      </profile_COMMON>");
                sb.AppendLine("    </effect>");
            }
            sb.AppendLine("  </library_effects>");

            sb.AppendLine("  <library_materials>");
            for (int i = 0; i < scene.GltfMaterials.Count; i++)
            {
                sb.AppendLine("    <material id=\"material_" + i + "\" name=\"material_" + i + "\">");
                sb.AppendLine("      <instance_effect url=\"#material_" + i + "-effect\"/>");
                sb.AppendLine("    </material>");
            }

            if (scene.GltfMaterials.Count == 0)
            {
                sb.AppendLine("    <material id=\"material_default\" name=\"material_default\">");
                sb.AppendLine("      <instance_effect url=\"#material_default-effect\"/>");
                sb.AppendLine("    </material>");
            }
            sb.AppendLine("  </library_materials>");

            sb.AppendLine("  <library_geometries>");
            for (int i = 0; i < scene.GlbPrimitives.Count; i++)
            {
                var prim = scene.GlbPrimitives[i];
                string meshId = "geometry_" + i;
                string geomName = string.IsNullOrEmpty(prim.GeomName) ? meshId : prim.GeomName;

                sb.AppendLine("    <geometry id=\"" + meshId + "\" name=\"" + System.Net.WebUtility.HtmlEncode(geomName) + "\">");
                sb.AppendLine("      <mesh>");

                sb.AppendLine("        <source id=\"" + meshId + "-positions\">");
                sb.Append("          <float_array id=\"" + meshId + "-positions-array\" count=\"" + prim.Positions.Length + "\">");
                sb.Append(string.Join(" ", prim.Positions));
                sb.AppendLine("</float_array>");
                sb.AppendLine("          <technique_common>");
                sb.AppendLine("            <accessor source=\"#" + meshId + "-positions-array\" count=\"" + (prim.Positions.Length / 3) + "\" stride=\"3\">");
                sb.AppendLine("              <param name=\"X\" type=\"float\"/>");
                sb.AppendLine("              <param name=\"Y\" type=\"float\"/>");
                sb.AppendLine("              <param name=\"Z\" type=\"float\"/>");
                sb.AppendLine("            </accessor>");
                sb.AppendLine("          </technique_common>");
                sb.AppendLine("        </source>");

                bool hasNormals = prim.Normals != null && prim.Normals.Length > 0;
                if (hasNormals)
                {
                    sb.AppendLine("        <source id=\"" + meshId + "-normals\">");
                    sb.Append("          <float_array id=\"" + meshId + "-normals-array\" count=\"" + prim.Normals.Length + "\">");
                    sb.Append(string.Join(" ", prim.Normals));
                    sb.AppendLine("</float_array>");
                    sb.AppendLine("          <technique_common>");
                    sb.AppendLine("            <accessor source=\"#" + meshId + "-normals-array\" count=\"" + (prim.Normals.Length / 3) + "\" stride=\"3\">");
                    sb.AppendLine("              <param name=\"X\" type=\"float\"/>");
                    sb.AppendLine("              <param name=\"Y\" type=\"float\"/>");
                    sb.AppendLine("              <param name=\"Z\" type=\"float\"/>");
                    sb.AppendLine("            </accessor>");
                    sb.AppendLine("          </technique_common>");
                    sb.AppendLine("        </source>");
                }

                bool hasUvs = prim.Uvs != null && prim.Uvs.Length > 0;
                if (hasUvs)
                {
                    sb.AppendLine("        <source id=\"" + meshId + "-map0\">");
                    sb.Append("          <float_array id=\"" + meshId + "-map0-array\" count=\"" + prim.Uvs.Length + "\">");
                    sb.Append(string.Join(" ", prim.Uvs));
                    sb.AppendLine("</float_array>");
                    sb.AppendLine("          <technique_common>");
                    sb.AppendLine("            <accessor source=\"#" + meshId + "-map0-array\" count=\"" + (prim.Uvs.Length / 2) + "\" stride=\"2\">");
                    sb.AppendLine("              <param name=\"S\" type=\"float\"/>");
                    sb.AppendLine("              <param name=\"T\" type=\"float\"/>");
                    sb.AppendLine("            </accessor>");
                    sb.AppendLine("          </technique_common>");
                    sb.AppendLine("        </source>");
                }

                sb.AppendLine("        <vertices id=\"" + meshId + "-vertices\">");
                sb.AppendLine("          <input semantic=\"POSITION\" source=\"#" + meshId + "-positions\"/>");
                sb.AppendLine("        </vertices>");

                int triangleCount = prim.Indices.Length / 3;
                string matRef = scene.GltfMaterials.Count > 0 ? "material_" + Math.Clamp(prim.MaterialIndex, 0, scene.GltfMaterials.Count - 1) : "material_default";

                int offsetCount = 1;
                if (hasNormals)
                {
                    offsetCount++;
                }
                if (hasUvs)
                {
                    offsetCount++;
                }

                sb.AppendLine("        <triangles material=\"" + matRef + "\" count=\"" + triangleCount + "\">");
                sb.AppendLine("          <input semantic=\"VERTEX\" source=\"#" + meshId + "-vertices\" offset=\"0\"/>");
                int currentOffset = 1;
                if (hasNormals)
                {
                    sb.AppendLine("          <input semantic=\"NORMAL\" source=\"#" + meshId + "-normals\" offset=\"" + currentOffset + "\"/>");
                    currentOffset++;
                }
                if (hasUvs)
                {
                    sb.AppendLine("          <input semantic=\"TEXCOORD\" source=\"#" + meshId + "-map0\" set=\"0\" offset=\"" + currentOffset + "\"/>");
                    currentOffset++;
                }
                sb.Append("          <p>");
                
                if (offsetCount == 1)
                {
                    sb.Append(string.Join(" ", prim.Indices));
                }
                else
                {
                    var interleaved = new List<string>();
                    for (int j = 0; j < prim.Indices.Length; j++)
                    {
                        int indexVal = (int)prim.Indices[j];
                        for (int k = 0; k < offsetCount; k++)
                        {
                            interleaved.Add(indexVal.ToString());
                        }
                    }
                    sb.Append(string.Join(" ", interleaved));
                }

                sb.AppendLine("</p>");
                sb.AppendLine("        </triangles>");

                sb.AppendLine("      </mesh>");
                sb.AppendLine("    </geometry>");
            }
            sb.AppendLine("  </library_geometries>");

            sb.AppendLine("  <library_visual_scenes>");
            sb.AppendLine("    <visual_scene id=\"DefaultScene\" name=\"DefaultScene\">");

            for (int i = 0; i < scene.GlbPrimitives.Count; i++)
            {
                string nodeId = "node_mesh_" + i;
                string geomId = "geometry_" + i;
                string nodeName = string.IsNullOrEmpty(scene.GlbPrimitives[i].GeomName) ? "Mesh_" + i : scene.GlbPrimitives[i].GeomName;

                sb.AppendLine("      <node id=\"" + nodeId + "\" name=\"" + System.Net.WebUtility.HtmlEncode(nodeName) + "\">");
                sb.AppendLine("        <translate sid=\"translate\">0 0 0</translate>");
                sb.AppendLine("        <instance_geometry url=\"#" + geomId + "\">");
                sb.AppendLine("          <bind_material>");
                sb.AppendLine("            <technique_common>");
                string matRef = scene.GltfMaterials.Count > 0 ? "material_" + Math.Clamp(scene.GlbPrimitives[i].MaterialIndex, 0, scene.GltfMaterials.Count - 1) : "material_default";
                sb.AppendLine("              <instance_material symbol=\"" + matRef + "\" target=\"#" + matRef + "\"/>");
                sb.AppendLine("            </technique_common>");
                sb.AppendLine("          </bind_material>");
                sb.AppendLine("        </instance_geometry>");
                sb.AppendLine("      </node>");
            }

            sb.AppendLine("    </visual_scene>");
            sb.AppendLine("  </library_visual_scenes>");

            sb.AppendLine("  <scene>");
            sb.AppendLine("    <instance_visual_scene url=\"#DefaultScene\"/>");
            sb.AppendLine("  </scene>");
            sb.AppendLine("</COLLADA>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
