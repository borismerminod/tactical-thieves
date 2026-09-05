using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TacticalThieves;

/// <summary>
/// Tests des procédures publiques de <see cref="APIClient"/>.
/// </summary>
/// <remarks>
/// APIClient émet de vraies requêtes HTTP via UnityWebRequest, impossibles à terminer dans un
/// [Test] synchrone (pas de boucle de frames). On injecte donc deux "coutures" :
/// <list type="bullet">
/// <item><see cref="APIClient.RequestSender"/> : un faux qui capture l'URL / la méthode / le corps
/// envoyés et simule une réponse (succès ou erreur) sans réseau.</item>
/// <item><see cref="APIClient.CoroutineStarter"/> : un runner synchrone (<see cref="RunCoroutine"/>)
/// utilisé par les wrappers Task, car StartCoroutine ne s'exécute pas en EditMode.</item>
/// </list>
/// Les coroutines directes (CollectTreasure, etc.) sont déroulées par <see cref="RunCoroutine"/>.
/// </remarks>
public class APIClientTest
{
    private const string ServerUrl = "http://test-server";
    private const string SessionId = "test-session-id";

    private GameManager gameManager;
    private APIClient apiClient;

    // Arguments capturés par le faux sender lors du dernier appel.
    private string capturedUrl;
    private string capturedMethod;
    private string capturedBody;

    // Configuration du faux sender pour le test courant.
    private bool fakeInvokeError;
    private string fakeResponse;
    private string fakeError;

    [SetUp]
    public void Setup()
    {
        // GameManager : son Awake (appelé dès Instantiate, même en EditMode) initialise
        // le singleton Instance et l'UnityGUID.
        GameObject gameManagerPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
        Assert.IsNotNull(gameManagerPrefab, "GameManager prefab should be loaded successfully.");
        GameObject gameManagerInstance = UnityEngine.Object.Instantiate(gameManagerPrefab);
        gameManager = gameManagerInstance.GetComponent<GameManager>();
        Assert.IsNotNull(gameManager, "GameManager component should be present on the instance.");
        gameManager.TestMode = true;

        // La prefab ne référence pas d'AppConfig : on en injecte un avec une URL connue.
        AppConfig config = ScriptableObject.CreateInstance<AppConfig>();
        config.serverUrl = ServerUrl;
        FieldInfo configField = typeof(GameManager).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance);
        configField.SetValue(gameManager, config);

        // SessionID connu (utilisé dans l'en-tête et dans le corps de GameStart).
        FieldInfo sessionField = typeof(GameManager).GetField("sessionID", BindingFlags.NonPublic | BindingFlags.Instance);
        sessionField.SetValue(gameManager, SessionId);

        // Valeurs par défaut du faux sender : succès avec un corps JSON neutre.
        fakeInvokeError = false;
        fakeResponse = "{}";
        fakeError = "simulated network error";

