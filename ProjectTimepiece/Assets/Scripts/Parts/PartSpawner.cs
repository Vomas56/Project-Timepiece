using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> m_prefabs; 
    
    private List<GameObject> m_spawnedInstances;

    void Start()
    {
        int x = -76;
        foreach(GameObject prefab in m_prefabs)
        {
            //GameObject part = 
            //m_spawnedInstances.Add(part);
            Instantiate(prefab, new Vector3(x,0,5), Quaternion.identity);
            x += 2;
        }
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 2. Remove the item from the scene
            //Destroy(spawnedInstance);

            // 3. You can STILL access and spawn the original prefab later
            //Debug.Log("Scene instance removed. Prefab asset name is: " + myPrefab.name);
        }
    }
}
