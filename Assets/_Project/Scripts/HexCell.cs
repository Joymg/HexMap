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

                Refresh();
            }
        }

        public float StreamBedY { get => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; }
        public float RiverSurfaceY
        {
            get => (elevation + HexMetrics.riverSurfaceElevationOffset) *
                    HexMetrics.elevationStep;

        }

        public bool HasIncomingRiver { get => hasIncomingRiver; }
        public bool HasOutgoingRiver { get => hasOutgoingRiver; }
        public HexDirection IncomingRiver { get => incomingRiver; }
        public HexDirection OutgoingRiver { get => outgoingRiver; }
        public bool HasRiver { get => hasIncomingRiver || hasOutgoingRiver; }
        public bool HasRiverStartOrEnd { get => hasIncomingRiver != hasOutgoingRiver; }


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

        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return
                hasIncomingRiver && incomingRiver == direction ||
                hasOutgoingRiver && outgoingRiver == direction;
        }

        #region Creating Rivers

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
            RefreshSelfOnly();

            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();
            neighbor.RefreshSelfOnly();
        }

        #endregion
        #region Removing Rivers
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
    }
}