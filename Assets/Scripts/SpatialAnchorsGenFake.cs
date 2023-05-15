using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
 
// Records the position of controllers, and does everything relevant.
// Eg calculations to compute signaled beats,
// saving data, etc

// TODO better name for this class
public class SpatialAnchorsGenFake : MonoBehaviour
{  
    // TODO make these constants for performance reasons
    static int framerate = 70; // THIS IS AN APPROXIMATION!
    // Tweak as necessary based on how the code runs.
    // TODO consider using fixedUpdate

    static int minutes_recording = 5; // tweak in case of memory issues
    static int len_recording = framerate*60*minutes_recording;

    // Data exported as 
    // ts, LH pos, LH orient quats, RH pos, RH orient quats
    static int width_recording = 1+3+4+3+4;

    // TODO conflict is imminent w the MidiPlayerScript ...
    public TMP_Text displayText;
    public TMP_Text debugText;

    OVRPose leftObjectPose;
    OVRPose rightObjectPose;

    float[,] array = new float[len_recording, width_recording];
    int record_idx = 0;
    bool is_recording = false;

    // Thresholds for defining "high" velocity
    // for acceleration-maxes, beat signals, accents, etc
    float velo_threshold_high = 1.5f;
    // (this is velo peak)
    bool in_peak = false;
    int peak_counter = 0;
    int peak_rising_edge_threshold_frames = 2;
    int peak_falling_edge_threshold_frames = 2;
    // Naively, pressing up/down to adjust a discrete integer
    // will change the integer by 1 per _frame_ which is way too much.
    // To get around this, we save a version of this variable that is controlled,
    // and is a float so it can be controlled in finer amounts.
    // TODO this should reference the above variable. That's hard to do in init tho
    float peak_rising_edge_threshold_frames_in_settings = 2.0f;

    // Should this be accel?
    // Should this only consider y-dir? 
    // Wait this makes no sense for beats that aren't beat 1, right?
    float velo_threshold_accent = 3.0f;
    // TODO if accents remain using a debounced peak detector,
    // this should probably be refactored into some reusable code
    bool in_peak_accent = false;
    int peak_counter_accent = 0;

    // TODO check the threshold on the beat just before the accent beat
    // TODO would need to somehow know on the beat _beforehand_ to accent.
    // Perhaps check if the gestures cross the velo threshold significantly early.
    // Or, could always pass in whenever it's crossed, to the midi player
    // since scheduling the midi player to check on beat is hard.
    // Easier to go "accent detected -> right time" than vv???

    // For computations about velocity, etc
    float curTime = 0;
    float lastTime = 0;
    float lastX = 0;
    float lastY = 0;
    float lastZ = 0;

    // Adjust the "4" as necessary; kinda arbitrary
    float frameskip_time_threshold = 1 / (framerate + 0.0f) / 4;
    // Note the "conversion" to a float. A cast would work too

    // references to other objcts, scripts, etc

    public MetronomeScheduled metronomeScheduled;
    public MidiPlayerScript midiPlayerScript;
    public ColorChanger colorChanger;


    void Start() {
        is_recording = true;

        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
    }

