using UnityEngine;

namespace joymg
{
	public class HexCell : MonoBehaviour
	{
		[SerializeField]
		private HexCoordinates coordinates;

		public Color color;

		public HexCoordinates Coordinates { get => coordinates; set => coordinates = value; }
    }
}