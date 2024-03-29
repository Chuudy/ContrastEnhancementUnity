﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;
using Valve.VR;

public class ExperimentLogic : MonoBehaviour
{
    [Header("Prefabs and materials")]
    public string participantID;

    [Header("Prefabs and materials")]
    public Transform playerCamera;
    public GameObject instancedObject;
    public GameObject[] instancedObjects;
    public Material SkyBoxMaterial;
    private Material SkyBoxMaterialTmp;
    public Material StimuliMaterial;

    [Header("Enhancements")]
    public ContrastEnhancementWanat wanatEnhancement;
    public ContrastEnhancementWolski wolskiEnhancement;

    [Header("UI")]
    public GameObject feedBackObjectCorrect;
    public GameObject feedBackObjectInCorrect;
    public GameObject Instructions;
    public GameObject InstructionsExperiment;
    public GameObject Ending;
    public GameObject ProgresbarSlider;
    public GameObject PorgressbarText;

    [Header("Stimuli contrast options")]
    [Range(0f, 1f)]
    public float backgroundValue = 0.5f;
    [Range(0f, 0.5f)]
    public float contrast = 0.1f;

    [Header("Stimuli transform options")]
    public float angleBetweenCenters = 30;
    public float baseDistance = 5;

    [Header("Experiment settings")]
    public int reversalsToTerminate = 20;
    public int trialsToTerminate = 50;
    [Range(0.001f, 0.2f)]
    public float startingDioptricDistanceDifference = 0.1f;
    public float dioptricDistanceStep = 0.005f;
    public int blankScreenTime = 1;
    public int sanityCheckTrials = 10;

    [Header("Results output")]
    public string resultsFilename = "results.csv";


    private List<Condition> conditions;
    private List<Condition> trials;

    private TrialGen trialGen;
    private TrialGen warmupTrialGen;
    private TrialGen currentTrialGen;
    private float totalNumberOfReversals;

    bool inputBlocked;

    public GameObject testObject;

    // Start is called before the first frame update
    void Start()
    {
        //if (UnityEngine.XR.XRDevice.model != "Index")
        //    QuitExperiment();

        FileWriter.SetParticipantId(participantID);

        InitializeMaterials();
        InitializeConditions();

        InitializeWarmupConditions();

        CreateTrialGenerator();

        currentTrialGen = warmupTrialGen;

        FileWriter.CreateResultsFile(resultsFilename);

        totalNumberOfReversals = conditions.Count * reversalsToTerminate;

    }

    // Update is called once per frame
    void Update()
    {
        KeyboardInput();
        UpdateMaterials();
        currentTrialGen.Update();
        SetProgressFeedback(currentTrialGen.ComputeProgress());

        if (currentTrialGen.HasExperimentFinished)
        {
            if(currentTrialGen.InstanceName == trialGen.InstanceName)
                ShowEnding();
            else
            {
                ShowExperimentStart();
                currentTrialGen = trialGen;
            }
        }
    }

