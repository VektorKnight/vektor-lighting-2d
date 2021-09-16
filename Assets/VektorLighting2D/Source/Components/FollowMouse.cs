using UnityEngine;

namespace VektorLighting2D.Components {
    public class FollowMouse : MonoBehaviour {
        private void Update() {
            var mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = mouseWorld;
        }
    }
}