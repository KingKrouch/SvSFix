using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class BlackBarController : MonoBehaviour
{
    public Image pillarboxLeft;
    public Image pillarboxRight;
    public Image letterboxTop;
    public Image letterboxBottom;
    public float originalAspectRatio = 1.777778f;
    public Camera mainCamera;
    [Range(0.0f, 1.0f)]
    public float opacity = 1.0f;
    public float fadeSpeed = 2.5f;
    void SetupCoordinates()
    {
        float resX = (float)mainCamera.pixelWidth;
        float resY = (float)mainCamera.pixelHeight;
        //Debug.Log( mainCamera.pixelWidth + "x" + mainCamera.pixelHeight);
        float currentAspectRatio = resX / resY;
        float aspectRatioOffset = originalAspectRatio / currentAspectRatio;

        // Set up the Vertical offsets.
        float originalAspectRatioApproximateY = resY * aspectRatioOffset;
        float verticalResDifference = resY - originalAspectRatioApproximateY;

        // Set up the Horizontal offsets.
        float originalAspectRatioApproximateX = resX * aspectRatioOffset;
        float horizontalResDifference = resX - originalAspectRatioApproximateX;
        
        // Set up our top side letterbox.
        letterboxTop.rectTransform.sizeDelta = new Vector2(0.0f, -(verticalResDifference * 2));
        letterboxTop.rectTransform.anchoredPosition = new Vector2(0.0f,1.0f);
        
        // Set up our bottom side letterbox.
        letterboxBottom.rectTransform.sizeDelta = new Vector2(0.0f, -(verticalResDifference * 2));
        letterboxBottom.rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
        
        // Set up our left side pillarbox.
        pillarboxLeft.rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);; // Positions the left bar on the left side of the screen.
        pillarboxLeft.rectTransform.sizeDelta = new Vector2(horizontalResDifference / 2, 0.0f);

        // Set up our right side pillarbox.
        pillarboxRight.rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f); // Positions the right bar on the right side of the screen.
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
        //if (Input.GetKeyDown(KeyCode.UpArrow)) {
            //StartCoroutine(FadeInBlackBars());
        //}
        //if (Input.GetKeyDown(KeyCode.DownArrow)) {
            //StartCoroutine(FadeOutBlackBars());
        //}
        Setup();
    }
}