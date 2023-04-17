using UnityEngine;

namespace joymg
{
    public class HexCell : MonoBehaviour
    {
        [SerializeField]
        private HexCoordinates coordinates;
        private Color color;
        private int elevation;

        [SerializeField]
        private HexCell[] neighbors;

        public RectTransform uiRect;

        public HexCoordinates Coordinates { get => coordinates; set => coordinates = value; }
        public Color Color { get => color; set => color = value; }
        public int Elevation
        {
            get => elevation;
            set
            {
                elevation = value;
                Vector3 position = transform.localPosition;
                position.y = value * HexMetrics.elevationStep;
                transform.localPosition = position;

                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = elevation * -HexMetrics.elevationStep;
                uiRect.localPosition = uiPosition;
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