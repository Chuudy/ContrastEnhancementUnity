using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class Experiment : MonoBehaviour
{
    public enum stage
    {
        Depth,
        Appearance
    }

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
    float notificationCurrentTime = 0;
    public float notificationTime = 2;


    public int nTrials; 
    private int counter = 0;
    private int modeChangeCounter = 0;

    private bool experimentStarted = false;

    [Header("Experiment settings")]
    public int repetitions = 5;

    [Header("Output")]
    public string filepath = "results.csv";


    [Header("Debug info")]
    public EnhancemenVsMode mode = EnhancemenVsMode.NONE;
    public stage currentStage;

    ContrastEnhancementWolski enhancementScript;
    ContrastEnhancementWanat enhancementScriptWanat;

    // epxperiment aux
    Vector3 startPosition;
    int stageNumber = 0;
    float deltaTime;

    List<TrialGenerator> trialGenerators;
    TrialGenerator currentTrialGenerator;


    // Start is called before the first frame update
    void Start()
    {
        CreateTrialGenerators();

        playerObject.transform.position = StartLocation.transform.position;
        playerObject.transform.rotation = StartLocation.transform.rotation;

        player = Valve.VR.InteractionSystem.Player.instance;

        CreateResultsFile();
        Debug.Log(Animator.StringToHash(SystemInfo.deviceName));
        //observerId = Animator.StringToHash(SystemInfo.deviceName).ToString();

        enhancementScript = GetComponent<ContrastEnhancementWolski>() as ContrastEnhancementWolski;
        enhancementScriptWanat = GetComponent<ContrastEnhancementWanat>() as ContrastEnhancementWanat;
        nTrials = repetitions * locations.Count;
        //SetNextLocation(false);

        ChangeModeHint();
    }

    // Update is called once per frame
    void Update()
    {
        notificationCurrentTime += Time.deltaTime;
        if (notificationCurrentTime >= notificationTime)
            notificationCanvas.active = false;


        if (experimentStarted)
        {
            deltaTime += Time.deltaTime;

            if (SteamVR_Input.GetStateDown("Teleport", SteamVR_Input_Sources.RightHand))
            {
                modeChangeCounter++;
                enhancementScript.toggle = !enhancementScript.toggle;
                if (mode == EnhancemenVsMode.WANAT)
                    enhancementScriptWanat.toggle = !enhancementScript.toggle;
                else
                    enhancementScriptWanat.toggle = false;
            }

            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.RightHand))
            {
                if (modeChangeCounter == 0)
                {
                    notificationCanvas.active = true;
                    notificationCurrentTime = 0;
                    return;
                }

                WriteAnswer();
                SetNextLocation();
            }

            //if (Input.GetKeyDown(KeyCode.Z))
            //    enhancementScript.toggle = !enhancementScript.toggle;

            //if (Input.GetKeyDown(KeyCode.X))
            //{
            //    WriteAnswer();
            //    SetNextLocation();
            //}
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

        Condition wanatConditionDepth = new Condition("wanat", repetitions, EnhancemenVsMode.WANAT, PreferenceMode.DEPTH);
        depthConditions.Add(wanatConditionDepth);

        Condition noEnhancementConditionDepth = new Condition("noenhancement", repetitions, EnhancemenVsMode.NONE, PreferenceMode.DEPTH);
        depthConditions.Add(noEnhancementConditionDepth);

        TrialGenerator depthTrialGen = new TrialGenerator(depthConditions, locations, PreferenceMode.DEPTH);
        trialGenerators.Add(depthTrialGen);


        //APPEARANCE
        List<Condition> appearanceConditions = new List<Condition>();

        Condition wanatConditionAppearance = new Condition("wanat", repetitions, EnhancemenVsMode.WANAT, PreferenceMode.APPEARANCE);
        appearanceConditions.Add(wanatConditionAppearance);

        Condition noEnhancementConditionAppearance = new Condition("noenhancement", repetitions, EnhancemenVsMode.NONE, PreferenceMode.APPEARANCE);
        appearanceConditions.Add(noEnhancementConditionAppearance);

        TrialGenerator appearanceTrialGen = new TrialGenerator(appearanceConditions, locations, PreferenceMode.APPEARANCE);
        trialGenerators.Add(appearanceTrialGen);

        //Generate trials
        foreach (TrialGenerator generator in trialGenerators)
            generator.CreateTrials();

        //Random selection of the first mode
        trialGenerators.Shuffle();
        currentTrialGenerator = trialGenerators[0];

        //Debug
        Debug.Log(currentTrialGenerator.PreferenceMode);
        Debug.Log(currentTrialGenerator.GetNumberOfTrials());
    }

    void Tutorial()
    {
        if (instructions[8].active)
            return;

        if (instructions[5].active || instructions[7].active)
        {
            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
            {
                instructions[5].active = false;
                instructions[7].active = false;
                StartExperiment();
            }
        }

        if (instructions[4].active || instructions[6].active)
        {
            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
            {
                instructions[4].active = false;
                instructions[6].active = false;
                StartExperiment();
            }
        }

        if (instructions[3].active)
        {
            if (SteamVR_Input.GetStateDown("InteractUI", SteamVR_Input_Sources.Any))
            {
                instructions[3].active = false;
                if(currentStage == stage.Depth)
                    instructions[4].active = true;
                else if (currentStage == stage.Appearance)
                    instructions[6].active = true;
            }
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
        }

        if (instructions[1].active)
        {
            if (SteamVR_Input.GetStateDown("Teleport", SteamVR_Input_Sources.Any))
            {
                instructions[1].active = false;
                instructions[2].active = true;
                enhancementScript.toggle = !enhancementScript.toggle;
                if (mode == EnhancemenVsMode.WANAT)
                    enhancementScriptWanat.toggle = !enhancementScript.toggle;
                else
                    enhancementScriptWanat.toggle = false;
                ConfirmHint();
            }
        }

        if (instructions[0].active)
        {
            if (showChangeModeHint && SteamVR_Input.GetStateDown("Teleport", SteamVR_Input_Sources.Any))
            {
                CancelChanegModeHint();
                showChangeModeHint = false;
                instructions[0].active = false;
                instructions[1].active = true;
                enhancementScript.toggle = !enhancementScript.toggle;
                if (mode == EnhancemenVsMode.WANAT)
                    enhancementScriptWanat.toggle = !enhancementScript.toggle;
                else
                    enhancementScriptWanat.toggle = false;
            }
        }
    }

    void SetNextLocation(bool increment = true)
    {
        if (increment)
            counter++;

        if (counter == nTrials)
        {
            ChangePhase();
            return;
        }

        int index = counter % locations.Count;
        playerObject.transform.position = locations[index].transform.position;
        SetRandomRotation();
        RandomToggleEnhancementMode();

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

    void SetRandomRotation()
    {
        Quaternion randomRotation = Random.rotation;
        Vector3 randomRotationEuler = randomRotation.eulerAngles;
        randomRotation = Quaternion.Euler(0, randomRotationEuler.y, 0);
        playerObject.transform.rotation = randomRotation;
    }

    void RandomToggleEnhancementMode()
    {
        enhancementScript.toggle = RandomBoolean();
        if (mode == EnhancemenVsMode.WANAT)
            enhancementScriptWanat.toggle = !enhancementScript.toggle;
        else
            enhancementScriptWanat.toggle = false;
    }

    bool RandomBoolean()
    {
        return (Random.value > 0.5f);
    }

    void WriteAnswer()
    {
        AddRecord();
    }
    
    public void AddRecord()
    {
        string phaseS;
        if (currentStage == stage.Depth)
            phaseS = "depth";
        else
            phaseS = "appear";

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, true))
        {
            file.WriteLine(observerId + "," + phaseS + "," + (counter % locations.Count).ToString() + "," + (enhancementScript.toggle).ToString() + "," + deltaTime.ToString());
        }
        deltaTime = 0;
        modeChangeCounter = 0;
    }

    void CreateResultsFile()
    {
        if (!System.IO.File.Exists(filepath))
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, false))
            {
                file.WriteLine("id,phase,location,answer,time");
            }
        }
    }

    public void StartExperiment()
    {
        experimentStarted = true;
        SetNextLocation(false);
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

    void ChangePhase()
    {
        if(stageNumber == 2)
        {
            //UnityEditor.EditorApplication.isPlaying = false;
            Application.Quit();
        }

        if(stageNumber == 1)
        {
            playerObject.transform.position = StartLocation.transform.position;
            playerObject.transform.rotation = StartLocation.transform.rotation;
            experimentStarted = false;
            instructions[8].active = true;
            return;
        }


        counter = 0;
        playerObject.transform.position = StartLocation.transform.position;
        playerObject.transform.rotation = StartLocation.transform.rotation;

        if (currentStage == stage.Appearance)
        {
            currentStage = stage.Depth;
            instructions[7].active = true;
        }
        else if(currentStage == stage.Depth)
        {
            currentStage = stage.Appearance;
            instructions[5].active = true;
        }
        
        experimentStarted = false;

        stageNumber++;
    }

}





