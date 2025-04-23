using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;
public class NarrativeManager : MonoBehaviour
{
    public float typingSpeed = 0.05f; //Dumi: this is the speed at which each letter will be typed in automatically so that it shows like subtitles.
    public TMP_Text narrativeText; //Dumi: the UI text element where the script will dynamically go into and appear 
    public GameObject narrativePanel;
    [TextArea(3, 10)]
    public string fullNarrativeText; // Dumi: the script or where the narrative will be.

    private void Start()
    {
        // Dumi :  show only  if the panel is active
        if (narrativePanel && narrativePanel.activeInHierarchy)
        {
            ShowNarrativeText(fullNarrativeText);
            Debug.Log("Panel is active and the script is currently showing ");
        }
    }

    public void ShowNarrativeText(string sentence)
    {
        if (narrativePanel != null && narrativePanel.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(TypeSentence(sentence));
        }
    }

    private IEnumerator TypeSentence(string sentence)
    {
        narrativeText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            narrativeText.text += letter;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                narrativeText.text = sentence;
                yield break;
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }


    //The Networking Set Up:

   // public override void OnStartClient()
   // {
   //     base.OnStartClient();

        // Dumi  : Only the host/server should trigger the narrative once
    //    if (isServer)
   //     {
  //          RpcShowNarrativeText(fullNarrativeText);
   //     }
   //     else
   //     {
   //         return;
    //    }
   // }

  //  [ClientRpc]
   // void RpcShowNarrativeText(string sentence)
  //  {
    //    if (narrativePanel != null && narrativePanel.activeInHierarchy)
     //   {
     //       StopAllCoroutines();
      //      StartCoroutine(TypeSentence(sentence));
    //    }
    //}
}
