using System.Diagnostics;
using UnityEngine;

public class FrameTimer : MonoBehaviour
{
    public static FrameTimer instance;
    private Stopwatch stopwatch;

    public long FrameDuration
    {
        get
        {
            if (this.stopwatch == null)
            {
                return 0;
            }
            else
            {
                return this.stopwatch.ElapsedMilliseconds;
            }
        }
    }

    void Awake()
    {
        instance = this;
        this.stopwatch = new Stopwatch();
    }

    void Update()
    {
        this.stopwatch.Reset();
        this.stopwatch.Start();
    }
}