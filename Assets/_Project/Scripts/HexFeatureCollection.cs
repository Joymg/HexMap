using UnityEngine;

namespace joymg
{
	[System.Serializable]
	public struct HexFeatureCollection
	{
		public Transform[] prefabs;

		public Transform Pick(float choice)
		{
			return prefabs[(int)(choice * prefabs.Length)];
		}
	}
}