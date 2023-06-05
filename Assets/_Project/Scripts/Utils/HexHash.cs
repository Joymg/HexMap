using UnityEngine;

namespace joymg
{
    public struct HexHash
    {
        public float a, b, c;

        public static HexHash Create()
        {
            HexHash hash;
            //making sure this values are never 1
            hash.a = Random.value * 0.999f;
            hash.b = Random.value * 0.999f;
            hash.c = Random.value * 0.999f;
            return hash;
        }
    }
}