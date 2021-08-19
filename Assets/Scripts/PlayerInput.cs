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

    public static PlayerInput instance;
    public FMOD.Studio.EventInstance musicInstance; //initialized in MusicManager.cs

    //Defines window for hitting a note
    public int hitWindow = 200;
    public int hitOffset = 50;

    //for proximity comparisons
    [HideInInspector] public int keyPos = 0;
    [HideInInspector] public int prevMarkerPos = 0;
    [HideInInspector] public int nextMarkerPos = 0;

    void Awake() {
        instance = this;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown("space")) {
            RhythmCheck();
        }
    }

    // Check wether input is close to an FMOD Marker
    void RhythmCheck() {
        // Where was the key pressed in ms?
        musicInstance.getTimelinePosition(out keyPos);

        // Calculate proximity to nearest marker
        int diffLeft = keyPos - prevMarkerPos;
        int diffRight = keyPos - nextMarkerPos;
        int diff = (diffLeft < -diffRight)? diffLeft : diffRight;
        Debug.Log("Proximity to Marker: " + diff);

        // If within the hit window, the note was hit
        if (diff > 0 - hitWindow + hitOffset && diff < hitWindow + hitOffset) {
            Debug.Log("Hit! :>");
        } else {
            Debug.Log("Miss :<");
        }
    }
}
