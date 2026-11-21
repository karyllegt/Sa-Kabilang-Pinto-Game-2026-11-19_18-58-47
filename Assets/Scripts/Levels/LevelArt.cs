using UnityEngine;

namespace Skp.Levels
{
    /// <summary>Sprite references the level view draws with: the provided art (reference/assets, M1) and the M7 gap art generated in the same palette.</summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Level Art", fileName = "LevelArt")]
    public class LevelArt : ScriptableObject
    {
        [Header("Ledges (48 x 16, middle repeats)")]
        public Sprite wood, spirit, inverted, crumble, moving;
        [Tooltip("64 x 48 ground block")] public Sprite floorBlock;

        [Header("Doors")]
        public Sprite doorSpirit, doorInvert, doorHome, exitLocked, exitOpen;

        [Header("Hazards")]
        public Sprite spike, spikeGauntlet, thorns, thornsBurnt, rootDormant, rootWarning, rootStrike;

        [Header("Items")]
        public Sprite flame, shard, clue;

        [Header("Creatures")]
        public Sprite[] anino = new Sprite[2];
        public Sprite[] tikbalang = new Sprite[2];
        public Sprite[] manananggal = new Sprite[2];

        [Header("M7: act ledges, flood, bells, curtain, props")]
        [Tooltip("Act II balete-root ledge and Act III church-stone ledge (48 x 16, middle repeats)")] public Sprite rootLedge, stoneLedge;
        public Sprite waterSurface, bell, curtain;
        [Tooltip("Act II hanging balete roots, Act III church candle (decoration only)")] public Sprite propRoots, propCandle;

        [Header("M7: the Bellkeeper")]
        public Sprite boss, bossStun, bossToll, shockwave;

        [Header("Backdrops per act")]
        public Sprite[] backdrops = new Sprite[3];
    }
}
