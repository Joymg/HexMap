using System;
using System.Collections.Generic;
using UnityEngine;

namespace joymg
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        private Mesh hexMesh;
        private MeshCollider meshCollider;

        private static List<Vector3> vertices = new List<Vector3>();
        private static List<int> triangles = new List<int>();
        private static List<Color> colors = new List<Color>();

        void Awake()
        {
            GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
            meshCollider = GetComponent<MeshCollider>();
            hexMesh.name = "Hex Mesh";
        }

        internal void Triangulate(HexCell[] cells)
        {
            hexMesh.Clear();
            vertices.Clear();
            triangles.Clear();
            colors.Clear();

            for (int i = 0; i < cells.Length; i++)
            {
                Triangulate(cells[i]);
            }
            hexMesh.vertices = vertices.ToArray();
            hexMesh.triangles = triangles.ToArray();
            hexMesh.colors = colors.ToArray();
            hexMesh.RecalculateNormals();

            meshCollider.sharedMesh = hexMesh;
        }

        private void Triangulate(HexCell hexCell)
        {
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                Triangulate(d, hexCell);
            }
        }

        private void Triangulate(HexDirection direction, HexCell hexCell)
        {
            Vector3 center = hexCell.Position;
            EdgeVertices edge = new EdgeVertices(
                center + HexMetrics.GetFirstSolidCorner(direction),
                center + HexMetrics.GetSecondSolidCorner(direction)
            );

            if (hexCell.HasRiver)
            {
                if (hexCell.HasRiverThroughEdge(direction))
                {
                    edge.v3.y = hexCell.StreamBedY;
                    if (hexCell.HasRiverStartOrEnd)
                    {
                        TriangulateWithRiverStartOrEnd(direction, hexCell, center, edge);
                    }
                    else
                    {
                        TriangulateWithRiver(direction, hexCell, center, edge);
                    }
                }
            }
            else
            {
                TriangulateEdgeFan(center, edge, hexCell.Color);
            }


            if (direction <= HexDirection.SE)
            {
                TriangulateConnection(direction, hexCell, edge);
            }
        }

        private void TriangulateWithRiver(HexDirection direction, HexCell hexCell, Vector3 center, EdgeVertices edge)
        {
            Vector3 centerLeft, centerRight;
            if (hexCell.HasRiverThroughEdge(direction.Opposite()))
            {
                centerLeft = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
                centerRight = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * .25f;
            }

            else if (hexCell.HasRiverThroughEdge(direction.Next()))
            {
                centerRight = Vector3.Lerp(center, edge.v5, 2f / 3f);
                centerLeft = center;
            }
            else if (hexCell.HasRiverThroughEdge(direction.Previous()))
            {
                centerLeft = Vector3.Lerp(center, edge.v1, 2f / 3f);
                centerRight = center;
            }
            else
            {
                centerLeft = centerRight = center;
            }

            center = Vector3.Lerp(centerLeft, centerRight, 0.5f);

            EdgeVertices middleEdge = new EdgeVertices(Vector3.Lerp(centerLeft, edge.v1, 0.5f), Vector3.Lerp(centerRight, edge.v5, 0.5f), 1f / 6f);
            middleEdge.v3.y = center.y = edge.v3.y;

            TriangulateEdgeStrip(middleEdge, hexCell.Color, edge, hexCell.Color);

            AddTriangle(centerLeft, middleEdge.v1, middleEdge.v2);
            AddTriangleColor(hexCell.Color);
            AddQuad(centerLeft, center, middleEdge.v2, middleEdge.v3);
            AddQuadColor(hexCell.Color);
            AddQuad(center, centerRight, middleEdge.v3, middleEdge.v4);
            AddQuadColor(hexCell.Color);
            AddTriangle(centerRight, middleEdge.v4, middleEdge.v5);
            AddTriangleColor(hexCell.Color);
        }

        private void TriangulateWithRiverStartOrEnd(HexDirection direction, HexCell hexCell, Vector3 center, EdgeVertices edge)
        {
            EdgeVertices middleEdge = new EdgeVertices(Vector3.Lerp(center, edge.v1, 0.5f), Vector3.Lerp(center, edge.v5, 0.5f));
            middleEdge.v3.y = edge.v3.y;

            TriangulateEdgeStrip(middleEdge, hexCell.Color, edge, hexCell.Color);
            TriangulateEdgeFan(center, middleEdge, hexCell.Color);


        }

        private void TriangulateConnection(HexDirection direction, HexCell hexCell, EdgeVertices edge)
        {
            HexCell neighbor = hexCell.GetNeighbor(direction);
            if (neighbor == null)
            {
                return;
            }

            Vector3 bridge = HexMetrics.GetBridge(direction);
            bridge.y = neighbor.Position.y - hexCell.Position.y;
            EdgeVertices edge2 = new EdgeVertices(
                edge.v1 + bridge,
                edge.v5 + bridge
            );

            if (hexCell.HasRiverThroughEdge(direction))
            {
                edge2.v3.y = hexCell.StreamBedY;
            }

            if (hexCell.GetEdgeType(direction) == HexEdgeType.Slope)
            {
                TriangulateEdgeTerraces(edge, hexCell, edge2, neighbor);
            }
            else
            {
                TriangulateEdgeStrip(edge, hexCell.Color, edge2, neighbor.Color);
            }

            HexCell nextNeighbor = hexCell.GetNeighbor(direction.Next());
            //creating connection triangles only in some of the directions, avoiding creating them trice
            if (direction <= HexDirection.E && nextNeighbor != null)
            {
                Vector3 v5 = edge.v5 + HexMetrics.GetBridge(direction.Next());
                v5.y = nextNeighbor.Position.y;


                if (hexCell.Elevation <= neighbor.Elevation)
                {
                    if (hexCell.Elevation <= nextNeighbor.Elevation)
                    {
                        //hexcell has lowest elevation or tied  for lowest
                        TriangulateCorner(edge.v5, hexCell, edge2.v5, neighbor, v5, nextNeighbor);
                    }
                    else
                    {
                        //next neighbor is ranked lowest
                        TriangulateCorner(v5, nextNeighbor, edge.v5, hexCell, edge2.v5, neighbor);
                    }
                }
                else if (neighbor.Elevation <= nextNeighbor.Elevation)
                {
                    //neighbor is lowest
                    TriangulateCorner(edge2.v5, neighbor, v5, nextNeighbor, edge.v5, hexCell);
                }
                else
                {
                    //next neighbor is lowest
                    TriangulateCorner(v5, nextNeighbor, edge.v5, hexCell, edge2.v5, neighbor);
                }
            }
        }

        private void TriangulateEdgeTerraces(EdgeVertices begin,
            HexCell beginCell, EdgeVertices end, HexCell endCell)
        {
            EdgeVertices edge2 = EdgeVertices.TerraceLerp(begin, end, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

            TriangulateEdgeStrip(begin, beginCell.Color, edge2, c2);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                EdgeVertices edge = edge2;
                Color c1 = c2;
                edge2 = EdgeVertices.TerraceLerp(begin, end, i);
                c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
                TriangulateEdgeStrip(edge, c1, edge2, c2);
            }

            TriangulateEdgeStrip(edge2, c2, end, endCell.Color);
        }

        void TriangulateCorner(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
            HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

            if (leftEdgeType == HexEdgeType.Slope)
            {
                if (rightEdgeType == HexEdgeType.Slope)
                {
                    TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (rightEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(
                        left, leftCell, right, rightCell, bottom, bottomCell
                    );
                }
                else
                    TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Slope)
            {
                if (leftEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else
                    TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                if (leftCell.Elevation < rightCell.Elevation)
                {
                    TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else
                {
                    TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
                }
            }
            else
            {
                AddTriangle(bottom, left, right);
                AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
            }

        }

        private void TriangulateCornerTerraces(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
            Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

            AddTriangle(begin, v3, v4);
            AddTriangleColor(beginCell.Color, c3, c4);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v3;
                Vector3 v2 = v4;
                Color c1 = c3;
                Color c2 = c4;
                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
                AddQuad(v1, v2, v3, v4);
                AddQuadColor(c1, c2, c3, c4);
            }

            AddQuad(v3, v4, left, right);
            AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
        }

        void TriangulateCornerTerracesCliff(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            float b = 1f / (rightCell.Elevation - beginCell.Elevation);
            if (b < 0)
            {
                b = -b;
            }
            Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

            TriangulateBoundaryTriangle(
                begin, beginCell, left, leftCell, boundary, boundaryColor
            );

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
                AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }

        void TriangulateCornerCliffTerraces(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            float b = 1f / (leftCell.Elevation - beginCell.Elevation);
            if (b < 0)
            {
                b = -b;
            }
            Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
            Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

            TriangulateBoundaryTriangle(
                right, rightCell, begin, beginCell, boundary, boundaryColor
            );

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
            }
            else
            {
                AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
                AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }

        private void TriangulateBoundaryTriangle(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 boundary, Color boundaryColor
        )
        {
            Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

            AddTriangleUnperturbed(Perturb(begin), v2, boundary);
            AddTriangleColor(beginCell.Color, c2, boundaryColor);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
                c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                AddTriangleUnperturbed(v1, v2, boundary);
                AddTriangleColor(c1, c2, boundaryColor);
            }

            AddTriangleUnperturbed(v2, Perturb(left), boundary);
            AddTriangleColor(c2, leftCell.Color, boundaryColor);
        }

        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
        {
            AddTriangle(center, edge.v1, edge.v2);
            AddTriangleColor(color);
            AddTriangle(center, edge.v2, edge.v3);
            AddTriangleColor(color);
            AddTriangle(center, edge.v3, edge.v4);
            AddTriangleColor(color);
            AddTriangle(center, edge.v4, edge.v5);
            AddTriangleColor(color);
        }

        private void TriangulateEdgeStrip(
            EdgeVertices e1, Color c1,
            EdgeVertices e2, Color c2
        )
        {
            AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            AddQuadColor(c1, c2);
            AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            AddQuadColor(c1, c2);
            AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            AddQuadColor(c1, c2);
            AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            AddQuadColor(c1, c2);
        }


        private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int vertexIndex = vertices.Count;
            vertices.Add(Perturb(v1));
            vertices.Add(Perturb(v2));
            vertices.Add(Perturb(v3));
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        private void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int vertexIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        void AddTriangleColor(Color color)
        {
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }
        /// <summary>
        /// Sets each vertex of a triangle of one color
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="c3"></param>
        void AddTriangleColor(Color c1, Color c2, Color c3)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
        }

        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            int vertexIndex = vertices.Count;
            vertices.Add(Perturb(v1));
            vertices.Add(Perturb(v2));
            vertices.Add(Perturb(v3));
            vertices.Add(Perturb(v4));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
        }

        private void AddQuadColor(Color color)
        {
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }
        private void AddQuadColor(Color c1, Color c2)
        {
            colors.Add(c1);
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c2);
        }
        private void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
            colors.Add(c4);
        }

        private Vector3 Perturb(Vector3 position)
        {
            Vector4 sample = HexMetrics.SampleNoise(position);
            position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbationStrength;
            //position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbationStrength;
            position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbationStrength;
            return position;
        }
    }
}