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
    public Transform center;
    public Transform topSpawn;
    public Transform top;
    public Transform leftSpawn;
    public Transform left;
    public Transform rightSpawn;
    public Transform right;
    public Transform bottomSpawn;
    public Transform bottom;

    private Fish spawningFish;
    private Fish currentFish;
    private Fish leavingFish;

    private static float timeElapsed = 0;
    private static float lerpDuration = 1;

    private class Fish {
        public Transform fish;
        public Transform spawnLocation;
        public Transform midLocation;
        public Transform center;
        public Transform endLocation;

        public Fish(Transform fish, Transform spawnLocation, Transform midLocation, Transform center, Transform endLocation) {
            this.fish = MonoBehaviour.Instantiate(fish, spawnLocation.position, Quaternion.identity);
            this.spawnLocation = spawnLocation;
            this.midLocation = midLocation;
            this.center = center;
            this.endLocation = endLocation;
        }

        public void Spawn() {
            Animate(spawnLocation, midLocation);
        }

        public void Center() {
            Animate(midLocation, center);
        }

        public void Leave() {
            Animate(center, endLocation);
        }

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

    // Start is called before the first frame update
    void Awake() {
        instance = this;
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

    public void InitFish() {
        ParseNextFishType(nextMarkerName);
    }

    public void AdvanceFish() {
        timeElapsed = 0;
        if (leavingFish != null)
            Destroy(leavingFish.fish.gameObject);
        leavingFish = currentFish;
        currentFish = spawningFish;
        Debug.Log(nextnextMarkerName);
        ParseNextFishType(nextnextMarkerName);
    }

    void ParseNextFishType(string markerName) {
        switch (markerName) {
            case "Long Fish":
                {
                spawningFish = new Fish(longFish, leftSpawn, left, center, rightSpawn);
                }
                break;
            case "Medium Fish":
                {
                spawningFish = new Fish(mediumFish, leftSpawn, left, center, rightSpawn);
                }
                break;
            case "Small Fish":
                {
                spawningFish = new Fish(smallFish, topSpawn, top, center, bottomSpawn);
                }
                break;
            case "End":
                spawningFish = null;
                break;
        }
    }
}