class TrialGenerator
{
    List<Condition> conditions;
    List<GameObject> locations;
    PreferenceMode preferenceMode;
    
    List<Trial> trials;

    Trial currentTrial;

    bool hasExperiemntStarted = false;
    bool hasExperiemntFinsihed = false;

    public bool HasExperiemntStarted { get => hasExperiemntStarted; }
    public bool HasExperiemntFinsihed { get => hasExperiemntFinsihed; }
    public PreferenceMode PreferenceMode { get => preferenceMode; }

    public TrialGenerator(List<Condition> conditions, List<GameObject> locations, PreferenceMode preferenceMode)
    {
        this.conditions = conditions;
        this.locations = locations;
        this.preferenceMode = preferenceMode;
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
    }

    public void StartExperiment()
    {
        hasExperiemntStarted = true;
        currentTrial = trials[0];
        currentTrial.StartTrial();
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
        currentTrial.StartTrial();
    }

    public int GetNumberOfTrials()
    {
        return trials.Count;
    }
}





class Trial
{
    Condition condition;
    int locationNumber;
    GameObject location;

    public Trial()
    {

    }

    public Trial(Condition condition, GameObject location)
    {
        this.condition = condition;
        this.location = location;
    }

    public void StartTrial()
    {
        condition.StartCondition();
    }

