using System;
using UnityEngine;

namespace joymg
{
    public class HexGridChunk : MonoBehaviour
    {

        private HexCell[] cells;

        public HexMesh terrain, rivers;
        private Canvas gridCanvas;

        private void Awake()
        {
            gridCanvas = GetComponentInChildren<Canvas>();

            cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
            ShowUI(false);
        }

        public void AddCell(int index, HexCell cell)
        {
            cells[index] = cell;
            cell.chunk = this;
            cell.transform.SetParent(transform, false);
            cell.uiRect.SetParent(gridCanvas.transform, false);
        }

        public void Refresh()
        {
            enabled = true;
        }

        public void ShowUI(bool visible)
        {
            gridCanvas.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            Triangulate();
            enabled = false;
        }

        public void Triangulate()
        {
            terrain.Clear();
            rivers.Clear();
            for (int i = 0; i < cells.Length; i++)
            {
                Triangulate(cells[i]);
            }
            terrain.Apply();
            rivers.Apply();
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
                else
                {
                    TriangulateAdjacentToRiver(direction, hexCell, center, edge);
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

        private void TriangulateAdjacentToRiver(HexDirection direction, HexCell hexCell, Vector3 center, EdgeVertices edge)
        {
            if (hexCell.HasRiverThroughEdge(direction.Next()))
            {
                if (hexCell.HasRiverThroughEdge(direction.Previous()))
                {
                    center += HexMetrics.GetSolidEdgeMiddle(direction) *
                        (HexMetrics.innerToOuter * 0.5f);
                }
                else if (hexCell.HasRiverThroughEdge(direction.Previous2()))
                {
                    center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
                }
            }
            else if (hexCell.HasRiverThroughEdge(direction.Previous()) &&
                hexCell.HasRiverThroughEdge(direction.Next2())
            )
            {
                center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
            }

            EdgeVertices middleEdge = new EdgeVertices(Vector3.Lerp(center, edge.v1, 0.5f), Vector3.Lerp(center, edge.v5, 0.5f));

            TriangulateEdgeStrip(middleEdge, hexCell.Color, edge, hexCell.Color);
            TriangulateEdgeFan(center, middleEdge, hexCell.Color);
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
            else if (hexCell.HasRiverThroughEdge(direction.Next2()))
            {
                centerLeft = center;
                centerRight = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
            }
            else
            {
                centerLeft = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
                centerRight = center;
            }

            center = Vector3.Lerp(centerLeft, centerRight, 0.5f);

            EdgeVertices middleEdge = new EdgeVertices(Vector3.Lerp(centerLeft, edge.v1, 0.5f), Vector3.Lerp(centerRight, edge.v5, 0.5f), 1f / 6f);
            middleEdge.v3.y = center.y = edge.v3.y;

            TriangulateEdgeStrip(middleEdge, hexCell.Color, edge, hexCell.Color);

            terrain.AddTriangle(centerLeft, middleEdge.v1, middleEdge.v2);
            terrain.AddTriangleColor(hexCell.Color);
            terrain.AddQuad(centerLeft, center, middleEdge.v2, middleEdge.v3);
            terrain.AddQuadColor(hexCell.Color);
            terrain.AddQuad(center, centerRight, middleEdge.v3, middleEdge.v4);
            terrain.AddQuadColor(hexCell.Color);
            terrain.AddTriangle(centerRight, middleEdge.v4, middleEdge.v5);
            terrain.AddTriangleColor(hexCell.Color);

            bool reversed = hexCell.IncomingRiver == direction;
            TriangulateRiverQuad(centerLeft, centerRight, middleEdge.v2, middleEdge.v4, hexCell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(middleEdge.v2, middleEdge.v4, edge.v2, edge.v4, hexCell.RiverSurfaceY, 0.6f, reversed);
        }

        private void TriangulateWithRiverStartOrEnd(HexDirection direction, HexCell hexCell, Vector3 center, EdgeVertices edge)
        {
            EdgeVertices middleEdge = new EdgeVertices(Vector3.Lerp(center, edge.v1, 0.5f), Vector3.Lerp(center, edge.v5, 0.5f));
            middleEdge.v3.y = edge.v3.y;

            TriangulateEdgeStrip(middleEdge, hexCell.Color, edge, hexCell.Color);
            TriangulateEdgeFan(center, middleEdge, hexCell.Color);

            bool reversed = hexCell.HasIncomingRiver;
            TriangulateRiverQuad(
                middleEdge.v2, middleEdge.v4, edge.v2, edge.v4, hexCell.RiverSurfaceY, 0.6f, reversed
            );

            center.y = middleEdge.v2.y = middleEdge.v4.y = hexCell.RiverSurfaceY;
            rivers.AddTriangle(center, middleEdge.v2, middleEdge.v4);
            if (reversed)
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f)
                );
            }
            else
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f), new Vector2(0f, .6f), new Vector2(1f, 0.6f)
                );
            }
        }

        void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed)
        {
            TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
        }

        private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed)
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            rivers.AddQuad(v1, v2, v3, v4);
            if (reversed)
            {
                rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
            }
            else
            {
                rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
            }
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
                edge2.v3.y = neighbor.StreamBedY;
                TriangulateRiverQuad(
                    edge.v2, edge.v4, edge2.v2, edge2.v4,
                    hexCell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                    hexCell.HasIncomingRiver && hexCell.IncomingRiver == direction
                );
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

        private void TriangulateCorner(
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
                terrain.AddTriangle(bottom, left, right);
                terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
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

            terrain.AddTriangle(begin, v3, v4);
            terrain.AddTriangleColor(beginCell.Color, c3, c4);

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
                terrain.AddQuad(v1, v2, v3, v4);
                terrain.AddQuadColor(c1, c2, c3, c4);
            }

            terrain.AddQuad(v3, v4, left, right);
            terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
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
            Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
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
                terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
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
            Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
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
                terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
            }
        }

        private void TriangulateBoundaryTriangle(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 boundary, Color boundaryColor
        )
        {
            Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
            Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
            terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
                c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
                terrain.AddTriangleUnperturbed(v1, v2, boundary);
                terrain.AddTriangleColor(c1, c2, boundaryColor);
            }

            terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
            terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
        }

        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
        {
            terrain.AddTriangle(center, edge.v1, edge.v2);
            terrain.AddTriangleColor(color);
            terrain.AddTriangle(center, edge.v2, edge.v3);
            terrain.AddTriangleColor(color);
            terrain.AddTriangle(center, edge.v3, edge.v4);
            terrain.AddTriangleColor(color);
            terrain.AddTriangle(center, edge.v4, edge.v5);
            terrain.AddTriangleColor(color);
        }

        private void TriangulateEdgeStrip(
            EdgeVertices e1, Color c1,
            EdgeVertices e2, Color c2
        )
        {
            terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            terrain.AddQuadColor(c1, c2);
        }
    }
}