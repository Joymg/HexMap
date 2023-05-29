using System;
using UnityEngine;

namespace joymg
{
    public class HexMapCamera : MonoBehaviour
    {
        private Transform swivel, stick;

        [SerializeField]
        private float stickMinZoom, stickMaxZoom;
        [SerializeField]
        private float swivelMinZoom, swivelMaxZoom;
        private float zoom = 1f;

        private void Awake()
        {
            swivel = transform.GetChild(0);
            stick = swivel.GetChild(0);
        }

        private void Update()
        {
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (zoomDelta != 0f)
            {
                AdjustZoom(zoomDelta);
            }
        }

        private void AdjustZoom(float delta)
        {
            zoom = Mathf.Clamp01(zoom + delta);

            float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
            stick.localPosition = new Vector3(0f, 0f, distance);

            float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
            swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}