using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RhythmVisuals : MonoBehaviour {

    public FMOD.Studio.EventInstance musicInstance;
    [HideInInspector] public int nextMarkerPos;

    public abstract void Init(MusicManager.MarkerInfo json);
    public abstract void IncrementMarkers();

    public abstract void Hit();
    public abstract void Miss();
}
