using UnityEngine;

namespace VektorLighting2D.Components.Lights {
    public abstract class VektorLight : MonoBehaviour {
        [SerializeField] protected Color _color = Color.white;
        [SerializeField] protected float _range = 10.0f;
        [SerializeField] protected float _intensity = 1.0f;

        public Color Color {
            get => _color;
            set => _color = value;
        }

        public float Range {
            get => _range;
            set => _range = value;
        }

        public float Intensity {
            get => _intensity;
            set => _intensity = value;
        }

        protected virtual void Awake() {
            VektorLightingSystem.AddLight(this);
        }

        protected virtual void OnDestroy() {
            VektorLightingSystem.RemoveLight(this);
        }
    }
}