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

    [Header("Enhancements")]
    public ContrastEnhancementWanat wanatEnhancement;
    public ContrastEnhancementWolski wolskiEnhancement;

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

    [Header("Experiment sewttings")]
    public int numberOfTrialsPerCondition = 20;
    public string resultsFilename = "results.csv";
    public int blankScreenTime = 1;
    public int reversalsToTerminate = 20;
    public float dioptricDistanceStep = 0.005f;


    private List<Condition> conditions;
    private List<Condition> trials;

    private TrialGen trialGen;

    bool inputBlocked;

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
        if (inputBlocked)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            trialGen.StartExperiment();
            NextTrial();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            trialGen.CheckAnswer(0);
            NextTrial();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            trialGen.CheckAnswer(1);
            NextTrial();
        }
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
        Condition noEnhancement = new Condition(startingDioptricDistance, dioptricDistanceStep, "no enhancement");
        conditions.Add(noEnhancement);

        ////cond 1
        Condition wanatEnhancement = new Condition(startingDioptricDistance, dioptricDistanceStep, "wanat", true, false);
        conditions.Add(wanatEnhancement);

        ////cond 2
        Condition wolskiEnhancement = new Condition(startingDioptricDistance, dioptricDistanceStep, "wolski", false, true);
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
        trialGen = new TrialGen(playerCamera, instancedObject, conditions, reversalsToTerminate);
        trialGen.blankScreenTime = blankScreenTime;
        trialGen.wanatEnhancement = wanatEnhancement;
        trialGen.wolskiEnhancement = wolskiEnhancement;
    }

    void NextTrial()
    {
        LockInput();
        Invoke("RunTrial", blankScreenTime);
        Invoke("UnlockInput", blankScreenTime);
    }

    void RunTrial()
    {
        trialGen.NextTrial(baseDistance, angleBetweenCenters);
    }

    void LockInput()
    {
        inputBlocked = true;
    }

    void UnlockInput()
    {
        inputBlocked = false;
    }
}







class TrialGen : MonoBehaviour
{
    private Transform playerCamera;
    private GameObject instancedObject;

    private List<Condition> conditions;
    private int reversalsToTerminate;

    public ContrastEnhancementWanat wanatEnhancement;
    public ContrastEnhancementWolski wolskiEnhancement;

    public float blankScreenTime;

    private List<GameObject> instances;

    private int closerIndex;
    private float baseDistance;
    private float angle;
    private float furtherObjectDistance;

    private Condition currentCondition;

    private bool hasExperimentStarted = false;
    private bool hasExperimentFinished = false;

    public TrialGen(Transform playerCamera, GameObject instancedObject, List<Condition> conditions, int reversalsToTerminate)
    {
        instances = new List<GameObject>();

        this.playerCamera = playerCamera;
        this.instancedObject = instancedObject;
        this.conditions = conditions;
        this.reversalsToTerminate = reversalsToTerminate;
    }

    public void StartExperiment()
    {
        hasExperimentStarted = true;
    }

    public void NextTrial(float baseDistance, float angle)
    {
        if (!hasExperimentStarted || hasExperimentFinished)
            return;

        this.baseDistance = baseDistance;
        this.angle = angle;

        // Select random condition
        int conditionIndex = UnityEngine.Random.Range(0, conditions.Count);
        currentCondition = conditions[conditionIndex];
        Debug.Log("Current condition: " + currentCondition.conditionName);

        SetEnhancements();

        closerIndex = (int)Mathf.Round(UnityEngine.Random.value);
        float closerAngleFactor = closerIndex * 2 - 1;

        Vector3 forwardDirection = new Vector3(0, 0, 1);
        Vector3 origin = new Vector3(0, 0, 0);
        Vector3 position = new Vector3(0, 0, 0);
        Quaternion rotation = Quaternion.identity;
        GameObject instance;

        for (int i = 0; i < 2; i++)
        {
            float angleFactor = i * 2 - 1;
            Vector3 direction = Quaternion.Euler(0, angleFactor * angle / 2, 0) * forwardDirection;

            float distance = baseDistance;

            if (i != closerIndex)
            {
                distance = 1 / (1f / baseDistance - currentCondition.CurrentDioptricDistance);
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
    }

    void SetEnhancements()
    {
        this.wanatEnhancement.toggle = currentCondition.WanatEnhancement;
        this.wolskiEnhancement.toggle = currentCondition.WolskiEnhancement;
    }


    public void CheckAnswer(int answer)
    {
        if (!hasExperimentStarted || hasExperimentFinished)
            return;

        currentCondition.CheckAnswer(answer == closerIndex);

        DeleteStimuliIfExists();
        DeleteCurrentConditionIfFinished();
    }

    void DeleteStimuliIfExists()
    {
        if (instances.Count == 0)
            return;

        foreach (GameObject instance in instances)
        {
            GameObject.Destroy(instance);
        }
        instances.Clear();
    }

    void DeleteCurrentConditionIfFinished()
    {        
        Debug.Log("Condition: " + currentCondition.conditionName + " - " + currentCondition.CurrentReversalsNumber.ToString() + " out of " + reversalsToTerminate);
        if (currentCondition.CurrentReversalsNumber >= reversalsToTerminate)
        {
            conditions.Remove(currentCondition);
        }
        ChechIfExperimentIsFinished();
        Debug.Log("Number of conditions: " + conditions.Count.ToString());
    }

    void ChechIfExperimentIsFinished()
    {
        if (conditions.Count == 0)
        {
            hasExperimentFinished = true;
            DeleteStimuliIfExists();
        }
    }
}









class Condition
{
    public string conditionName;
    string resultsFilename;
    float distanceStep;
    float currentDioptricDistance;
    readonly bool wanatEnhancement = false;
    readonly bool wolskiEnhancement = false;

    private int currentCorrectAnswersNumber = 0;
    private int currentReversalsNumber = 0;
    private bool lastAnswer;

    public bool WanatEnhancement { get => wanatEnhancement; }
    public bool WolskiEnhancement { get => wolskiEnhancement; }
    public float CurrentDioptricDistance { get => currentDioptricDistance; }
    public int CurrentReversalsNumber { get => currentReversalsNumber; }

    public Condition(float startDioptricDistance, float distanceStep, string conditionName, bool wanatEnhancement = false, bool wolskiEnhancement = false)
    {
        this.currentDioptricDistance = startDioptricDistance;
        this.distanceStep = distanceStep;
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

    public void CheckAnswer(bool isAnswerCorrect)
    {
        UpdateReversalParameters(isAnswerCorrect);
    }

    void UpdateReversalParameters(bool isAnswerCorrect)
    {
        if (isAnswerCorrect)
        {
            Debug.Log("Correct");
            currentCorrectAnswersNumber++;
            if (currentCorrectAnswersNumber % 2 == 0)
                ChangeDistance(-1);
        }
        else
        {
            Debug.Log("Incorrect");
            currentCorrectAnswersNumber = 0;
            ChangeDistance(1);
        }

        if (isAnswerCorrect != lastAnswer)
            currentReversalsNumber++;

        lastAnswer = isAnswerCorrect;
    }

    public void ChangeDistance(int direction)
    {
        currentDioptricDistance = Math.Max(currentDioptricDistance + direction * distanceStep, 0.0000f);
        return;
    }

    void WriteRecordToFile()
    {

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