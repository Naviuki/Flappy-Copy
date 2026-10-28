using FlappyCopy.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlappyCopy.UI
{
    /// <summary>
    /// Invisible full-screen button behind the HUD: a click or tap anywhere (except on other buttons)
    /// is forwarded to the game as one press. HUD buttons stay on top and keep their own clicks.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class PlayAreaButton : MonoBehaviour
    {
        [SerializeField] private GameController game;

        private void Awake()
        {
            Button button = GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private void OnEnable()
        {
            GetComponent<Button>().onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            GetComponent<Button>().onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            if (game != null)
            {
                game.RequestPress();
            }
        }
    }
}
