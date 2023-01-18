using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{

    [SerializeField]
    private AudioClip clip;
    private AudioSource source;


    void Awake()
    {
        this.source = GetComponent<AudioSource>();
    }


    void Start()
    {
        StartCoroutine(ttest());
    }

    private IEnumerator ttest()
    {
        while(this.clip.LoadAudioData() != true) yield return null;
        this.source.clip = clip;

        Debug.Log("Ambisonic: " + this.source.clip.ambisonic);
        Debug.Log("Length: " + this.source.clip.length);
        Debug.Log("Samples: " + this.source.clip.samples);
        Debug.Log("Channels: " + this.source.clip.channels);
        Debug.Log("Frequency: " + this.source.clip.frequency);

        Debug.Log("0.85: " +getExceedTime(clip, 0.85f));
        Debug.Log("0.75: " + getExceedTime(clip, 0.75f));
        // Debug.Log("0.85: " + (getExceedTime(reference, 0.85f) - getExceedTime(clip, 0.8f)));
        // Debug.Log("0.90: " + (getExceedTime(reference, 0.9f) - getExceedTime(clip, 0.9f)));
        // Debug.Log("0.95: " + (getExceedTime(reference, 0.95f) - getExceedTime(clip, 0.95f)));
        // Debug.Log("1.00: " + (getExceedTime(reference, 1f) - getExceedTime(clip, 1f)));

        
    }

    private float getExceedTime(AudioClip clip, float exceedThreshold)
    {

        float[] samples = new float[this.source.clip.samples * this.source.clip.channels];

        this.source.clip.GetData(samples, 0);
        

        for(int i = 0; i < samples.Length; i += 1){
            if(Mathf.Abs(samples[i]) > exceedThreshold){
                // Exceeded, calculate time
                int exceededAbsoluteFrequency = i / clip.channels;
                // then divide by frequency
                return ((float)exceededAbsoluteFrequency) / (float)clip.frequency;
            }
        }
        return 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
