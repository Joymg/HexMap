using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace joymg
{
    public class HexMapEditor : MonoBehaviour
    {
        public Color[] colors;
        private bool applyColor;

        public HexGrid hexGrid;

        private bool applyElevation = true;
        private Color currentColor;
        private int currentElevation;

        private int brushSize;

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
                EditCells(hexGrid.GetCell(hit.point));
            }
        }

        private void EditCells(HexCell center)
        {
            int centerX = center.Coordinates.X;
            int centerZ = center.Coordinates.Z;

            //bottom half
            for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
            {
                for (int x = centerX - r; x <= centerX + brushSize; x++)
                {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }

            //top half minus the middle row 
            for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
            {
                for (int x = centerX - brushSize; x <= centerX + r; x++)
                {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
        }

        void EditCell(HexCell cell)
        {
            if (cell)
            {
                if (applyColor)
                {
                    cell.Color = currentColor;
                }
                if (applyElevation)
                {
                    cell.Elevation = currentElevation;
                }
            }
        }

        public void ShowUI(bool visible)
        {
            hexGrid.ShowUI(visible);
        }

        public void SelectColor(int index)
        {
            applyColor = index >= 0;
            if (applyColor)
            {
                currentColor = colors[index];
            }
        }

        public void SetElevation(float elevation)
        {
            currentElevation = (int)elevation;
        }

        public void SetApplyElevation(bool toggle)
        {
            applyElevation = toggle;
        }

        public void SetBrushSize(float size)
        {
            brushSize = (int)size;
        }
    }
}