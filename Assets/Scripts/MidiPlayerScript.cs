// TODO am I even using the system.collections and .generic imports??
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;
using TMPro;

public class MidiPlayerScript : MonoBehaviour
{
    public MidiFilePlayer midiFilePlayer;
    public SpatialAnchorsGenFake spatialAnchorsGenFake;

    public TMP_Text mainText;
    public TMP_Text bottomText;

    bool isPlaying = false;

    // For computing beat/measure within piece
    long tickFirstNote;
    // This is float (not long, as is given by the MIDI player)
    // in order to enable offbeat division
    float ticksPerQuarter;
    long currentTick;
    // Float, so decimals can be nicely displayed
    float currentQuarter;
    // To only update on demand. 
    // Note that it starts at -1, so that beat 0 displays afresh.
    long lastTick = -1;

    // Start is called before the first frame update
    void Start()
    {
        // TODO verify that this _works_
        // IDK whether this has to be called before/after play.
        // Easiest is to find a midi that definitely changes tempo early and noticeably
        midiFilePlayer.MPTK_EnableChangeTempo = false;
        
        // TODO better queueing mechanism.
        // Right now it's like "start beats and then play when you're ready!"
        midiFilePlayer.MPTK_StartPlayAtFirstNote = true;

        midiFilePlayer.MPTK_Volume = 0.5f;

        // These values are set to 0 before the midi player begins
        // so don't read them yet
        // ticksPerQuarter = midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
        // tickFirstNote = midiFilePlayer.MPTK_TickFirstNote;

        // Initialize text
        // Perhaps inefficient, but eh it's only run once
        mainText.text = "Welcome!";
        bottomText.text = "";
        bottomText.text += "Controls:" + "\n";
        bottomText.text += "X to play/pause" + "\n";
        bottomText.text += "Y to stop and reset" + "\n";
        bottomText.text += "LH stick up/down to adjust beat detection sensitivity" + "\n";


        // TODO I can't get original tempo to work fsr...
        // Might have to do with timing on load or play or something
        // midiFilePlayer.MPTK_Tempo = 140;

        // Print some info about the MIDI.
        // Is mainly useful for debugging and proof-of-concept/demo.
        // MidiLoad midiloaded = midiFilePlayer.MPTK_Load();
        // if (midiloaded != null)
        // {
        //     infoMidi = "Duration: " + midiloaded.MPTK_Duration.TotalSeconds + " seconds\n";
        //     infoMidi += "Tempo: " + midiloaded.MPTK_InitialTempo + "\n";
        //     List<MPTKEvent> listEvents = midiloaded.MPTK_ReadMidiEvents();
        //     infoMidi += "Count MIDI Events: " + listEvents.Count + "\n";
        //     Debug.Log(infoMidi);
        // }

        // midiFilePlayer.MPTK_Play();
    }

    // Update is called once per frame
    void Update()
    {
        // Do _not_ call OVRInput.Update
        // since it's already called in the OVRManager, 
        // and multiple calls to it prevent GetDown, GetUp, and other frame-specific methods
        // to fail (something to do with polling too many times)
        // See, eg, https://lab.popul-ar.com/t/ovr-controller-buttons-not-working/1033
        // OVRInput.Update();

        // Check for both headset input and laptop-keyboard input (in debugging)
        bool playTogglePressed = 
            OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch) || Input.GetKeyDown(KeyCode.P);

        if (playTogglePressed)
        {
            Debug.Log("Play input was pressed");
            if (!isPlaying) {
                midiFilePlayer.MPTK_Play();

                // Get tempo-related values, which now hold valid data,
                // now that we're playing
                ticksPerQuarter = (float) midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
                tickFirstNote = midiFilePlayer.MPTK_TickFirstNote;

                spatialAnchorsGenFake.StartRecording();

                mainText.text = "playing";

                // TODO I couldn't find an "isPlaying" attribute of the midiFilePlayer class
                // so we have to keep track of this manually.
                // would be much better if not...
                isPlaying = true;
            }
            else {
                midiFilePlayer.MPTK_Pause();
                mainText.text = "pausing...";
                isPlaying = false;
            }
        }

        bool resetPressed = 
            OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch) || Input.GetKeyDown(KeyCode.R);

        // TODO pressing R throws an error.
        // Turns out the R button is hooked up to some other code already lmao
        // Switch the keyboard button in use to something else
        if (resetPressed)
        {
            Debug.Log("Reset was pressed");
            mainText.text = "resetting...";

            // yes, stop method, not reset, as per the package this is what does what we want 
            // (stops and brings to beginning)
            midiFilePlayer.MPTK_Stop();

            // TODO I couldn't find an "isPlaying" attribute of the midiFilePlayer class
            // so we have to keep track of this manually.
            // would be much better if not...
            isPlaying = false;

            // To make the beat still display upon restart
            lastTick = -1;

            spatialAnchorsGenFake.StopRecording();
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("t key was pressed, slowing tempo");
            midiFilePlayer.MPTK_Tempo *= 0.7;
        }

        if (isPlaying) {
            // Display things
            // TODO should display, or handling buttons, come first?
            // Is there a better practice? Eran might know??
            currentTick = midiFilePlayer.MPTK_TickCurrent;
            if (currentTick > lastTick) {
                currentQuarter = (currentTick - tickFirstNote) / ticksPerQuarter;
                Debug.Log("q: " + currentQuarter); 
                // TODO turn this into beat and measure, math should be simple
                // TODO it's annoying to show . of a beat. Drop this. 
                // Maybe ask around and try different things
                mainText.text = "q: " + currentQuarter;
                lastTick = currentTick;
            }

        }
    }

    public void SetTempo(double tempo)
    {
        midiFilePlayer.MPTK_Tempo = tempo;
        Debug.Log("tempo set in MidiPlayerScript");
    }

    public void SetVolume(float volume){
        midiFilePlayer.MPTK_Volume = volume;
    }

    // To test whether this is being called anyways
    public void PrintDebug()
    {
        Debug.Log("printdebug called");
    }
}