        // APIClient sous test + injection des deux coutures.
        GameObject apiObject = new GameObject("APIClientUnderTest");
        apiClient = apiObject.AddComponent<APIClient>();
        apiClient.RequestSender = FakeSend;
        apiClient.CoroutineStarter = RunCoroutine;
    }

    [TearDown]
    public void TearDown()
    {
        // DestroyImmediate car Object.Destroy est différé et peu fiable en EditMode.
        if (apiClient != null)
            UnityEngine.Object.DestroyImmediate(apiClient.gameObject);

        if (gameManager != null)
            UnityEngine.Object.DestroyImmediate(gameManager.gameObject);

        // Réinitialise le singleton statique pour éviter toute fuite entre tests.
        PropertyInfo instanceProp = typeof(GameManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        MethodInfo instanceSetter = instanceProp.GetSetMethod(true);
        instanceSetter.Invoke(null, new object[] { null });
    }

    // ---------------------------------------------------------------------
    // A. CollectTreasure
    // ---------------------------------------------------------------------

    [Test]
    public void CollectTreasure_UsesPostMethod()
    {
        IEnumerator routine = apiClient.CollectTreasure(50);
        RunCoroutine(routine);

        Assert.AreEqual("POST", capturedMethod);
    }

    [Test]
    public void CollectTreasure_TargetsCollectTreasureEndpoint()
    {
        IEnumerator routine = apiClient.CollectTreasure(50);
        RunCoroutine(routine);

        Assert.AreEqual(ServerUrl + "/api/Game/collect-treasure", capturedUrl);
    }

    [Test]
    public void CollectTreasure_SerializesAmountInBody()
    {
        IEnumerator routine = apiClient.CollectTreasure(50);
        RunCoroutine(routine);

        StringAssert.Contains("\"Amount\":50", capturedBody);
    }

    [Test]
    public void CollectTreasure_SerializesNonPositiveAmount()
    {
        IEnumerator routine = apiClient.CollectTreasure(-5);
        RunCoroutine(routine);

        StringAssert.Contains("\"Amount\":-5", capturedBody);
    }

    // ---------------------------------------------------------------------
    // B. ThiefReachedExit
    // ---------------------------------------------------------------------

    [Test]
    public void ThiefReachedExit_UsesPostMethod()
    {
        IEnumerator routine = apiClient.ThiefReachedExit(3);
        RunCoroutine(routine);

        Assert.AreEqual("POST", capturedMethod);
    }

    [Test]
    public void ThiefReachedExit_TargetsExitReachedEndpoint()
    {
        IEnumerator routine = apiClient.ThiefReachedExit(3);
        RunCoroutine(routine);

        Assert.AreEqual(ServerUrl + "/api/Game/exit-reached", capturedUrl);
    }

    [Test]
    public void ThiefReachedExit_SerializesLevelInBody()
    {
        IEnumerator routine = apiClient.ThiefReachedExit(3);
        RunCoroutine(routine);

        StringAssert.Contains("\"CurrentLevel\":3", capturedBody);
    }

    [Test]
    public void ThiefReachedExit_SendsHardcodedPseudo()
    {
        // Le pseudo est codé en dur "userTest" dans APIClient : comportement figé.
        IEnumerator routine = apiClient.ThiefReachedExit(3);
        RunCoroutine(routine);

        StringAssert.Contains("\"Pseudo\":\"userTest\"", capturedBody);
    }

    // ---------------------------------------------------------------------
    // C. AllThievesDied
    // ---------------------------------------------------------------------

    [Test]
    public void AllThievesDied_UsesPostMethod()
    {
        IEnumerator routine = apiClient.AllThievesDied();
        RunCoroutine(routine);

        Assert.AreEqual("POST", capturedMethod);
    }

    [Test]
    public void AllThievesDied_TargetsThievesDiedEndpoint()
    {
        IEnumerator routine = apiClient.AllThievesDied();
        RunCoroutine(routine);

        Assert.AreEqual(ServerUrl + "/api/Game/thieves-died", capturedUrl);
    }

    [Test]
    public void AllThievesDied_SendsEmptyBody()
    {
        IEnumerator routine = apiClient.AllThievesDied();
        RunCoroutine(routine);

        Assert.IsTrue(string.IsNullOrEmpty(capturedBody), "Le corps de la requête doit être vide.");
    }

    // ---------------------------------------------------------------------
    // D. GameStart
    // ---------------------------------------------------------------------

    [Test]
    public void GameStart_UsesPostMethod()
    {
        IEnumerator routine = apiClient.GameStart();
        RunCoroutine(routine);

        Assert.AreEqual("POST", capturedMethod);
    }

    [Test]
    public void GameStart_TargetsGameStartEndpoint()
    {
        IEnumerator routine = apiClient.GameStart();
        RunCoroutine(routine);

        Assert.AreEqual(ServerUrl + "/api/Game/game-start", capturedUrl);
    }

    [Test]
    public void GameStart_SerializesSessionId()
    {
        IEnumerator routine = apiClient.GameStart();
        RunCoroutine(routine);

        StringAssert.Contains("\"SessionID\":\"" + SessionId + "\"", capturedBody);
    }

    [Test]
    public void GameStart_SerializesUnityGuid()
    {
        string unityGuid = gameManager.UnityGUID;
        Assert.IsFalse(string.IsNullOrEmpty(unityGuid), "UnityGUID doit être renseigné par le GameManager.");

        IEnumerator routine = apiClient.GameStart();
        RunCoroutine(routine);

        StringAssert.Contains("\"UnityGUID\":\"" + unityGuid + "\"", capturedBody);
    }

    // ---------------------------------------------------------------------
    // E. SaveLevelAsync
    // ---------------------------------------------------------------------

    [Test]
    public void SaveLevelAsync_Success_CompletesTask()
    {
        Task task = apiClient.SaveLevelAsync("alice", 4);

        Assert.IsTrue(task.IsCompleted, "La Task doit être terminée (runner synchrone).");
        Assert.IsFalse(task.IsFaulted, "La Task ne doit pas être en échec sur succès.");
    }

    [Test]
    public void SaveLevelAsync_TargetsSaveLevelEndpoint()
    {
        Task task = apiClient.SaveLevelAsync("alice", 4);
        WaitCompleted(task);

        Assert.AreEqual("POST", capturedMethod);
        Assert.AreEqual(ServerUrl + "/api/Game/save-level", capturedUrl);
    }

    [Test]
    public void SaveLevelAsync_SerializesPseudoAndLevel()
    {
        Task task = apiClient.SaveLevelAsync("alice", 4);
        WaitCompleted(task);

        StringAssert.Contains("\"CurrentLevel\":4", capturedBody);
        StringAssert.Contains("\"Pseudo\":\"alice\"", capturedBody);
    }

    [Test]
    public void SaveLevelAsync_ServerError_FaultsTask()
    {
        fakeInvokeError = true;
        LogAssert.Expect(LogType.Error, new Regex("Error:"));

        Task task = apiClient.SaveLevelAsync("alice", 4);

        Assert.IsTrue(task.IsFaulted, "La Task doit être en échec sur erreur serveur.");
        AggregateException observed = task.Exception; // observe l'exception
        Assert.IsNotNull(observed);
    }

    // ---------------------------------------------------------------------
    // F. LoadLevelAsync
    // ---------------------------------------------------------------------

    [Test]
    public void LoadLevelAsync_UsesGetMethod()
    {
        fakeResponse = "{\"success\":true,\"id\":1,\"pseudo\":\"alice\",\"level\":7}";

        Task<int> task = apiClient.LoadLevelAsync("alice");
        WaitCompleted(task);

        Assert.AreEqual("GET", capturedMethod);
    }

    [Test]
    public void LoadLevelAsync_TargetsLoadLevelEndpoint()
    {
        fakeResponse = "{\"success\":true,\"id\":1,\"pseudo\":\"alice\",\"level\":7}";

        Task<int> task = apiClient.LoadLevelAsync("alice");
        WaitCompleted(task);

        Assert.AreEqual(ServerUrl + "/api/Game/load-level/alice", capturedUrl);
    }

    [Test]
    public void LoadLevelAsync_Success_ReturnsLevel()
    {
        fakeResponse = "{\"success\":true,\"id\":1,\"pseudo\":\"alice\",\"level\":7}";

        Task<int> task = apiClient.LoadLevelAsync("alice");
        WaitCompleted(task);

        Assert.IsTrue(task.IsCompletedSuccessfully, "La Task doit se terminer avec succès.");
        Assert.AreEqual(7, task.Result);
    }

    [Test]
    public void LoadLevelAsync_SuccessFalse_FaultsTask()
    {
        fakeResponse = "{\"success\":false,\"id\":0,\"pseudo\":\"alice\",\"level\":0}";

        Task<int> task = apiClient.LoadLevelAsync("alice");
        WaitCompleted(task);

        Assert.IsTrue(task.IsFaulted);
        string message = task.Exception.InnerException.Message;
        StringAssert.Contains("LoadLevel failed", message);
    }

    [Test]
    public void LoadLevelAsync_InvalidJson_FaultsTask()
    {
        fakeResponse = "pas du json";

        Task<int> task = apiClient.LoadLevelAsync("alice");
        WaitCompleted(task);

        Assert.IsTrue(task.IsFaulted);
        string message = task.Exception.InnerException.Message;
        StringAssert.Contains("LoadLevel parse error", message);
    }

    [Test]
    public void LoadLevelAsync_ServerError_FaultsTask()
    {
        fakeInvokeError = true;
        LogAssert.Expect(LogType.Error, new Regex("Error:"));

        Task<int> task = apiClient.LoadLevelAsync("alice");

        Assert.IsTrue(task.IsFaulted);
        AggregateException observed = task.Exception; // observe l'exception
        Assert.IsNotNull(observed);
    }

    [Test]
    public void LoadLevelAsync_EscapesPseudoInUrl()
    {
        fakeResponse = "{\"success\":true,\"id\":1,\"pseudo\":\"a&b\",\"level\":1}";

        Task<int> task = apiClient.LoadLevelAsync("a&b");
        WaitCompleted(task);

        // "&" doit être encodé pour ne pas casser le chemin de l'URL.
        StringAssert.Contains("load-level/a%26b", capturedUrl);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Faux sender injecté dans APIClient : capture les arguments et simule une réponse
    /// (succès ou erreur selon la configuration du test), sans appel réseau.
    /// </summary>
    private IEnumerator FakeSend(string url, string method, string jsonBody, Action<string> onSuccess, Action<string> onError)
    {
        capturedUrl = url;
        capturedMethod = method;
        capturedBody = jsonBody;

        if (fakeInvokeError)
            onError?.Invoke(fakeError);
        else
            onSuccess?.Invoke(fakeResponse);

        yield break;
    }

    /// <summary>
    /// Vérifie qu'une Task pilotée par le runner synchrone est bien terminée.
    /// Avec <see cref="RunCoroutine"/> comme CoroutineStarter, elle l'est dès le retour.
    /// </summary>
    private static void WaitCompleted(Task task)
    {
        Assert.IsTrue(task.IsCompleted, "La Task devrait être terminée immédiatement avec le runner synchrone.");
    }

    /// <summary>
    /// Déroule une coroutine (et ses IEnumerator imbriqués) de façon synchrone.
    /// Suppose qu'aucune instruction asynchrone Unity (AsyncOperation, WaitForSeconds...)
    /// n'est produite — ce qui est garanti ici par le faux sender.
    /// </summary>
    private static void RunCoroutine(IEnumerator routine)
    {
        Stack<IEnumerator> stack = new Stack<IEnumerator>();
        stack.Push(routine);

        while (stack.Count > 0)
        {
            IEnumerator current = stack.Peek();
            bool moved = current.MoveNext();

            if (!moved)
            {
                stack.Pop();
                continue;
            }

            IEnumerator nested = current.Current as IEnumerator;
            if (nested != null)
                stack.Push(nested);
        }
    }
}