    void KeyboardInput()
    {
        if (inputBlocked)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || SteamVR_Input.GetStateDown("Teleport", SteamVR_Input_Sources.RightHand))
        {
            currentTrialGen.StartExperiment();
            HideInstructions();
            NextTrial();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.LeftHand))
        {
            currentTrialGen.CheckAnswer(0);
            ShowFeedback(currentTrialGen.Answer);
            NextTrial();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.RightHand))
        {
            currentTrialGen.CheckAnswer(1);
            ShowFeedback(currentTrialGen.Answer);
            NextTrial();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitExperiment();
        }
    }
    // Here add conditions manually
    void InitializeConditions() 
    {
        conditions = new List<Condition>();

        //cond 0
        Condition noEnhancement = new Condition(startingDioptricDistanceDifference, dioptricDistanceStep, baseDistance, "no enhancement");
        noEnhancement.SetMaxTrialsAndReversals(trialsToTerminate, reversalsToTerminate);
        conditions.Add(noEnhancement);

        ////cond 1
        Condition wanatEnhancement = new Condition(startingDioptricDistanceDifference, dioptricDistanceStep, baseDistance, "wanat", true, false);
        wanatEnhancement.SetMaxTrialsAndReversals(trialsToTerminate, reversalsToTerminate);
        conditions.Add(wanatEnhancement);

        //cond 2
        Condition wolskiEnhancement = new Condition(startingDioptricDistanceDifference, dioptricDistanceStep, baseDistance, "wolski", false, true);
        wolskiEnhancement.SetMaxTrialsAndReversals(trialsToTerminate, reversalsToTerminate);
        conditions.Add(wolskiEnhancement);

        //cond 3
        Condition foo = new Condition(0.19f, 0, baseDistance, "foo");
        foo.SetMaxTrialsAndReversals(sanityCheckTrials, sanityCheckTrials);
        conditions.Add(foo);
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
        trialGen = new TrialGen("main", playerCamera, instancedObject, conditions, reversalsToTerminate);
        trialGen.blankScreenTime = blankScreenTime;
        trialGen.wanatEnhancement = wanatEnhancement;
        trialGen.wolskiEnhancement = wolskiEnhancement;
        trialGen.instancedObjects = instancedObjects;
    }

    void InitializeWarmupConditions()
    {
        List<Condition> conditions = new List<Condition>();

        //cond 3
        Condition warmupCondition = new Condition(0.19f, 0, baseDistance, "warmup", false, true);
        warmupCondition.SetMaxTrialsAndReversals(sanityCheckTrials, sanityCheckTrials);
        conditions.Add(warmupCondition);

        warmupTrialGen = new TrialGen("warmup", playerCamera, null, conditions, 10);
        warmupTrialGen.blankScreenTime = blankScreenTime;
        warmupTrialGen.wanatEnhancement = wanatEnhancement;
        warmupTrialGen.wolskiEnhancement = wolskiEnhancement;
        warmupTrialGen.instancedObjects = instancedObjects;
    }


    void NextTrial()
    {
        LockInput();
        Invoke("RunTrial", blankScreenTime);
        Invoke("UnlockInput", blankScreenTime);
    }

    void RunTrial()
    {
        currentTrialGen.NextTrial(baseDistance, angleBetweenCenters);
    }

    void LockInput()
    {
        inputBlocked = true;
    }

    void UnlockInput()
    {
        inputBlocked = false;
    }

    void ShowFeedback(bool answer)
    {
        if (currentTrialGen.HasExperimentFinished || !currentTrialGen.HasExperimentStarted)
            return;

        if (answer)
            feedBackObjectCorrect.active = true;
        else
            feedBackObjectInCorrect.active = true;


        Invoke("RemoveFeedback", 0.75f);
    }

    void RemoveFeedback()
    {
        feedBackObjectCorrect.active = false;
        feedBackObjectInCorrect.active = false;
    }

    void HideInstructions()
    {
        Instructions.active = false;
        InstructionsExperiment.active = false;
    }

    void ShowEnding()
    {
        Ending.active = true;
    }

    private void ShowExperimentStart()
    {
        InstructionsExperiment.active = true;
    }

    void SetProgressFeedback(float value)
    {
        int valueInt = (int)(value*100);
        ProgresbarSlider.GetComponent<Slider>().value = value;
        PorgressbarText.GetComponent<Text>().text = valueInt.ToString() + " %";
    }

    void QuitExperiment()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}







class TrialGen : MonoBehaviour
{
    private string instanceName;

    private Transform playerCamera;
    private GameObject instancedObject;
    public GameObject[] instancedObjects;
    private List<GameObject> objectsToInstance;

    private List<Condition> conditions;
    private List<Condition> finishedConditions;
    private int reversalsToTerminate;
    private int trialsToTerminate;

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

    private float timer;

    private bool answer;

    private float totalNumberOfReversals;
    private float currentNumberOfReversals;

