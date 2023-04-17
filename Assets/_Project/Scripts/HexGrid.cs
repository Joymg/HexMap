using UnityEngine;
using TMPro;

namespace joymg
{
	public class HexGrid : MonoBehaviour
	{
		[SerializeField]
		private int width = 6, height = 6;

		[SerializeField]
		private HexCell cellPrefab;
		[SerializeField]
		public TextMeshProUGUI cellLabelPrefab;

		private Canvas gridCanvas;
		private HexMesh hexMesh;
		private HexCell[] cells;

		public Color defaultColor = Color.white;
		public Color touchedColor = new Color(210,45,150,255);

		void Awake()
		{
			gridCanvas = GetComponentInChildren<Canvas>();
			hexMesh = GetComponentInChildren<HexMesh>();

			cells = new HexCell[height * width];

			for (int z = 0, i = 0; z < height; z++)
			{
				for (int x = 0; x < width; x++)
				{
					CreateCell(x, z, i++);
				}
			}
		}

        private void Start()
        {
			hexMesh.Triangulate(cells);
        }

        void CreateCell(int x, int z, int i)
		{
			Vector3 position;
			position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2);
			position.y = 0f;
			position.z = z * (HexMetrics.outerRadius * 1.5f);

			HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
			cell.transform.SetParent(transform, false);
			cell.transform.localPosition = position;
			cell.Coordinates = HexCoordinates.FromOffsetCoordiantes(x, z);
			cell.color = defaultColor;

			TextMeshProUGUI label = Instantiate<TextMeshProUGUI>(cellLabelPrefab);
			label.rectTransform.SetParent(gridCanvas.transform, false);
			label.rectTransform.anchoredPosition =
				new Vector2(position.x, position.z);
			label.text =cell.Coordinates.ToStringOnSeparateLines();
		}

		public void ColorCell(Vector3 position,Color color)
		{
			position = transform.InverseTransformPoint(position);
			HexCoordinates coordinates = HexCoordinates.FromPosition(position);
			int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
			HexCell cell = cells[index];
			cell.color = color;
			hexMesh.Triangulate(cells);
		}
	}
}