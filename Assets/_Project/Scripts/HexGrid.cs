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

        [SerializeField]
        private Color defaultColor = Color.white;
        [SerializeField]
        private Color touchedColor = new Color(210, 45, 150, 255);

        [SerializeField]
        private Texture2D noiseSource;


        void Awake()
        {
            HexMetrics.noiseSource = noiseSource;
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

        private void OnEnable()
        {
            HexMetrics.noiseSource = noiseSource;
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
            cell.Color = defaultColor;

            if (x > 0)
            {
                cell.SetNeighbor(HexDirection.W, cells[i - 1]);
            }

            if (z > 0)
            {
                if ((z & 1) == 0)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                    if (x > 0)
                    {
                        cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                    }
                }
                else
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                    if (x < width - 1)
                    {
                        cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                    }
                }
            }

            TextMeshProUGUI label = Instantiate<TextMeshProUGUI>(cellLabelPrefab);
            label.rectTransform.SetParent(gridCanvas.transform, false);
            label.rectTransform.anchoredPosition =
                new Vector2(position.x, position.z);
            label.text = cell.Coordinates.ToStringOnSeparateLines();

            cell.uiRect = label.rectTransform;
            cell.Elevation = 0;
        }

        public HexCell GetCell(Vector3 position)
        {

            position = transform.InverseTransformPoint(position);
            HexCoordinates coordinates = HexCoordinates.FromPosition(position);
            int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
            return cells[index];
        }

        public void Refresh()
        {
            hexMesh.Triangulate(cells);
        }
    }
}