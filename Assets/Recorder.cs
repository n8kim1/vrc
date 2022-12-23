using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// Manages the spatial anchors of the project
/// </summary>
public class Recorder : MonoBehaviour
{
    float[,] array;
    int record_idx;

    // Start is called before the first frame update
    void Start()
    {
        array = new float[30*60*10, 1+2*3+2*4]; // 10 minutes total.
        // TODO check, can you really hold this in memory? OTOH how laggy is streamwriter?
        record_idx = 0;
    }

    void FixedUpdate()
    {
        OVRInput.FixedUpdate();
    }

    /// <summary>
    /// Update
    /// </summary>
    void Update()
    {
        OVRInput.Update();
        // OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);

        bool trigger1Pressed = OVRInput.GetDown(OVRInput.Button.One);
        bool trigger2Pressed = OVRInput.GetDown(OVRInput.Button.Three);

        //if the user has pressed the index trigger on one of the two controllers, generate an object in that position
        if (trigger1Pressed)
            GenerateObject(true);

        if (trigger2Pressed)
            GenerateObject(false);
    }

    /// <summary>
    /// Generates an object with the same pose of the controller
    /// </summary>
    /// <param name="isLeft">If the controller to take as reference is the left or right one</param>
    private void GenerateObject(bool isLeft)
    {
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
        Debug.Log("Button pressed");
        
        OVRDebug.Log("Button pressed");
        
        // TODO check. should be good enough precision (since docs use this in sub-second precision)
        // but I wanna double chck
        array[record_idx, 0] = Time.time;
        
        if (record_idx == 290)
        {
            string fname = System.DateTime.Now.ToString("HH-mm-ss") + ".csv";
            string path = Path.Combine(Application.persistentDataPath, fname);
            StreamWriter file = new StreamWriter(path);

            for (int i=0; i<290; i++) {
                for (int j=0; j<8; j++) {
                    file.Write(array[i, j]);
                }
                file.WriteLine();
            }
            file.Close();

            // TODO consider a simpler writer
        }

        else
        {
            record_idx += 1;
        }
        

        //get the pose of the left controller in local tracking coordinates
        OVRPose objectPose = new OVRPose()
        {
            position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch),
            orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch)
        };

        // TODO do i even need to convert? Consider not, cuz faster

        //Convert it to world coordinates
        OVRPose worldObjectPose = OVRExtensions.ToWorldSpacePose(objectPose);
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
        worldObjectPose = OVRExtensions.ToWorldSpacePose(objectPose);
        array[record_idx, 1+7] = objectPose.position.x;
        array[record_idx, 2+7] = objectPose.position.y;
        array[record_idx, 3+7] = objectPose.position.z;
        array[record_idx, 4+7] = objectPose.orientation.w;
        array[record_idx, 5+7] = objectPose.orientation.x;
        array[record_idx, 6+7] = objectPose.orientation.y;
        array[record_idx, 7+7] = objectPose.orientation.z;


        // TODO optimization for later: 5(?) arrays, simply directly write.
        // need to check if, eg, a quat is simply writable like that or if i have to destructure

        // Legacy -- visual feedback ig
        GameObject.Instantiate(Resources.Load<GameObject>(isLeft ? "Object1" : "Object2"),
            worldObjectPose.position,
            worldObjectPose.orientation
        );
    }
}
