using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class EnemySpawner : MonoBehaviour
{
    //This is a singleton simply to have a separate place to handle enemies
    //It is quite easily removeable from all code and used solely as a helper to show what the player
    //controller is capable of.

    [SerializeField]
    List<Transform> totalEnemyList = new List<Transform>();

    private static EnemySpawner spawner;

    [SerializeField]
    GameObject enemyPrefab;

    //This is used to keep the hierarchy clean for debugging
    [SerializeField]
    Transform enemyParent;

    [SerializeField]
    float enemyRespawnInSeconds = 5.0f;
    WaitForSeconds enemyRespawn;

    //Singleton pattern
    public static EnemySpawner Instance
    {
        get { return spawner; }
        private set
        {
            if(spawner == null)
            {
                spawner = value;
            }
            else if(Instance != value)
            {
                Destroy(value.gameObject);
            }
        }
    }

    private void Awake()
    {
        Instance = this;

        //Spawns every "enemy" on level load
        foreach(Transform t in totalEnemyList)
        {
            LeanPool.Spawn(enemyPrefab, t.position, t.rotation, enemyParent);
        }
        //Sets up the time between enemy spawns
        enemyRespawn = new WaitForSeconds(enemyRespawnInSeconds);
    }

    public void EnemyKilled(Transform trans)
    {
        LeanPool.Despawn(trans);
        StartCoroutine(SpawnEnemy(trans.position,trans.rotation));
    }

    //Spawns with the delay set when the game starts
    //This ensures that dashing through enemy challenges are not impossible
    //after the first try
    IEnumerator SpawnEnemy(Vector3 pos, Quaternion rot)
    {
        yield return enemyRespawn;
        LeanPool.Spawn(enemyPrefab, pos, rot, enemyParent);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach(Transform t in totalEnemyList)
        {
            Gizmos.DrawWireSphere(t.position, 1.0f);
        }
    }
}
