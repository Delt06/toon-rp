using System.Collections.Generic;
using UnityEngine;

// https://github.com/cjacobwade/HelpfulScripts/blob/master/SmearEffect/SmearEffect.cs
namespace Samples.StylizedEffects.Effects.Smear
{
    public class SmearEffect : MonoBehaviour
    {
        private static readonly int MotionId = Shader.PropertyToID("_Motion");

        [SerializeField] private Renderer _renderer;
        [SerializeField] [Min(0)] private int _frameLag;

        private readonly Queue<Vector3> _recentPositions = new();

        private Material _material;

        private void Start()
        {
            _material = _renderer.material;
        }

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;

            if (_recentPositions.Count > _frameLag)
            {
                _material.SetVector(MotionId, currentPosition - _recentPositions.Dequeue());
            }

            _recentPositions.Enqueue(currentPosition);
        }
    }
}