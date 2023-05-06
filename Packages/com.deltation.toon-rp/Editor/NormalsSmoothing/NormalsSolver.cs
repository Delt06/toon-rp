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
using System.Linq;
using UnityEngine;

namespace DELTation.ToonRP.Editor.NormalsSmoothing
{
    internal static class NormalSolver
    {
        public static void CalculateNormalsAndWriteToChannel(this Mesh mesh, float smoothingAngle, int? uvChannel)
        {
            Vector3[] oldNormals = mesh.normals;
            mesh.RecalculateNormals(smoothingAngle);
            Vector3[] smoothedNormals = mesh.normals;
            mesh.normals = oldNormals;

            if (uvChannel.HasValue)
            {
                mesh.SetUVs(uvChannel.Value, smoothedNormals);
            }
            else
            {
                mesh.SetTangents(smoothedNormals.Select(v => (Vector4) v).ToArray());
            }
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
        private static void RecalculateNormals(this Mesh mesh, float angle)
        {
            float cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            var resultingNormals = new Vector3[vertices.Length];

            var vertexGroups = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);

            for (int index = 0; index < vertices.Length; index++)
            {
                var key = new VertexKey(vertices[index]);
                if (!vertexGroups.TryGetValue(key, out List<VertexEntry> entries))
                {
                    vertexGroups[key] = entries = new List<VertexEntry>();
                }

                entries.Add(new VertexEntry(index, normals[index].normalized));
            }

            foreach (List<VertexEntry> vertexGroup in vertexGroups.Values)
            {
                foreach (VertexEntry entry1 in vertexGroup)
                {
                    Vector3 sum = Vector3.zero;

                    foreach (VertexEntry entry2 in vertexGroup)
                    {
                        float dot = Vector3.Dot(entry1.Normal, entry2.Normal);
                        if (dot >= cosineThreshold)
                        {
                            sum += entry2.Normal;
                        }
                    }

                    resultingNormals[entry1.VertexIndex] = sum.normalized;
                }
            }

            mesh.normals = resultingNormals;
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
            public readonly int VertexIndex;
            public readonly Vector3 Normal;

            public VertexEntry(int vertexIndex, Vector3 normal)
            {
                VertexIndex = vertexIndex;
                Normal = normal;
            }
        }
    }
}