    public void SwapEnhancement()
    {
        condition.SwapEnhancement();
    }
}






class Condition
{
    private string conditionName;
    private EnhancemenVsMode enhancementMode;
    private PreferenceMode prefenrenceMode;
    private bool wolskiOn;
    private int repetitions;

    ContrastEnhancementWolski wolskiEnhancement;
    ContrastEnhancementWanat wanatEnhancement;

    public Condition()
    {
    }

    public Condition(string conditionName, int repetitions, EnhancemenVsMode enhancementMode, PreferenceMode prefenrenceMode)
    {
        this.conditionName = conditionName;
        this.repetitions = repetitions;
        this.enhancementMode = enhancementMode;
        this.prefenrenceMode = prefenrenceMode;
    }

    public int Repetitions { get => repetitions; }

    public void StartCondition()
    {
        wolskiOn = System.Convert.ToBoolean(Random.Range(0, 2));
    }

    public void SwapEnhancement()
    {
        wolskiOn = !wolskiOn;
        SetEnhancements();
    }

    private void SetEnhancements()
    {
        wolskiEnhancement.toggle = wolskiOn;

        switch (enhancementMode)
        {
            case EnhancemenVsMode.NONE:
                wanatEnhancement.toggle = false;
                break;
            case EnhancemenVsMode.WANAT:
                wanatEnhancement.toggle = !wolskiOn;
                break;
            default:
                break;
        }
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
}







public enum EnhancemenVsMode
{
    NONE,
    WANAT
};

public enum PreferenceMode
{
    DEPTH,
    APPEARANCE
};
