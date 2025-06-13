using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/*Dumi: This scripts handles the visual effects and Ui for the flashback display.
 * It applies color fliters  and  camera setting to make the flashback display to look and  feel like an actual flashback, 
 * similar to how you would normally findit depicted in movies*/

public class FlashbackManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject flashbackPanel;
    public TextMeshProUGUI flashbackText;
    public Image backgroundOverlay;
    public RectTransform dialogueContainer;

    [Header("Visual Settings")]
    public Color flashbackTint = new Color(0.8f, 0.7f, 0.5f, 0.8f); // Sepia-ish
    public Color flashbackTextColor = new Color(1f, 0.95f, 0.8f, 1f); // Warm white

    [Header("Animation Settings")]
    public float transitionDuration = 2f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Filter (Alternative to Post-Process)")]
    public Camera mainCamera;
   

    [Header("Typewriter Effect")]
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.05f;
    public AudioSource typewriterSound;

    void Start()
    {
        
        if (flashbackPanel != null)
            flashbackPanel.SetActive(false);
    }

    public void StartFlashbackEffect()
    {
        StartCoroutine(TransitionToFlashback());
    }

    public void EndFlashbackEffect()
    {
        StartCoroutine(TransitionFromFlashback());
    }

    IEnumerator TransitionToFlashback()
    {
        //Dumi: Activate flashback UI
        if (flashbackPanel != null)
            flashbackPanel.SetActive(true);

        SetupFlashbackText();

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float progress = elapsedTime / transitionDuration;
            float curveValue = fadeCurve.Evaluate(progress);

            //Dumi:  Fade in background overlay
            if (backgroundOverlay != null)
            {
                Color overlayColor = flashbackTint;
                overlayColor.a = Mathf.Lerp(0f, flashbackTint.a, curveValue);
                backgroundOverlay.color = overlayColor;
            }

            //Dumi:  Scaling in dialogue container with slight bounce
            if (dialogueContainer != null)
            {
                float scale = Mathf.Lerp(0.8f, 1f, curveValue);
                dialogueContainer.localScale = Vector3.one * scale;
            }

            //Dumi: Apply camera filter if available
            ApplyCameraFilter(curveValue);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //Dumi: Ensure final values
        if (backgroundOverlay != null)
            backgroundOverlay.color = flashbackTint;

        if (dialogueContainer != null)
            dialogueContainer.localScale = Vector3.one;

        ApplyCameraFilter(1f);
    }

    IEnumerator TransitionFromFlashback()
    {
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float progress = elapsedTime / transitionDuration;

            //D: Fade out background overlay
            if (backgroundOverlay != null)
            {
                Color overlayColor = flashbackTint;
                overlayColor.a = Mathf.Lerp(flashbackTint.a, 0f, progress);
                backgroundOverlay.color = overlayColor;
            }

            //D: Scale out dialogue container
            if (dialogueContainer != null)
            {
                float scale = Mathf.Lerp(1f, 0.8f, progress);
                dialogueContainer.localScale = Vector3.one * scale;
            }

            //D: Remove camera filter
            ApplyCameraFilter(1f - progress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //D: Reset to normal
        ApplyCameraFilter(0f);

        if (flashbackPanel != null)
            flashbackPanel.SetActive(false);
    }

    void ApplyCameraFilter(float intensity) //Dumi: applying a fliter to the camera so it gives of the film flashback aesthetic
    {
       
        if (mainCamera != null)
        {
           
            Color normalBg = Color.black;
            Color flashbackBg = new Color(0.1f, 0.08f, 0.05f); // Dark sepia
            mainCamera.backgroundColor = Color.Lerp(normalBg, flashbackBg, intensity);
        }
    }

    public void SetupFlashbackText()
    {
        if (flashbackText != null)
        {
            flashbackText.fontSize = 28;
            flashbackText.color = flashbackTextColor;

            //D: Style the text for flashback feel
            flashbackText.fontStyle = FontStyles.Italic;
            flashbackText.enableVertexGradient = true;

            //D: Create a subtle gradient
            VertexGradient gradient = new VertexGradient();
            gradient.topLeft = flashbackTextColor;
            gradient.topRight = flashbackTextColor;
            gradient.bottomLeft = new Color(flashbackTextColor.r * 0.8f, flashbackTextColor.g * 0.8f, flashbackTextColor.b * 0.6f);
            gradient.bottomRight = new Color(flashbackTextColor.r * 0.8f, flashbackTextColor.g * 0.8f, flashbackTextColor.b * 0.6f);
            flashbackText.colorGradient = gradient;
        }
    }

    //Dumi: Updated dialogue methods
    public IEnumerator StartFlashbackDialogue(string initialText, float duration)
    {
        if (flashbackText != null)
        {
            if (useTypewriterEffect)
            {
                yield return StartCoroutine(TypeText(initialText, true));
                yield return new WaitForSeconds(duration - (initialText.Length * typewriterSpeed));
            }
            else
            {
                flashbackText.text = initialText;
                yield return new WaitForSeconds(duration);
            }
        }
    }

    public IEnumerator AddFlashbackDialogue(string newDialogue, float duration)
    {
        if (flashbackText != null)
        {
            if (useTypewriterEffect)
            {
                string currentText = flashbackText.text;
                yield return StartCoroutine(TypeText(currentText + "\n\n" + newDialogue, false));
                yield return new WaitForSeconds(duration - (newDialogue.Length * typewriterSpeed));
            }
            else
            {
                flashbackText.text += "\n\n" + newDialogue;
                yield return new WaitForSeconds(duration);
            }
        }
    }

    private IEnumerator TypeText(string fullText, bool clearFirst = false)//Dumi: For immersion purposes, the text will display as if its being typoed on type write with a typewrite sound playing in the background.
    {
        if (clearFirst)
            flashbackText.text = "";

        string currentText = clearFirst ? "" : flashbackText.text;

        for (int i = currentText.Length; i <= fullText.Length; i++)
        {
            flashbackText.text = fullText.Substring(0, i);

            //D: Play typing sound
            if (typewriterSound != null && i < fullText.Length)
                typewriterSound.Play();

            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
}
