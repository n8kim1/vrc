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
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("p key was pressed");
                midiFilePlayer.MPTK_Play();
            }
    }
}
