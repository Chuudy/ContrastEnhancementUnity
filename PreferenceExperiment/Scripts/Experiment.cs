using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.UI;

public class Experiment : MonoBehaviour
{
    public enum stage
    {
        Depth,
        Appearance
    }

    [Header("Participant")]
    public string participantID = "id";


    [Header("Game objects")]
    public GameObject playerObject;

    Valve.VR.InteractionSystem.Player player;


    private Coroutine hintCoroutine = null;
    bool showConfrimHint = false;
    bool showChangeModeHint = false;

    private string observerId;


    [Header("Teleportation")]
    public GameObject StartLocation;
    public List<GameObject> locations;

    [Header("UI")]
    public GameObject[] instructions;
    public GameObject notificationCanvas;
    public GameObject boundary;
    public GameObject progressBarSlider;
    public GameObject progressBarText;
    public GameObject taskText;
    float notificationCurrentTime = 0;
    public float notificationTime = 2;

    private bool experimentStarted = false;

    [Header("Experiment settings")]
    public int repetitions = 5;

    [Header("Output")]
    public string filepath = "results.csv";

    ContrastEnhancementWolski enhancementScriptWolski;
    ContrastEnhancementWanat enhancementScriptWanat;
    WeakendStereo monoRendering;
    UglyImage uglyImage;

    // epxperiment aux
    Vector3 startPosition;
    int stageNumber = 0;

    List<TrialGenerator> trialGenerators;
    TrialGenerator currentTrialGenerator;
    private bool experimentFinished;

    private float NumberOfAllTrials;
    private float NumberOfFinishedTrials;

    // Start is called before the first frame update
    void Start()
    {
        FileWriter.CreateResultsFile(filepath);
        FileWriter.SetParticipantId(participantID);

        enhancementScriptWolski = GetComponent<ContrastEnhancementWolski>() as ContrastEnhancementWolski;
        enhancementScriptWanat = GetComponent<ContrastEnhancementWanat>() as ContrastEnhancementWanat;
        monoRendering = GetComponent<WeakendStereo>() as WeakendStereo;
        uglyImage = GetComponent<UglyImage>() as UglyImage;

        CreateTrialGenerators();
        ResetInstructions();
        instructions[0].active = true;
        boundary.active = true;

        playerObject.transform.position = StartLocation.transform.position;
        playerObject.transform.rotation = StartLocation.transform.rotation;

        player = Valve.VR.InteractionSystem.Player.instance;
        
        Debug.Log(Animator.StringToHash(SystemInfo.deviceName));
        //observerId = Animator.StringToHash(SystemInfo.deviceName).ToString();
        //SetNextLocation(false);

        ChangeModeHint();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitExperiment();
        }

        currentTrialGenerator.Update();
        ChangeTrialGenerator();

        notificationCurrentTime += Time.deltaTime;
        if (notificationCurrentTime >= notificationTime)
            notificationCanvas.active = false;


