using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stage
{
    public List<GameObject> stageObjects = new List<GameObject>();
    public bool             isDone = false;
    public string           name = string.Empty;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Timer))]
public class StageSystem : MonoBehaviour
{
    public int CurrentStageIndex
    {
        get { return _stageListIndex; }
        private set { _stageListIndex = value; }
    }

    [Header("Stages")]
    public List<Stage> stagesList = new List<Stage>();

    [Header("Current Stage")]
    public Stage currentStage = null;
    public Timer timer;

    [SerializeField] private int _stageListIndex = 0;
    [SerializeField] private int _listPassed = 0;

    [Header("Events")]
    public List<Action> onTimerStart = new List<Action>();
    public List<Action> onTimerEnd = new List<Action>();

    protected void Start()
    {
        Debug.Log($"[StageSystem] Stage system is started...");

        //Disable all scenario packs
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("ScenarioPack"))
            go.SetActive(false);

        timer = GetComponent<Timer>();
        timer.StartEvent += OnTimerStart;
        timer.EndEvent += OnTimerEnd;        
        timer.StartTimer();
    }

    public void AddedPass()
    {
        if (currentStage != null)
        {
            _listPassed++;
            Debug.Log($"[StageSystem] Added pass | Count {currentStage.stageObjects.Count} Index{_stageListIndex} Passes {_listPassed}");
            //If all scenario lists passed we are passed this stage
            if (_listPassed == currentStage.stageObjects.Count)
                StagePassed();
        }
    }

    private void StagePassed()
    {
        Debug.Log($"[StageSystem] Stage passed! index: {_stageListIndex}");

        currentStage.isDone = true;

        foreach (GameObject go in currentStage.stageObjects)
            go.SetActive(false);

        _stageListIndex++;
        _listPassed = 0;

        if(stagesList.IndexOf(currentStage) == (stagesList.Count - 1))
        {
            //TODO: Show player result screen or main menu

            //This is part when we are finish every stages
            //Basically, this is the game end 
            currentStage = null;
            return;
        }

        timer.ResetTimer();
        timer.StartTimer();
    }

    public void OnTimerStart()
    {        
        foreach (Stage st in stagesList)
        {
            Debug.Log($"[StageSystem] Choice stage... index: {stagesList.IndexOf(st)} stage objects count:{st.stageObjects.Count}");
            //If current stage is passed we are go to another
            if (st.isDone)
                continue;            
            currentStage = st;   
            break;
        }

        //Invoke action when timer is runned
        foreach (Action action in onTimerStart)
            action?.Invoke();
    }

    //Dynamicly add pack and activate it
    public void AddToStage(GameObject go)
    {
        currentStage.stageObjects.Add(go);
        Debug.Log($"[StageSystem] Added | Last Index:{currentStage.stageObjects.Count}");
        Activate(go);
    }

    //Activate existing pack
    public void Activate(GameObject go)
    {
        PathSystem ps = null;
        if (go.GetType() == typeof(GameObject))
            ps = go.GetComponentInChildren<PathSystem>(); //Legacy pack style        
        else
            ps = go.GetComponent<PathSystem>();

        Debug.Assert(ps);        

        //Enable Game Object
        go.SetActive(true);

        //Enable spawner in PathSystem
        ps?.EnableSpawner();

        Debug.Log($"[StageSystem] Activate | Index:{currentStage.stageObjects.IndexOf(go)}");
    }

    public void OnTimerEnd()
    {
        //Invoke action when timer is ended
        foreach (Action action in onTimerEnd)
            action?.Invoke();

        if (currentStage != null)
        {
            if (currentStage.isDone)
                return;

            //Remove all null objects 
            currentStage.stageObjects.RemoveAll(o => o == null);

            Debug.Log($"[StageSystem] Try to start another stage Current: {_stageListIndex}");

            //If we don't have any objects, we are skip this stage
            if (currentStage.stageObjects.Count == 0)
            {
                Debug.Log($"[StageSystem] Skip {_stageListIndex}");
                StagePassed();
                return;
            }

            //Activate objects in pack
            foreach (GameObject go in currentStage.stageObjects.ToList())
                Activate(go);
        }
        else
        {
            Debug.LogError($"Current stage is null | Stage list index:{_stageListIndex}");
        }
    }

}
