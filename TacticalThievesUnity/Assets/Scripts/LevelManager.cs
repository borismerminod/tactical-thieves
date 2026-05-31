using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages loading and restarting of game levels. This component stores a list of level
/// prefabs, supports loading a specific or random level and computes the next level index
/// according to internal state.
/// </summary>
public class LevelManager : MonoBehaviour
{
    /// <summary>
    /// Array of level prefabs available for instantiation.
    /// </summary>
    [SerializeField] GameObject[] levels;

    /// <summary>
    /// Index of the level that should be loaded next if explicitly set (used for tests). When -1, the
    /// LevelManager will use the <see cref="loadedLevelIndex"/> instead.
    /// </summary>
    [SerializeField] int currentLevelIndex;

    /// <summary>
    /// Index of the level that was requested to be loaded when <see cref="currentLevelIndex"/>
    /// is not set. Used as a fallback during the load process.
    /// </summary>
    [SerializeField] int loadedLevelIndex;


    /// <summary>
    /// Exposes the array of level prefabs. The setter allows replacement of the available
    /// levels at runtime if needed.
    /// </summary>
    public GameObject[] Levels { get => levels; set => levels = value; }


    /// <summary>
    /// Restarts the currently active scene.
    /// </summary>
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Computes the index of the next level to load. If <see cref="currentLevelIndex"/>
    /// is -1, the next index is derived from <see cref="loadedLevelIndex"/> + 1. The result
    /// wraps to zero when exceeding the available levels count.
    /// </summary>
    /// <returns>The computed next level index.</returns>
    public int ComputeNextLevel()
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


    /// <summary>
    /// Loads the level at the specified index by instantiating the corresponding prefab.
    /// If <see cref="currentLevelIndex"/> is set (&gt;= 0), that index takes precedence and
    /// the provided <paramref name="levelIndex"/> is ignored. When a valid level index is
    /// not provided, the method logs an error and returns <c>null</c>.
    /// </summary>
    /// <param name="levelIndex">The index of the level to load.</param>
    /// <returns>The instantiated level GameObject, or <c>null</c> if the index is invalid.</returns>
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

    /// <summary>
    /// Loads a random level from the available level prefabs.
    /// </summary>
    /// <returns>The instantiated random level GameObject.</returns>
    public GameObject LoadRandomLevel()
    {
        int levelIndex = Random.Range(0, levels.Length);

        return LoadLevel(levelIndex);
    }
}
