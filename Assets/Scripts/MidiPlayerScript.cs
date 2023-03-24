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
