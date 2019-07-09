using UnityEngine;

class TimeCounter
{
    float start;

    float length;

    public TimeCounter(float length)
    {
        this.length = length;
        if(length != 0)
            Reset();
    }

    public void Reset()
    {
        start = Time.time;
    }

    public bool ready
    {
        get
        {
            return Time.time > start + length;
        }
    }

    public float progress
    {
        get
        {
            return ready || length == 0 ? 1 : (Time.time - start) / length;
        }
    }

    public float timeLeft
    {
        get
        {
            return start + length - Time.time;
        }
    }
}