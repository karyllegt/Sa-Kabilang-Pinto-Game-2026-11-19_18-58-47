using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Skp.UI
{
    /// <summary>Forwards taps on the open map (not on a button) to the map controller in screen pixels.</summary>
    public class MapTapArea : MonoBehaviour, IPointerClickHandler
    {
        public event Action<Vector2> Tapped;
        public void OnPointerClick(PointerEventData e) => Tapped?.Invoke(e.position);
    }
}
