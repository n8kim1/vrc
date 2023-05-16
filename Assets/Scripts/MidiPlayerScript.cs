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
    public MetronomeScheduled metronomeScheduled;

    public TMP_Text mainText;
    public TMP_Text bottomText;

    bool isPlaying = false;
    bool isResetQueued = true;

    // For computing beat/measure within piece
    long tickFirstNote;
    // This is float (not long, as is given by the MIDI player)
    // in order to enable offbeat division
    long ticksPerQuarter;
    long currentTick;
    // Float, so decimals can be nicely displayed
    float currentQuarter;
    // To only update on demand. 
    // Note that it starts at -1, so that beat 0 displays afresh.
    long lastTick = -1;

    // For representing state of accent and playing them
    bool is_accent = false;
    HashSet<long> accentBeats = new HashSet<long>();
    // TODO I'm not sure whether we should be doing math in beats or ticks as the standard unit
    // Beats is more intuitive, but demands more frequent converstions on the fly
    // which is bad for FPS
    // (since on the fly we easily have ticks)
    long beat_accent_signaled = -2;

    private int piece_choice_idx = 0;
    private int piece_choice_len = 2;

    public void ChoosePrevPiece () {
        piece_choice_idx -= 1;
        if (piece_choice_idx <= 0) {
            piece_choice_idx = piece_choice_len - 1;
        }
    }

    public void ChooseNextPiece () {
        piece_choice_idx += 1;
        if (piece_choice_idx >= piece_choice_len) {
            piece_choice_idx = 0;
        }
    }

    public int GetPieceChoiceIdx () {
        return piece_choice_idx;
    }

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

        midiFilePlayer.OnMidiEvent = OnMidiEvent;

        // These values are set to 0 before the midi player begins
        // so don't read them yet
        // ticksPerQuarter = midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
        // tickFirstNote = midiFilePlayer.MPTK_TickFirstNote;

        // Initialize text
        // Perhaps inefficient, but eh it's only run once
        mainText.text = "Welcome!";
        bottomText.text = "";
        // bottomText.text += "Controls:" + "\n";
        bottomText.text += "X to play/pause, Y to stop and reset" + "\n";
        bottomText.text += "LH stick up/down to select setting" + "\n";
        bottomText.text += "LH stick left/right to change setting" + "\n";

        // Lazy way to integrate accents
        // TODO this should become a piece by piece thing
        for (int i = 0; i <= 282*2*2; i = i+2) {
            accentBeats.Add(i);
        }

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
            Debug.Log("Play input was pressed", this);

            if (isResetQueued) {
                Debug.Log("Initializing playing, piece " + piece_choice_idx);

                // Load proper piece and initialize data.

                if (piece_choice_idx == 0) {
                    midiFilePlayer.MPTK_MidiName = "eine_kle_mvt_2_tempo_prep";
                    metronomeScheduled.InitBpm(60.0f);
                }

                else if (piece_choice_idx == 1) {
                    midiFilePlayer.MPTK_MidiName = "eine_kle_prep_measure";
                    metronomeScheduled.InitBpm(150.0f);
                }

                Debug.Log(midiFilePlayer.MPTK_MidiName);

                // TODO setup tempo correctly
                // TODO init accents etc correctly

                isResetQueued = false;
            }
            if (!isPlaying) {
                midiFilePlayer.MPTK_Play();

                // Get tempo-related values, which now hold valid data,
                // now that we're playing
                ticksPerQuarter = (long) midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
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
            // TODO should differentiate states between "stop" and "reset"
            // (in this file comments)
            // and then also change the verbage used in the code and display and etc
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

            // TODO externally tracking this is rough
            isResetQueued = true;
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

 
    public void SignalAccent () {
        long currentBeat = (midiFilePlayer.MPTK_TickCurrent - tickFirstNote) / ticksPerQuarter;
        beat_accent_signaled = currentBeat;
    }

    void OnMidiEvent (MPTKEvent midiEvent)
    {
        switch (midiEvent.Command)
        {
            case MPTKCommand.NoteOn:
                long currentTick2 = (midiFilePlayer.MPTK_TickCurrent - tickFirstNote) / ticksPerQuarter;
                if (accentBeats.Contains(currentTick2) && (currentTick2 - beat_accent_signaled <= 1.5)) {
                        midiEvent.Velocity = 127;
                    }
                else {
                    // To keep the overall piece quieter, so that accents stand out
                    midiEvent.Velocity = midiEvent.Velocity / 2;
                }
                // Note that velocity in MIDI ranges from 0 to 127;
                // setting the variable to an int above 127
                // makes the synth produce nothing

                // Debug.Log(midiEvent.Velocity);

            break;
        }
    }
}
