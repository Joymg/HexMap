using System;
using UnityEngine;

namespace joymg
{
    public class HexCell : MonoBehaviour
    {
        [SerializeField]
        private HexCoordinates coordinates;
        private Color color;
        private int elevation = int.MinValue;

        [SerializeField]
        private HexCell[] neighbors;
        public HexGridChunk chunk;
        [SerializeField]
        private bool[] roads;

        public RectTransform uiRect;

        //River Data
        private bool hasIncomingRiver, hasOutgoingRiver;
        private HexDirection incomingRiver, outgoingRiver;

        public HexCoordinates Coordinates { get => coordinates; set => coordinates = value; }
        public Color Color
        {
            get => color;
            set
            {
                if (color == value)
                {
                    return;
                }
                color = value;
                Refresh();
            }
        }

        public Vector3 Position
        {
            get
            {
                return transform.localPosition;
            }
        }

        public int Elevation
        {
            get => elevation;
            set
            {
                if (elevation == value)
                {
                    return;
                }
                elevation = value;
                Vector3 position = transform.localPosition;
                position.y = value * HexMetrics.elevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1) * HexMetrics.elevationPerturbationStrength;
                transform.localPosition = position;

                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;

                if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation)
                {
                    RemoveOutgoingRiver();
                }
                if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation)
                {
                    RemoveIncomingRiver();
                }

                for (int direction = 0; direction < roads.Length; direction++)
                {
                    if (roads[direction] && GetElevationDifference((HexDirection)direction) > 1)
                    {
                        SetRoad(direction, false);
                    }
                }

                Refresh();
            }
        }

        public float StreamBedY { get => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; }
        public float RiverSurfaceY
        {
            get => (elevation + HexMetrics.riverSurfaceElevationOffset) *
                    HexMetrics.elevationStep;

        }

        //River properties
        public bool HasIncomingRiver { get => hasIncomingRiver; }
        public bool HasOutgoingRiver { get => hasOutgoingRiver; }
        public HexDirection IncomingRiver { get => incomingRiver; }
        public HexDirection OutgoingRiver { get => outgoingRiver; }
        public bool HasRiver { get => hasIncomingRiver || hasOutgoingRiver; }
        public bool HasRiverStartOrEnd { get => hasIncomingRiver != hasOutgoingRiver; }

        //Road Properties
        public bool HasRoads
        {
            get
            {
                for (int i = 0; i < roads.Length; i++)
                {
                    if (roads[i])
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        void Refresh()
        {
            if (chunk)
            {
                chunk.Refresh();
                for (int i = 0; i < neighbors.Length; i++)
                {
                    HexCell neighbor = neighbors[i];
                    if (neighbor != null && neighbor.chunk != null)
                    {
                        neighbor.chunk.Refresh();
                    }
                }
            }
        }


        private void RefreshSelfOnly()
        {
            //no need to check for chunk as no rivers are changin when inicializing the map
            chunk.Refresh();
        }

        public HexCell GetNeighbor(HexDirection direction)
        {
            return neighbors[(int)direction];
        }

        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            neighbors[(int)direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }

        public HexEdgeType GetEdgeType(HexDirection direction)
        {
            return HexMetrics.GetEdgeType(
                elevation, neighbors[(int)direction].elevation
            );
        }

        public HexEdgeType GetEdgeType(HexCell otherCell)
        {
            return HexMetrics.GetEdgeType(
                elevation, otherCell.elevation
            );
        }

        public int GetElevationDifference(HexDirection direction)
        {
            int difference = elevation - GetNeighbor(direction).elevation;
            return difference >= 0 ? difference : -difference;
        }

        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return
                hasIncomingRiver && incomingRiver == direction ||
                hasOutgoingRiver && outgoingRiver == direction;
        }

        public bool HasRoadThroughEdge(HexDirection direction)
        {
            return roads[(int)direction];
        }

        #region Create Rivers

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (hasOutgoingRiver && outgoingRiver == direction)
            {
                return;
            }

            HexCell neighbor = GetNeighbor(direction);
            //rivers can not go uphill
            if (!neighbor || elevation < neighbor.elevation)
            {
                return;
            }

            RemoveOutgoingRiver();
            if (hasIncomingRiver && incomingRiver == direction)
            {
                RemoveIncomingRiver();
            }

            hasOutgoingRiver = true;
            outgoingRiver = direction;

            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();

            //Rivers disallow roads, so removing the road refresh the mesh, own and neighbor's
            SetRoad((int)direction, false);
        }

        #endregion
        #region Remove Rivers
        public void RemoveIncomingRiver()
        {
            if (!hasIncomingRiver)
            {
                return;
            }
            hasIncomingRiver = false;
            RefreshSelfOnly();

            HexCell neighbor = GetNeighbor(incomingRiver);
            neighbor.hasOutgoingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        public void RemoveRiver()
        {
            RemoveOutgoingRiver();
            RemoveIncomingRiver();
        }

        public void RemoveOutgoingRiver()
        {
            if (!hasOutgoingRiver)
            {
                return;
            }
            hasOutgoingRiver = false;
            RefreshSelfOnly();

            HexCell neighbor = GetNeighbor(outgoingRiver);
            neighbor.hasIncomingRiver = false;
            neighbor.RefreshSelfOnly();
        }
        #endregion



        public void AddRoad(HexDirection direction)
        {
            if (!roads[(int)direction] &&
                !HasRiverThroughEdge(direction) &&
                GetElevationDifference(direction) <= 1)
            {
                SetRoad((int)direction, true);
            }
        }

        public void RemoveRoads()
        {
            for (int direction = 0; direction < neighbors.Length; direction++)
            {
                if (roads[direction])
                {
                    SetRoad(direction, false);
                }
            }
        }
        private void SetRoad(int direction, bool state)
        {
            roads[direction] = state;
            neighbors[direction].roads[(int)((HexDirection)direction).Opposite()] = state;
            neighbors[direction].RefreshSelfOnly();
            RefreshSelfOnly();
        }
    }
}