    public bool Answer { get => answer; }
    public bool HasExperimentFinished { get => hasExperimentFinished; }
    public bool HasExperimentStarted { get => hasExperimentStarted; }
    public string InstanceName { get => instanceName; }

    public TrialGen(string instanceName, Transform playerCamera, GameObject instancedObject, List<Condition> conditions, int reversalsToTerminate)
    {
        this.instanceName = instanceName;

        instances = new List<GameObject>();
        objectsToInstance = new List<GameObject>();
        finishedConditions = new List<Condition>();

        this.playerCamera = playerCamera;
        this.instancedObject = instancedObject;
        this.conditions = conditions;
        this.reversalsToTerminate = reversalsToTerminate;

        this.totalNumberOfReversals = conditions.Count * reversalsToTerminate;
        this.currentNumberOfReversals = 0;
    }

    public void Update()
    {
        timer += Time.deltaTime;
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
        SetRandomGameObjects();

        closerIndex = (int)Mathf.Round(UnityEngine.Random.value);
        Debug.Log("Closer index: " + closerIndex.ToString());
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
            instance = Instantiate(objectsToInstance[i], position, rotation);
            instance.transform.LookAt(position);

            if (i != closerIndex)
            {
                instance.transform.localScale = instance.transform.localScale * (furtherObjectDistance / baseDistance);
            }

            instance.transform.SetParent(playerCamera, false);
            instance.transform.Rotate(new Vector3(0, 0, 1), UnityEngine.Random.Range(0, 360));
            //instance.transform.Rotate(new Vector3(1, 0, 0), 180);
            instances.Add(instance);
        }

