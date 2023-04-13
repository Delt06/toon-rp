/* 
 * The following code was taken from: http://schemingdeveloper.com
 *
 * Visit our game studio website: http://stopthegnomes.com
 *
 * License: You may use this code however you see fit, as long as you include this notice
 *          without any modifications.
 *
 *          You may not publish a paid asset on Unity store if its main function is based on
 *          the following code, but you may publish a paid asset that uses this code.
 *
 *          If you intend to use this in a Unity store asset or a commercial project, it would
 *          be appreciated, but not required, if you let me know with a link to the asset. If I
 *          don't get back to you just go ahead and use it anyway!
 */

// Further adapted from https://github.com/Delt06/urp-toon-shader/blob/master/Packages/com.deltation.toon-shader/Assets/DELTation/ToonShader/Editor/NormalsSmoothing/NormalsSolver.cs

using System.Collections.Generic;
using UnityEngine;

namespace DELTation.ToonRP.Editor.NormalsSmoothing
{
    internal static class NormalSolver
    {
        public static void CalculateNormalsAndWriteToUv(this Mesh mesh, float smoothingAngle, int uvChannel)
        {
            Vector3[] oldNormals = mesh.normals;
            mesh.RecalculateNormals(smoothingAngle);
            Vector3[] smoothedNormals = mesh.normals;
            mesh.normals = oldNormals;
            mesh.SetUVs(uvChannel, smoothedNormals);
        }

        /// <summary>
        ///     Recalculate the normals of a mesh based on an angle threshold. This takes
        ///     into account distinct vertices that have the same position.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="angle">
        ///     The smoothing angle. Note that triangles that already share
        ///     the same vertex will be smooth regardless of the angle!
        /// </param>
        public static void RecalculateNormals(this Mesh mesh, float angle)
        {
            float cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

            Vector3[] vertices = mesh.vertices;
            var normals = new Vector3[vertices.Length];

            // Holds the normal of each triangle in each sub mesh.
            var triNormals = new Vector3[mesh.subMeshCount][];

            var dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);

            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
            {
                int[] triangles = mesh.GetTriangles(subMeshIndex);

                triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = triangles[i];
                    int i2 = triangles[i + 1];
                    int i3 = triangles[i + 2];

                    // Calculate the normal of the triangle
                    var p1 = Vector3.Normalize(vertices[i2] - vertices[i1]);
                    var p2 = Vector3.Normalize(vertices[i3] - vertices[i1]);
                    Vector3 normal = Vector3.Cross(p1, p2).normalized;
                    int triIndex = i / 3;
                    triNormals[subMeshIndex][triIndex] = normal;

                    VertexKey key;

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out List<VertexEntry> entry))
                    {
                        entry = new List<VertexEntry>(4);
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
                }
            }

            // Each entry in the dictionary represents a unique vertex position.

            foreach (List<VertexEntry> vertList in dictionary.Values)
            {
                foreach (VertexEntry v1 in vertList)
                {
                    var sum = new Vector3();

                    foreach (VertexEntry v2 in vertList)
                    {
                        if (v1.VertexIndex == v2.VertexIndex)
                        {
                            sum += triNormals[v2.MeshIndex][v2.TriangleIndex];
                        }
                        else
                        {
                            // The dot product is the cosine of the angle between the two triangles.
                            // A larger cosine means a smaller angle.
                            float dot = Vector3.Dot(
                                triNormals[v1.MeshIndex][v1.TriangleIndex],
                                triNormals[v2.MeshIndex][v2.TriangleIndex]
                            );
                            if (dot >= cosineThreshold)
                            {
                                sum += triNormals[v2.MeshIndex][v2.TriangleIndex];
                            }
                        }
                    }

                    normals[v1.VertexIndex] = sum.normalized;
                }
            }

            mesh.normals = normals;
        }

        private readonly struct VertexKey
        {
            private readonly long _x;
            private readonly long _y;
            private readonly long _z;

            // Change this if you require a different precision.
            private const int Tolerance = 100000;

            // Magic FNV values. Do not change these.
            private const long Fnv32Init = 0x811c9dc5;
            private const long Fnv32Prime = 0x01000193;

            public VertexKey(Vector3 position)
            {
                _x = (long) Mathf.Round(position.x * Tolerance);
                _y = (long) Mathf.Round(position.y * Tolerance);
                _z = (long) Mathf.Round(position.z * Tolerance);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                var key = (VertexKey) obj;
                return _x == key._x && _y == key._y && _z == key._z;
            }

            public override int GetHashCode()
            {
                long rv = Fnv32Init;
                rv ^= _x;
                rv *= Fnv32Prime;
                rv ^= _y;
                rv *= Fnv32Prime;
                rv ^= _z;
                rv *= Fnv32Prime;

                return rv.GetHashCode();
            }
        }

        private struct VertexEntry
        {
            public readonly int MeshIndex;
            public readonly int TriangleIndex;
            public readonly int VertexIndex;

            public VertexEntry(int meshIndex, int triIndex, int vertIndex)
            {
                MeshIndex = meshIndex;
                TriangleIndex = triIndex;
                VertexIndex = vertIndex;
            }
        }
    }
}