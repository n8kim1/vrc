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

    bool isInitializeQueued = true;

    // For computing beat/measure within piece
    long tickFirstNote;

    long ticksPerQuarter;

    readonly HashSet<float> accentBeats = new();
    readonly int accentBeatFrequency = 2;
    float signaledAccentBeat = -2;

    private int piece_choice_idx = 0;
    private readonly int piece_choice_len = 2;

    public void ChoosePrevPiece()
    {
        piece_choice_idx -= 1;
        if (piece_choice_idx <= 0) { piece_choice_idx = piece_choice_len - 1; }
    }

    public void ChooseNextPiece()
    {
        piece_choice_idx += 1;
        if (piece_choice_idx >= piece_choice_len) { piece_choice_idx = 0; }
    }

    public int GetPieceChoiceIdx()
    {
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
        }
        else if (piece_choice_idx == 1)
        {
            midiFilePlayer.MPTK_MidiName = "eine_kle_prep_measure";
        }

        Debug.Log("Initializing playing, piece " + midiFilePlayer.MPTK_MidiName);
        MidiLoad midiLoaded = midiFilePlayer.MPTK_Load();

        if (midiLoaded != null)
        {
            ticksPerQuarter = midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
            tickFirstNote = midiFilePlayer.MPTK_TickFirstNote;
            metronomeScheduled.InitBpm((float)midiLoaded.MPTK_InitialTempo);

            string infoMidi = "MIDI file's given duration: " + midiLoaded.MPTK_Duration.TotalSeconds + " seconds\n";
            infoMidi += "MIDI file's given tempo: " + midiLoaded.MPTK_InitialTempo + "\n";
            Debug.Log(infoMidi);
        }

        // Max out volume, for best signal clarity; users can always reduce as needed
        midiFilePlayer.MPTK_Volume = 1.0f;
        InitializeAccents();

        isInitializeQueued = false;

        spatialAnchorsGenFake.StartRecording();
    }

    float GetCurrentQuarterBeat()
    {
        if (ticksPerQuarter == 0) { return 0; }
        return (midiFilePlayer.MPTK_TickCurrent - tickFirstNote) / ticksPerQuarter;
    }

    void PlayPiece()
    {
        midiFilePlayer.MPTK_Play();
        mainText.text = "playing";
    }

    void PausePiece()
    {
        midiFilePlayer.MPTK_Pause();
        mainText.text = "pausing...";
    }

    void ResetPlayer()
    {
        mainText.text = "resetting...";
        // Yes, stop method, not reset, as
        // this is the method that stops and resets to beginning)
        midiFilePlayer.MPTK_Stop();
        spatialAnchorsGenFake.StopRecording();
        isInitializeQueued = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Handle inputs

        // Check for both headset input and laptop-keyboard input (in debugging)
        bool playTogglePressed = OVRInput.GetDown(OVRInput.RawButton.X) || Input.GetKeyDown(KeyCode.X);
        if (playTogglePressed)
        {
            Debug.Log("Play input was pressed", this);
            if (isInitializeQueued) { InitializePiece(); }
            if (!midiFilePlayer.MPTK_IsPlaying || (midiFilePlayer.MPTK_IsPlaying && midiFilePlayer.MPTK_IsPaused))
            {
                PlayPiece();
            }
            else { PausePiece(); }
        }

        bool resetPressed = OVRInput.GetDown(OVRInput.RawButton.Y) || Input.GetKeyDown(KeyCode.Y);
        if (resetPressed) { ResetPlayer(); }

        // Update state as needed 
        if (midiFilePlayer.MPTK_IsPlaying)
        {
            mainText.text = "q: " + GetCurrentQuarterBeat();
        }
    }

    public void SetTempo(double tempo)
    {
        midiFilePlayer.MPTK_Tempo = tempo;
        Debug.Log("tempo set to " + tempo);
    }

    public void SetVolume(float volume)
    {
        midiFilePlayer.MPTK_Volume = volume;
    }

    public void SignalAccent()
    {
        signaledAccentBeat = GetCurrentQuarterBeat();
    }

    // Process each midi event on the fly, according to conductor's signals and inputs
    public bool OnMidiEvent(MPTKEvent midiEvent)
    {
        switch (midiEvent.Command)
        {
            case MPTKCommand.NoteOn:
                float currentBeat = GetCurrentQuarterBeat();

                // Control accents on the note to play, according to what was signaled
                // Allow some margin of error for signaling accent beats
                if (accentBeats.Contains(currentBeat) && (currentBeat - signaledAccentBeat <= 1.5))
                {
                    // Max velocity, for accent to stand out
                    midiEvent.Velocity = 127;
                }
                else
                {
                    // To keep the overall piece quieter, so that accents stand out
                    midiEvent.Velocity /= 2;
                }

                break;
        }

        return true;
    }
}
