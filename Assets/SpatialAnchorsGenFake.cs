using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
 
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

    void Start() {
        len_record = 30 * 60 * 2;
        array = new float[len_record, 1 + 2 * 3 + 2 * 4]; // 10 minutes total.
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

            if (record_idx <= 10)
            {
                displayText.text = objectPose.position.x.ToString();
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
    }
}