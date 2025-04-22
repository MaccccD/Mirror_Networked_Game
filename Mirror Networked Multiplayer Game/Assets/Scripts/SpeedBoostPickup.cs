using UnityEngine;
using Mirror;
public class SpeedBoostPickup : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is a networked player
        if (!isServer) return;
        if (other.TryGetComponent(out FPSPlayer player))
        {
            // Give the player a speed boost via Command
            player.ApplySpeedBoost();
            // Destroy this boost object across the network
            NetworkServer.Destroy(gameObject);
        }
    }
}
