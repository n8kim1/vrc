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

    int framerate = 70; // THIS IS AN APPROXIMATION!
    // Tweak as necessary based on how the code runs.
    // TODO consider using fixedUpdate

    int minutes_recording = 2; // tweak in case of memory issues
    int len_recording = framerate*60*minutes_recording;

    // Data exported as 
    // ts, LH pos, LH orient quats, RH pos, RH orient quats
    int width_recording = 1+3+4+3+4;

    public TMP_Text displayText;
    float[,] array = new float[len_recording, width_recording];;
    int record_idx = 0;
    bool is_recording = false;

    bool in_peak = false;
    int peak_counter = 0;
    float lastTime = 0;
    float lastX = 0;
    float lastY = 0;
    float lastZ = 0;

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
            if (trigger1Pressed or (record_idx >= len_recording - 10)) {
                StopRecording();
            }

            // TODO conside building each row of array incrementally
            // eg 
            // row[0] = Time.time
            // ...
            // array[record_idx] = row
            // Might be helpful for performance

            array[record_idx, 0] = Time.time;

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

            // display some stuff for debug
            // TODO flag this but oh well
            if (record_idx <= 60 * 10)
            {
                displayText.text = objectPose.position.x.ToString() + "\n" + record_idx.ToString();
            }

            float velo = (lastX - objectPose.position.x) * (lastX - objectPose.position.x) + (lastY - objectPose.position.y) * (lastY - objectPose.position.y) + (lastX - objectPose.position.z) * (lastX - objectPose.position.z);
            velo = Math.pow(velo, 0.5);
            velo = velo / (lastTime - array[record_idx, 0]);

            if (in_peak) {
                if (velo < 1.5) {
                    peak_counter = peak_counter+1;
                    if (peak_counter >= 2) {
                        in_peak = false;
                    }
                }
            }

            else {
                if (velo > 1.5) {
                    peak_counter = peak_counter+1;
                    if (peak_counter >= 2) {
                        in_peak = true;
                        OVRInput.SetControllerVibration(1, 0.1, OVRInput.Controller.RTouch);
                    }
                }
            }



            lastTime = array[record_idx, 0]; 
            // TODO sigh ^
            lastX = objectPose.position.x;
            lastX = objectPose.position.y;
            lastX = objectPose.position.z;



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