    /// <summary>
    /// Update
    /// </summary>
    void Update()
    {
        // Do _not_ call OVRInput.Update
        // since it's already called in the OVRManager, 
        // and multiple calls to it prevent GetDown, GetUp, and other frame-specific methods
        // to fail (something to do with polling too many times)
        // See, eg, https://lab.popul-ar.com/t/ovr-controller-buttons-not-working/1033
        // OVRInput.Update();
           
        curTime = Time.time;

        // If the current update frame is too close in time to the previous one,
        // then we need to handle it nicely. 
        // (For example, naively, if two frames have the same timestamp, then 
        // the time delta is 0, and speeds are infinite, which is weird.)
        // The simple way is to pretend the frame doesn't exist.
        // More robust fix would involve still recording, or etc
        // Also would need to investigate the lengths of frames that seem odd
        // TODO should really crank out a notebook for this, since it'd be good to write about
        if ((curTime - lastTime) < frameskip_time_threshold)
        {
            // debugText.text = "frame dropped!";
            return;
        }


        // Get left position
        leftObjectPose = new OVRPose()
        {
            position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch),
            orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch)
        };

        // TODO consider converting to world coordinates. 
        // Probably unnecessary, just use translations, unless scale is different.
        // What's the diff btwn the two coordinates? Need to google
        // worldObjectPose = OVRExtensions.ToWorldSpacePose(leftObjectPose);

        // sim for right
        rightObjectPose = new OVRPose()
        {
            position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch),
            orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch)
        };
        // worldObjectPose = OVRExtensions.ToWorldSpacePose(rightObjectPose);


        // TODO rename velo to RH speed
        float velo = Mathf.Pow(lastX - rightObjectPose.position.x, 2) + Mathf.Pow(lastY - rightObjectPose.position.y, 2) + Mathf.Pow(lastZ - rightObjectPose.position.z, 2);
        // using Unity's Math package
        velo = Mathf.Pow(velo, 0.5f);
        velo = velo / (curTime - lastTime);

        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstickUp) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            peak_rising_edge_threshold_frames_in_settings += 0.1f;
            // TODO errr don't let this go below 1
            peak_rising_edge_threshold_frames = (int) peak_rising_edge_threshold_frames_in_settings;
        }
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstickDown) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            peak_rising_edge_threshold_frames_in_settings -= 0.1f;
            // TODO errr don't let this go below 1
            peak_rising_edge_threshold_frames = (int) peak_rising_edge_threshold_frames_in_settings;
        }

        // display some stuff for debug
        debugText.text = "RH spd: " + (Mathf.Round(velo*100)/100).ToString() + "\n";
        debugText.text += "RH pk frame thresh: " + peak_rising_edge_threshold_frames.ToString() + "\n";
        // debugText.text += "timeskip thresh: " + frameskip_time_threshold;
        // TODO display fps

        // Check hand velocity to see if a beat is being signaled.
        if (in_peak) {
            if (velo < velo_threshold_high) {
                peak_counter = peak_counter+1;
                if (peak_counter >= peak_falling_edge_threshold_frames) {
                    in_peak = false;


                    // Stop vibration since we're not in a peak
                    // To do this, set amplitude and freq to 0, per docs
                    OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
                    colorChanger.SetGray();

                    peak_counter  = 0;
                }
            }
        }

        else {
            if (velo > velo_threshold_high) {
                peak_counter = peak_counter+1;
                if (peak_counter >= peak_rising_edge_threshold_frames) {
                    in_peak = true;
                    Debug.Log("beat was signaled");

                    metronomeScheduled.AskForBeat();

                    // Note that vibration stays on until explictly shut off elsewhere
                    // TODO 0.2f is amplitude, the strength of vibration.
                    // Would be nice to make this controllable
                    OVRInput.SetControllerVibration(1, 0.2f, OVRInput.Controller.RTouch);
                    colorChanger.SetBlue();

                    peak_counter = 0;
                }
            }
        }

        // Check hand velocity to see if an accented beat is being signaled.
        if (in_peak_accent) {
            if (velo < velo_threshold_accent) {
                peak_counter_accent = peak_counter_accent+1;
                // TODO make the 2 a variable, possibly configurable in program too
                if (peak_counter_accent >= 2) {
                    in_peak_accent = false;
                    peak_counter_accent = 0;
                }
            }
        }

        else {
            if (velo > velo_threshold_accent) {
                peak_counter_accent = peak_counter_accent+1;
                // TODO make the 2 a variable, possibly configurable in program too
                if (peak_counter_accent >= 2) {
                    in_peak_accent = true;
                    peak_counter_accent = 0;

                    midiPlayerScript.SignalAccent();
                    Debug.Log("Accent requested");

                    // TODO remove the following when debug:
                    colorChanger.SetRed();
                }
            }
        }

        if (is_recording) 
        {
            // 10-frame buffer, to avoid going past the end of the array during the stopping process
            // User-requesting stopping of recording (via button press) is handled from MidiPlayerScript,
            // which handles all button inputs
            if ((record_idx >= len_recording - 10)) {
                StopRecording();
            }

             // TODO conside building each row of array incrementally
            // eg 
            // row[0] = Time.time
            // ...
            // array[record_idx] = row
            // Might be helpful for performance
            array[record_idx, 0] = curTime;

            // TODO consider saving a row directly.
            // Depends on if position can be exposed as a row, 
            // and then also this probably requires 4 distinct arrays 
            // cuz destructring hard.
            array[record_idx, 1] = leftObjectPose.position.x;
            array[record_idx, 2] = leftObjectPose.position.y;
            array[record_idx, 3] = leftObjectPose.position.z;
            array[record_idx, 4] = leftObjectPose.orientation.w;
            array[record_idx, 5] = leftObjectPose.orientation.x;
            array[record_idx, 6] = leftObjectPose.orientation.y;
            array[record_idx, 7] = leftObjectPose.orientation.z;

            array[record_idx, 1 + 7] = rightObjectPose.position.x;
            array[record_idx, 2 + 7] = rightObjectPose.position.y;
            array[record_idx, 3 + 7] = rightObjectPose.position.z;
            array[record_idx, 4 + 7] = rightObjectPose.orientation.w;
            array[record_idx, 5 + 7] = rightObjectPose.orientation.x;
            array[record_idx, 6 + 7] = rightObjectPose.orientation.y;
            array[record_idx, 7 + 7] = rightObjectPose.orientation.z;

            // prep for the next loop
            record_idx += 1;
        }

        // Prep for next iter
        lastTime = curTime; 
        lastX = rightObjectPose.position.x;
        lastY = rightObjectPose.position.y;
        lastZ = rightObjectPose.position.z;

        // Alternatively, for debug, allow for keypresses to simulate signaled beats.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("space key was pressed");
            metronomeScheduled.AskForBeat();
            midiPlayerScript.SetVolume(1.0f);
            colorChanger.SetBlue();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            // cubeMaterial.color = Color.green;
            midiPlayerScript.SetVolume(0.5f);
            colorChanger.SetGray();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("a key was pressed");
            midiPlayerScript.SignalAccent();
            colorChanger.SetRed();
        }
    }

    public void StartRecording()
    {
        array = new float[len_recording, width_recording];
        record_idx = 0;

        is_recording = true;
    }

    public void StopRecording()
    {
        is_recording = false;

        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
        displayText.text = "Stopping...";

        // TODO filename should have YMD as well. 
        // I haven't really cared cuz I can observe file details in Explorer,
        // but it's more efficient
        string fname = System.DateTime.Now.ToString("HH-mm-ss") + ".csv";
        string path = Path.Combine(Application.persistentDataPath, fname);
        StreamWriter file = new StreamWriter(path);

        for (int i = 0; i < len_recording; i++)
        {
            for (int j = 0; j < width_recording; j++)
            {
                file.Write(array[i, j]);
                file.Write(",");
            }
            file.WriteLine();
        }
        file.Close();

        // To access recording, connect to laptop
        // TODO could use better more detailed instructions
        // TODO could also display the path, too
        displayText.text = "Saved recording!";
        Debug.Log("Recording path:" +  path);

        
    }
}