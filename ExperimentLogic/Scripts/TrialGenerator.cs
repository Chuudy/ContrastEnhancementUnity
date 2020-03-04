using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialGenerator : MonoBehaviour
{
    [Header("Prefabs and materials")]
    public GameObject player;
    public GameObject instancedObject;
    public Material SkyBoxMaterial;
    private Material SkyBoxMaterialTmp;
    public Material StimuliMaterial;

    [Header("Stimuli transform options")]
    public Vector2 distanceRange = new Vector2(5f, 10f);
    public Vector2 scaleRange = new Vector2(0.8f,1.2f);
    public float angleBetweenCenters = 30;
    public float baseDistance;
    public float secondObjectDistance;
    [Range(0.001f, 0.2f)]
    public float dioptricDistnaceDifference = 0.1f;

    [Header("Stimuli contrast options")]
    [Range(0f, 1f)]
    public float backgroundValue = 0.5f;
    [Range(0f, 0.5f)]
    public float contrast = 0.1f;

    [Header("Modes")]
    public bool perceptualyEqualizeSize = false;
    public bool removeParalax = false;


    private List<GameObject> instances;
    private Vector3 initialPlayerDirection;
    private Vector3 initialPlayerPosition;



    // Start is called before the first frame update
    void Start()
    {
        SkyBoxMaterialTmp = new Material(SkyBoxMaterial);
        RenderSettings.skybox = SkyBoxMaterialTmp;

        instances = new List<GameObject>();
        initialPlayerDirection = player.transform.forward;
        initialPlayerPosition = player.transform.position;
        CreateInstances();
    }

    // Update is called once per frame
    void Update()
    {
        secondObjectDistance = 1 / (1f / baseDistance - dioptricDistnaceDifference);

        Vector3 tmpDirecrtion;

        if (removeParalax)
            tmpDirecrtion = player.transform.forward;
        else
            tmpDirecrtion = initialPlayerDirection;

        instances[0].transform.position = initialPlayerPosition + Quaternion.AngleAxis(-angleBetweenCenters/2, Vector3.up) * tmpDirecrtion * baseDistance;
        secondObjectDistance = 1 / (1f / baseDistance - dioptricDistnaceDifference);
        instances[1].transform.position = initialPlayerPosition + Quaternion.AngleAxis(angleBetweenCenters/2, Vector3.up) * tmpDirecrtion * secondObjectDistance;

        if(perceptualyEqualizeSize)
            instances[1].transform.localScale = instances[0].transform.localScale * (secondObjectDistance / baseDistance);

        ChangeContrastParameters();
    }

    void CreateInstances()
    {
        Vector3 position;
        Quaternion rotation;
        GameObject instance;
        LookAtConstraint constraint;

        baseDistance = Random.Range(distanceRange.x, distanceRange.y);
        position = initialPlayerPosition + initialPlayerDirection * baseDistance;
        rotation = Quaternion.identity;

        instance = Instantiate(instancedObject, position, rotation);
        constraint = instance.GetComponent(typeof(LookAtConstraint)) as LookAtConstraint;
        constraint.lookAtObject = player.transform;
        instances.Add(instance);
        

        secondObjectDistance = 1 / (1f / baseDistance + dioptricDistnaceDifference);
        position = initialPlayerPosition + initialPlayerDirection * secondObjectDistance;
        rotation = Quaternion.identity;

        instance = Instantiate(instancedObject, position, rotation);
        constraint = instance.GetComponent(typeof(LookAtConstraint)) as LookAtConstraint;
        constraint.lookAtObject = player.transform;
        instances.Add(instance);

    }

    void ChangeContrastParameters()
    {

        SkyBoxMaterialTmp.SetFloat("_BaseValue", backgroundValue);
        StimuliMaterial.SetFloat("_BaseValue", backgroundValue);
        StimuliMaterial.SetFloat("_Contrast", contrast);        
    }
}