        if (currentTrialGenerator.HasExperiemntStarted)
        {
            if (experimentFinished)
                return;

            if (SteamVR_Input.GetStateDown("Teleport", SteamVR_Input_Sources.RightHand))
            {
                currentTrialGenerator.SwapEnhancement();
            }

            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.RightHand))
            {
                if (currentTrialGenerator.Swaps == 0)
                {
                    notificationCanvas.active = true;
                    notificationCurrentTime = 0;
                    return;
                }

                currentTrialGenerator.WriteAnswerToFile();
                currentTrialGenerator.NextTrial();
                SetProgressFeedback();
            }            
        } 
        else
        {
            Tutorial();
        }
    }

    // Manually set Conditions here
    void CreateTrialGenerators()
    {
        trialGenerators = new List<TrialGenerator>();

        //DEPTH
        List<Condition> depthConditions = new List<Condition>();

        Condition wanatConditionDepth = new Condition("wanat", repetitions, EnhancemenVsMode.WANAT, PreferenceMode.DEPTH, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        depthConditions.Add(wanatConditionDepth);

        Condition noEnhancementConditionDepth = new Condition("noEnhancement", repetitions, EnhancemenVsMode.NONE, PreferenceMode.DEPTH, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        depthConditions.Add(noEnhancementConditionDepth);

        Condition fooDepth = new Condition("fooDepth", 1, EnhancemenVsMode.FOODEPTH, PreferenceMode.DEPTH, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        depthConditions.Add(fooDepth);

        TrialGenerator depthTrialGen = new TrialGenerator(playerObject, depthConditions, locations, PreferenceMode.DEPTH);
        trialGenerators.Add(depthTrialGen);



        //DEPTH WARMUP
        List<Condition> warmupDepthConditions = new List<Condition>();

        Condition warmupDepth = new Condition("warmup", 1, EnhancemenVsMode.FOODEPTH, PreferenceMode.DEPTH, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        warmupDepthConditions.Add(warmupDepth);

        TrialGenerator warmupDepthTrialGen = new TrialGenerator(playerObject, warmupDepthConditions, locations, PreferenceMode.DEPTH);
        //trialGenerators.Add(warmupDepthTrialGen);



        //APPEARANCE
        List<Condition> appearanceConditions = new List<Condition>();

        Condition wanatConditionAppearance = new Condition("wanat", repetitions, EnhancemenVsMode.WANAT, PreferenceMode.APPEARANCE, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        appearanceConditions.Add(wanatConditionAppearance);

        Condition noEnhancementConditionAppearance = new Condition("noEnhancement", repetitions, EnhancemenVsMode.NONE, PreferenceMode.APPEARANCE, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        appearanceConditions.Add(noEnhancementConditionAppearance);

        Condition fooAppear = new Condition("fooAppear", 1, EnhancemenVsMode.FOOAPPEAR, PreferenceMode.APPEARANCE, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        appearanceConditions.Add(fooAppear);

        TrialGenerator appearanceTrialGen = new TrialGenerator(playerObject, appearanceConditions, locations, PreferenceMode.APPEARANCE);
        trialGenerators.Add(appearanceTrialGen);



        //DEPTH WARMUP
        List<Condition> warmupAppearConditions = new List<Condition>();

        Condition warmupAppear = new Condition("warmup", 1, EnhancemenVsMode.FOOAPPEAR, PreferenceMode.APPEARANCE, enhancementScriptWanat, enhancementScriptWolski, monoRendering, uglyImage);
        warmupAppearConditions.Add(warmupAppear);

        TrialGenerator warmupAppearTrialGen = new TrialGenerator(playerObject, warmupAppearConditions, locations, PreferenceMode.APPEARANCE);
        //trialGenerators.Add(warmupAppearTrialGen);



        //Random selection of the first mode
        trialGenerators.Shuffle();

        //Inject warmup sessions;
        if(trialGenerators[0].PreferenceMode == PreferenceMode.DEPTH)
        {
            trialGenerators.Insert(0, warmupDepthTrialGen);
            trialGenerators.Insert(2, warmupAppearTrialGen);
        }
        else
        {
            trialGenerators.Insert(0, warmupAppearTrialGen);
            trialGenerators.Insert(2, warmupDepthTrialGen);
        }

        currentTrialGenerator = trialGenerators[0];

        //Generate trials
        foreach (TrialGenerator generator in trialGenerators)
        {
            generator.CreateTrials();
            generator.PrintOutConditionName();
        }
    }

    void ResetInstructions()
    {
        foreach (GameObject gameObject in instructions)
        {
            gameObject.active = false;
        }
        boundary.active = false;
    }

    void ResetEnhancements()
    {
        enhancementScriptWanat.toggle = false;
        enhancementScriptWolski.toggle = false;
        uglyImage.toggle = false;
        monoRendering.toggle = false;
    }

    void Tutorial()
    {
        if (instructions[8].active)
            return;

        //if (instructions[5].active || instructions[7].active)
        //{
        //    if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
        //    {
        //        instructions[5].active = false;
        //        instructions[7].active = false;
        //        ResetInstructions();
        //        currentTrialGenerator.StartExperiment();
        //    }
        //}

        //if (instructions[4].active || instructions[6].active)
        //{
        //    if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
        //    {
        //        instructions[4].active = false;
        //        instructions[6].active = false;
        //        ResetInstructions();
        //        currentTrialGenerator.StartExperiment();
        //    }
        //}

        if(instructions[4].active || instructions[5].active || instructions[6].active || instructions[7].active || instructions[9].active || instructions[10].active)
        {
            boundary.active = true;
            SetProgressFeedback();
            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
            {
                ResetInstructions();
                currentTrialGenerator.StartExperiment();
                SetProgressFeedback();
            }
            return;
        }

        if (instructions[3].active)
        {
            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
            {
                if (currentTrialGenerator.PreferenceMode == PreferenceMode.DEPTH)
                    instructions[4].active = true;
                else if (currentTrialGenerator.PreferenceMode == PreferenceMode.APPEARANCE)
                    instructions[6].active = true;
            }
            return;
        }

        if (instructions[2].active)
        {
            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
            {
                CancelConfirmHint();
                showConfrimHint = false;
                instructions[2].active = false;
                instructions[3].active = true;
            }
            return;
        }

        if (instructions[1].active)
        {
            if (SteamVR_Input.GetStateDown("Teleport", SteamVR_Input_Sources.Any))
            {
                instructions[1].active = false;
                instructions[2].active = true;
                //enhancementScriptWolski.toggle = !enhancementScriptWolski.toggle;
                //enhancementScriptWanat.toggle = !enhancementScriptWolski.toggle;
                uglyImage.toggle = false;
                ConfirmHint();
            }
            return;
        }

        if (instructions[0].active)
        {
            if (showChangeModeHint && SteamVR_Input.GetStateDown("Teleport", SteamVR_Input_Sources.Any))
            {
                CancelChanegModeHint();
                showChangeModeHint = false;
                instructions[0].active = false;
                instructions[1].active = true;
                //enhancementScriptWolski.toggle = !enhancementScriptWolski.toggle;
                //enhancementScriptWanat.toggle = !enhancementScriptWolski.toggle;
                uglyImage.toggle = true;
            }
            return;
        }
    }

    void ConfirmHint()
    {
        showConfrimHint = true;
        hintCoroutine = StartCoroutine(ConfirmHintCoroutine());
    }

    void ChangeModeHint()
    {
        showChangeModeHint = true;
        hintCoroutine = StartCoroutine(ChangeModeHintCoroutine());
    }

    private IEnumerator ConfirmHintCoroutine()
    {
        float prevBreakTime = Time.time;
        float prevHapticPulseTime = Time.time;

        while (true)
        {
            bool pulsed = false;

            //Show the hint on each eligible hand
            foreach (Valve.VR.InteractionSystem.Hand hand in player.hands)
            {
                //bool showHint = IsEligibleForTeleport(hand);
                bool showHint = showConfrimHint;
                bool isShowingHint = !string.IsNullOrEmpty(Valve.VR.InteractionSystem.ControllerButtonHints.GetActiveHintText(hand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI")));
                if (showHint)
                {
                    if (!isShowingHint)
                    {
                        Valve.VR.InteractionSystem.ControllerButtonHints.ShowTextHint(hand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI"), "Confirm");
                        prevBreakTime = Time.time;
                        prevHapticPulseTime = Time.time;
                    }

                    if (Time.time > prevHapticPulseTime + 0.05f)
                    {
                        //Haptic pulse for a few seconds
                        pulsed = true;

                        hand.TriggerHapticPulse(500);
                    }
                }
                else if (!showHint && isShowingHint)
                {
                    Valve.VR.InteractionSystem.ControllerButtonHints.HideTextHint(hand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI"));
                }
            }

            if (Time.time > prevBreakTime + 3.0f)
            {
                //Take a break for a few seconds
                yield return new WaitForSeconds(3.0f);

                prevBreakTime = Time.time;
            }

            if (pulsed)
            {
                prevHapticPulseTime = Time.time;
            }

            yield return null;
        }
    }

    public void CancelConfirmHint()
    {
        if (hintCoroutine != null)
        {
            Valve.VR.InteractionSystem.ControllerButtonHints.HideTextHint(player.leftHand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI"));
            Valve.VR.InteractionSystem.ControllerButtonHints.HideTextHint(player.rightHand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI"));

            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        //CancelInvoke("ShowTeleportHint");
    }

    private IEnumerator ChangeModeHintCoroutine()
    {
        float prevBreakTime = Time.time;
        float prevHapticPulseTime = Time.time;

        while (true)
        {
            bool pulsed = false;

            //Show the hint on each eligible hand
            foreach (Valve.VR.InteractionSystem.Hand hand in player.hands)
            {
                //bool showHint = IsEligibleForTeleport(hand);
                bool showHint = showChangeModeHint;
                bool isShowingHint = !string.IsNullOrEmpty(Valve.VR.InteractionSystem.ControllerButtonHints.GetActiveHintText(hand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport")));
                if (showHint)
                {
                    if (!isShowingHint)
                    {
                        Valve.VR.InteractionSystem.ControllerButtonHints.ShowTextHint(hand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport"), "Change Mode");
                        prevBreakTime = Time.time;
                        prevHapticPulseTime = Time.time;
                    }

                    if (Time.time > prevHapticPulseTime + 0.05f)
                    {
                        //Haptic pulse for a few seconds
                        pulsed = true;

                        hand.TriggerHapticPulse(500);
                    }
                }
                else if (!showHint && isShowingHint)
                {
                    Valve.VR.InteractionSystem.ControllerButtonHints.HideTextHint(hand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport"));
                }
            }

            if (Time.time > prevBreakTime + 3.0f)
            {
                //Take a break for a few seconds
                yield return new WaitForSeconds(3.0f);

                prevBreakTime = Time.time;
            }

            if (pulsed)
            {
                prevHapticPulseTime = Time.time;
            }

            yield return null;
        }
    }

    public void CancelChanegModeHint()
    {
        if (hintCoroutine != null)
        {
            Valve.VR.InteractionSystem.ControllerButtonHints.HideTextHint(player.leftHand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport"));
            Valve.VR.InteractionSystem.ControllerButtonHints.HideTextHint(player.rightHand, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport"));

            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        //CancelInvoke("ShowTeleportHint");
    }
    
    void ChangeTrialGenerator()
    {
        if (!currentTrialGenerator.HasExperiemntFinsihed)
            return;

        ResetEnhancements();

        playerObject.transform.position = StartLocation.transform.position;
        playerObject.transform.rotation = StartLocation.transform.rotation;

        if (trialGenerators.IndexOf(currentTrialGenerator) == trialGenerators.Count-1)
        {
            instructions[8].active = true;
            experimentFinished = true;
            boundary.active = true;
            return;
        }

        currentTrialGenerator = trialGenerators[trialGenerators.IndexOf(currentTrialGenerator) + 1];

        switch (currentTrialGenerator.PreferenceMode)
        {
            case PreferenceMode.DEPTH:
                if(trialGenerators.IndexOf(currentTrialGenerator) == 1 || trialGenerators.IndexOf(currentTrialGenerator) == 3)
                    instructions[9].active = true;
                else
                    instructions[5].active = true;
                break;
            case PreferenceMode.APPEARANCE:
                if (trialGenerators.IndexOf(currentTrialGenerator) == 1 || trialGenerators.IndexOf(currentTrialGenerator) == 3)
                    instructions[10].active = true;
                else
                    instructions[7].active = true;
                break;
            default:
                break;
        }
    }

    float GetProgress()
    {
        NumberOfAllTrials = 0;
        NumberOfFinishedTrials = 0;

        foreach (TrialGenerator generator in trialGenerators)
        {
            NumberOfAllTrials += generator.GetNumberOfTrials();
            NumberOfAllTrials -= 1;
            NumberOfFinishedTrials += generator.GetNumberOfFinishedTrials();
        }

        float progress = (NumberOfFinishedTrials / NumberOfAllTrials);

        Debug.Log("Progress: " + ((int)(progress*100)).ToString() + "%");

        return progress;
    }

    void SetProgressFeedback()
    {
        progressBarSlider.active = true;
        progressBarText.active = true;
        taskText.active = true;


        float value = GetProgress();
        int valueInt = (int)(value * 100);
        progressBarSlider.GetComponent<Slider>().value = value;
        progressBarText.GetComponent<Text>().text = valueInt.ToString() + " %";
        if(currentTrialGenerator.PreferenceMode == PreferenceMode.DEPTH)
            taskText.GetComponent<Text>().text = "Task: Depth";
        else
            taskText.GetComponent<Text>().text = "Task: Appearance";
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





class TrialGenerator
{
    GameObject playerObject;
    List<Condition> conditions;
    List<GameObject> locations;
    PreferenceMode preferenceMode;
    
    List<Trial> trials;

    Trial currentTrial;

    bool hasExperiemntStarted = false;
    bool hasExperiemntFinsihed = false;

    float timer = 0;
    int swaps = 0;

    public bool HasExperiemntStarted { get => hasExperiemntStarted; }
    public bool HasExperiemntFinsihed { get => hasExperiemntFinsihed; }
    public PreferenceMode PreferenceMode { get => preferenceMode; }
    public int Swaps { get => swaps; }

    public TrialGenerator(GameObject playerObject, List<Condition> conditions, List<GameObject> locations, PreferenceMode preferenceMode)
    {
        this.playerObject = playerObject;
        this.conditions = conditions;
        this.locations = locations;
        this.preferenceMode = preferenceMode;
    }

    public void Update()
    {
        timer += Time.deltaTime;
    }

    public void CreateTrials()
    {
        trials = new List<Trial>();

        foreach (GameObject location in locations)
        {
            foreach (Condition condition in conditions)
            {
                for (int i = 0; i < condition.Repetitions; i++)
                {
                    Trial tmp = new Trial(condition, location);
                    trials.Add(tmp);
                }
            }
        }

        trials.Shuffle();

        currentTrial = trials[0];
    }

    public void StartExperiment()
    {
        if (hasExperiemntFinsihed)
            return;

        hasExperiemntStarted = true;
        currentTrial = trials[0];

        Debug.Log(PreferenceMode);
        currentTrial.StartTrial(playerObject);

        ZeroOutTimer();
        ZeroOutSwaps();
    }

    public void NextTrial()
    {
        if (!hasExperiemntStarted || hasExperiemntFinsihed)
            return;

        int newTrialIndex = trials.IndexOf(currentTrial) + 1;

        if(newTrialIndex == trials.Count)
        {
            hasExperiemntFinsihed = true;
            return;
        }

        currentTrial = trials[newTrialIndex];
        currentTrial.StartTrial(playerObject);

        ZeroOutTimer();
        ZeroOutSwaps();
    }

    public int GetNumberOfTrials()
    {
        return trials.Count;
    }

    public int GetNumberOfFinishedTrials()
    {
        return trials.IndexOf(currentTrial);
    }

    public void SwapEnhancement()
    {
        if (!hasExperiemntStarted || hasExperiemntFinsihed)
            return;

        swaps++;
        
        currentTrial.SwapEnhancement();
    }

    public void ZeroOutTimer()
    {
        timer = 0;
    }

    public void ZeroOutSwaps()
    {
        swaps = 0;
    }

    public void WriteAnswerToFile()
    {
        if (!hasExperiemntStarted || hasExperiemntFinsihed)
            return;

        ConditionData data = currentTrial.GetConditionData();
        FileWriter.AddRecord(data.preferenceMode.ToString(), data.conditionName, locations.IndexOf(currentTrial.Location), data.better, timer, swaps);
    }

    public void PrintOutConditionName()
    {
        Debug.Log(conditions[0].ConditionName.ToString() + "   " + conditions[0].PrefenrenceMode.ToString());
    }
}





class Trial
{
    Condition condition;
    int locationNumber;
    GameObject location;

    public GameObject Location { get => location; }

    public Trial()
    {

    }

    public Trial(Condition condition, GameObject location)
    {
        this.condition = condition;
        this.location = location;
    }

    public void StartTrial(GameObject playerObject)
    {
        TeleportPlayer(playerObject);
        SetRandomPlayerRotation(playerObject);
        condition.StartCondition();
    }

    public void SwapEnhancement()
    {
        condition.SwapEnhancement();
    }

    private void TeleportPlayer(GameObject playerObject)
    {
        playerObject.transform.position = location.transform.position;
    }

    private void SetRandomPlayerRotation(GameObject playerObject)
    {
        Quaternion randomRotation = Random.rotation;
        Vector3 randomRotationEuler = randomRotation.eulerAngles;
        randomRotation = Quaternion.Euler(0, randomRotationEuler.y, 0);
        playerObject.transform.rotation = randomRotation;
    }

    public ConditionData GetConditionData()
    {
        return condition.GetConditionData();
    }
}






class Condition
{
    private string conditionName;
    private EnhancemenVsMode enhancementMode;
    private PreferenceMode preferenceMode;
    private bool better;
    private int repetitions;

    ContrastEnhancementWolski wolskiEnhancement;
    ContrastEnhancementWanat wanatEnhancement;
    WeakendStereo monoRendering;
    UglyImage uglyImage;

    public Condition()
    {
    }

    public Condition(string conditionName, int repetitions, EnhancemenVsMode enhancementMode, PreferenceMode preferenceMode, ContrastEnhancementWanat wanat, ContrastEnhancementWolski wolski, WeakendStereo monoRendering, UglyImage uglyImage)
    {
        this.conditionName = conditionName;
        this.repetitions = repetitions;
        this.enhancementMode = enhancementMode;
        this.preferenceMode = preferenceMode;
        this.wanatEnhancement = wanat;
        this.wolskiEnhancement = wolski;
        this.monoRendering = monoRendering;
        this.uglyImage = uglyImage;
    }

    public ConditionData GetConditionData()
    {
        ConditionData data = new ConditionData(conditionName, enhancementMode, preferenceMode, better);
        return data;
    }

    public int Repetitions { get => repetitions; }
    public string ConditionName { get => conditionName; }
    public EnhancemenVsMode EnhancementMode { get => enhancementMode; }
    public PreferenceMode PrefenrenceMode { get => preferenceMode; }
    public bool Better { get => better; }

    public void StartCondition()
    {
        better = System.Convert.ToBoolean(Random.Range(0, 2));
        Debug.Log(enhancementMode);
        SetEnhancements();
    }

    public void SwapEnhancement()
    {
        better = !better;
        SetEnhancements();
    }

    private void SetEnhancements()
    {
        Debug.Log(better);
        switch (enhancementMode)
        {
            case EnhancemenVsMode.NONE:
                wolskiEnhancement.toggle = better;
                wanatEnhancement.toggle = false;
                monoRendering.toggle = false;
                uglyImage.toggle = false;
                break;
            case EnhancemenVsMode.WANAT:
                wolskiEnhancement.toggle = better;
                wanatEnhancement.toggle = !better;
                monoRendering.toggle = false;
                uglyImage.toggle = false;
                break;
            case EnhancemenVsMode.FOODEPTH:
                wolskiEnhancement.toggle = false;
                wanatEnhancement.toggle = false;
                monoRendering.toggle = !better;
                uglyImage.toggle = false;
                break;
            case EnhancemenVsMode.FOOAPPEAR:
                wolskiEnhancement.toggle = false;
                wanatEnhancement.toggle = false;
                monoRendering.toggle = false;
                uglyImage.toggle = !better;
                break;
            default:
                wolskiEnhancement.toggle = false;
                wanatEnhancement.toggle = false;
                monoRendering.toggle = false;
                uglyImage.toggle = false;
                break;
        }
    }

    public void SetOption(bool option)
    {
        better = option;
    }
}




struct ConditionData
{
    public string conditionName;
    public EnhancemenVsMode enhancementMode;
    public PreferenceMode preferenceMode;
    public bool better;

    public ConditionData(string conditionName, EnhancemenVsMode enhancementMode, PreferenceMode preferenceMode, bool better)
    {
        this.conditionName = conditionName;
        this.enhancementMode = enhancementMode;
        this.preferenceMode = preferenceMode;
        this.better = better;
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
    }

    public static void CreateResultsFile(string resultsFilename)
    {
        SetResultsFilename(resultsFilename);

        if (!System.IO.File.Exists(resultsFilename))
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(resultsFilename, false))
            {
                file.WriteLine("id,mode,condition,location,answer,time,swaps");
            }
        }
    }

    public static void AddRecord(string modeName, string conditionName, int locationIndex,  bool answer, float time, int swaps)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(resultsFilename, true))
        {
            file.WriteLine(participantId + "," + modeName + "," + conditionName + "," + locationIndex.ToString() + "," + answer.ToString() + "," + time.ToString() + "," + swaps.ToString());
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







public enum EnhancemenVsMode
{
    NONE,
    WANAT,
    FOOAPPEAR,
    FOODEPTH
};

public enum PreferenceMode
{
    DEPTH,
    APPEARANCE
};
