// Unity and System Stuff
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SvSFix.Tools;

public class ResolutionManager : MonoBehaviour
{
    public bool enableDebug = false;
    public struct Resolution
    {
        // // Example Usage:
        // static Resolution resolutionNew = new Resolution(1920, 1080);
        // private int resolutionNewX      = resolutionNew.Width;
        public int Width { get; set; }
        public int Height { get; set; }

        public Resolution(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
    }

    public static List<ResolutionManager.Resolution> ScreenResolutions()
    {
        var eResolutions = Screen.resolutions.Where(resolution => resolution.refreshRate == Screen.currentResolution.refreshRate); // Filter out any resolution that isn't supported by the current refresh rate.
        eResolutions.OrderBy(s => s); // Order by least to greatest.
        var aScreenResolutions = eResolutions as UnityEngine.Resolution[] ?? eResolutions.ToArray(); // Convert our Enumerable to an Array.
        var screenResolutions = new List<ResolutionManager.Resolution>(); // Creates the List we will be sorting resolutions in.
        for (int i = 0; i < aScreenResolutions.Length; i++) { // Run a for loop for each screen resolution in the array, since Unity's resolutions are incompatible with our own.
            var screenResolution = new ResolutionManager.Resolution(aScreenResolutions[i].width, aScreenResolutions[i].height);
            screenResolutions.Add(screenResolution);
        }

        // Our Hardcoded list of resolutions. We plan on appending these values to our resolution list only if the largest available display resolution is greater than one of these.
        var aHcResolutions = new ResolutionManager.Resolution[14];
        var hcResolutions   = new List<ResolutionManager.Resolution>();
        aHcResolutions[0].Width  = 640;   aHcResolutions[0].Height  = 360;
        aHcResolutions[1].Width  = 720;   aHcResolutions[1].Height  = 405;
        aHcResolutions[2].Width  = 800;   aHcResolutions[2].Height  = 450;
        aHcResolutions[3].Width  = 960;   aHcResolutions[3].Height  = 540;
        aHcResolutions[4].Width  = 1024;  aHcResolutions[4].Height  = 576;
        aHcResolutions[5].Width  = 1152;  aHcResolutions[5].Height  = 648;
        aHcResolutions[6].Width  = 1280;  aHcResolutions[6].Height  = 720;
        aHcResolutions[7].Width  = 1360;  aHcResolutions[7].Height  = 765;
        aHcResolutions[8].Width  = 1366;  aHcResolutions[8].Height  = 768;
        aHcResolutions[9].Width  = 1600;  aHcResolutions[9].Height  = 900;
        aHcResolutions[10].Width = 1920;  aHcResolutions[10].Height = 1080;
        aHcResolutions[11].Width = 2560;  aHcResolutions[11].Height = 1440;
        aHcResolutions[12].Width = 3840;  aHcResolutions[12].Height = 2160;
        aHcResolutions[13].Width = 7680;  aHcResolutions[13].Height = 4320;
        for (int i = 0; i < aHcResolutions.Length; i++) {
            hcResolutions.Add(aHcResolutions[i]);
        }
        int screenResolutionsCount = screenResolutions.Count - 1;
        for (int i = 0; i < hcResolutions.Count; i++) {
            if (screenResolutions[screenResolutionsCount].Width + screenResolutions[screenResolutionsCount].Height >
                hcResolutions[i].Width + hcResolutions[i].Height) {
                screenResolutions.Add(hcResolutions[i]);
            }
        }
        var resolutions = screenResolutions.Distinct().ToList();

        var resSort = from r in resolutions orderby r.Width + r.Height ascending select r;
        var resolutionsSorted   = new List<ResolutionManager.Resolution>();
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
            if (enableDebug) { Debug.Log(sr[i].Width + "x" + sr[i].Height); } // In this case, print a debug log to show we are doing things right.
        }
    }
}