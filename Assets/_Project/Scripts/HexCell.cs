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

                Refresh();
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
    }
}