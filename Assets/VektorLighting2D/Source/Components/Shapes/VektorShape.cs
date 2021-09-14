using UnityEngine;

namespace VektorLighting2D.Components.Shapes {
    public abstract class VektorShape : MonoBehaviour {
        protected virtual void Awake() {
            VektorLightingSystem.AddShape(this);
        }

        protected virtual void OnDestroy() {
            VektorLightingSystem.RemoveShape(this);
        }
    }
}