using System;
using UnityEngine;

namespace joymg
{
	[System.Serializable]
	public struct HexCoordinates 
	{
		[field: SerializeField]
		public int X { get; private set; }
		[field: SerializeField]
		public int Z { get; private set; }

		public int Y
		{
			get
			{
				return -X - Z;
			}
		}

		public HexCoordinates(int x, int z)
		{
			X = x;
			Z = z;
		}

		public static HexCoordinates FromOffsetCoordiantes(int x, int z)
        {
			return new HexCoordinates(x - z / 2, z);
		}

		public override string ToString()
		{
			return $"({X}, {Y}, {Z})";
		}

		public string ToStringOnSeparateLines()
		{
			return $"{X}\n{Y}\n{Z}";
		}

        internal static HexCoordinates FromPosition(Vector3 position)
        {
			float x = position.x / (HexMetrics.innerRadius * 2f);
			float y = -x;

			float offset = position.z / (HexMetrics.outerRadius * 3f);
			x -= offset;
			y -= offset;

			int iX = Mathf.RoundToInt(x);
			int iY = Mathf.RoundToInt(y);
			int iZ = Mathf.RoundToInt(-x - y);

			if (iX + iY + iZ != 0)
			{
				float dX = Mathf.Abs(x - iX);
				float dY = Mathf.Abs(y - iY);
				float dZ = Mathf.Abs(-x - y - iZ);

				if (dX > dY && dX > dZ)
				{
					iX = -iY - iZ;
				}
				else if (dZ > dY)
				{
					iZ = -iX - iY;
				}
			}

			return new HexCoordinates(iX, iZ);
		}
    }
}