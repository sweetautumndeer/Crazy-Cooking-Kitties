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
using System.Runtime.InteropServices;
using FMODUnity;
using UnityEngine;
//using JsonUtility;

public class MusicManager : MonoBehaviour {
    
    public static MusicManager instance;
    private PlayerInput input;

    [SerializeField]
    [EventRef]
    private string music = null;

    // FMOD Variables
    private FMOD.Studio.EventInstance musicInstance;
    private FMOD.Studio.EVENT_CALLBACK beatCallback;
    public TimelineInfo timelineInfo = null;
    private GCHandle timelineHandle;

    // For updating current marker info in PlayerInput
    private MarkerInfo markerInfo;
    public TextAsset markerInfoJSON; //where is FMOD's full marker info stored?
    private int currentMarkerNum = 0;

    #region MarkerInfo
    // class for extracting all marker data from MarkerInfo.json
    [Serializable]
    public class MarkerInfo {
        [Serializable]
        public class Marker { public String name; public float position; }

        public Marker[] markers;
    }

    void InitMarkers() {
        Debug.Log("Reading JSON...");
            string json = markerInfoJSON.text;
            markerInfo = JsonUtility.FromJson<MarkerInfo>(json);
            input.prevMarkerPos = 0;
            Debug.Log("Start: " + input.prevMarkerPos);
            input.nextMarkerPos = (int) (markerInfo.markers[0].position * 1000);
    }

    // Advance position in the markers array when a marker is passed
    void UpdateMarkers() {
        if (input.prevMarkerPos < timelineInfo.markerPos) {
            input.prevMarkerPos = (int) (markerInfo.markers[currentMarkerNum].position * 1000);
            Debug.Log(markerInfo.markers[currentMarkerNum].name + ": " + input.prevMarkerPos);
            input.nextMarkerPos = (int) (markerInfo.markers[currentMarkerNum + 1].position * 1000);
            ++currentMarkerNum;
        }
    }
    #endregion
    
    #region FMOD Callback
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

    // Play music on startup, if music exists
    private void Awake() {
        instance = this;

        if (music != null) {
            musicInstance = RuntimeManager.CreateInstance(music);
            musicInstance.start();

            input = PlayerInput.instance;
            input.musicInstance = musicInstance;
        }
    }

    // executes later than Awake()
    private void Start() {
        if (music != null) {
            InitTimelineInfo();
            InitMarkers();
        }
    }

    // called every frame
    void Update() {
        UpdateMarkers();
    }

    // Stop music when object is destroyed
    private void OnDestroy() {
        FreeTimelineInfo();
    }

    // GUI to display debug info
    void OnGUI() {
        GUILayout.Box($"Current beat = {timelineInfo.currentBeat}, Current Bar = {timelineInfo.currentBar}, Last Marker = {(string)timelineInfo.lastMarker}");
    }

    
}
