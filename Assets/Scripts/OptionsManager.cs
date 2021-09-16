using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour {
    [SerializeField] FMOD.Studio.VCA musicVCA;
    [SerializeField] FMOD.Studio.VCA sfxVCA;
    [SerializeField] FMOD.Studio.VCA ambientVCA;
    [SerializeField] FMOD.Studio.Bus master;

    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider ambientSlider;
    [SerializeField] Slider masterSlider;

    [Range(-80f, 10f)] private float musicVolume;
    [Range(-80f, 10f)] private float sfxVolume;
    [Range(-80f, 10f)] private float ambientVolume;
    [Range(-80f, 10f)] private float masterVolume;

    void Start() {
        musicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Music");
        sfxVCA = FMODUnity.RuntimeManager.GetVCA("vca:/SFX");
        ambientVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Ambient");
    }

    public void AdjustMusicVolume() {
        musicVolume = musicSlider.value;
        float result = Mathf.Pow(10f, musicVolume / 20f);
        musicVCA.setVolume(musicVolume);
        Debug.Log(musicVolume);
    }
}
