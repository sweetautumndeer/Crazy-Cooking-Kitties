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
    private RhythmVisuals visuals;

    [SerializeField]
    [EventRef]
    private string music = null;

    public bool GUIEnabled;

    // FMOD Variables
    public FMOD.Studio.EventInstance musicInstance;
    private FMOD.Studio.EVENT_CALLBACK beatCallback;
    public TimelineInfo timelineInfo = null;
    private GCHandle timelineHandle; 

    private MarkerInfo markerInfo;   // class instance for dumping JSON info into
    public TextAsset markerInfoJSON; // where is FMOD's full marker info stored?
    private string currentMarker;
    
    private MarkerList down;
    private MarkerList right;
    private MarkerList left;
    private MarkerList up;

    public int musicEnd;
    public RectTransform songSliderBG;
    public RectTransform songSlider;

    #region Marker Info
    [Serializable]
    public class Marker { public String name; public float position; }
    
    // class for extracting all marker data from MarkerInfo.json
    [Serializable]
    public class MarkerInfo { public Marker[] markers; }

    public class MarkerList {
        public List<Marker> notes;
        public int pos = 0;

        public MarkerList() { notes = new List<Marker>(); }
        public void AddMarker(Marker x) { notes.Add(x); }

        public int Increment() {
            int result;

            if (notes.Count > pos) {
                result = (int) (notes[pos].position * 1000);
            } else {
                result = 99999999; //so far away it'll never be hit
            }

            ++pos;
            return result;
        }
    }

    void InitMarkers() {
        Debug.Log("Reading JSON...");
        string json = markerInfoJSON.text;
        markerInfo = JsonUtility.FromJson<MarkerInfo>(json);

        // init lists
        down = new MarkerList();
        right = new MarkerList();
        left = new MarkerList();
        up = new MarkerList();

        //sort markers into the above lists for parsing
        foreach (Marker x in markerInfo.markers) {
            if (x.name == "D")
                down.AddMarker(x);
            else if (x.name == "R")
                right.AddMarker(x);
            else if (x.name == "L")
                left.AddMarker(x);
            else if (x.name == "U")
                up.AddMarker(x);
        }

        // increment each list once to init them
        IncrementInputMarkers("Down");
        IncrementInputMarkers("Right");
        IncrementInputMarkers("Left");
        IncrementInputMarkers("Up");
        visuals.Init(markerInfo);
    }

    // Advance position in the Notes array when a "Hit" marker is passed
    public void IncrementInputMarkers(string pos) {
        switch(pos){
            case "Down":
                input.downTargetPos = down.Increment();
                break;
            case "Right":
                input.rightTargetPos = right.Increment();
                break;
            case "Left":
                input.leftTargetPos = left.Increment();
                break;
            case "Up":
                input.upTargetPos = up.Increment();
                break;
            default:
                Debug.Log("Invalid Increment Input!");
                break;
        }
        
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
        instance = this; // init MonoBehaviour instance

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
        visuals = FishHandler.instance;
        //visuals.musicInstance = musicInstance;

        // if music exists, init data structures
        if (music != null) {
            InitTimelineInfo();
            InitMarkers();
        }
    }

    // called every frame
    void Update() {
        // if we have passed FishHandler's next marker, update it
        if (visuals.nextMarkerPos == timelineInfo.markerPos)
            visuals.IncrementMarkers();

        // This is true every time the last marker changes
        if (currentMarker != timelineInfo.lastMarker) {
            currentMarker = timelineInfo.lastMarker;
        }  

        CalculateSongPercentageSlider();
    }

    private void CalculateSongPercentageSlider() {
        float songPercentage = ((float) input.keyPos / (float) musicEnd);
        if (songPercentage > 1) { songPercentage = 1; }
        songSlider.sizeDelta = new Vector2(songPercentage * songSliderBG.sizeDelta.x, songSliderBG.sizeDelta.y);
    }

    // Stop music when object is destroyed
    private void OnDestroy() {
        FreeTimelineInfo();
    }

    // GUI to display debug info (if enabled)
    void OnGUI() {
        if (GUIEnabled)
            GUILayout.Box($"Current beat = {timelineInfo.currentBeat}, Current Bar = {timelineInfo.currentBar}, Last Marker = {(string)timelineInfo.lastMarker}");
    }
    #endregion
}
