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

    [HideInInspector] public string prevMarkerName = "";
    [HideInInspector] public int prevMarkerPos = 0;
    [HideInInspector] public string nextMarkerName = "";
    [HideInInspector] public int nextMarkerPos = 0;
    [HideInInspector] public string nextnextMarkerName = "";

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

    #region Fish Class and Functions
    private class Fish {
        public Transform fish;
        public Transform spawnLocation;
        public Transform midLocation;
        public Transform endLocation;
        public int state = 0;
        public string startingPos;

        public Fish(Transform fish, string startingPos) {
            Quaternion rotation = Quaternion.identity;
            this.startingPos = startingPos;

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

        public void Spawn() { Animate(spawnLocation, midLocation); }
        public void Center() { Animate(midLocation, center); }
        public void Leave() { Animate(center, endLocation); }

        void Animate(Transform start, Transform end) {
            if (timeElapsed < lerpDuration) {
                float t = timeElapsed / lerpDuration;
                t = t * t * (3f - 2f * t);
                fish.position = Vector2.Lerp(start.position, end.position, t);
            } else {
                fish.position = end.position;
            }
        }
    }

    public void InitFish(string direction) {
        ParseNextFishType(nextMarkerName, direction);
    }

    public void AdvanceFish(string direction) {
        timeElapsed = 0;
        if (leavingFish != null)
            Destroy(leavingFish.fish.gameObject);
        leavingFish = currentFish;
        currentFish = spawningFish;
        ParseNextFishType(nextnextMarkerName, direction);
    }

    void ParseNextFishType(string markerName, string direction) {
        switch (markerName) {
            case "Long Fish":
                {
                spawningFish = new Fish(longFish, direction);
                }
                break;
            case "Medium Fish":
                {
                spawningFish = new Fish(mediumFish, direction);
                }
                break;
            case "Small Fish":
                {
                spawningFish = new Fish(smallFish, direction);
                }
                break;
            case "End":
                spawningFish = null;
                break;
        }
    }

    public void HitFish() {
        Transform slice = currentFish.fish.GetChild(currentFish.state++);
        slice.GetComponent<SpriteRenderer>().color = new Color(0, 255, 0);
    }

    public void MissFish() {
        Transform slice = currentFish.fish.GetChild(currentFish.state++);
        slice.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0);
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

    // Update is called once per frame
    void Update() {
        if (spawningFish != null)
            spawningFish.Spawn();
        if (currentFish != null)
            currentFish.Center();
        if (leavingFish != null)
            leavingFish.Leave();
        timeElapsed += Time.deltaTime;
    }
    #endregion

    
}
