using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ChattyUI : NetworkBehaviour
{ //Dumi: here im setting up the chat fucntionality ui where players can send messages to each pther and it displays as chat histury as players text each other
    [Header("UI Elements")]
    [SerializeField] private  TMP_Text chatHistory;
    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private TMP_InputField chatMessage;
    [SerializeField] private  Button sendButton;


    private PlayerNetwork localChatPlayer;
    void Start()
    {
        sendButton.onClick.AddListener(OnSendMessage);
        StartCoroutine(FindLocalPlayer());
    }

    IEnumerator FindLocalPlayer()
    {
        while (localChatPlayer == null)
        {
            foreach (var player in FindObjectsOfType<PlayerNetwork>(true))
            {
                if (player.isLocalPlayer)
                {
                    localChatPlayer = player;
                    break;
                }
            }
            yield return null;
        }
    }

    void OnSendMessage()
    {
        if (!string.IsNullOrWhiteSpace(chatMessage.text) && localChatPlayer != null)
        {
            localChatPlayer.CmdSendMessage(chatMessage.text);
            chatMessage.text = "";
            Debug.Log("message has been sent and works for both player and client, yayyyy");
        }
    }

    public void AppendMessage(string message)
    {
        chatHistory.text += message + "\n";
        Canvas.ForceUpdateCanvases();
        scrollbar.value = 0;
        Debug.Log("chat history is updating as players send and receive each other's messages ");
    }

  

}
