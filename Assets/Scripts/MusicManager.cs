/*
Music Manager
Autumn Moulios
Last Updated 07/24/2021

Reads information from the FMOD timeline to  
create events that are on beat with the music.

References:
https://www.youtube.com/watch?v=hNQX1fsQL4Q
https://www.fmod.com/resources/documentation-unity?version=2.01&page=examples-timeline-callbacks.html
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMODUnity;
using UnityEngine;
//using JsonUtility;

public class MusicManager : MonoBehaviour {
    
    // Script instances
    public static MusicManager instance;
    private PlayerInput input;
    private FishHandler fish;

    [SerializeField]
    [EventRef]
    private string music = null;

    // FMOD Variables
    private FMOD.Studio.EventInstance musicInstance;
    private FMOD.Studio.EVENT_CALLBACK beatCallback;
    public TimelineInfo timelineInfo = null;
    private GCHandle timelineHandle; 

    private MarkerInfo markerInfo;   // class instance for dumping JSON info into
    public TextAsset markerInfoJSON; // where is FMOD's full marker info stored?
    private string currentMarker;

    private List<Marker> Fish;      // defines upcoming fish
    private List<string> directions;
    private int fishMarkerNum = 0;  // position in Fish list
    private List<Marker> Notes;     // notes that the player must hit during gameplay
    private int inputMarkerNum = 0; // position in Notes List


    #region Marker Info
    [Serializable]
    public class Marker { public String name; public float position; }
    
    // class for extracting all marker data from MarkerInfo.json
    [Serializable]
    public class MarkerInfo { public Marker[] markers; }

    void InitMarkers() {
        Debug.Log("Reading JSON...");
        string json = markerInfoJSON.text;
        markerInfo = JsonUtility.FromJson<MarkerInfo>(json);

        Fish = new List<Marker>();
        Notes = new List<Marker>();
        directions = new List<string>();

        foreach (Marker x in markerInfo.markers) {
            if (x.name == "Hit")
                Notes.Add(x);
            else if (x.name == "Top" || x.name == "Left" || x.name == "Right" || x.name == "Bottom")
                directions.Add(x.name);
            else if (x.name != "Advance")
                Fish.Add(x);
        }

        input.prevMarkerPos = -500;
        fish.prevMarkerPos = -500;
        input.nextMarkerPos = (int) (Notes[0].position * 1000);
        fish.nextMarkerName = Fish[0].name;
        fish.nextMarkerPos = (int) (Fish[0].position * 1000);
        input.nextnextMarkerPos = (int) (Notes[1].position * 1000);
        fish.nextnextMarkerName = Fish[1].name;

        fish.InitFish(directions[0]);
    }

    // Advance position in the Notes array when a "Hit" marker is passed
    public void UpdateInputMarkers() {
        input.prevMarkerPos = input.nextMarkerPos;
        input.nextMarkerPos = input.nextnextMarkerPos;

        if (Notes.Count > inputMarkerNum + 2) {
            input.nextnextMarkerPos = (int) (Notes[inputMarkerNum + 2].position * 1000);
        } else {
            input.nextnextMarkerPos = 99999999; //so far away it'll never be hit
        }

        ++input.currentNote;
        ++inputMarkerNum;
    }

    // Advance position in the Fish array
    void UpdateFishMarkers() {
        fish.prevMarkerPos = fish.nextMarkerPos;
        if (Fish.Count > fishMarkerNum + 1)
            fish.nextMarkerPos = (int) (Fish[fishMarkerNum + 1].position * 1000);
        else
            fish.nextMarkerPos = 99999999;

        fish.prevMarkerName = fish.nextMarkerName;
        //Debug.Log(fish.prevMarkerName);
        fish.nextMarkerName = fish.nextnextMarkerName;

        if (Fish.Count > fishMarkerNum + 2) {
            fish.nextnextMarkerName = Fish[fishMarkerNum + 2].name;
        } else {
            fish.nextnextMarkerName = "End";
        }

        ++fishMarkerNum;
    }
    #endregion
    
    #region FMOD Callback and Timeline Info
    // class for grabbing the most recent timeline data
    // Variables that are modified in the callback need to be part of a seperate class.
    // This class needs to be 'blittable' otherwise it can't be pinned in memory.
    [StructLayout(LayoutKind.Sequential)]
    public class TimelineInfo {
        public int currentBeat = 0;
        public int currentBar = 0;
        public int currentPos = 0;
        public FMOD.StringWrapper lastMarker = new FMOD.StringWrapper();
        public int markerPos = 0;
    }

    void InitTimelineInfo() {
        //instantiate variables if music exists
        timelineInfo = new TimelineInfo();

        //this links to BeatEventCallback() below
        beatCallback = new FMOD.Studio.EVENT_CALLBACK(BeatEventCallback);

        //this variable is pinned, meaning it ignores garbage collection
        timelineHandle = GCHandle.Alloc(timelineInfo, GCHandleType.Pinned);
        musicInstance.setUserData(GCHandle.ToIntPtr(timelineHandle));
        musicInstance.setCallback(beatCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT | FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
    }

    void FreeTimelineInfo() {
        musicInstance.setUserData(IntPtr.Zero); // remove userdata
        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        musicInstance.release();
        timelineHandle.Free(); // free data manually to avoid leaks
    }

    // FMOD Callback function
    // Grabs information from the currently playing song and puts into a TimelineInfo object
    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT BeatEventCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr) {
        FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);
        IntPtr timelineInfoPtr;
        FMOD.RESULT result = instance.getUserData(out timelineInfoPtr);

        if (result != FMOD.RESULT.OK) {
            Debug.LogError("Timeline Callback Error: " + result);
        } else if (timelineInfoPtr != IntPtr.Zero) {
            GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
            TimelineInfo timelineInfo = (TimelineInfo)timelineHandle.Target;

            // Which type of event is it? A beat or a manually placed marker?
            // Once found, grab a package with event info and assign to our variables.
            switch(type) {
                case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
                    {
                    var parameter = (FMOD.Studio.TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_BEAT_PROPERTIES));
                    timelineInfo.currentBeat = parameter.beat;
                    timelineInfo.currentBar = parameter.bar;
                    }
                    break;
                case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
                    {
                    var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                    timelineInfo.lastMarker = parameter.name;
                    timelineInfo.markerPos = parameter.position;
                    }
                    break;
            }
        }
        return FMOD.RESULT.OK;
    }
    #endregion

    #region Unity Functions
    // Play music on startup, if music exists
    private void Awake() {
        instance = this;

        if (music != null) {
            musicInstance = RuntimeManager.CreateInstance(music);
            musicInstance.start();
        }
    }

    // executes later than Awake()
    private void Start() {
        // grab instances of other scripts
        input = PlayerInput.instance;
        input.musicInstance = musicInstance;
        fish = FishHandler.instance;

        // if music exists, init data structures
        if (music != null) {
            InitTimelineInfo();
            InitMarkers();
        }
    }

    // called every frame
    void Update() {
        if (input.nextMarkerPos <= timelineInfo.markerPos)
            UpdateInputMarkers();
        if (fish.nextMarkerPos == timelineInfo.markerPos)
            UpdateFishMarkers();
        if (currentMarker != timelineInfo.lastMarker) {
            currentMarker = timelineInfo.lastMarker;
            if (timelineInfo.lastMarker == "Advance") {
                if (directions.Count > fishMarkerNum + 1)
                    fish.AdvanceFish(directions[fishMarkerNum + 1]);
                else
                    fish.AdvanceFish("top");
            }
        }  
    }

    // Stop music when object is destroyed
    private void OnDestroy() {
        FreeTimelineInfo();
    }

    // GUI to display debug info
    void OnGUI() {
        GUILayout.Box($"Current beat = {timelineInfo.currentBeat}, Current Bar = {timelineInfo.currentBar}, Last Marker = {(string)timelineInfo.lastMarker}");
    }
    #endregion
}
