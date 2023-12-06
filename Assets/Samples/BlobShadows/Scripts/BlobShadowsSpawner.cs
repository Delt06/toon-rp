using DELTation.ToonRP.Shadows.Blobs;
using UnityEngine;

namespace Samples.BlobShadows.Scripts
{
    public class BlobShadowsSpawner : MonoBehaviour
    {
        public Vector2 BoundsX;
        public Vector2 BoundsZ;
        public GameObject[] Prefabs;
        public int Count;
        public int Seed;
        public bool Static;

        private void Start()
        {
            Random.State oldState = Random.state;
            Random.InitState(Seed);

            for (int i = 0; i < Count; i++)
            {
                int prefabIndex = Random.Range(0, Prefabs.Length);
                GameObject prefab = Prefabs[prefabIndex];
                Vector3 position = transform.position +
                                   new Vector3(
                                       Random.Range(BoundsX.x, BoundsX.y),
                                       prefab.transform.position.y,
                                       Random.Range(BoundsZ.x, BoundsZ.y)
                                   );
                Quaternion rotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f) *
                                      prefab.transform.rotation;
                GameObject o = Instantiate(prefab, position, rotation);
                o.GetComponent<ToonBlobShadowRenderer>().IsStatic = Static;
            }

            Random.state = oldState;
        }
    }
}