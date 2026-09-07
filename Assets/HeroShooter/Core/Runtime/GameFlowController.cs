using UnityEngine;
using UnityEngine.SceneManagement;

namespace Engchanok.HeroShooter
{
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private HeroInput input;
        private bool _paused;

        private void Start()
        {
            SetPaused(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (input != null && input.PausePressed)
                SetPaused(!_paused);
        }

        public void Resume() => SetPaused(false);

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void MainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void SetPaused(bool paused)
        {
            _paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(paused);
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }
    }
}
