// TODO am I even using the system.collections and .generic imports??
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;
using TMPro;

public class MidiPlayerScript : MonoBehaviour
{
    public MidiFilePlayer midiFilePlayer;

    public TMP_Text mainText;

    bool isPlaying = false;

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

        mainText.text = "";
        mainText.text += "Controls:" + "\n";
        mainText.text += "A to start" + "\n";

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
        // Check for both headset input and laptop-keyboard input (in debugging)
        bool playTogglePressed = 
            OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch) || Input.GetKeyDown(KeyCode.P);

        if (playTogglePressed)
        {
            Debug.Log("Play input was pressed");
            if (!isPlaying) {
                midiFilePlayer.MPTK_Play();
                mainText.text = "playing";
                // TODO I couldn't find an "isPlaying" attribute of the midiFilePlayer class
                // so we have to keep track of this manually.
                // would be much better if not...
                isPlaying = true;
            }
            else {
                midiFilePlayer.MPTK_Pause();
                mainText.text = "pausing";
                isPlaying = false;
            }
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
