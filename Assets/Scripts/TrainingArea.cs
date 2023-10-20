using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

[System.Serializable]
public struct Food
{
    public GameObject prefab;
    public int zoneAmount;
    public int amountPerZone;
}

public class TrainingArea : MonoBehaviour
{
    [Tooltip("How faster is the simulation compared to real time. Default 60x faster: 12mins = 12hrs")]
    public float timeScale = 60f;
    [Tooltip("Maximum environment step before resetting scene")]
    public int MaxEnvironmentSteps = 200000;
    public List<Food> rabbitFoodList;
    public List<GameObject> rabbitPrefab;
    public int foodCount;
    public float spawnZoneSize;
    public float width;
    public float height;
    public GameObject[] lakes;
    public List<GameObject> spawnedLakes;

    private int resetTimer;

    // Start is called before the first frame update
    void Start()
    {
        //ResetLakes();
        ResetRabbitSpawn();
        ResetFoodSpawn();
        InvokeRepeating("RespawnFood", (12*60*60)/timeScale, (6*60*60)/timeScale); // Starts on day 1, repeats every 0.5 day afterwards. 
    }

    void FixedUpdate()
    {
        resetTimer += 1;
        if (resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            //ResetLakes();
            ResetRabbitSpawn();
            ResetFoodSpawn();
        }
    }

    public void ResetFoodSpawn()
    {
        foodCount = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("RabbitFood")) Destroy(child.gameObject);

        }
        RespawnFood();
    }

    public void DespawnFoodObject(GameObject obj)
    {
        try
        {
            Destroy(obj);
            foodCount -= 1;
        }
        catch { return; }
        
    }

    void RespawnFood()
    {
        Debug.Log("Spawn Food");
        float spawnAmount = Academy.Instance.EnvironmentParameters.GetWithDefault("spawn_amount", 100) / 100f;
        foreach (var rabbitFood in rabbitFoodList)
        {
            //for (int i = 0; i < 0.5f* rabbitFood.zoneAmount/7; i++)
            for (int i = 0; i < rabbitFood.zoneAmount / 8; i++)
            {
                if (foodCount > 250) return;
                Vector2 newSpawnPoint = GetNewSpawnPoint();
                for (int j = 0; j < Mathf.RoundToInt(1f * rabbitFood.amountPerZone / 8); j++)
                //for (int j = 0; j < Mathf.RoundToInt(0.35f * rabbitFood.amountPerZone/10); j++)
                //for (int j = 0; j < Mathf.RoundToInt(rabbitFood.amountPerZone/10); j++)
                {
                    Instantiate(rabbitFood.prefab, newSpawnPoint + Random.insideUnitCircle * spawnZoneSize, Quaternion.identity, transform);
                    foodCount += 1;
                }
            }
        }
    }

    public Vector2 GetNewSpawnPoint()
    {
        Vector2 newSpawnPoint = new Vector2 (transform.localPosition.x, transform.localPosition.y) + new Vector2(Random.Range(-width/2 + 1 + spawnZoneSize, width/2 - 1 - spawnZoneSize), Random.Range(-height/2 + 1 + spawnZoneSize, height/2 - 1 - spawnZoneSize));
        while (!CheckValidSpawnPoint(newSpawnPoint))
        {
            newSpawnPoint = new Vector2(transform.localPosition.x, transform.localPosition.y) + new Vector2(Random.Range(-width / 2 + 1 + spawnZoneSize, width / 2 - 1 - spawnZoneSize), Random.Range(-height / 2 + 1 + spawnZoneSize, height / 2 - 1 - spawnZoneSize));
        }
        return newSpawnPoint;
    }
    public bool CheckValidSpawnPoint(Vector2 spawnPoint)
    {
        //Debug.Log("Checking spawnPoint: " + (spawnPoint - new Vector2(transform.localPosition.x, transform.localPosition.y)));
        foreach (GameObject blockZone in spawnedLakes)
        {
            if (Mathf.Sqrt(blockZone.transform.GetComponent<EdgeCollider2D>().bounds.SqrDistance(spawnPoint)) < spawnZoneSize)
                return false;
        }
        return true;
    }

    public void ResetRabbitSpawn()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("Male Rabbit"))
            {
                child.gameObject.GetComponent<RabbitAgent>().EpisodeInterrupted();
                Destroy(child.gameObject);
            }
            if (child.CompareTag("Female Rabbit"))
            {
                child.gameObject.GetComponent<RabbitAgent>().EpisodeInterrupted();
                Destroy(child.gameObject);
            }
        }
        SpawnNewRabbits(41);
        resetTimer = 0;
    }

    public void SpawnNewRabbits()
    {
        for (int i = 0; i < Random.Range(1,5); i++)
        {
            Vector2 newSpawnPoint = GetNewSpawnPoint();
            GameObject rabbit = rabbitPrefab[0];
            Instantiate(rabbit, newSpawnPoint, Quaternion.identity, transform);
        }
    }

    public void SpawnNewRabbits(int spawnAmount)
    {
        for (int i = 0; i < Random.Range(40, spawnAmount+1); i++)
        //for (int i = 0; i < spawnAmount; i++)
        {
            Vector2 newSpawnPoint = GetNewSpawnPoint();
            GameObject rabbit = rabbitPrefab[0];
            Instantiate(rabbit, newSpawnPoint, Quaternion.identity, transform);
        }
    }

    public void SpawnLakes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            bool overlap = true;
            GameObject lake = lakes[Random.Range(0, lakes.Length)];

            // Check distance to every spawned lake, ensure that minimum distance is 5 unit
            while (overlap)
            {
                Vector2 newLocation = new Vector2(transform.localPosition.x, transform.localPosition.y) + new Vector2(Random.Range(-width / 2 + lake.GetComponent<BlockZone>().width/2, width / 2 - lake.GetComponent<BlockZone>().width/2),
                    Random.Range(-height / 2 + lake.GetComponent<BlockZone>().height/2, height / 2 - lake.GetComponent<BlockZone>().height/2));
                // Temporarily instantiate lake to check collider distance
                GameObject tempLake = Instantiate(lake, newLocation, Quaternion.identity, transform);

                if (spawnedLakes.Count == 0) { spawnedLakes.Add(tempLake); break; }
                foreach (GameObject spawnedLake in spawnedLakes)
                {
                    if (!Physics2D.Distance(spawnedLake.GetComponent<EdgeCollider2D>(), tempLake.GetComponent<EdgeCollider2D>()).isValid || Physics2D.Distance(spawnedLake.GetComponent<EdgeCollider2D>(), tempLake.GetComponent<EdgeCollider2D>()).distance < 5)
                    {
                        Destroy(tempLake);
                        overlap = true;
                        break;
                    }
                    overlap = false;
                }
                if(!overlap) spawnedLakes.Add(tempLake);
            }
        }
    }

    public void ResetLakes()
    {
        foreach (GameObject spawnedLake in spawnedLakes)
        {
            Destroy(spawnedLake);
        }
        spawnedLakes = new List<GameObject>();
        SpawnLakes(5);
    }

    // Update is called once per frame
    void Update()
    {
        if (foodCount < 175) RespawnFood();

    }
}
