using UnityEngine; 
 
public class MetronomeScheduled : MonoBehaviour { 
    public AudioSource audioSourceTickBasic; 
 
    public double bpm = 140.0F; 
 
    private double nextTickTime = 0.0F; 
    private double beatDuration; 
 
    void Start() { 
        beatDuration = 60.0F / bpm;  // seconds per beat. use this to time and stuff
        double startTick = AudioSettings.dspTime; 
        nextTickTime = startTick; 
    } 
 
    void Update() { 
        if (IsNearlyTimeForNextTick()) 
            BeatAction(); 
    } 

    public void AskForBeat() {
        // TODO
        Debug.Log("Beat asked for");
    }

    private bool IsNearlyTimeForNextTick() { 
        float lookAhead = 0.1F; 
        if ((AudioSettings.dspTime + lookAhead) >= nextTickTime) 
            return true; 
        else 
            return false; 
    } 
 
    private void BeatAction() { 
        audioSourceTickBasic.PlayScheduled(nextTickTime);  
        nextTickTime += beatDuration; 
    } 

} 