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
    private RhythmVisuals visuals;
    private MusicManager music;
    public FMOD.Studio.EventInstance musicInstance; // initialized in MusicManager.cs

    private string keystroke;

    // Defines window for hitting a note
    public int excellentWindow = 50;
    public int greatWindow = 100;
    public int goodWindow = 150;
    public int offset;
    public int missWindow;

    //for proximity comparisons in RhythmCheck()
    [HideInInspector] public int keyPos = 0;      // time when key was pressed in ms
    [HideInInspector] public int downTargetPos;
    [HideInInspector] public int rightTargetPos;
    [HideInInspector] public int leftTargetPos;
    [HideInInspector] public int upTargetPos;
    private int nextMarker;

    //variables for player score
    [HideInInspector] public int playerScore = 0;
    public int goodValue = 50;
    public int greatValue = 100;
    public int excellentValue = 200;

    #region Rhythm Functions
    // Check wether input is close to an FMOD "Hit" Marker
    void RhythmCheck(int target, string direction) {
        // Calculate proximity to nearest marker
        int diff = keyPos - target;
        Debug.Log("Proximity to Marker: " + diff);

        // If within the hit window, the note was hit
        if (diff > 0 - excellentWindow + offset && diff < excellentWindow + offset) {
            playerScore += excellentValue;
            Hit(direction);
        } else if (diff > 0 - greatWindow + offset && diff < greatWindow + offset) {
            playerScore += greatValue;
            Hit(direction);
        } else if (diff > 0 - goodWindow + offset && diff < goodWindow + offset) {
            playerScore += goodValue;
            Hit(direction);
        } else if (diff > 0 - missWindow + offset && diff < goodWindow + offset) {
            Miss(direction);
        }
    }

    // Send hit/miss message to FishHandler
    // Mark current note as having been interacted with
    void Hit(string direction) {
        Debug.Log("Hit! :>");
        visuals.Hit();
        music.IncrementInputMarkers(direction);
        Debug.Log("Score: " + playerScore);
    }
    void Miss(string direction) {
        Debug.Log("Miss :<");
        visuals.Miss();
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
        visuals = FishHandler.instance;
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
        if (keyPos > downTargetPos + goodWindow + 3 * offset)
            Miss("Down");
        if (keyPos > rightTargetPos + goodWindow + 3 * offset)
            Miss("Right");
        if (keyPos > leftTargetPos + goodWindow + 3 * offset)
            Miss("Left");
        if (keyPos > upTargetPos + goodWindow + 3 * offset)
            Miss("Up");
    }
    #endregion
}
