using UnityEngine;

public class RoomJoining : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OdinHandler.Instance.JoinRoom("TheLitZone"); //Dumi: this handler is a singleton( a design pattern that ensures that a class only has one instance throughout). It will assign this script to an empty game object and ensures that it does not destroy on load to keep the singleton alove even if the scene changes.
         Debug.Log("Attempting to join room: TheLitZone");
        //Dumi: In Odin, a rooom is a virtual spave where peers(users in the room) can communicate with each other.
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
