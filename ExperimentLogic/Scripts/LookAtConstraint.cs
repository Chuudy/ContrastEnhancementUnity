using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtConstraint : MonoBehaviour
{
    public Transform lookAtObject;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(lookAtObject,lookAtObject.transform.up);
    }
}
