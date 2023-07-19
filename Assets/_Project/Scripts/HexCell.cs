using System;
using System.IO;
using UnityEngine;

namespace joymg
{
    public class HexCell : MonoBehaviour
    {
        [SerializeField]
        private HexCoordinates coordinates;
        private Color color;
        private int terrainTypeIndex;
        private int elevation = int.MinValue;
        private int waterLevel;

        [SerializeField]
        private HexCell[] neighbors;
        public HexGridChunk chunk;
        [SerializeField]
        private bool[] roads;

        public RectTransform uiRect;

        //River Data
        private bool hasIncomingRiver, hasOutgoingRiver;
        private HexDirection incomingRiver, outgoingRiver;

        private int urbanLevel, farmLevel, plantLevel;

        private bool hasWalls;

        private int specialIndex;

        public HexCoordinates Coordinates { get => coordinates; set => coordinates = value; }
        public Color Color
        {
            get => HexMetrics.colors[terrainTypeIndex];
        }

        public int TerrainTypeIndex
        {
            get => terrainTypeIndex;
            set
            {
                if (terrainTypeIndex != value)
                {
                    terrainTypeIndex = value;
                    Refresh();
                }
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
                RefreshPosition();
                ValidateRivers();

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

        private void RefreshPosition()
        {
            Vector3 position = transform.localPosition;
            position.y = elevation * HexMetrics.elevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1) * HexMetrics.elevationPerturbationStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }

        public int WaterLevel
        {
            get => waterLevel;
            set
            {
                if (waterLevel == value)
                {
                    return;
                }
                waterLevel = value;
                ValidateRivers();
                Refresh();
            }
        }
        public int FarmLevel
        {
            get => farmLevel;
            set
            {
                if (farmLevel == value)
                {
                    return;
                }
                farmLevel = value;
                ValidateRivers();
                Refresh();
            }
        }

        public int PlantLevel
        {
            get => plantLevel;
            set
            {
                if (plantLevel == value)
                {
                    return;
                }
                plantLevel = value;
                ValidateRivers();
                Refresh();
            }
        }

        public int UrbanLevel
        {
            get => urbanLevel;
            set
            {
                if (urbanLevel!= value)
                {
                    urbanLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public bool HasWalls
        {
            get => hasWalls;
            set
            {
                if (hasWalls != value)
                {
                    hasWalls = value;
                    Refresh();
                }
            }
        }

        public int SpecialIndex
        {
            get => specialIndex;
            set
            {
                if (specialIndex != value && !HasRiver)
                {
                    specialIndex = value;
                    RemoveRoads();
                    RefreshSelfOnly();
                }
            }
        }

        public float StreamBedY { get => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; }
        public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        public float WaterSurfaceY => (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

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

        public bool IsUnderwater => waterLevel > elevation;

        public bool IsSpecial => specialIndex > 0;

        bool IsValidRiverDestination(HexCell neighbor)
        {
            return neighbor && (
                elevation >= neighbor.elevation || waterLevel == neighbor.elevation
            );
        }


        private void Refresh()
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
        public HexDirection RiverStartOrEndDirection { get => hasIncomingRiver ? incomingRiver : outgoingRiver; }

        public bool HasRoadThroughEdge(HexDirection direction)
        {
            return roads[(int)direction];
        }

        #region Create Rivers

        private void ValidateRivers()
        {
            if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver)))
            {
                RemoveOutgoingRiver();
            }
            if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this))
            {
                RemoveIncomingRiver();
            }
        }

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (hasOutgoingRiver && outgoingRiver == direction)
            {
                return;
            }

            HexCell neighbor = GetNeighbor(direction);
            //rivers can not go uphill
            if (!IsValidRiverDestination(neighbor))
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
            specialIndex = 0;

            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();
            neighbor.specialIndex = 0;

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
                !IsSpecial && !GetNeighbor(direction).IsSpecial &&
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

        public void Save(BinaryWriter writer)
        {
            writer.Write((byte)terrainTypeIndex);
            writer.Write((byte)elevation);
            writer.Write((byte)waterLevel);
            writer.Write((byte)urbanLevel);
            writer.Write((byte)farmLevel);
            writer.Write((byte)plantLevel);
            writer.Write((byte)specialIndex);
            writer.Write(hasWalls);

            if (hasIncomingRiver)
            {
                writer.Write((byte)(incomingRiver + 128));
            }
            else
            {
                writer.Write((byte)0);
            }

            if (hasOutgoingRiver)
            {
                writer.Write((byte)(outgoingRiver + 128));
            }
            else
            {
                writer.Write((byte)0);
            }

            for (int i = 0; i < roads.Length; i++)
            {
                writer.Write(roads[i]);
            }
        }

        public void Load(BinaryReader reader)
        {
            terrainTypeIndex = reader.ReadByte();
            elevation = reader.ReadByte();
            RefreshPosition();
            waterLevel = reader.ReadByte();
            urbanLevel = reader.ReadByte();
            farmLevel = reader.ReadByte();
            plantLevel = reader.ReadByte();
            specialIndex = reader.ReadByte();

            hasWalls = reader.ReadBoolean();

            byte riverData = reader.ReadByte();
            if (riverData >= 128)
            {
                hasIncomingRiver = true;
                incomingRiver = (HexDirection)(riverData - 128);
            }
            else
            {
                hasIncomingRiver = false;
            }

            riverData = reader.ReadByte();
            if (riverData >= 128)
            {
                hasOutgoingRiver = true;
                outgoingRiver = (HexDirection)(riverData - 128);
            }
            else
            {
                hasOutgoingRiver = false;
            }

            for (int i = 0; i < roads.Length; i++)
            {
                roads[i] = reader.ReadBoolean();
            }
        }
    }
}