using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TeleportToNewLocation : MonoBehaviour
{
    public GameObject player;
    public GameObject location1;
    public GameObject location2;

    bool toggle = false;

    private SteamVR_Input_Sources leftHand = SteamVR_Input_Sources.LeftHand;

    // Start is called before the first frame update
    void Start()
    {
        player.transform.position = location1.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (SteamVR_Input.GetStateDown("GrabPinch", leftHand))
        {
            toggle = !toggle;
            if (toggle)
                player.transform.position = location2.transform.position;
            if (!toggle)
                player.transform.position = location1.transform.position;
        }

       
    }
}
