using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
 
// TODO docstrings
// TOOD better names lmao
/// <summary>
/// Manages the spatial anchors of the project
/// </summary>
public class SpatialAnchorsGenFake : MonoBehaviour
{  
    // TODO make these constants for performance reasons
    static int framerate = 70; // THIS IS AN APPROXIMATION!
    // Tweak as necessary based on how the code runs.
    // TODO consider using fixedUpdate

    static int minutes_recording = 2; // tweak in case of memory issues
    static int len_recording = framerate*60*minutes_recording;

    // Data exported as 
    // ts, LH pos, LH orient quats, RH pos, RH orient quats
    static int width_recording = 1+3+4+3+4;

    public TMP_Text displayText;
    public TMP_Text debugText;


    float[,] array = new float[len_recording, width_recording];
    int record_idx = 0;
    bool is_recording = false;

    bool in_peak = false;
    int peak_counter = 0;
    float curTime = 0;
    float lastTime = 0;
    float lastX = 0;
    float lastY = 0;
    float lastZ = 0;

    // references to metronome stuff
    public MetronomeScheduled metronomeScheduled;


    void Start() {
        is_recording = true;

        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
    }

    /// <summary>
    /// Update
    /// </summary>
    void Update()
    {
        OVRInput.Update();

        if (is_recording)
        {
            // check button presses
            bool trigger1Pressed = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
            if (trigger1Pressed || (record_idx >= len_recording - 10)) {
                StopRecording();
            }

            // TODO conside building each row of array incrementally
            // eg 
            // row[0] = Time.time
            // ...
            // array[record_idx] = row
            // Might be helpful for performance

            curTime = Time.time;
            array[record_idx, 0] = curTime;

            // Get left position
            OVRPose objectPose = new OVRPose()
            {
                position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch),
                orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch)
            };

            // TODO consider converting to world coordinates. 
            // Probably unnecessary, just use translations, unless scale is different.
            // What's the diff btwn the two coordinates? Need to google
            // worldObjectPose = OVRExtensions.ToWorldSpacePose(objectPose);

            // TODO consider saving a row directly.
            // Depends on if position can be exposed as a row, 
            // and then also this probably requires 4 distinct arrays 
            // cuz destructring hard.
            array[record_idx, 1] = objectPose.position.x;
            array[record_idx, 2] = objectPose.position.y;
            array[record_idx, 3] = objectPose.position.z;
            array[record_idx, 4] = objectPose.orientation.w;
            array[record_idx, 5] = objectPose.orientation.x;
            array[record_idx, 6] = objectPose.orientation.y;
            array[record_idx, 7] = objectPose.orientation.z;


            // sim for right
            objectPose = new OVRPose()
            {
                position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch),
                orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch)
            };
            array[record_idx, 1 + 7] = objectPose.position.x;
            array[record_idx, 2 + 7] = objectPose.position.y;
            array[record_idx, 3 + 7] = objectPose.position.z;
            array[record_idx, 4 + 7] = objectPose.orientation.w;
            array[record_idx, 5 + 7] = objectPose.orientation.x;
            array[record_idx, 6 + 7] = objectPose.orientation.y;
            array[record_idx, 7 + 7] = objectPose.orientation.z;

            // TODO rename velo to RH speed
            float velo = Mathf.Pow(lastX - objectPose.position.x, 2) + Mathf.Pow(lastY - objectPose.position.y, 2) + Mathf.Pow(lastZ - objectPose.position.z, 2);
            // using Unity's Math package
            velo = Mathf.Pow(velo, 0.5f);
            velo = velo / (curTime - lastTime);

            // display some stuff for debug
            // TODO flag this but oh well
            if (true)
            {
                // debugText.text = "RH spd: " + (Mathf.Round(velo*10)/10).ToString() + "\n";
            }

            if (in_peak) {
                if (velo < 1.5) {
                    peak_counter = peak_counter+1;
                    if (peak_counter >= 2) {
                        in_peak = false;
                        peak_counter  = 0;
                    }
                }
            }

            else {
                if (velo > 1.5) {
                    peak_counter = peak_counter+1;
                    if (peak_counter >= 2) {
                        in_peak = true;
                        Debug.Log("beat was signaled");
                        metronomeScheduled.AskForBeat();

                        // TODO schedule a quick turn-off
                        OVRInput.SetControllerVibration(1, 0.1f, OVRInput.Controller.RTouch);
                        peak_counter = 0;
                    }
                }
            }

            // Prep for next iter
            lastTime = curTime; 
            // TODO sigh ^
            lastX = objectPose.position.x;
            lastY = objectPose.position.y;
            lastZ = objectPose.position.z;

            // Alternatively, for debug, allow for keypresses to simulate signaled beats.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("space key was pressed");
                metronomeScheduled.AskForBeat();

            }

            // prep for the next loop
            record_idx += 1;
        }


    }

    private void StopRecording()
    {
        is_recording = false;

        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
        displayText.text = "Stopping...";

        string fname = System.DateTime.Now.ToString("HH-mm-ss") + ".csv";
        string path = Path.Combine(Application.persistentDataPath, fname);
        StreamWriter file = new StreamWriter(path);

        for (int i = 0; i < len_recording - 10; i++)
        {
            for (int j = 0; j < width_recording; j++)
            {
                file.Write(array[i, j]);
                file.Write(",");
            }
            file.WriteLine();
        }
        file.Close();

        displayText.text = "Dumped recording, hopefully!";
    }
}