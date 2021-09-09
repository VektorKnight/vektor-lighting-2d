using UnityEngine;

namespace VektorLighting2D {
    [CreateAssetMenu(fileName = "VektorLightingConfig", menuName = "/SpawnManagerScriptableObject", order = 1)]
    public class VektorLightingConfig : ScriptableObject {
        [SerializeField] private float _renderScale = 1.0f;
        [SerializeField] private ComputeShader _rayMarchCompute;
    }
}