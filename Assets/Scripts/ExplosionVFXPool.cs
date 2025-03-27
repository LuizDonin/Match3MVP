using System.Collections.Generic;
using UnityEngine;

public class ExplosionVFXPool : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int poolSize;

    private Queue<GameObject> pool;

    void Start()
    {
        StartPool();
    }

    //Starts the pool and deactivates the vfx
    private void StartPool()
    {
        pool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            var fx = Instantiate(explosionPrefab);
            fx.SetActive(false);
            pool.Enqueue(fx);
        }
    }

    // Gets an explosion VFX from the pool
    public GameObject GetExplosion()
    {
        if (pool.Count > 0)
        {
            var fx = pool.Dequeue();
            fx.SetActive(true);
            return fx;
        }
        else
        {
            var fx = Instantiate(explosionPrefab);
            return fx;
        }
    }

    // Return an explosion VFX to the pool
    public void ReturnExplosion(GameObject fx)
    {
        if (fx != null && fx.activeInHierarchy)
        {
            fx.SetActive(false);
            pool.Enqueue(fx);
        }
        else
        {
            Debug.Log("Trying to return a destroyed vfx to the pool");
        }
    }

}
