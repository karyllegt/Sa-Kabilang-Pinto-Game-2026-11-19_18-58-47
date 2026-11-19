using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SaKabilangPinto.UI
{
    /// <summary>Makes sure taps and clicks reach the UI, even in scenes without an EventSystem.</summary>
    public static class UIEventSystem
    {
        public static void Ensure()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
