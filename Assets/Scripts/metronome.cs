using UnityEngine;

// Source: a simplification of 
// https://docs.unity3d.com/ScriptReference/AudioSettings-dspTime.html
// Note that dspTime suffers from inaccuracies. 
// Many sources online, such as https://forum.unity.com/threads/do-people-not-realize-how-bad-audiosource-dsptime-is-can-someone-explain-how-it-works.402308/#post-5486028

// The code example shows how to implement a metronome that procedurally generates the click sounds via the OnAudioFilterRead callback.
// While the game is paused or suspended, this time will not be updated and sounds playing will be paused. Therefore developers of music scheduling routines do not have to do any rescheduling after the app is unpaused

[RequireComponent(typeof(AudioSource))]
public class Metronome : MonoBehaviour
{
    public double bpm = 140.0F;
    public float gain = 0.5F;
    private double nextTick = 0.0F;
    private float amp = 0.0F;
    private float phase = 0.0F;
    private double sampleRate = 0.0F;
    private bool running = false;
    void Start()
    {
        double startTick = AudioSettings.dspTime;
        sampleRate = AudioSettings.outputSampleRate;
        nextTick = startTick * sampleRate;
        // Toggle to false for this "metronome" to stop producing audio
        running = true;
        // running = false;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;

        double samplesPerTick = sampleRate * 60.0F / bpm;
        double sample = AudioSettings.dspTime * sampleRate;
        // data is interleaved; 
        // dataLen gets the number of samples per channel
        int dataLen = data.Length / channels;
        for (int n = 0; n < dataLen; n++)
        {
            float x = gain * amp * Mathf.Sin(phase);
            int i = 0;
            while (i < channels)
            {
                data[n * channels + i] += x; 
                i++;
            }
            while (sample + n >= nextTick)
            {
                nextTick += samplesPerTick;
                amp = 1.0F;
            }
            phase += 0.125F;
            // To approximate exponential decay over time
            amp *= 0.9999F;
        }
    }
}