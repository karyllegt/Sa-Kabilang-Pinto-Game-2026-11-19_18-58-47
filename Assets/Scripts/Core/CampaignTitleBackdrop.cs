using Skp.Preview;
using UnityEngine;

namespace Skp.Core
{
    public sealed class CampaignTitleBackdrop : MonoBehaviour
    {
        SmoothUiArt _art;
        Camera _camera;
        float _aspect;

        public void Initialize(SmoothUiArt art, Camera camera)
        {
            _art = art;
            _camera = camera;
            _aspect = camera.aspect;
        }

        void LateUpdate()
        {
            if (Mathf.Approximately(_camera.aspect, _aspect)) return;
            _aspect = _camera.aspect;
            _art.FitTitle(gameObject.scene, _aspect);
        }
    }
}
