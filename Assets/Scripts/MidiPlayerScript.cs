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

    readonly HashSet<long> accentBeats = new();
    readonly int accentBeatFrequency = 2;
    long signaledAccentBeat = -2;

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

    void InitializePiece()
    {
        // Load proper piece and initialize data.
        if (piece_choice_idx == 0)
        {
            midiFilePlayer.MPTK_MidiName = "eine_kle_mvt_2_tempo_prep";
            // TODO read this properly from MIDI files
            metronomeScheduled.InitBpm(60.0f);
        }

        else if (piece_choice_idx == 1)
        {
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

    void PlayPiece()
    {
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

    void PausePiece()
    {
        midiFilePlayer.MPTK_Pause();
        mainText.text = "pausing...";
        isPlaying = false;
    }

    void ResetPlayer()
    {
        mainText.text = "resetting...";

        // Yes, stop method, not reset, as
        // this is the method that stops and resets to beginning)
        midiFilePlayer.MPTK_Stop();

            // TODO I couldn't find an "isPlaying" attribute of the midiFilePlayer class
            // so we have to keep track of this manually.
            // would be much better if not...
            isPlaying = false;

            spatialAnchorsGenFake.StopRecording();

            // TODO externally tracking this is rough
            isResetQueued = true;
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
        bool playTogglePressed = OVRInput.GetDown(OVRInput.RawButton.X) || Input.GetKeyDown(KeyCode.X);

        // TODO restructure all of these blocks into helper methods. To see control flow more at a glance
        if (playTogglePressed)
        {
            Debug.Log("Play input was pressed", this);

            if (isResetQueued)
            {
                InitializePiece();
            }
            if (!isPlaying)
            {
                PlayPiece();
            }
            else
            {
                PausePiece();
            }
        }

        bool resetPressed = OVRInput.GetDown(OVRInput.RawButton.Y) || Input.GetKeyDown(KeyCode.Y);

        if (resetPressed)
        {
            ResetPlayer();
        }

        // TODO extract currentBeat calculation to a helper
        currentTick = midiFilePlayer.MPTK_TickCurrent;
        currentQuarter = (currentTick - tickFirstNote) / ticksPerQuarter;
        mainText.text = "q: " + currentQuarter;
    }

    public void SetTempo(double tempo)
    {
        midiFilePlayer.MPTK_Tempo = tempo;
        Debug.Log("tempo set to " + tempo);
    }

    public void SetVolume(float volume){
        midiFilePlayer.MPTK_Volume = volume;
    }

    public void SignalAccent () {
        // TODO extract currentBeat calculation to a helper
        long currentBeat = (midiFilePlayer.MPTK_TickCurrent - tickFirstNote) / ticksPerQuarter;
        signaledAccentBeat = currentBeat;
    }

    // Edit midi events on the fly, according to conductor's signals and inputs
    public bool OnMidiEvent(MPTKEvent midiEvent)
    {
        switch (midiEvent.Command)
        {
            case MPTKCommand.NoteOn:
                long currentBeat = (midiFilePlayer.MPTK_TickCurrent - tickFirstNote) / ticksPerQuarter;

                // Control accents on the note to play, according to what was signaled
                // Allow some margin of error for signaling accent beats
                if (accentBeats.Contains(currentBeat) && (currentBeat - signaledAccentBeat <= 1.5))
                {
                    // Max velocity, for accent to stand out
                    midiEvent.Velocity = 127;
                }
                else {
                    // To keep the overall piece quieter, so that accents stand out
                    midiEvent.Velocity /= 2;
                }
                break;
        }

        return true;
    }
}
