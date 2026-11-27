using UnityEngine;

namespace Skp.Core
{
    /// <summary>Read before loading the replacement assets, so rollback skips their startup cost.</summary>
    public sealed class CampaignArtSelection : ScriptableObject
    {
        public const string ResourcePath = "Presentation/CampaignArtSelection";
        public const string AssetPath = "Assets/Resources/Presentation/CampaignArtSelection.asset";
        public bool useSmoothArt;
    }
}
