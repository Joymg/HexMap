using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace joymg
{
    public class HexMapEditor : MonoBehaviour
    {
        private enum OptionalToggle
        {
            Ignore,
            Yes,
            No
        }

        public HexGrid hexGrid;

        private bool applyColor;
        public Color[] colors;
        private Color currentColor;

        private bool applyElevation = true;
        private int currentElevation;

        private bool applyWaterLevel = false;
        private int currentWaterLevel;
        
        private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel;
        private int currentUrbanLevel, currentFarmLevel, currentPlantLevel;

        private int brushSize;

        private OptionalToggle riverMode, roadMode, wallMode;

        private bool isDrag;
        private HexDirection dragDirection;
        private HexCell previousCell;

        void Awake()
        {
            SelectColor(0);
        }

        void Update()
        {
            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                HandleInput();
            }
            else
            {
                previousCell = null;
            }
        }

        void HandleInput()
        {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit))
            {
                HexCell currentCell = hexGrid.GetCell(hit.point);
                if (previousCell && previousCell != currentCell)
                {
                    ValidateDrag(currentCell);
                }
                else
                {
                    isDrag = false;
                }
                EditCells(currentCell);
                previousCell = currentCell;
            }
            else
            {
                previousCell = null;
            }

        }

        private void ValidateDrag(HexCell currentCell)
        {
            for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
            {
                if (previousCell.GetNeighbor(dragDirection) == currentCell)
                {
                    isDrag = true;
                    return;
                }
            }
            isDrag = false;
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
                if (applyWaterLevel)
                {
                    cell.WaterLevel = currentWaterLevel;
                }
                if (applyUrbanLevel)
                {
                    cell.UrbanLevel = currentUrbanLevel;
                }
                if (applyFarmLevel)
                {
                    cell.FarmLevel = currentFarmLevel;
                }
                if (applyPlantLevel)
                {
                    cell.PlantLevel = currentPlantLevel;
                }

                if (riverMode == OptionalToggle.No)
                {
                    cell.RemoveRiver();
                }

                if (roadMode == OptionalToggle.No)
                {
                    cell.RemoveRoads();
                }

                if (wallMode != OptionalToggle.Ignore)
                {
                    cell.HasWalls = wallMode == OptionalToggle.Yes;
                }
                {

                }

                if (isDrag)
                {
                    HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                    if (otherCell)
                    {
                        if (riverMode == OptionalToggle.Yes)
                        {
                            otherCell.SetOutgoingRiver(dragDirection);
                        }
                        if (roadMode == OptionalToggle.Yes)
                        {
                            otherCell.AddRoad(dragDirection);
                        }
                    }
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

        public void SetRiverMode(int mode)
        {
            riverMode = (OptionalToggle)mode;
        }

        public void SetRoadMode(int mode)
        {
            roadMode = (OptionalToggle)mode;
        }

        public void SetApplyWaterLevel(bool toogle)
        {
            applyWaterLevel = toogle;
        }

        public void SetWaterLevel(float level)
        {
            currentWaterLevel = (int)level;
        }

        public void SetApplyUrbanLevel(bool toggle)
        {
            applyUrbanLevel = toggle;
        }

        public void SetUrbanLevel(float level)
        {
            currentUrbanLevel = (int)level;
        }

        public void SetApplyFarmLevel(bool toggle)
        {
            applyFarmLevel = toggle;
        }

        public void SetFarmLevel(float level)
        {
            currentFarmLevel = (int)level;
        }

        public void SetApplyPlantLevel(bool toggle)
        {
            applyPlantLevel = toggle;
        }

        public void SetPlantLevel(float level)
        {
            currentPlantLevel = (int)level;
        }

        public void SetWallMode(int mode)
        {
            wallMode = (OptionalToggle)mode;
        }
    }
}