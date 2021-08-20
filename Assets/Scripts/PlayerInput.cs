/*
Player Input
Autumn Moulios
Last Updated 07/24/2021

Parses player input using the information from MusicManager.cs
*/

using System;
using FMODUnity;
using UnityEngine;

public class PlayerInput : MonoBehaviour {

    // Class instances
    public static PlayerInput instance;
    private FishHandler fish;
    public FMOD.Studio.EventInstance musicInstance; //initialized in MusicManager.cs

    // Defines window for hitting a note
    public int hitWindow = 200;
    public int offset = 50;
    public int missWindow = 3000;

    // Keeps track of which notes have already been hit to avoid desync with music
    public int currentNote = 0; //upcoming note
    public int lastNote = 0; //last note interacted with

    //for proximity comparisons in RhythmCheck()
    [HideInInspector] public int keyPos = 0;
    [HideInInspector] public int prevMarkerPos = 0;
    [HideInInspector] public int nextMarkerPos = 0;
    [HideInInspector] public int nextnextMarkerPos = 0;

    

    #region Rhythm Functions
    // Check wether input is close to an FMOD Marker
    void RhythmCheck() {
        // Calculate proximity to nearest marker
        int diffLeft = keyPos - prevMarkerPos;
        int diffRight = keyPos - nextMarkerPos;
        int diff = (diffLeft < -diffRight)? diffLeft : diffRight;
        Debug.Log("Proximity to Marker: " + diff);

        // If within the hit window, the note was hit
        if (diff > 0 - hitWindow + offset && diff < hitWindow + offset) {
            Hit();
        } else if (diff > 0 - missWindow + offset && diff < hitWindow + offset) {
            Miss();
        }
    }

    // Send hit/miss message to FishHandler
    // Mark current note as having been interacted with
    void Hit() {
        Debug.Log("Hit! :>");
        fish.HitFish();
        ++lastNote;
    }
    void Miss() {
        Debug.Log("Miss :<");
        fish.MissFish();
        ++lastNote;
    }
    #endregion

    #region Unity Functions
    // init instance and necessary variables
    void Awake() {
        instance = this;
        currentNote = 0;
        lastNote = 0;
    }

    // grab other script instances
    void Start() {
        fish = FishHandler.instance;
    }

    // Update is called once per frame
    void Update() {
        // Where was the key pressed in ms?
        musicInstance.getTimelinePosition(out keyPos);

        //Player Input
        if (Input.GetKeyDown("space"))
            RhythmCheck();
        //if past hit window, and the current note hasn't already been interacted with
        if (keyPos >= prevMarkerPos + hitWindow + offset && currentNote > lastNote)
            Miss();
    }
    #endregion
}
