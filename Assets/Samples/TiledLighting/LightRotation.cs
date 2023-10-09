using System.Linq;
using UnityEngine;

namespace Samples.TiledLighting
{
    public class LightRotation : MonoBehaviour
    {
        [SerializeField] private Vector2 _speedRange;

        private (Light light, float speed)[] _lights;


        private void Awake()
        {
            _lights = FindObjectsOfType<Light>()
                .Where(l => l.type == LightType.Point)
                .Select(l =>
                    {
                        float speed = Random.Range(_speedRange.x, _speedRange.y);
                        if (Random.value > 0.5f)
                        {
                            speed *= -1;
                        }

                        return (l, speed);
                    }
                )
                .ToArray();
        }

        private void Update()
        {
            Vector3 origin = transform.position;

            foreach ((Light l, float speed) in _lights)
            {
                Transform lightTransform = l.transform;
                Vector3 position = lightTransform.position;
                Vector3 offset = position - origin;
                offset = Quaternion.Euler(0, speed * Time.deltaTime, 0) * offset;
                lightTransform.position = origin + offset;
            }
        }
    }
}