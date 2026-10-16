using System;
using System.Collections.Generic;
using OpenSkp;
using Xunit;

namespace OpenSkp.Tests
{
    public class ObjMtlColorTests
    {
        private static string ExportWithColorFactor(object factor)
        {
            var scene = new Scene
            {
                GltfMaterials = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "ColorTest",
                        ["pbrMetallicRoughness"] = new Dictionary<string, object>
                        {
                            ["baseColorFactor"] = factor
                        }
                    }
                }
            };

            return ObjExport.ToMtl(scene);
        }

        private static void AssertColors(object factor, string diffuse, string alpha)
        {
            string mtl = ExportWithColorFactor(factor);
            string[] lines = mtl.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            Assert.Contains("newmtl ColorTest", lines);
            Assert.Contains($"Kd {diffuse}", lines);
            Assert.Contains($"d {alpha}", lines);
        }

        [Fact]
        public void ToMtl_UsesDoubleArrayFromSceneBuilder()
        {
            AssertColors(new double[] { 1.0, 0.0, 0.25, 0.5 },
                "1.000000 0.000000 0.250000", "0.500000");
        }

        [Fact]
        public void ToMtl_StillAcceptsFloatLists()
        {
            AssertColors(new List<float> { 0.0f, 1.0f, 0.0f, 1.0f },
                "0.000000 1.000000 0.000000", "1.000000");
        }

        [Fact]
        public void ToMtl_AcceptsFloatArrays()
        {
            AssertColors(new float[] { 0.25f, 0.5f, 0.75f, 0.125f },
                "0.250000 0.500000 0.750000", "0.125000");
        }

        [Fact]
        public void ToMtl_StillAcceptsObjectListsFromDeserializedData()
        {
            AssertColors(new List<object> { 0.25d, 0.5d, 0.75d, 0.5d },
                "0.250000 0.500000 0.750000", "0.500000");
        }

        [Fact]
        public void ToMtl_ThreeChannelsLeaveAlphaOpaque()
        {
            AssertColors(new List<object> { 0.25d, 0.5d, 0.75d },
                "0.250000 0.500000 0.750000", "1.000000");
        }

        [Fact]
        public void ToMtl_StringFactorKeepsDefaultColors()
        {
            // A string is IEnumerable, but was not a supported color factor before.
            AssertColors("not a color factor",
                "0.800000 0.800000 0.800000", "1.000000");
        }

        [Fact]
        public void ToMtl_TooFewChannelsKeepDefaultColors()
        {
            AssertColors(new double[] { 0.25, 0.5 },
                "0.800000 0.800000 0.800000", "1.000000");
        }

        [Fact]
        public void ToMtl_InvalidNestedObjectListStillThrows()
        {
            // Retain Convert.ToSingle's prior behavior for unsupported objects.
            Assert.Throws<InvalidCastException>(() =>
                ExportWithColorFactor(new List<object> { 1.0d, new List<object>(), 0.25d }));
        }

        [Fact]
        public void ToMtl_NullObjectChannelRetainsConvertToSingleZeroBehavior()
        {
            // Convert.ToSingle(null, provider) yielded 0 in the old List<object> path.
            AssertColors(new List<object> { null!, 0.5d, 0.75d },
                "0.000000 0.500000 0.750000", "1.000000");
        }
    }
}
