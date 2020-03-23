using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class CreateNavmesh : MonoBehaviour
{
    public GameObject[] navmeshes;

    // Start is called before the first frame update
    void Start()
    {
        navmeshes = GameObject.FindGameObjectsWithTag("Teleport");

        foreach (GameObject respawn in navmeshes)
        {
            GameObject instance = Instantiate(respawn, respawn.transform.position+new Vector3(0,0.001f,0), respawn.transform.rotation);
            instance.AddComponent<Valve.VR.InteractionSystem.TeleportArea>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
