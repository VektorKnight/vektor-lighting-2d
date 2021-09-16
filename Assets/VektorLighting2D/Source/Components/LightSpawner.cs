using System;
using UnityEngine;
using VektorLighting2D.Components.Lights;
using Random = UnityEngine.Random;

namespace VektorLighting2D.Components {
    public class LightSpawner : MonoBehaviour {
        public VektorPointLight _lightPrefab;
        public int LightCount = 32;
        public int Seed = 1337;

        private void Start() {
            Random.InitState(Seed);
            for (var i = 0; i < LightCount; i++) {
                var rX = Random.Range(0f, 1f);
                var rY = Random.Range(0f, 1f);

                var lightPos = Camera.main.ScreenToWorldPoint(new Vector3(rX * Screen.width, rY * Screen.height));
                lightPos.z = 0;

                var light = Instantiate(_lightPrefab, lightPos, Quaternion.identity);
                light.Range = Random.Range(5f, 20f);
                light.Color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1.0f);
            }
        }
    }
}