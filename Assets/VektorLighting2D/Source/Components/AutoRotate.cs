using System;
using UnityEngine;

namespace VektorLighting2D.Components {
    public sealed class AutoRotate : MonoBehaviour {
        public Vector3 DegreesPerSecond;
        public Vector3 AxisRange;

        private Vector3 _origin;

        private void Awake() {
            _origin = transform.position;
        }

        public void Update() {
            transform.position = _origin + AxisRange * Mathf.Sin(Time.time);
            transform.rotation *= Quaternion.Euler(DegreesPerSecond * Time.deltaTime);
        }
    }
}