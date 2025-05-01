using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;
public class NarrativeManager : NetworkBehaviour
{
    public float typingSpeed = 0.05f; //Dumi: this is the speed at which each letter will be typed in automatically so that it shows like subtitles.
    public TMP_Text narrativeText; //Dumi: the UI text element where the script will dynamically go into and appear 
    public GameObject narrativePanel;
    public GameObject nextBtn;
    [TextArea(3, 10)]
    public string fullNarrativeText; // Dumi: the script or where the narrative will be.


    public override void OnStartClient()
    {
        base.OnStartClient();

        // Dumi: Only the host/server should trigger the narrative once
        if (isServer)
        {
            RpcShowNarrativeText(fullNarrativeText);
            
        }
        else
        {
            Debug.Log("The host has not triggered the narrative !!!!!");
        }
    }

    [ClientRpc]
    void RpcShowNarrativeText(string sentence)
    {
        if (narrativePanel != null && narrativePanel.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(TypeSentence(sentence));
            nextBtn.gameObject.SetActive(false);
            StartCoroutine(DelayNextBtn());
            Debug.Log("the co-routine has started!");
            Debug.Log("narrative is sshowingggggg");
        }
        else
        {
            Debug.Log("narrative is  not sshowingggggg");
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

    private IEnumerator DelayNextBtn()
    {
        yield return new WaitForSeconds(93);// Dumi: disable the next btn untill players are done reading and understanding the narrative. Creates a merge between the pacing of the story and the timing in place for the next set of action.
        nextBtn.gameObject.SetActive(true);
    }
}

