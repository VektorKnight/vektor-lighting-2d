using UnityEngine;

namespace VektorLighting2D.Lights {
    public sealed class VektorLight : MonoBehaviour {
        [Header("Light Config")]
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private float _radius = 5.0f;
        [SerializeField] private float _intensity = 1.0f;

    }
}