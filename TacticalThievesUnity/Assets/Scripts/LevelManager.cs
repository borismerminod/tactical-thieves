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

    public GameObject[] Levels { get => levels; set => levels = value; }

    // Start is called before the first frame update
    private async void Start()
    {
        GameManager.Instance?.OnLevelManagerStarted(this);
        await LoadLevel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public async Task SaveLevel(GameManager gameManager)
    {
        int nextLevelIndex = loadedLevelIndex + 1;
        if (nextLevelIndex >= levels.Length)
        {
            nextLevelIndex = 0;
        }
        await gameManager.SaveNextLevelAsync(nextLevelIndex);
    }

    public async Task LoadLevel()
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
    }

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
}
