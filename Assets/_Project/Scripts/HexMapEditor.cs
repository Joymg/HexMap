using UnityEngine;
using UnityEngine.EventSystems;

namespace joymg
{
    public class HexMapEditor : MonoBehaviour
    {
        public Color[] colors;

        public HexGrid hexGrid;

        private Color currentColor;
        private int currentElevation;

        void Awake()
        {
            SelectColor(0);
        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                HandleInput();
            }
        }

        void HandleInput()
        {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit) &&
                !EventSystem.current.IsPointerOverGameObject())
            {
                EditCell(hexGrid.GetCell(hit.point));
            }
        }

        void EditCell(HexCell cell)
        {
            cell.Color = currentColor;
            cell.Elevation = currentElevation;
        }

        public void SelectColor(int index)
        {
            currentColor = colors[index];
        }

        public void SetElevation(float elevation)
        {
            currentElevation = (int)elevation;
        }
    }
}