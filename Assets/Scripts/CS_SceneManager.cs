using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UnityEngine.SceneManagementをラップする薄いシングルトン。通常のシーン切り替え(Single)と、
/// コールバック付きのAdditiveロード/アンロードを提供する
/// </summary>
public class CS_SceneManager
{
    private static CS_SceneManager _instance;
    public static CS_SceneManager Instance 
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CS_SceneManager();
            }
            return _instance;
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneAdditive(string sceneName, Action onLoaded)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += (asyncOperation) =>
        {
            onLoaded?.Invoke();
        };
    }

    public void UnloadSceneAdditive(string sceneName, Action onUnloaded)
    {
        SceneManager.UnloadSceneAsync(sceneName).completed += (asyncOperation) =>
        {
            onUnloaded?.Invoke();
        };
    }
}
