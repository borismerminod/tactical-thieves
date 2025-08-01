using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class PingTest : MonoBehaviour
{
    // URL de ton API ASP.NET
    private string apiUrl = "https://localhost:7186/api/Ping/ping";

    // Start is called before the first frame update
    void Start()
    {
        // Lancer le test de communication au démarrage
        StartCoroutine(SendPing());
    }

    IEnumerator SendPing()
    {
        // Corps JSON
        string jsonBody = "{\"message\": \"Hello from Unity\"}";

        // Création de la requête POST
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Ignorer les certificats SSL locaux si nécessaire (en dev)
        // ⚠️ Pour les builds WebGL, il faut un certificat valide
        request.certificateHandler = new BypassCertificate();

        // Envoyer la requête
        yield return request.SendWebRequest();

        // Vérifier la réponse
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Réponse du serveur : " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Erreur : " + request.error);
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}

// Classe pour ignorer la validation SSL en dev (⚠️ pas en prod)
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Toujours accepter
    }
}


