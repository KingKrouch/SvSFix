// Unity and System Stuff
using System;
using UnityEngine;
// Framerate Cap Stuff
using System.Runtime.InteropServices;
using System.Threading;

namespace SvSFix.Tools;

public class FramerateLimitManager : MonoBehaviour
{
    private FramerateLimitManager m_Instance;
    public FramerateLimitManager Instance { get { return m_Instance; } }
    public double fpsLimit  = 0.0f;

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
    
    private static long SystemTimePrecise()
    {
        long stp = 0;
        GetSystemTimePreciseAsFileTime(out stp);
        return stp;
    }
    
    private long _lastTime = SystemTimePrecise();

    void Awake()
    {
        m_Instance = this;
    }

    void OnDestroy()
    {
        m_Instance = null;
    }

    void Update()
    {
        if (fpsLimit == 0.0) return;
        _lastTime += TimeSpan.FromSeconds(1.0 / fpsLimit).Ticks;
        long now = SystemTimePrecise();

        if (now >= _lastTime) {
            _lastTime = now;
            return;
        }
        else {
            SpinWait.SpinUntil(() => { return (SystemTimePrecise() >= _lastTime); });
        }
    }
}