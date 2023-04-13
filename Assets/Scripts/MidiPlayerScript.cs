using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;

public class MidiPlayerScript : MonoBehaviour
{
    public MidiFilePlayer midiFilePlayer;

    // Start is called before the first frame update
    void Start()
    {
        // TODO verify that this _works_
        // IDK whether this has to be called before/after play.
        // Easiest is to find a midi that definitely changes tempo early and noticeably
        midiFilePlayer.MPTK_EnableChangeTempo = false;

        MidiLoad midiloaded = midiFilePlayer.MPTK_Load();

        // TODO I can't get original tempo to work fsr...
        // Might have to do with timing on load or play or something
        // midiFilePlayer.MPTK_Tempo = 140;

        // Print some info about the MIDI.
        // Is mainly useful for debugging and proof-of-concept/demo.
        // if (midiloaded != null)
        // {
        //     infoMidi = "Duration: " + midiloaded.MPTK_Duration.TotalSeconds + " seconds\n";
        //     infoMidi += "Tempo: " + midiloaded.MPTK_InitialTempo + "\n";
        //     List<MPTKEvent> listEvents = midiloaded.MPTK_ReadMidiEvents();
        //     infoMidi += "Count MIDI Events: " + listEvents.Count + "\n";
        //     Debug.Log(infoMidi);
        // }

        midiFilePlayer.MPTK_Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("p key was pressed");
                midiFilePlayer.MPTK_Play();
            }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("t key was pressed, slowing tempo");
            midiFilePlayer.MPTK_Tempo *= 0.7;
        }
    }

    public void SetTempo(double tempo)
    {
        midiFilePlayer.MPTK_Tempo = tempo;
        Debug.Log("tempo set in MidiPlayerScript");
    }

    // To test whether this is being called anyways
    public void PrintDebug()
    {
        Debug.Log("printdebug called");
    }
}
