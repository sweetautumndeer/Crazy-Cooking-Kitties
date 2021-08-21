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
    private MusicManager music;
    public FMOD.Studio.EventInstance musicInstance; // initialized in MusicManager.cs

    // Defines window for hitting a note
    public int hitWindow = 100;
    public int offset = 50;
    public int missWindow = 3000;

    //for proximity comparisons in RhythmCheck()
    [HideInInspector] public int keyPos = 0;      // time when key was pressed in ms
    [HideInInspector] public int targetMarkerPos; // time of the next marker in ms

    #region Rhythm Functions
    // Check wether input is close to an FMOD "Hit" Marker
    void RhythmCheck() {
        // Calculate proximity to nearest marker
        int diff = keyPos - targetMarkerPos;
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
        music.IncrementInputMarkers();
    }
    void Miss() {
        Debug.Log("Miss :<");
        fish.MissFish();
        music.IncrementInputMarkers();
    }
    #endregion

    #region Unity Functions
    // init instance and necessary variables
    void Awake() {
        instance = this;
    }

    // grab other script instances
    void Start() {
        fish = FishHandler.instance;
        music = MusicManager.instance;
    }

    // Update is called once per frame
    void Update() {
        // Where was the key pressed in ms?
        musicInstance.getTimelinePosition(out keyPos);

        //Player Input
        if (Input.GetKeyDown("space"))
            RhythmCheck();
        //if past hit window of the current note
        if (keyPos > targetMarkerPos + hitWindow + offset)
            Miss();
    }
    #endregion
}
