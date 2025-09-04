using Unity.Netcode;
using UnityEngine;

public class StartMultiplayerButtons : MonoBehaviour
{
   [SerializeField] private GameObject buttons;
   
   public void Host()
   {
      NetworkManager.Singleton.StartHost();
      buttons.SetActive(false);
   }

   public void Client()
   {
      NetworkManager.Singleton.StartClient();
      buttons.SetActive(false);
   }
   
}
