using UnityEngine;

namespace joymg
{
    public class HexFeatureManager : MonoBehaviour
    {
        public HexFeatureCollection[] urbanCollection, farmCollections, plantCollections;
        public HexMesh walls;

        private Transform container;

        public void Clear()
        {
            if (container)
            {
                Destroy(container.gameObject);
            }
            container = new GameObject("Features Container").transform;
            container.SetParent(transform, false);

            walls.Clear();
        }

        public void Apply() {
            walls.Apply();
        }

        public void AddFeature(HexCell hexCell, Vector3 position)
        {
            HexHash hash = HexMetrics.SampleHashGrid(position);
            Transform prefab = PickPrefab(urbanCollection, hexCell.UrbanLevel, hash.a, hash.d);
            Transform otherPrefab = PickPrefab(farmCollections, hexCell.FarmLevel, hash.b, hash.d);

            float usedHash = hash.a;
            if (prefab)
            {
                if (otherPrefab && hash.b < hash.a)
                {
                    prefab = otherPrefab;
                    usedHash = hash.b;
                }
            }
            else if (otherPrefab)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }

            otherPrefab = PickPrefab(plantCollections, hexCell.PlantLevel, hash.c, hash.d);

            if (prefab)
            {
                if (otherPrefab && hash.c < usedHash)
                {
                    prefab = otherPrefab;
                }
            }
            else if (otherPrefab)
            {
                prefab = otherPrefab;
            }
            else
            {
                return;
            }

            Transform instance = Instantiate(prefab);
            position.y += instance.localScale.y * 0.5f;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
            instance.SetParent(container);
        }

        public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell)
        {
            if (nearCell.HasWalls != farCell.HasWalls)
            {
                AddWallSegment(near.v1, far.v1, near.v5, far.v5);
            }
        }

        public void AddWall(Vector3 cornerVertex1, HexCell cell1,
            Vector3 cornerVertex2, HexCell cell2,
            Vector3 cornerVertex3, HexCell cell3)
        {
            if (cell1.HasWalls)
            {
                if (cell2.HasWalls)
                {
                    if (!cell3.HasWalls)
                    {
                        AddWallSegment(cornerVertex3, cell3, cornerVertex1, cell1, cornerVertex2, cell2);
                    }
                }
                else if (cell3.HasWalls)
                {
                    AddWallSegment(cornerVertex2, cell2, cornerVertex3, cell3, cornerVertex1, cell1);
                }
                else
                {
                    AddWallSegment(cornerVertex1, cell1, cornerVertex2, cell2, cornerVertex3, cell3);
                }
            }
            else if (cell2.HasWalls) 
            {
                if (cell3.HasWalls)
                {
                    AddWallSegment(cornerVertex1, cell1, cornerVertex2, cell2, cornerVertex3, cell3);
                }
                else
                {
                    AddWallSegment(cornerVertex2, cell2, cornerVertex3, cell3, cornerVertex1, cell1);
                }
            }
            else if (cell3.HasWalls)
            {
                AddWallSegment(cornerVertex3, cell3, cornerVertex1, cell1, cornerVertex2, cell2);
            }
        }

        public void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight)
        {
            Vector3 left = Vector3.Lerp(nearLeft, farLeft, 0.5f);
            Vector3 right = Vector3.Lerp(nearRight, farRight, 0.5f);

            Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
            Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

            float leftTop = left.y + HexMetrics.wallHeight;
            float rightTop = right.y + HexMetrics.wallHeight;

            Vector3 v1, v2, v3, v4;
            v1 = v3 = left - leftThicknessOffset;
            v2 = v4 = right - rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            walls.AddQuad(v1, v2, v3, v4);

            Vector3 t1 = v3, t2 = v4;

            v1 = v3 = left + leftThicknessOffset;
            v2 = v4 = right + rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            walls.AddQuad(v2, v1, v4, v3);

            walls.AddQuad(t1, t2, v3, v4);
        }

        public void AddWallSegment(Vector3 pivot, HexCell pivotCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            AddWallSegment(pivot, left, pivot, right);
        }

        private Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
        {
            if (level > 0)
            {
                float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
                for (int i = 0; i < thresholds.Length; i++)
                {
                    if (hash < thresholds[i])
                    {
                        return collection[i].Pick(choice);
                    }
                }
            }
            return null;
        }
    }
}