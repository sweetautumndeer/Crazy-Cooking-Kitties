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

    private string keystroke;

    // Defines window for hitting a note
    public int hitWindow;
    public int offset;
    public int missWindow;

    //for proximity comparisons in RhythmCheck()
    [HideInInspector] public int keyPos = 0;      // time when key was pressed in ms
    [HideInInspector] public int downTargetPos;
    [HideInInspector] public int rightTargetPos;
    [HideInInspector] public int leftTargetPos;
    [HideInInspector] public int upTargetPos;
    private int nextMarker;

    #region Rhythm Functions
    // Check wether input is close to an FMOD "Hit" Marker
    void RhythmCheck(int target, string direction) {
        // Calculate proximity to nearest marker
        int diff = keyPos - target;
        Debug.Log("Proximity to Marker: " + diff);

        // If within the hit window, the note was hit
        if (diff > 0 - hitWindow + offset && diff < hitWindow + offset) {
            Hit(direction);
        } else if (diff > 0 - missWindow + offset && diff < hitWindow + offset) {
            Miss(direction);
        }
    }

    // Send hit/miss message to FishHandler
    // Mark current note as having been interacted with
    void Hit(string direction) {
        Debug.Log("Hit! :>");
        fish.HitFish();
        music.IncrementInputMarkers(direction);
    }
    void Miss(string direction) {
        Debug.Log("Miss :<");
        fish.MissFish();
        music.IncrementInputMarkers(direction);
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
        if (Input.GetKeyDown(KeyCode.DownArrow))
            RhythmCheck(downTargetPos, "Down");
        if (Input.GetKeyDown(KeyCode.RightArrow))
            RhythmCheck(rightTargetPos, "Right");
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            RhythmCheck(leftTargetPos, "Left");
        if (Input.GetKeyDown(KeyCode.UpArrow))
            RhythmCheck(upTargetPos, "Up");
        // if reasonably past hit window of the current note (to avoid players hitting near this point and missing two notes at once)
        if (keyPos > downTargetPos + hitWindow + 3 * offset)
            Miss("Down");
        if (keyPos > rightTargetPos + hitWindow + 3 * offset)
            Miss("Right");
        if (keyPos > leftTargetPos + hitWindow + 3 * offset)
            Miss("Left");
        if (keyPos > upTargetPos + hitWindow + 3 * offset)
            Miss("Up");
    }
    #endregion
}
