using System.Collections.Generic;
using UnityEngine;

namespace ColliderVisualizer
{
    /// <summary>
    /// メッシュ生成クラス
    /// </summary>
    public static class MeshCreator
    {
        /// <summary>
        /// 球状メッシュを生成
        /// </summary>
        public static Mesh CreateSphere(int quality)
        {
            if (quality <= 0)
            {
                Debug.LogWarning("Qualityは正の値を指定してください。");
                quality = 1;
            }

            int latitudeSegments = quality * 2 + 2;
            int longitudeSegments = quality * 2 + 2;

            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            var thetaCoef = Mathf.PI / latitudeSegments;
            var phiCoef = 2 * Mathf.PI / longitudeSegments;

            // 緯度ループ
            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                float theta = lat * thetaCoef;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                // 経度ループ
                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    float phi = lon * phiCoef;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    vertices.Add(new Vector3(
                        cosPhi * sinTheta,
                        cosTheta,
                        sinPhi * sinTheta
                    ));

                    if (lat < latitudeSegments && lon < longitudeSegments)
                    {
                        int current = lat * (longitudeSegments + 1) + lon;
                        int next = current + longitudeSegments + 1;

                        triangles.Add(current + 1);
                        triangles.Add(next);
                        triangles.Add(current);

                        triangles.Add(next + 1);
                        triangles.Add(next);
                        triangles.Add(current + 1);
                    }
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// 半球メッシュを生成
        /// </summary>
        public static Mesh CreateHemisphere(int quality)
        {
            if (quality <= 0)
            {
                Debug.LogWarning("Qualityは正の値を指定してください。");
                quality = 1;
            }

            int latitudeSegments = quality * 10 + 1;
            int longitudeSegments = quality * 20 + 2;

            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            var thetaCoef = Mathf.PI / 2 / latitudeSegments;
            var phiCoef = 2 * Mathf.PI / longitudeSegments;

            // 緯度ループ
            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                float theta = lat * thetaCoef;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                // 経度ループ
                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    float phi = lon * phiCoef;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    vertices.Add(new Vector3(
                        cosPhi * sinTheta,
                        cosTheta,
                        sinPhi * sinTheta
                    ));

                    if (lat < latitudeSegments && lon < longitudeSegments)
                    {
                        int current = lat * (longitudeSegments + 1) + lon;
                        int next = current + longitudeSegments + 1;

                        triangles.Add(current);
                        triangles.Add(next);
                        triangles.Add(current + 1);

                        triangles.Add(current + 1);
                        triangles.Add(next);
                        triangles.Add(next + 1);
                    }
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// 筒状メッシュを生成
        /// </summary>
        public static Mesh CreateTube(int quality)
        {
            if (quality <= 0)
            {
                Debug.LogWarning("Qualityは正の値を指定してください。");
                quality = 1;
            }

            int longitudeSegments = quality * 20 + 2;

            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            float radius = 0.5f;
            var phiCoef = 2 * Mathf.PI / longitudeSegments;

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = lon * phiCoef;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                vertices.Add(new Vector3(cosPhi * radius, 0.5f, sinPhi * radius));
                vertices.Add(new Vector3(cosPhi * radius, -0.5f, sinPhi * radius));

                if (lon < longitudeSegments)
                {
                    int currentTop = lon * 2;
                    int nextTop = currentTop + 2;
                    int currentBottom = currentTop + 1;
                    int nextBottom = nextTop + 1;

                    triangles.Add(currentTop);
                    triangles.Add(nextTop);
                    triangles.Add(currentBottom);

                    triangles.Add(currentBottom);
                    triangles.Add(nextTop);
                    triangles.Add(nextBottom);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}