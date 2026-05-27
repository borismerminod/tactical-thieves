using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Threading.Tasks;

public class LevelManager : MonoBehaviour
{
    [SerializeField] GameObject[] levels;
    [SerializeField] int currentLevelIndex;
    [SerializeField] int loadedLevelIndex;
    [SerializeField] bool levelManagerStarted;

    public GameObject[] Levels { get => levels; set => levels = value; }

    // Start is called before the first frame update
    private void Start()
    {
        levelManagerStarted = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        /*if(!levelManagerStarted && GameManager.Instance != null && GameManager.Instance.IsAPIClientStarted())
        {
            levelManagerStarted = true;
            //GameManager.Instance.OnLevelManagerStarted(this);
            //await LoadLevel();
        }*/
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //Obsolète ?
    /*public async Task SaveLevel(GameManager gameManager)
    {
        int nextLevelIndex = loadedLevelIndex + 1;
        if (nextLevelIndex >= levels.Length)
        {
            nextLevelIndex = 0;
        }
        await gameManager.SaveNextLevelAsync(nextLevelIndex);
    }*/

    public int SaveLevel()
    {
        int nextLevelIndex;
        if(currentLevelIndex == -1)
        {
            nextLevelIndex = loadedLevelIndex + 1;
        }
        else
        {
            nextLevelIndex = currentLevelIndex;
        }

        if (nextLevelIndex >= levels.Length)
        {
            nextLevelIndex = 0;
        }

        return nextLevelIndex;
    }

    //Obsolète ?
    /*public async Task LoadLevel()
    {
        if(currentLevelIndex == -1)
        {
            loadedLevelIndex = await GameManager.Instance.GetCurrentLevelAsync();
            LoadLevel(loadedLevelIndex, GameManager.Instance);

        }
        else
        {
            LoadLevel(currentLevelIndex, GameManager.Instance);
        }
    }*/

    //Obsolète ? 
    public bool LoadLevel(int levelIndex, GameManager gameManager)
    {
        if (gameManager == null)
        {
            //Debug.LogError("No levels available to load");
            return false;
        }

        bool bLevelLoaded = true;
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            //Debug.LogError("Level index out of range");
            return false;
        }

       
        GameObject level = Instantiate(levels[levelIndex], gameManager.transform.position, gameManager.transform.rotation);
        level.transform.DOMoveY(10, 1.0f).From().SetEase(Ease.OutBounce).SetLink(gameObject).OnComplete( () =>
        {
            gameManager.OnLevelLoaded(level);
        });

        return bLevelLoaded;

    }

    public GameObject LoadLevel(int levelIndex)
    {

        if (currentLevelIndex >= 0 )
        {
            levelIndex = currentLevelIndex;
        }
        else
        {
            loadedLevelIndex = levelIndex;
        }

        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError("Level index out of range");
            return null;
        }

        GameObject level = Instantiate(levels[levelIndex], transform.position, transform.rotation);

        return level;
    }

    public GameObject LoadRandomLevel()
    {
        int levelIndex = Random.Range(0, levels.Length);

        return LoadLevel(levelIndex);
    }
}
