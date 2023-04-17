using System;
using System.Collections;
using UnityEngine;

namespace joymg
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BaseGrid : MonoBehaviour
    {
        [SerializeField]
        private int xSize, ySize;

        private Vector3[] vertices;

        private Mesh gridMesh;

		private void Awake()
		{
			Generate();
		}

		private void Generate()
		{
			GetComponent<MeshFilter>().mesh = gridMesh = new Mesh();
			gridMesh.name = "Procedural Grid";

			vertices = new Vector3[(xSize + 1) * (ySize + 1)];

			Vector2[] uvs = new Vector2[vertices.Length];

			Vector4[] tangents = new Vector4[vertices.Length];
			Vector4 tangent = new Vector4(1f, 0f, 0f, 0f);

			for (int i = 0, y = 0; y <= ySize; y++)
			{
				for (int x = 0; x <= xSize; x++, i++)
				{
					vertices[i] = new Vector3(x, y);
					uvs[i] = new Vector2((float)x / xSize, (float)y / ySize);
					tangents[i] = tangent;
				}
			}
			gridMesh.vertices = vertices;
			gridMesh.uv = uvs;
			gridMesh.tangents = tangents;

			int[] triangles = new int[xSize * ySize * 6];
			for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
			{
				for (int x = 0; x < xSize; x++, ti += 6, vi++)
				{
					triangles[ti] = vi;
					triangles[ti + 3] = triangles[ti + 2] = vi + 1;
					triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
					triangles[ti + 5] = vi + xSize + 2;
				}
			}
			gridMesh.triangles = triangles;
			gridMesh.RecalculateNormals();
		}



		private void OnDrawGizmos()
        {
            //on editor mode array is empty
            if (vertices == null)
                return;


            Gizmos.color = Color.blue;
            for (int i = 0; i < vertices.Length; i++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.1f);
            }
        }
    }
}