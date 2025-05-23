using System;
using System.Collections;
using System.Collections.Generic;
using RosSharp.RosBridgeClient;
using UnityEngine;


public class DataManager : MonoBehaviour
{
    
    public class Data
    {
        public string DataName;
        public float Value;
    }
    
    // Start is called before the first frame update
    public static DataManager Instance { get; private set; }
    public GameObject rosConnector;
    public GameObject endUI;
    public int collisionCount = 0;
    public int targetNum = 0;
    public float alpha = 0;
    public int collectCount = 0;
    public bool taskFinished = false;
    public bool taskStart = false;
    public bool reachGoal = false;
    public bool isWithAssitanceCues = false;
    public bool isWithEnvironmentCues = false;
    public bool isWithGuidanceForce = false;
    public bool isWithRepulsiveForce = false;
    public string modeName = null;
    public bool nav_vel_signal = false;
    public List<Data> Records = new List<Data>();
    
    [Header("Seconds")]
    public float taskTime = 300f;

    [Header("Mode")] 
    public bool isTeleop;
    public bool isAuto;
    public bool isFullControl;
    
    private bool locker = false;
    private float time_temp = 0f;

    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        time_temp = taskTime;
    }
    
    private void StatusUpdate()
    {
        isTeleop = rosConnector.GetComponent<IsTelePublisher>().sharedControl;
        isAuto = rosConnector.GetComponent<IsAutoPublisher>().autoOperation;
        isFullControl = rosConnector.GetComponent<IsFullControlPublisher>().fullControl;
        alpha = rosConnector.GetComponent<GetAlphaValue>().GetValue();
    }
    private void Update()
    {
        StatusUpdate();
        TimeCount();
        if (taskFinished && !locker)
        {
            // when task is finished, show the end UI
            HandleData();
        }else if (collectCount == targetNum && !locker)
        {
            Debug.Log(222);
            HandleData();
            taskFinished = true;
        }
    }
    private void HandleData()
    {
        Records.Add(new Data(){DataName = "Collision", Value = collisionCount});
        Records.Add(new Data(){DataName = "Target", Value = collectCount});
        Records.Add(new Data(){DataName = "Time", Value = time_temp-taskTime});
        endUI.SetActive(true);
        rosConnector.SetActive(false);  
        locker = true;
    }
    public void StartTask()
    {
        taskStart = true;
    }

    public void TimeCount()
    {
        //set the time to 0 when the task is finished
        if (taskTime > 0 && taskStart && !taskFinished)
        {
            taskTime -= Time.deltaTime;
        }
        else if (taskTime <= 0)
        {
            taskFinished = true;
            
        }
    }

    public void ModeA()
    {
        //only shared control without any cues
        modeName = "ModeA";
        isWithAssitanceCues = false;
        isWithEnvironmentCues = false;
        isWithRepulsiveForce = false;
        isWithGuidanceForce = false;
    }
    public void ModeB()
    {
        modeName = "ModeB";
        isWithAssitanceCues = false;
        isWithEnvironmentCues = true;
        
        isWithRepulsiveForce = true;
        isWithGuidanceForce = false;
    }
    public void ModeC()
    {
        modeName = "ModeC";
        isWithAssitanceCues = true;
        isWithEnvironmentCues = false;
        
        isWithGuidanceForce = true;
        isWithRepulsiveForce = false;
    }
    public void ModeD()
    {
        modeName = "ModeD";
        isWithGuidanceForce = true;
        isWithRepulsiveForce = true;
        isWithAssitanceCues = true;
        isWithEnvironmentCues = true;
    }
    
}
