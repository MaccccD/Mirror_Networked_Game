using UnityEngine;
using Mirror;

public class PlayerStats : NetworkBehaviour
{
    [SyncVar]// Automatically syncs this variable's value from the server to all clients
    public int health = 100;

    public void Damage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            HandleDeath();
        }
    }

    void HandleDeath()
    {
        // Disable the player object on all clients
        //add custom death scene here - fade to black, UI, respawn etc.
        gameObject.SetActive(false);
    }
}