        Debug.Log(furtherObjectDistance);
        ZerOutTimer();
    }

    void SetEnhancements()
    {
        this.wanatEnhancement.toggle = currentCondition.WanatEnhancement;
        this.wolskiEnhancement.toggle = currentCondition.WolskiEnhancement;
    }


    public void CheckAnswer(int leftOrRight)
    {
        if (!hasExperimentStarted || hasExperimentFinished)
            return;

        answer = (leftOrRight == closerIndex);
        currentCondition.CheckAnswer(answer);

        FileWriter.AddRecord(currentCondition.conditionName, GetDioptricDistance(), answer, timer);

        DeleteStimuliIfExists();
        DeleteCurrentConditionIfFinished();
        ComputeCurrentNumberOfReversals();
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
        if (currentCondition.IsConditionFinished())
        {
            finishedConditions.Add(currentCondition);
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

    void ZerOutTimer()
    {
        timer = 0;
    }

    float GetDioptricDistance()
    {
        float baseDistanceDiopter = 1 / baseDistance;
        float furtherDistanceDiopter = 1 / furtherObjectDistance;
        return baseDistanceDiopter - furtherDistanceDiopter;
    }

    void SetRandomGameObjects()
    {
        if (objectsToInstance.Count != 0)
            objectsToInstance.Clear();

        int baseIndex = UnityEngine.Random.Range(0, 3);
        int furtherIndex = UnityEngine.Random.Range(0, 3);
        while(furtherIndex == baseIndex)
        {
            furtherIndex = UnityEngine.Random.Range(0, 3);
        }

        objectsToInstance.Add(instancedObjects[baseIndex]);
        objectsToInstance.Add(instancedObjects[furtherIndex]);
    }

    void ComputeCurrentNumberOfReversals()
    {
        float reversals = 0;
        foreach (Condition condition in conditions)
        {
            reversals += condition.CurrentReversalsNumber;
        }
        while (reversals < currentNumberOfReversals)
            reversals += reversalsToTerminate;

        currentNumberOfReversals = reversals;
    }

    public float ComputeProgress()
    {
        float totalNumberOfTrials = 0;
        float numberOfFinishedTrials = 0;

        foreach (Condition condition in conditions)
        {
            totalNumberOfTrials += condition.TrialsToTerminate;
            numberOfFinishedTrials += condition.CurrentTrialNumber;
        }

        foreach (Condition condition in finishedConditions)
        {
            totalNumberOfTrials += condition.TrialsToTerminate;
            numberOfFinishedTrials += condition.CurrentTrialNumber;
        }

        return numberOfFinishedTrials / totalNumberOfTrials;
    }

    public float GetProgressValue()
    {


        return currentNumberOfReversals / totalNumberOfReversals;
    }
}









class Condition
{
    public string conditionName;
    string resultsFilename;
    float distanceStep;
    float currentDioptricDistanceDifference;
    readonly bool wanatEnhancement = false;
    readonly bool wolskiEnhancement = false;

    private int currentCorrectAnswersNumber = 0;
    private int currentReversalsNumber = 0;
    private int currentTrialNumber = 0;
    private int reversalsToTerminate;
    private int trialsToTerminate;
    private bool lastAnswer;
    private float baseDistance;

    public bool WanatEnhancement { get => wanatEnhancement; }
    public bool WolskiEnhancement { get => wolskiEnhancement; }
    public float CurrentDioptricDistance { get => currentDioptricDistanceDifference; }
    public int CurrentReversalsNumber { get => currentReversalsNumber; }
    public int CurrentTrialNumber { get => currentTrialNumber; }
    public int ReversalsToTerminate { get => reversalsToTerminate; }
    public int TrialsToTerminate { get => trialsToTerminate; }

    public Condition(float startDioptricDistance, float distanceStep, float baseDistance, string conditionName, bool wanatEnhancement = false, bool wolskiEnhancement = false)
    {
        this.currentDioptricDistanceDifference = startDioptricDistance;
        this.distanceStep = distanceStep;
        this.conditionName = conditionName;
        this.wanatEnhancement = wanatEnhancement;
        this.wolskiEnhancement = wolskiEnhancement;
    }

    public Condition(Condition _condition)
    {
        this.conditionName = _condition.conditionName;
        this.resultsFilename = _condition.resultsFilename;
        this.currentDioptricDistanceDifference = _condition.currentDioptricDistanceDifference;
    }

    public void SetMaxTrialsAndReversals(int trials, int reversals)
    {
        this.reversalsToTerminate = reversals;
        this.trialsToTerminate = trials;
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
        currentTrialNumber++;
    }

    public void ChangeDistance(int direction)
    {
        currentDioptricDistanceDifference = Math.Min(Math.Max((float)System.Math.Round(System.Convert.ToDouble(currentDioptricDistanceDifference + direction * distanceStep), 3), 0.0000f), 1/baseDistance - distanceStep);
        return;
    }

    public void FinishCondition(int reversals, int trialsNumber)
    {
        this.currentTrialNumber = trialsNumber;
        this.currentReversalsNumber = reversals;
    }

    public bool IsConditionFinished()
    {
        if (currentReversalsNumber >= reversalsToTerminate || currentTrialNumber >= trialsToTerminate)
        {
            FinishCondition(reversalsToTerminate, trialsToTerminate);
            return true;
        }
        else
            return false;
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

static class FileWriter
{
    static string participantId;
    static string resultsFilename;

    static FileWriter()
    {
        participantId = SystemInfo.deviceName.GetHashCode().ToString();
        //participantId = "adwait";
    }

    public static void CreateResultsFile(string resultsFilename)
    {
        SetResultsFilename(resultsFilename);

        if (!System.IO.File.Exists(resultsFilename))
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(resultsFilename, false))
            {
                file.WriteLine("id,condition,distance,answer,time");
            }
        }
    }

    public static void AddRecord(string conditionName, float dioptricDistnaceDifference, bool answer, float time)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(resultsFilename, true))
        {
            file.WriteLine(participantId + "," + conditionName + "," + dioptricDistnaceDifference + "," + answer.ToString() + "," + time.ToString());
        }
    }

    public static void SetResultsFilename(string filename)
    {
        resultsFilename = filename;
    }

    public static void SetParticipantId(string participantID)
    {
        participantId = participantID;
    }
}