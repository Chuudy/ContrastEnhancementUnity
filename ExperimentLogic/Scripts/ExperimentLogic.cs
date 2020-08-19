using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ExperimentLogic : MonoBehaviour
{
    [Header("Prefabs and materials")]
    public Transform playerCamera;
    public GameObject instancedObject;
    public Material SkyBoxMaterial;
    private Material SkyBoxMaterialTmp;
    public Material StimuliMaterial;

    [Header("Stimuli contrast options")]
    [Range(0f, 1f)]
    public float backgroundValue = 0.5f;
    [Range(0f, 0.5f)]
    public float contrast = 0.1f;

    [Header("Stimuli transform options")]
    public float angleBetweenCenters = 30;
    public float baseDistance = 5;
    [Range(0.001f, 0.2f)]
    public float startingDioptricDistance = 0.1f;

    public int numberOfTrialsPerCondition = 20;
    private int numberOfConditions = 3;
    
    private List<Condition> conditions;
    private List<Condition> trials;

    private TrialGen trialGen;
    
    private int currentTrial = 0;

    // Start is called before the first frame update
    void Start()
    {
        InitializeMaterials();
        InitializeConditionsandTrials();

        CreateTrialGenerator();
    }

    // Update is called once per frame
    void Update()
    {
        KeyboardInput();
        UpdateMaterials();
    }

    void KeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            trialGen.NextTrial(baseDistance, angleBetweenCenters);            
    }

    void InitializeConditionsandTrials()
    {
        SelectConditions();
        PopulateTrials();
    }

    // Here add conditions manually
    void SelectConditions() 
    {
        conditions = new List<Condition>();

        //cond 0
        Condition noEnhancement = new Condition(startingDioptricDistance, "no enhancement");
        conditions.Add(noEnhancement);

        //cond 1
        Condition wanatEnhancement = new Condition(startingDioptricDistance, "wanat", true, false);
        conditions.Add(wanatEnhancement);

        //cond 2
        Condition wolskiEnhancement = new Condition(startingDioptricDistance, "wolski", false, true);
        conditions.Add(wolskiEnhancement);
    }

    void PopulateTrials()
    {
        trials = new List<Condition>();

        for (int i = 0; i < conditions.Count; i++)
        {
            for (int j = 0; j < numberOfTrialsPerCondition; j++)
            {
                Condition tmpCondtion = conditions[i];
                trials.Add(tmpCondtion);
            }
        }

        trials.Shuffle();
    }

    void InitializeMaterials()
    {
        SkyBoxMaterialTmp = new Material(SkyBoxMaterial);
        RenderSettings.skybox = SkyBoxMaterialTmp;
    }

    void UpdateMaterials()
    {
        SkyBoxMaterialTmp.SetFloat("_BaseValue", backgroundValue);
        StimuliMaterial.SetFloat("_BaseValue", backgroundValue);
        StimuliMaterial.SetFloat("_Contrast", contrast);
    }

    void CreateTrialGenerator()
    {
        Debug.Log(trials.Count);
        trialGen = new TrialGen(playerCamera, instancedObject, trials);
    }
}

class Condition
{
    public string conditionName;
    string resultsFilename;
    float currentDioptricDistance;
    readonly bool wanatEnhancement = false;
    readonly bool wolskiEnhancement = false;

    public bool WanatEnhancement { get => wanatEnhancement; }
    public bool WolskiEnhancement { get => wolskiEnhancement; }
    public float CurrentDioptricDistance { get => currentDioptricDistance; }

    public Condition(float startDioptricDistance, string conditionName, bool wanatEnhancement = false, bool wolskiEnhancement = false)
    {
        this.currentDioptricDistance = startDioptricDistance;
        this.conditionName = conditionName;
        this.wanatEnhancement = wanatEnhancement;
        this.wolskiEnhancement = wolskiEnhancement;
    }

    public Condition(Condition _condition)
    {
        this.conditionName = _condition.conditionName;
        this.resultsFilename = _condition.resultsFilename;
        this.currentDioptricDistance = _condition.currentDioptricDistance;
    }

    public void ChangeDistance(bool correct)
    {
        return;
    }

    void WriteRecordToFile()
    {

    }
}

class TrialGen : MonoBehaviour
{
    private Transform playerCamera;
    private GameObject instancedObject;

    private List<Condition> trials;
    private int currentTrialIndex = 0;

    private List<GameObject> instances;

    private int closerIndex;
    private float baseDistance;
    private float furtherObjectDistance;

    public TrialGen(Transform playerCamera, GameObject instancedObject, List<Condition> trials)
    {
        instances = new List<GameObject>();

        this.playerCamera = playerCamera;
        this.instancedObject = instancedObject;
        this.trials = trials;
    }

    public int NextTrial(float baseDistance, float angle)
    {
        DeleteExistingStimuliIfExists();

        //Check if there is any trial left
        if (currentTrialIndex == trials.Count)
            return -1;

        Condition currentTrial = trials[currentTrialIndex];

        this.baseDistance = baseDistance;

        closerIndex = (int)Mathf.Round(UnityEngine.Random.value);
        float closerAngleFactor = closerIndex * 2 - 1;

        Vector3 initialPlayerDirection = playerCamera.transform.forward;
        Vector3 initialPlayerUpVector = playerCamera.transform.up;
        Vector3 initialPlayerPosition = playerCamera.transform.position;
        initialPlayerPosition = new Vector3(0, 0, 0);
        initialPlayerDirection = new Vector3(0, 0, 1);

        Vector3 origin = new Vector3(0, 0, 0);
        Vector3 position = new Vector3(0, 0, 0);
        Quaternion rotation = Quaternion.identity;
        GameObject instance;

        for (int i = 0; i < 2; i++)
        {
            float angleFactor = i * 2 - 1;
            Vector3 direction = Quaternion.Euler(0, angleFactor * angle / 2, 0) * initialPlayerDirection;

            float distance = baseDistance;

            if (i != closerIndex)
            {
                distance = 1 / (1f / baseDistance + currentTrial.CurrentDioptricDistance);
                furtherObjectDistance = distance;
            }

            position = origin + direction * distance;
            instance = Instantiate(instancedObject, position, rotation);
            instance.transform.LookAt(position);

            if (i != closerIndex)
            {
                instance.transform.localScale = instance.transform.localScale * (furtherObjectDistance / baseDistance);
            }

            instance.transform.SetParent(playerCamera, false);
            instance.transform.Rotate(new Vector3(0, 0, 1), UnityEngine.Random.Range(0, 360));
            instance.transform.Rotate(new Vector3(1, 0, 0), 180);
            instances.Add(instance);
        }

        currentTrialIndex++;
        return 0;
    }

    void DeleteExistingStimuliIfExists()
    {
        if (instances.Count == 0)
            return;

        foreach (GameObject instance in instances)
        {
            GameObject.Destroy(instance);
        }
        instances.Clear();
    }
}


// Shuffle extension for the List
static class MyExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}