using System;
using AudioTerrain;
using UnityEngine;
using UnityEngine.UI;

namespace VektorLighting2D.Debugging {
    public class FramerateCounter : MonoBehaviour {
        public Text CounterDisplay;

        private FloatRingBuffer _ringBuffer;
        
        // Start is called before the first frame update
        void Start() {
            _ringBuffer = new FloatRingBuffer(60, float.Epsilon);
        }

        // Update is called once per frame
        void Update() {
            _ringBuffer.Push(Time.deltaTime);
            CounterDisplay.text = $"FPS: {1f / _ringBuffer.Average():n0}";
        }
    }
}
