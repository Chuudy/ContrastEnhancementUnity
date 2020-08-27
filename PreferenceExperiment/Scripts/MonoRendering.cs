using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoRendering : MonoBehaviour
{
    public GameObject VRCamera;
    public bool toggle;

    private Vector3 MonoScale;
    private Vector3 StereoScale;

    // Start is called before the first frame update
    void Start()
    {
        MonoScale = new Vector3(0, 1, 1);
        StereoScale = new Vector3(1, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (toggle)
            VRCamera.transform.localScale = MonoScale;
        else
            VRCamera.transform.localScale = StereoScale;
    }
}
