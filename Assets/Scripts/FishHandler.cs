/*
Music Manager
Autumn Moulios
Last Updated 07/24/2021

Keeps a list of upcoming fish, and displays and
animates them in the scene
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishHandler : MonoBehaviour {

    public static FishHandler instance;
    public PlayerInput input;
    public FMOD.Studio.EventInstance musicInstance; // initialized in MusicManager.cs

    [HideInInspector] public int nextMarkerPos = 0;

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

    private Fish spawningFish;
    private Fish currentFish;
    private Fish leavingFish;

    private static float timeElapsed = 0;
    private static float lerpDuration = 1;
    
    private int keyPos;
    private static float bpm = 134;

    #region Fish Class and Functions
    private class Fish {
        public Transform fish;
        public Transform spawnLocation;
        public Transform midLocation;
        public Transform endLocation;
        public int state = 0;
        public int markerPos;
        public string startingPos;

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

        public void Spawn() { Animate(fish, spawnLocation, midLocation, lerpDuration); }
        public void Center() { Animate(fish, midLocation, center, lerpDuration); }
        public void Leave() { Animate(fish, center, endLocation, lerpDuration); }

        void Animate(Transform obj, Transform start, Transform end, float duration) {
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
        ParseNextFishType(nextMarkerName, direction, pos);
    }

    public void AdvanceFish(string nextnextMarkerName, string direction, int pos) {
        timeElapsed = 0;
        if (leavingFish != null)
            Destroy(leavingFish.fish.gameObject);
        leavingFish = currentFish;
        currentFish = spawningFish;
        ParseNextFishType(nextnextMarkerName, direction, pos);
    }

    void ParseNextFishType(string markerName, string direction, int pos) {
        switch (markerName) {
            case "Long Fish":
                spawningFish = new Fish(longFish, direction, pos);
                break;
            case "Medium Fish":
                spawningFish = new Fish(mediumFish, direction, pos);
                break;
            case "Small Fish":
                spawningFish = new Fish(smallFish, direction, pos);
                break;
            case "End":
                spawningFish = null;
                break;
        }
    }

    void CalculateKnifePosition() {
        musicInstance.getTimelinePosition(out keyPos); // current song position

        float beatsPerSec = bpm / 60f;
        float markerDistance = currentFish.fish.GetChild(3).position.x - currentFish.fish.GetChild(2).position.x; // distance between two markers on the fish
        markerDistance = 0.8f;
        float beatDistance = 1.5f; // beats between said two markers
        float unitsPerSec =  markerDistance / beatDistance * beatsPerSec; // speed at which the knife should move

        float t = (keyPos - currentFish.markerPos) - input.offset; // time to/until the first marker, with offset accounted for
        float firstNote = currentFish.fish.GetChild(2).position.x; // x coor of the first marker
        firstNote = -0.4f;
        float unitsPerMS = unitsPerSec / 1000f; // changed to ms for finer movement

        float result = firstNote + t * unitsPerMS; // full x position lerp with respect to song time
        currentFish.fish.GetChild(1).position = (currentFish.fish.rotation * new Vector3(result, 0.75f, -2.0f) + currentFish.fish.position); // account for prefab rotation and position
    }

    public void HitFish() {
        Transform slice = currentFish.fish.GetChild(2 + currentFish.state++);
        slice.GetComponent<SpriteRenderer>().color = new Color(0, 255, 0);
        slice.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(0, 255, 0);
    }

    public void MissFish() {
        Transform slice = currentFish.fish.GetChild(2 + currentFish.state++);
        slice.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0);
        slice.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(255, 0, 0);
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
    }

    // Update is called once per frame
    void Update() {
        if (spawningFish != null)
            spawningFish.Spawn();
        if (currentFish != null) {
            currentFish.Center();
            CalculateKnifePosition();
        }
        if (leavingFish != null)
            leavingFish.Leave();
        timeElapsed += Time.deltaTime;
    }
    #endregion

    
}
