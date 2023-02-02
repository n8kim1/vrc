using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
 
/// <summary>
/// Manages the spatial anchors of the project
/// </summary>
public class SpatialAnchorsGenFake : MonoBehaviour
{  

    public TMP_Text displayText;
    int len_record;
    float[,] array;
    int record_idx;
    bool is_recording;
    int width_recording;

    void Start() {
        // TODO make framerate var. is closer to, like, 70-80 fps, def not 30...ooof
        len_record = 30 * 60 * 2;
        width_recording = 1 + 2 * 3 + 2 * 4;
        array = new float[len_record, width_recording]; // 10 minutes total.
        // TODO check, can you really hold this in memory? OTOH how laggy is streamwriter?
        record_idx = 0;
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
            if (trigger1Pressed)
            {
                StopRecording();
            }

            array[record_idx, 0] = Time.time;

            // Get left position, converted
            OVRPose objectPose = new OVRPose()
            {
                position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch),
                orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch)
            };
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
            // worldObjectPose = OVRExtensions.ToWorldSpacePose(objectPose);
            array[record_idx, 1 + 7] = objectPose.position.x;
            array[record_idx, 2 + 7] = objectPose.position.y;
            array[record_idx, 3 + 7] = objectPose.position.z;
            array[record_idx, 4 + 7] = objectPose.orientation.w;
            array[record_idx, 5 + 7] = objectPose.orientation.x;
            array[record_idx, 6 + 7] = objectPose.orientation.y;
            array[record_idx, 7 + 7] = objectPose.orientation.z;

            record_idx += 1;

            // display some stuff for debug
            if (record_idx <= 60 * 10)
            {
                displayText.text = objectPose.position.x.ToString() + "\n" + record_idx.ToString();
            }


            if (record_idx >= len_record - 10)
            {
                StopRecording();
            }
        }



    }

    private void StopRecording()
    {
        is_recording = false;

        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
        displayText.text = "press!";

        string fname = System.DateTime.Now.ToString("HH-mm-ss") + ".csv";
        string path = Path.Combine(Application.persistentDataPath, fname);
        StreamWriter file = new StreamWriter(path);

        for (int i = 0; i < len_record - 10; i++)
        {
            for (int j = 0; j < width_recording; j++)
            {
                file.Write(array[i, j]);
                file.Write(",");
            }
            file.WriteLine();
        }
        file.Close();

        displayText.text = "dumped recording, hopefully!";
    }
}