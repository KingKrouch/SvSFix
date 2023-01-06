using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
// Framerate Cap Stuff
using System.Runtime.InteropServices;
using System.Threading;
// SteamInput and Input Stuff
using IF.Steam;
using Steamworks;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

namespace KingKrouch.Utility.Helpers;

public class InputManager : MonoBehaviour
{
    public static bool steamInputInitialized;
    public InputHandle_t[] inputHandles = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
    
    private void Start()
    { 
        InitInput();
    }
    
    public void InitInput()
    {
        bool initialized = SteamworksAccessor.IsSteamworksReady;
        if (initialized)
        {
            steamInputInitialized = SteamInput.Init(false);
            if (steamInputInitialized)
            {
                SteamInput.RunFrame();
                int result = SteamInput.GetConnectedControllers(inputHandles);
                //foreach (var controller in inputHandles) {
                //ESteamInputType inputType = SteamInput.GetInputTypeForHandle(controller);
                //Debug.Log(inputType + " is being used.");
                //}
                // Grabs Player 1 Controller Type.
                var inputType = SteamInput.GetInputTypeForHandle(inputHandles[0]);
                switch (inputType)
                {
                    case ESteamInputType.k_ESteamInputType_Unknown:
                        break;
                    case ESteamInputType.k_ESteamInputType_SteamController:
                        break;
                    case ESteamInputType.k_ESteamInputType_XBox360Controller:
                        break;
                    case ESteamInputType.k_ESteamInputType_XBoxOneController:
                        break;
                    case ESteamInputType.k_ESteamInputType_PS4Controller:
                        SteamInput.SetLEDColor(inputHandles[0], 147, 117, 219, 0);
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchJoyConPair:
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchJoyConSingle:
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchProController:
                        break;
                    case ESteamInputType.k_ESteamInputType_PS3Controller:
                        break;
                    case ESteamInputType.k_ESteamInputType_PS5Controller:
                        SteamInput.SetLEDColor(inputHandles[0], 147, 117, 219, 0);
                        break;
                    case ESteamInputType.k_ESteamInputType_SteamDeckController:
                        break;
                    case ESteamInputType.k_ESteamInputType_Count:
                        break;
                    case ESteamInputType.k_ESteamInputType_MaximumPossibleValue:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (SteamUtils.IsSteamRunningOnSteamDeck())
                {
                    Debug.Log("Running on Steam Deck!");
                }
            }
            if (!initialized || !steamInputInitialized)
            {
                Debug.Log(UnityEngine.InputSystem.Gamepad.all[0].device.name);
                switch (UnityEngine.InputSystem.Gamepad.all[0].device)
                {
                    case DualSenseGamepadHID dualSenseGamepadHid:
                        var dualSense = DualShockGamepad.current;
                        dualSense?.SetLightBarColor(new Color(147, 117, 219));
                        break;
                    case DualShock3GamepadHID dualShock3GamepadHid:
                        break;
                    case DualShock4GamepadHID dualShock4GamepadHid:
                        var dualShock = DualShockGamepad.current;
                        dualShock?.SetLightBarColor(new Color(147, 117, 219));
                        break;
                    case SwitchProControllerHID switchProControllerHid:
                        break;
                    case XInputControllerWindows xInputControllerWindows:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}

public class ResolutionManager : MonoBehaviour
{
    public bool enableDebug = false;
    public struct resolution
    {
        public int width;
        public int height;
    }
    public static List<resolution> ScreenResolutions()
    {
        var eResolutions = Screen.resolutions.Where(resolution => resolution.refreshRate == Screen.currentResolution.refreshRate); // Filter out any resolution that isn't supported by the current refresh rate.
        eResolutions.OrderBy(s => s); // Order by least to greatest.
        var aScreenResolutions = eResolutions as Resolution[] ?? eResolutions.ToArray(); // Convert our Enumerable to an Array.
        var screenResolutions = new List<resolution>(); // Creates the List we will be sorting resolutions in.
        for (int i = 0; i < aScreenResolutions.Length; i++) { // Run a for loop for each screen resolution in the array, since Unity's resolutions are incompatible with our own.
            var screenResolution = new resolution {
                width = aScreenResolutions[i].width,
                height = aScreenResolutions[i].height
            };
            screenResolutions.Add(screenResolution);
        }

        // Our Hardcoded list of resolutions. We plan on appending these values to our resolution list only if the largest available display resolution is greater than one of these.
        var aHcResolutions = new resolution[14];
        var hcResolutions   = new List<resolution>();
        aHcResolutions[0].width  = 640;   aHcResolutions[0].height  = 360;
        aHcResolutions[1].width  = 720;   aHcResolutions[1].height  = 405;
        aHcResolutions[2].width  = 800;   aHcResolutions[2].height  = 450;
        aHcResolutions[3].width  = 960;   aHcResolutions[3].height  = 540;
        aHcResolutions[4].width  = 1024;  aHcResolutions[4].height  = 576;
        aHcResolutions[5].width  = 1152;  aHcResolutions[5].height  = 648;
        aHcResolutions[6].width  = 1280;  aHcResolutions[6].height  = 720;
        aHcResolutions[7].width  = 1360;  aHcResolutions[7].height  = 765;
        aHcResolutions[8].width  = 1366;  aHcResolutions[8].height  = 768;
        aHcResolutions[9].width  = 1600;  aHcResolutions[9].height  = 900;
        aHcResolutions[10].width = 1920;  aHcResolutions[10].height = 1080;
        aHcResolutions[11].width = 2560;  aHcResolutions[11].height = 1440;
        aHcResolutions[12].width = 3840;  aHcResolutions[12].height = 2160;
        aHcResolutions[13].width = 7680;  aHcResolutions[13].height = 4320;
        for (int i = 0; i < aHcResolutions.Length; i++) {
            hcResolutions.Add(aHcResolutions[i]);
        }
        int screenResolutionsCount = screenResolutions.Count - 1;
        for (int i = 0; i < hcResolutions.Count; i++) {
            if (screenResolutions[screenResolutionsCount].width + screenResolutions[screenResolutionsCount].height >
                hcResolutions[i].width + hcResolutions[i].height) {
                screenResolutions.Add(hcResolutions[i]);
            }
        }
        var resolutions = screenResolutions.Distinct().ToList();

        var resSort = from r in resolutions orderby r.width + r.height ascending select r;
        var resolutionsSorted   = new List<resolution>();
        foreach (var r in resSort) {
            resolutionsSorted.Add(r);
        }
        return resolutionsSorted;
    }
    // Start is called before the first frame update
    void Start()
    {
        var sr = ResolutionManager.ScreenResolutions().ToList();

        for (int i = 0; i < sr.Count; i++) { // Now we will finally do what we want with the display resolution list.
            if (enableDebug) { Debug.Log(sr[i].width + "x" + sr[i].height); } // In this case, print a debug log to show we are doing things right.
        }
    }
}
    
public class BlackBarController : MonoBehaviour
{
    public bool enableDebug = false;
    //public Camera camera;
    public Image pillarboxLeft;
    public Image pillarboxRight;
    public Image letterboxTop;
    public Image letterboxBottom;
    public float originalAspectRatio = 1.777777777777778f;
    [Range(0.0f, 1.0f)]
    public float opacity = 1.0f;
    public float fadeSpeed = 2.5f;
    void SetupCoordinates()
    {
        float resX = Screen.width; // You can grab a camera and use camera.pixelWidth during editor builds, but Screen calls should be just fine.
        float resY = Screen.height;
        if (enableDebug) { Debug.Log( Screen.width + "x" + Screen.height); }
        float currentAspectRatio = resX / resY;
        float aspectRatioOffset = originalAspectRatio / currentAspectRatio;

        // Set up the Vertical offsets.
        float originalAspectRatioApproximateY = resY * aspectRatioOffset;
        float verticalResDifference = resY - originalAspectRatioApproximateY;

        // Set up the Horizontal offsets.
        float originalAspectRatioApproximateX = resX * aspectRatioOffset;
        float horizontalResDifference = resX - originalAspectRatioApproximateX;
        
        // Set up our top side letterbox.
        letterboxTop.rectTransform.sizeDelta =  new Vector2(0.0f,-(verticalResDifference / 2));
        letterboxTop.rectTransform.anchoredPosition = new Vector2(0.0f, verticalResDifference / 2);
        
        // Set up our bottom side letterbox.
        letterboxBottom.rectTransform.sizeDelta = new Vector2(0.0f, -(verticalResDifference / 2));
        letterboxBottom.rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
        
        // Set up our left side pillarbox.
        pillarboxLeft.rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);; // Positions the left bar on the left side of the screen.
        pillarboxLeft.rectTransform.sizeDelta = new Vector2(horizontalResDifference / 2, 0.0f);

        // Set up our right side pillarbox.
        pillarboxRight.rectTransform.anchoredPosition = new Vector2(-(horizontalResDifference / 2), 0.0f); // Positions the right bar on the right side of the screen.
        pillarboxRight.rectTransform.sizeDelta = new Vector2(horizontalResDifference / 2, 0.0f);
        
        // Toggle our Letterbox and Pillarbox Components based on the display aspect ratio.
        if (currentAspectRatio < originalAspectRatio) {
            pillarboxLeft.enabled = false; pillarboxRight.enabled  = false;
            letterboxTop.enabled  = true;  letterboxBottom.enabled = true;
        }
        else if (currentAspectRatio > originalAspectRatio) {
            pillarboxLeft.enabled = true; pillarboxRight.enabled  = true;
            letterboxTop.enabled  = false; letterboxBottom.enabled = false;
        }
        else {
            pillarboxLeft.enabled = false; pillarboxRight.enabled  = false;
            letterboxTop.enabled  = false; letterboxBottom.enabled = false;
        }
    }
    void SetupOpacity() // Need to set up some events that will fade in or out the opacity based on a set timeframe.
    {
        letterboxTop.color = new Color(0, 0, 0, opacity);
        letterboxBottom.color = new Color(0, 0, 0, opacity);
        pillarboxLeft.color = new Color(0, 0, 0, opacity);
        pillarboxRight.color = new Color(0, 0, 0, opacity);
    }
    void Setup()
    {
        SetupCoordinates();
        SetupOpacity();
    }
    // Fade Out Black Bars event
    public IEnumerator FadeOutBlackBars()
    {
        while (opacity > 0.00f) {
            opacity = opacity - (fadeSpeed * Time.deltaTime);
            yield return null;
        }
    }
    // Fade In Back Bars event
    public IEnumerator FadeInBlackBars()
    {
        while (opacity < 1.00f) {
            opacity = opacity + (fadeSpeed * Time.deltaTime);
            yield return null;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        Setup();
    }
    // Update is called once per frame
    void Update()
    {
        Setup();
    } 
}

public class FramerateLimiter : MonoBehaviour
{
    private FramerateLimiter m_Instance;
    public FramerateLimiter Instance { get { return m_Instance; } }
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

        if (now >= _lastTime)
        {
            _lastTime = now;
            return;
        }
        else
        {
            SpinWait.SpinUntil(() => { return (SystemTimePrecise() >= _lastTime); });
        }
    }
}