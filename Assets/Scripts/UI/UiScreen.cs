using System.Collections.Generic;
using Skp.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Skp.UI
{
    /// <summary>
    /// Base for every screen and modal box. A screen is a child of a canvas with a CanvasGroup; showing
    /// it pushes it on the scene's <see cref="UiNavigator"/>, which routes Back (Escape, Android back,
    /// gamepad B) to the top screen. Hidden screens are inactive, so they cost nothing.
    /// </summary>
    public abstract class UiScreen : MonoBehaviour
    {
        [Tooltip("Selected when the screen opens (keyboard and gamepad start here).")]
        public PixelButton firstSelected;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Back pressed while this screen is on top. Return false to ignore it.</summary>
        public virtual bool OnBack() => false;

        public virtual void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UiNavigator.Push(this);
            SelectDefault();
        }

        public virtual void Close()
        {
            UiNavigator.Remove(this);
            gameObject.SetActive(false);
        }

        public void SelectDefault()
        {
            var es = EventSystem.current;
            if (es == null) return;
            if (firstSelected != null && firstSelected.isActiveAndEnabled) es.SetSelectedGameObject(firstSelected.gameObject);
            else es.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// The open screens, top last. One per process (screens register themselves); Update routes Back to
    /// the top screen, or to <see cref="Fallback"/> when nothing is open (gameplay: pause).
    /// Also keeps a selection for keyboard and gamepad when the selected object disappears.
    /// </summary>
    public class UiNavigator : MonoBehaviour
    {
        static readonly List<UiScreen> Stack = new List<UiScreen>();
        static UiNavigator _instance;

        /// <summary>Called on Back when no screen is open. Return true if handled.</summary>
        public static System.Func<bool> Fallback;

        public static UiScreen Top
        {
            get
            {
                Stack.RemoveAll(s => s == null || !s.IsOpen);
                return Stack.Count > 0 ? Stack[Stack.Count - 1] : null;
            }
        }

        public static int Count { get { var _ = Top; return Stack.Count; } }

        public static void Push(UiScreen s)
        {
            Stack.Remove(s);
            Stack.Add(s);
            Ensure();
        }

        public static void Remove(UiScreen s)
        {
            Stack.Remove(s);
            var top = Top;
            if (top != null) top.SelectDefault();
        }

        /// <summary>Clears the stack (scene changes).</summary>
        public static void Reset()
        {
            Stack.Clear();
            Fallback = null;
        }

        /// <summary>The one Back handler: top screen first, then the fallback.</summary>
        public static bool Back()
        {
            if (UiGate.Blocked) return false;
            var top = Top;
            if (top != null) return top.OnBack();
            return Fallback != null && Fallback();
        }

        static void Ensure()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("UiNavigator");
            _instance = go.AddComponent<UiNavigator>();
            DontDestroyOnLoad(go);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void Update()
        {
            var input = GameRoot.Instance != null ? GameRoot.Instance.Input : null;
            if (input == null || UiGate.Blocked) return;
            var top = Top;
            // In gameplay Cancel shares gamepad B with Interact, so only Pause opens the pause box.
            bool back = top != null ? (input.CancelPressed || input.PausePressed) : input.PausePressed;
            if (back) { Back(); return; }

            var es = EventSystem.current;
            if (top != null && es != null && top.firstSelected != null && input.Navigate.sqrMagnitude > 0.25f)
            {
                var sel = es.currentSelectedGameObject;
                if (sel == null || !sel.activeInHierarchy || !sel.transform.IsChildOf(top.transform)) top.SelectDefault();
            }
        }
    }
}
