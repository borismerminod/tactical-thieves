using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/AppConfig")]
public class AppConfig : ScriptableObject
{
   public string serverUrl = "https://localhost:7186/";
    public string websocketURL = "wss://localhost:7186/ws?clientId={$clientId}";
}
