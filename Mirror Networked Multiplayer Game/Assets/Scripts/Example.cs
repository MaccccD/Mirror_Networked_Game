using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
public class Example : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    private float moveDirection = 0f;
    public Transform playerCamera;
    public UIManager uiManager;
    [ClientCallback]
    void Start()
    {
        if (!isLocalPlayer)
        {
            playerCamera.gameObject.SetActive(false);
        }
    }
    [ClientCallback] 
    private void Update()
    {
        if (!isLocalPlayer) return;
        transform.Translate(Vector3.right * moveDirection * moveSpeed *
        Time.deltaTime);
    }
    [TargetRpc]
    public void TargetShowWelcomeMessage(NetworkConnection target, string message)
    {
        if (uiManager != null)
        {
            uiManager.ShowMessage(message);
        }
    }
    [Command]
    void CmdChangeMyColour() //methos that will generate the colour that needs to change and be applied to a material
    {
        Color newColor = Random.ColorHSV();
        RpcChangeColor(newColor);
    }
    [ClientRpc]
    void RpcChangeColor(Color color) //colour change action called
    {
        GetComponent<Renderer>().material.color = color;
    }
    public void OnColorChange(InputValue value)
    {
        if (value.isPressed)
        {
            CmdChangeMyColour();
        }
    }
    public void OnMove(InputValue value)
    {
        moveDirection = value.Get<Vector2>().x;
    }
}