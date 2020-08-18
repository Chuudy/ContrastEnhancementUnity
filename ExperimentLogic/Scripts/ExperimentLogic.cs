using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ExperimentLogic : MonoBehaviour
{
    public Transform playerCamera;
    public float startingDioptricDistance = 0.5f;

    public int numberOfTrialsPerCondition = 20;
    private int numberOfConditions = 3;
    
    private List<Condition> conditions;
    private List<Condition> trials;
    
    private int currentTrial = 0;

    // Start is called before the first frame update
    void Start()
    {
        SelectConditions();

        PopulateTrials();

        for (int i = 0; i < trials.Count; i++)
        {
            Debug.Log(trials[i].conditionName);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Here add conditions manually
    void SelectConditions() 
    {
        conditions = new List<Condition>();

        Condition noEnhancement = new Condition(startingDioptricDistance, "no enhancement");
        conditions.Add(noEnhancement);

        Condition wanatEnhancement = new Condition(startingDioptricDistance, "wanat");
        conditions.Add(wanatEnhancement);

        Condition wolskiEnhancement = new Condition(startingDioptricDistance, "wolski");
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


}

class Condition
{
    public string conditionName;
    string resultsFilename;
    float currentDioptricDistance;

    public Condition(float startDioptricDistance, string conditionName)
    {
        this.currentDioptricDistance = startDioptricDistance;
        this.conditionName = conditionName;
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


class Trial
{

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