using UnityEngine; 
 
public class MetronomeScheduled : MonoBehaviour { 
    public AudioSource audioSourceTickBasic; 
 
    public double bpmInitial = 140.0F; 
 
    private double nextTickTime = 0.0F; 
    private double beatDuration; 

    private double lastBeatIntended = 0.0F;
    private double bpmIntended = 140.0F;
 
    void Start() { 
        beatDuration = 60.0F / bpmInitial; // seconds per beat
        double startTick = AudioSettings.dspTime; 
        nextTickTime = startTick; 

        lastBeatIntended = startTick;
    } 
 
    void Update() { 
        return;
        if (IsNearlyTimeForNextTick()) 
            BeatAction(); 
    } 

    public void AskForBeat() {
        double time = AudioSettings.dspTime;

        double beatDurationIntended = time - lastBeatIntended;
        // TODO naive way of quickly setting beatDuration, try more dynamic things
        // beatDuration = beatDurationIntended;
        // TODO make these weights a var
        beatDuration = 0.5 * beatDurationIntended + 0.5 * beatDuration;

        double bpmIntended = 60.0F / beatDurationIntended;
        Debug.Log("Beat asked for, intended bpm" + bpmIntended);
        double bpm = 60.0F / beatDuration;
        Debug.Log("Beat asked for, adjusted bpm" + bpm);
        // TODO if you look at the raw bpm intended (ie from beatDurationIntended),
        // it takes on only so many _discrete_ values
        // (127, 134, 140, 148, 156...)
        // This suggests _scary framerate limitations_ --
        // that AskForBeat can only be called every, like, 30 FPS or so
        // which would not be good.
        // It remains to be seen how much using guards (max/min bounds,
        // dynamic shifting over time rather than a full reset etc)
        // would help
        
        lastBeatIntended = time;
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