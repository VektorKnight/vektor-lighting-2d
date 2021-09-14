using UnityEngine;

namespace VektorLighting2D.Components.Lights {
    public abstract class VektorLight : MonoBehaviour {
        [SerializeField] protected Color _color = Color.white;
        [SerializeField] protected float _range = 10.0f;
        [SerializeField] protected float _intensity = 1.0f;

        protected virtual void Awake() {
            VektorLightingSystem.AddLight(this);
        }

        protected virtual void OnDestroy() {
            VektorLightingSystem.RemoveLight(this);
        }
    }
}