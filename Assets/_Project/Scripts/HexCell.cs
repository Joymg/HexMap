using UnityEngine;

namespace joymg
{
	public class HexCell : MonoBehaviour
	{
		[SerializeField]
		private HexCoordinates coordinates;

        private Color color;

        [SerializeField]
		private HexCell[] neighbors;

		public HexCoordinates Coordinates { get => coordinates; set => coordinates = value; }
        public Color Color { get => color; set => color = value; }

        public HexCell GetNeighbor(HexDirection direction)
        {
			return neighbors[(int)direction];
        }

		public void SetNeighbor(HexDirection direction, HexCell cell)
        {
			neighbors[(int)direction] = cell;
			cell.neighbors[(int)direction.Opposite()] = this;
        }
    }
}