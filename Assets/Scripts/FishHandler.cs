/*
Music Manager
Autumn Moulios
Last Updated 07/24/2021

Keeps a list of upcoming fish, and displays and
animates them in the scene
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishHandler : RhythmVisuals {

    public static FishHandler instance;
    public PlayerInput input;

    // fish types
    public Transform smallFish;
    public Transform mediumFish;
    public Transform longFish;

    // fish waypoints
    public Transform fishWaypoints;
    public static Transform center;
    public static Transform topSpawn;
    public static Transform top;
    public static Transform leftSpawn;
    public static Transform left;
    public static Transform rightSpawn;
    public static Transform right;
    public static Transform bottomSpawn;
    public static Transform bottom;

    private List<Fish> currentFish;
    private int currentFishNum = 0;

    private List<MusicManager.Marker> fishList;
    private int fishMarkerNum = 0;
    private List<string> fishDirections;

    private static float timeElapsed = 0;
    private static float lerpDuration = 1;
    
    private int keyPos;
    private static float bpm = 132;

    #region JSON Parsing
    public override void Init(MusicManager.MarkerInfo json) {
        fishList = new List<MusicManager.Marker>();
        fishDirections = new List<string>();
        currentFish = new List<Fish>();

        foreach (MusicManager.Marker x in json.markers) {
            if (x.name == "Top" || x.name == "Left" || x.name == "Right" || x.name == "Bottom")
                fishDirections.Add(x.name);
            else if (x.name == "Small Fish" || x.name == "Medium Fish" || x.name == "Long Fish")
                fishList.Add(x);
        }

        IncrementMarkers();
        InitFish(fishList[0].name, fishDirections[0], (int) (fishList[0].position * 1000));
    }

    // Advance position in the Fish array
    public override void IncrementMarkers() {
        if (fishList.Count > fishMarkerNum) {
            nextMarkerPos = (int) (fishList[fishMarkerNum].position * 1000);
        } else
            nextMarkerPos = 99999999;

        ++fishMarkerNum;
    }
    #endregion

    #region Fish Class and Functions
    private class Fish {
        public Transform fish;
        public Transform spawnLocation;
        public Transform midLocation;
        public Transform endLocation;
        public int state = 0;
        public int finalState;
        public int markerPos;
        public string startingPos;
        public float centerTime = 0;
        public float leaveTime = 0;

        public Fish(Transform fish, string startingPos, int pos) {
            Quaternion rotation = Quaternion.identity;
            this.startingPos = startingPos;
            this.markerPos = pos;

            switch (startingPos) {
                case "Top":
                    this.spawnLocation = topSpawn;
                    this.midLocation = top;
                    this.endLocation = bottomSpawn;
                    rotation = Quaternion.identity;
                    break;
                case "Left":
                    this.spawnLocation = leftSpawn;
                    this.midLocation = left;
                    this.endLocation = rightSpawn;
                    rotation = Quaternion.Euler(Vector3.forward * 90);
                    break;
                case "Right":
                    this.spawnLocation = rightSpawn;
                    this.midLocation = right;
                    this.endLocation = leftSpawn;
                    rotation = Quaternion.Euler(Vector3.forward * 270);
                    break;
                case "Bottom":
                    this.spawnLocation = bottomSpawn;
                    this.midLocation = bottom;
                    this.endLocation = topSpawn;
                    rotation = Quaternion.Euler(Vector3.forward * 180);
                    break;
            }

            this.fish = MonoBehaviour.Instantiate(fish, spawnLocation.position, rotation);
        }

        //public void Spawn() { Animate(fish, spawnLocation, midLocation, lerpDuration); }
        public void Center() { Animate(fish, spawnLocation, midLocation, centerTime, lerpDuration); }
        public void Leave() { Animate(fish, midLocation, spawnLocation, leaveTime, lerpDuration); }

        void Animate(Transform obj, Transform start, Transform end, float timeElapsed, float duration) {
            if (timeElapsed < duration) {
                float t = timeElapsed / duration;
                t = t * t * (3f - 2f * t);
                obj.position = Vector2.Lerp(start.position, end.position, t);
            } else {
                obj.position = end.position;
            }
        }
    }

    public void InitFish(string nextMarkerName, string direction, int pos) {
        currentFish.Add(ParseNextFishType(nextMarkerName, direction, pos));
    }

    Fish ParseNextFishType(string markerName, string direction, int pos) {
        Fish spawningFish = null;
        switch (markerName) {
            case "Long Fish":
                spawningFish = new Fish(longFish, direction, pos);
                spawningFish.finalState = 4;
                break;
            case "Medium Fish":
                spawningFish = new Fish(mediumFish, direction, pos);
                spawningFish.finalState = 3;
                break;
            case "Small Fish":
                spawningFish = new Fish(smallFish, direction, pos);
                spawningFish.finalState = 2;
                break;
            case "End":
                spawningFish = null;
                break;
            default:
                Debug.LogError("Failed to Spawn Target Fish: Unknown Marker Name");
                break;
        }

        return spawningFish;
    }

    void CalculateKnifePosition(Fish x) {
        musicInstance.getTimelinePosition(out keyPos); // current song position

        float beatsPerSec = bpm / 60f;
        float markerDistance = (Quaternion.Inverse(x.fish.rotation) * x.fish.GetChild(0).GetChild(1).position).x - 
                               (Quaternion.Inverse(x.fish.rotation) * x.fish.GetChild(0).GetChild(0).position).x; // distance between first two markers on the fish
        float beatDistance = 1.5f; // beats between said two markers
        float unitsPerSec =  markerDistance / beatDistance * beatsPerSec; // speed at which the knife should move

        float t = (keyPos - x.markerPos) - input.offset; // time to/until the first marker, with offset accounted for
        float firstNote = (Quaternion.Inverse(x.fish.rotation) * (x.fish.GetChild(0).GetChild(0).position - x.fish.GetChild(0).position)).x; // x coor of the first marker
        float unitsPerMS = unitsPerSec / 1000f; // changed to ms for finer movement

        float result = - firstNote - t * unitsPerMS; // full x position lerp with respect to song time
        Debug.Log(result);
        x.fish.GetChild(1).position = (x.fish.rotation * new Vector3(0, 0.75f, -2.0f) + x.fish.position);
        x.fish.GetChild(0).position = (x.fish.rotation * new Vector3(result, 0, 0) + x.fish.position); // account for prefab rotation and position
    }

    public override void Hit() {
        try {
            Transform slice = currentFish[currentFishNum].fish.GetChild(0).GetChild(currentFish[currentFishNum].state++);
            slice.GetComponent<SpriteRenderer>().color = new Color(0, 255, 0);
            slice.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(0, 255, 0);
            if (currentFish[currentFishNum].state == currentFish[currentFishNum].finalState) {
                ++currentFishNum;
            }
        } catch (NullReferenceException e) {
            //Debug.LogError("hey");
        }
    }

    public override void Miss() {
        try {
            Transform slice = currentFish[currentFishNum].fish.GetChild(0).GetChild(currentFish[currentFishNum].state++);
            slice.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0);
            slice.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(255, 0, 0);
            if (currentFish[currentFishNum].state == currentFish[currentFishNum].finalState) {
                ++currentFishNum;
            }
        } catch (NullReferenceException e) {
            //Debug.LogError("hey");
        }
        
    }
    #endregion

    #region Unity Functions
    // Start is called before the first frame update
    void Awake() {
        instance = this;


        //init waypoints
        center = fishWaypoints.GetChild(0);
        topSpawn = fishWaypoints.GetChild(1);
        top = fishWaypoints.GetChild(2);
        leftSpawn = fishWaypoints.GetChild(3);
        left = fishWaypoints.GetChild(4);
        rightSpawn = fishWaypoints.GetChild(5);
        right = fishWaypoints.GetChild(6);
        bottomSpawn = fishWaypoints.GetChild(7);
        bottom = fishWaypoints.GetChild(8);
    }

    void Start() {
        input = PlayerInput.instance;
        musicInstance = MusicManager.instance.musicInstance;
    }

    // Update is called once per frame
    void Update() {
        timeElapsed += Time.deltaTime;

        if ((int) (fishList[fishMarkerNum - 1].position * 1000) - input.keyPos < 1000) {
            if (fishDirections.Count > fishMarkerNum) {
                currentFish.Add(ParseNextFishType(fishList[fishMarkerNum].name, fishDirections[fishMarkerNum], (int) (fishList[fishMarkerNum].position * 1000)));
                IncrementMarkers();
            } else {
                currentFish.Add(null);
                Debug.Log("fuck");
            }   
        }

        foreach (Fish x in currentFish) {
            if (x.markerPos + 2000 < input.keyPos) {
                x.leaveTime += Time.deltaTime;
                x.Leave();
            } else {
                x.centerTime += Time.deltaTime;
                x.Center();
                CalculateKnifePosition(x);
            }
        }
    }
    #endregion
}
