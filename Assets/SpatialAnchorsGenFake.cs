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

    void Start() {
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
    }

    /// <summary>
    /// Update
    /// </summary>
    void Update()
    {
        OVRInput.Update();

        bool trigger1Pressed = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
        bool trigger2Pressed = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
 
        //if the user has pressed the index trigger on one of the two controllers, generate an object in that position
        if (trigger1Pressed) {
            GenerateObject(true);
        }
 
        if (trigger2Pressed) {
            GenerateObject(false);
        }

    }
 
    /// <summary>
    /// Generates an object with the same pose of the controller
    /// </summary>
    /// <param name="isLeft">If the controller to take as reference is the left or right one</param>
    private void GenerateObject(bool isLeft)
    {
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
        displayText.text = "press!";
        // //get the pose of the controller in local tracking coordinates
        // OVRPose objectPose = new OVRPose()
        // {
        //     position = OVRInput.GetLocalControllerPosition(isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch),
        //     orientation = OVRInput.GetLocalControllerRotation(isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch)
        // };
 
        // //Convert it to world coordinates
        // OVRPose worldObjectPose = OVRExtensions.ToWorldSpacePose(objectPose);
 
    
    }
}