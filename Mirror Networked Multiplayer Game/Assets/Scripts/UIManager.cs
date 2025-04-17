using UnityEngine;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    public Text messageText;
    public void ShowMessage(string msg)
    {
        messageText.text = msg;
    }
}
