/*
Music Manager
Autumn Moulios
Last Updated 07/24/2021

Reads information from the FMOD timeline to  
create events that are on beat with the music.
Mostly taken from https://www.youtube.com/watch?v=hNQX1fsQL4Q
https://www.fmod.com/resources/documentation-unity?version=2.01&page=examples-timeline-callbacks.html
*/

using System;
using System.Runtime.InteropServices;
using FMODUnity;
using UnityEngine;

public class MusicManager : MonoBehaviour {
    
    public static MusicManager instance;

    [SerializeField]
    [EventRef]
    private string music = null;

    //FMOD Variables
    private FMOD.Studio.EventInstance musicInstance;
    private FMOD.Studio.EVENT_CALLBACK beatCallback;
    public TimelineInfo timelineInfo = null;
    private GCHandle timelineHandle;

    private bool keyPressed = false;
    private int keyPos = 0;
    private int keyTimer = 0;
    private int lastMarkerPos = 0;

    //defines how close the player must hit the key for it to count
    //measured in ms
    public int hitWindow = 200;
    public int hitOffset = 50;

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

    //Play music on startup, if music exists
    private void Awake() {
        instance = this;

        if (music != null) {
            musicInstance = RuntimeManager.CreateInstance(music);
            musicInstance.start();
        }
    }

    //executes later than Awake()
    private void Start() {
        if (music != null) {
            //instantiate variables if music exists
            timelineInfo = new TimelineInfo();
            //this links to BeatEventCallback() below
            beatCallback = new FMOD.Studio.EVENT_CALLBACK(BeatEventCallback);
            //this variable is pinned, meaning it ignores garbage collection
            timelineHandle = GCHandle.Alloc(timelineInfo, GCHandleType.Pinned);
            musicInstance.setUserData(GCHandle.ToIntPtr(timelineHandle));
            musicInstance.setCallback(beatCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT | FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
        }
    }

    //called every frame
    void Update() {
        RhythmCheck();
    }

    //check for rudimentary user input, and whether said input is close to an FMOD Marker
    void RhythmCheck() {
        if (Input.GetKeyDown("space")) {
            musicInstance.getTimelinePosition(out keyPos);
            int diff = keyPos - timelineInfo.markerPos;
            Debug.Log("Proximity to Marker: " + diff);
            keyPressed = true;
        }

        if (keyPressed) {
            int currentPos;
            musicInstance.getTimelinePosition(out currentPos);
            keyTimer = currentPos - keyPos;

            if (keyTimer > hitWindow) {
                keyPressed = false;
                keyTimer = 0;
                Debug.Log("Miss :<");
            }

            int diff = keyPos - timelineInfo.markerPos - hitOffset;
            if (diff > 0 - hitWindow && diff < hitWindow) {
                Debug.Log("Hit! :>");
                lastMarkerPos = timelineInfo.markerPos;
                keyPressed = false;
            }
        }
    }

    //Stop music when object is destroyed
    private void OnDestroy() {
        musicInstance.setUserData(IntPtr.Zero); //remove userdata
        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        musicInstance.release();
        timelineHandle.Free(); //free data manually to avoid leaks
    }

    //GUI to display debug info
    void OnGUI() {
        GUILayout.Box($"Current beat = {timelineInfo.currentBeat}, Current Bar = {timelineInfo.currentBar}, Last Marker = {(string)timelineInfo.lastMarker}");
    }

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

            //Which type of event is it? A beat or a manually placed marker?
            //Once found, grab a package with event info and assign to our variables.
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
}
