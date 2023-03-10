using UnityEngine; 
 
public class MetronomeScheduled : MonoBehaviour { 
    public AudioSource audioSourceTickBasic; 
    public AudioSource audioSourceTickAccent; 
 
    public double bpm = 140.0F; 
    public int beatsPerMeasure = 4; 
 
    private double nextTickTime = 0.0F; 
    private int beatCount; 
    private double beatDuration; 
 
    void Start() { 
        beatDuration = 60.0F / bpm;  // seconds per beat. use this to time and stuff
        beatCount = beatsPerMeasure; // so about to do a beat 
        double startTick = AudioSettings.dspTime; 
        nextTickTime = startTick; 
    } 
 
    void Update() { 
        if (IsNearlyTimeForNextTick()) 
            BeatAction(); 
    } 

    public void AskForBeat() {
        // TODO
    }

    private bool IsNearlyTimeForNextTick() { 
        float lookAhead = 0.1F; 
        if ((AudioSettings.dspTime + lookAhead) >= nextTickTime) 
            return true; 
        else 
            return false; 
    } 
 
    private void BeatAction() { 
        beatCount++; 
        string accentMessage = ""; 
 
        if (beatCount > beatsPerMeasure) 
            accentMessage = AccentBeatAction(); 
        else 
            audioSourceTickBasic.PlayScheduled(nextTickTime); 
 
        nextTickTime += beatDuration; 
        print("Tick: " + beatCount + "/" + accentMessage); 
    } 
 
    private string AccentBeatAction() { 
        audioSourceTickAccent.PlayScheduled(nextTickTime); 
        beatCount = 1; 
        return " -- ACCENT ---"; 
    } 
} 