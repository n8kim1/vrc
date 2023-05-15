using UnityEngine; 
using MidiPlayerTK;
using TMPro;


public class MetronomeScheduled : MonoBehaviour { 
    public AudioSource audioSourceTickBasic;
    public MidiFilePlayer midiFilePlayer; 
    public MidiPlayerScript scriptName;

    // TODO should change for each piece
    public double bpmInitial = 150.0F; 
 
    private double nextTickTime; 
    private double beatDuration; 

    private double lastBeatIntended;
    // TODO should change for each piece
    private double bpmIntended = 150.0F;

    public TMP_Text debugText;


    // When updating BPM,
    // how much weight is given to the latest signaled beat's duration
    // (as opposed to the already-present beat duration)
    // TODO fine-tune this based on "perceptible change in tempo"
    // (ie what tempo stability is needed so that odd timing quirks don't really throw you off)
    // TODO implement some stuff that's better w dropping beats etc
    // TODO should really be exposed via getter/setter oneliner
    public double beatDurationIntendedWeight = 0.15F;
 
    void Start() { 
        beatDuration = 60.0F / bpmInitial; // seconds per beat
        double startTick = AudioSettings.dspTime; 

        // Set up initial state
        lastBeatIntended = startTick;
        nextTickTime = lastBeatIntended + beatDuration; 
        // TODO
        // Note that as of now, this will autoplay, 
        // until the first beat is called for.
        // BPM will drastically slow (because the inferred beat includes that pause at the beginning)
        // Better safeguards should take care of this?

        // Find the script hooked to the midiFilePlayer
        // scriptName = midiFilePlayer.GetComponent<MidiPlayerScript>();
        Debug.Log("script name " + scriptName);

    } 
 
    void Update() {
        // If you want a quick-and-dirty "mute", uncomment the following return stmt: 
        // return;
        if (IsNearlyTimeForNextTick()) 
            BeatAction(); 
    } 

    public void AskForBeat() {
        double time = AudioSettings.dspTime;

        double beatDurationIntended = time - lastBeatIntended;
        // A naive way of quickly setting beatDuration.
        // Good to write abt as an introductory method
        // beatDuration = beatDurationIntended;

        // TODO this needs some math, explanation, and tweaking.
        // (param tweaking: tweak so that beats signaled halfway are dropped, and also so that skipping one entire beat but being a litttleee bit fast works fine. factors will need a teensy bit of buffer room)
        // TODO also clean up the code
        // TODO consider making these configurable too but IDK
        if (0.6 < beatDurationIntended / beatDuration && beatDurationIntended / beatDuration < 1.67)
        {
            beatDuration = beatDurationIntendedWeight * beatDurationIntended + (1-beatDurationIntendedWeight) * beatDuration;
        }
        else {
            Debug.Log("out of range, dropping");
        }


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

        // midiPlayerScript.setTempo(bpm);
        // scriptName.PrintDebug();
        scriptName.SetTempo(bpm);
        if (true)
        {
            debugText.text = "";
            debugText.text += "Bpm: " + (Mathf.Round((float) bpm*10)/10).ToString() + "\n";
            debugText.text += "(Raw input: " + (Mathf.Round((float) bpmIntended*10)/10).ToString() + ")\n";
        }
    }

    private bool IsNearlyTimeForNextTick() { 
        float lookAhead = 0.1F; 
        if ((AudioSettings.dspTime + lookAhead) >= nextTickTime) 
            return true; 
        else 
            return false; 
    } 
 
    private void BeatAction() { 
        // TODO this feels a little offbeat...
        // TODO change to a real debug flag
        if (false) {
            audioSourceTickBasic.PlayScheduled(nextTickTime);  
        }
        // Note that tempo changes are only _taken into account_ once a beat finishes
        // TODO is it possible to, like, not do this, for _this_ class's scheduler?
        // I think so, but I don't terribly plan on trying this
        // rather than other approaches
        nextTickTime += beatDuration; 
    } 

} 