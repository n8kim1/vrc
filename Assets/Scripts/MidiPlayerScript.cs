// Controls the playing of MIDI notes. 
// (Also controls other related scripts and objects as needed,
// such as piece selection)

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

    // TODO try switching out for midiFilePlayer.is_playing
    bool isPlaying = false;
    bool isResetQueued = true;

    // For computing beat/measure within piece
    long tickFirstNote;

    long ticksPerQuarter;
    long currentTick;
    // Float, so decimals can be nicely displayed
    float currentQuarter;
    // To track last played tick, to prevent accidentally going backwards 
    // (starts at -1, which is never played)
    // TODO Keeping track of this, and using this, might be redundant
    long lastTick = -1;

    readonly HashSet<long> accentBeats = new();
    readonly int accentBeatFrequency = 2;
    long beat_accent_signaled = -2;

    private int piece_choice_idx = 0;
    private readonly int piece_choice_len = 2;

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
        // Disable all tempo changes in piece, 
        // so that conductor (and tempo changing script) can control these
        midiFilePlayer.MPTK_EnableChangeTempo = false;

        midiFilePlayer.MPTK_StartPlayAtFirstNote = true;

        midiFilePlayer.OnMidiEvent = OnMidiEvent;

        // Initialize text
        // Perhaps inefficient, but eh it's only run once
        mainText.text = "Welcome!";
        bottomText.text = "";
        bottomText.text += "X to play/pause, Y to stop and reset" + "\n";
        bottomText.text += "LH stick up/down to select setting" + "\n";
        bottomText.text += "LH stick left/right to change setting" + "\n";
    }

    void InitializeAccents()
    {
        // Currently adds accents on every n'th beat.
        // Better would be to work this into MIDI info
        for (int i = 0; i <= 282 * 2 * 2; i += accentBeatFrequency)
        {
            accentBeats.Add(i);
        }

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
            // TODO use specific X button here. Similar for other buttons too
            OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch) || Input.GetKeyDown(KeyCode.P);

        // TODO restructure all of these blocks into helper methods. To see control flow more at a glance
        if (playTogglePressed)
        {
            Debug.Log("Play input was pressed", this);

            if (isResetQueued)
            {
                // Load proper piece and initialize data.
                if (piece_choice_idx == 0) {
                    midiFilePlayer.MPTK_MidiName = "eine_kle_mvt_2_tempo_prep";
                    // TODO read this properly from MIDI files
                    metronomeScheduled.InitBpm(60.0f);
                }

                else if (piece_choice_idx == 1) {
                    midiFilePlayer.MPTK_MidiName = "eine_kle_prep_measure";
                    // TODO read this properly from MIDI files
                    metronomeScheduled.InitBpm(150.0f);
                }

                Debug.Log("Initializing playing, piece " + midiFilePlayer.MPTK_MidiName);
                MidiLoad midiloaded = midiFilePlayer.MPTK_Load();

                // Print some info about the MIDI.
                // Is mainly useful for debugging and proof-of-concept/demo.
                if (midiloaded != null)
                {
                    string infoMidi = "MIDI file's given duration: " + midiloaded.MPTK_Duration.TotalSeconds + " seconds\n";
                    infoMidi += "MIDI file's given tempo: " + midiloaded.MPTK_InitialTempo + "\n";
                    Debug.Log(infoMidi);
                    ticksPerQuarter = midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
                    tickFirstNote = midiFilePlayer.MPTK_TickFirstNote;
                    Debug.Log(tickFirstNote);
                    Debug.Log(ticksPerQuarter);
                }

                // TODO better volume controls?
                midiFilePlayer.MPTK_Volume = 0.5f;
                InitializeAccents();

                isResetQueued = false;
            }
            if (!isPlaying) {
                midiFilePlayer.MPTK_Play();

                // Get tempo-related values, which now hold valid data,
                // now that we're playing
                // (Note: Before the midi player begins,
                // these values are set to 0)
                ticksPerQuarter = midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
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

    public void SignalAccent () {
        long currentBeat = (midiFilePlayer.MPTK_TickCurrent - tickFirstNote) / ticksPerQuarter;
        beat_accent_signaled = currentBeat;
    }

    public bool OnMidiEvent(MPTKEvent midiEvent)
    {
        switch (midiEvent.Command)
        {
            case MPTKCommand.NoteOn:
                long currentBeat = (midiFilePlayer.MPTK_TickCurrent - tickFirstNote) / ticksPerQuarter;
                if (accentBeats.Contains(currentBeat) && (currentBeat - beat_accent_signaled <= 1.5))
                {
                    // Max velocity, for accent to stand out
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

        return true;
    }
}
