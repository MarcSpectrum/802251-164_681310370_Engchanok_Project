using UnityEngine;
using UnityEngine.SceneManagement;

namespace Engchanok.HeroShooter
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public void Play() => SceneManager.LoadScene("HeroSandbox");

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
