namespace Skp.Core
{
    /// <summary>The four scenes of the game, in build order.</summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Title = "Title";
        public const string Map = "Map";
        public const string Level = "Level";

        public static readonly string[] All = { Boot, Title, Map, Level };
    }
}
