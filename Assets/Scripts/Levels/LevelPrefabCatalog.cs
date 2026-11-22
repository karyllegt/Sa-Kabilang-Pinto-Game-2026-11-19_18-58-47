using System.Collections.Generic;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// R1: which prefab draws each kind of level, boss and map object, per act where the look depends on the act. The level,
    /// boss and map views instantiate from here instead of building objects in code. Rebuilt by
    /// <c>Sa Kabilang Pinto &gt; Setup &gt; Run M1 Setup</c> (SkpPrefabSetup).
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Level Prefab Catalog", fileName = "LevelPrefabCatalog")]
    public class LevelPrefabCatalog : ScriptableObject
    {
        public const string ResourcePath = "Level/LevelPrefabCatalog";

        [Header("Ledges")]
        [Tooltip("Ordinary ledge per act: I house wood, II balete root, III church stone.")]
        public LevelPiece[] ledgeByAct = new LevelPiece[3];
        public LevelPiece ledgeSpirit, ledgeInverted, ledgeCrumble, ledgeMoving, groundBlock;

        [Header("Doors (exit states: 0 locked, 1 open)")]
        public LevelPiece doorSpirit, doorInverted, doorHome, exit;

        [Header("Hazards (root states: dormant, warning, strike; thorn states: whole, burnt)")]
        public LevelPiece spike, spikeGauntlet, root, thorns, curtain;

        [Header("Items and checkpoints")]
        public LevelPiece flame, fragment, letter, lantern;

        [Header("Creatures (states: animation frames)")]
        public LevelPiece anino, tikbalang, manananggal;

        [Header("Act pieces")]
        public LevelPiece fallingBell, bellMark, waterBody, waterSurface;
        [Tooltip("Decoration per act (I none, II hanging roots, III candles).")]
        public LevelPiece[] decorByAct = new LevelPiece[3];
        public LevelPiece bolt;
        public LevelFx fx;

        [Header("The Bellkeeper (body states: normal, stunned, tolling)")]
        public Skp.Enemies.BossView bossView;
        public LevelPiece bossBody, shockwave, bossMark, bossBell, tollRing;

        [Header("Level map (node icon states: flame star, bell)")]
        public LevelPiece mapDot, mapNodeShadow, mapNodePlate, mapNodeIcon, mapNumber, mapLola, mapLolaMark, mapToken, mapCorner;

        public static LevelPrefabCatalog Load() => Resources.Load<LevelPrefabCatalog>(ResourcePath);

        public LevelPiece Ledge(LedgeKind kind, int act, bool ground)
        {
            if (ground) return groundBlock;
            return kind switch
            {
                LedgeKind.Spirit => ledgeSpirit,
                LedgeKind.Inverted => ledgeInverted,
                LedgeKind.Crumble => ledgeCrumble,
                LedgeKind.Moving => ledgeMoving,
                _ => ledgeByAct[Mathf.Clamp(act, 1, 3) - 1],
            };
        }

        public LevelPiece Creature(EnemyKind k) => k == EnemyKind.Tikbalang ? tikbalang : k == EnemyKind.Manananggal ? manananggal : anino;

        /// <summary>Every entry with its name (tests check that each is set).</summary>
        public IEnumerable<(string name, Object prefab)> Entries()
        {
            for (int i = 0; i < 3; i++) yield return ($"ledgeByAct[{i}]", ledgeByAct[i]);
            yield return (nameof(ledgeSpirit), ledgeSpirit); yield return (nameof(ledgeInverted), ledgeInverted);
            yield return (nameof(ledgeCrumble), ledgeCrumble); yield return (nameof(ledgeMoving), ledgeMoving);
            yield return (nameof(groundBlock), groundBlock);
            yield return (nameof(doorSpirit), doorSpirit); yield return (nameof(doorInverted), doorInverted);
            yield return (nameof(doorHome), doorHome); yield return (nameof(exit), exit);
            yield return (nameof(spike), spike); yield return (nameof(spikeGauntlet), spikeGauntlet);
            yield return (nameof(root), root); yield return (nameof(thorns), thorns); yield return (nameof(curtain), curtain);
            yield return (nameof(flame), flame); yield return (nameof(fragment), fragment); yield return (nameof(letter), letter);
            yield return (nameof(lantern), lantern);
            yield return (nameof(anino), anino); yield return (nameof(tikbalang), tikbalang); yield return (nameof(manananggal), manananggal);
            yield return (nameof(fallingBell), fallingBell); yield return (nameof(bellMark), bellMark);
            yield return (nameof(waterBody), waterBody); yield return (nameof(waterSurface), waterSurface);
            yield return ("decorByAct[1]", decorByAct[1]); yield return ("decorByAct[2]", decorByAct[2]);
            yield return (nameof(bolt), bolt); yield return (nameof(fx), fx);
            yield return (nameof(bossView), bossView); yield return (nameof(bossBody), bossBody); yield return (nameof(shockwave), shockwave);
            yield return (nameof(bossMark), bossMark); yield return (nameof(bossBell), bossBell); yield return (nameof(tollRing), tollRing);
            yield return (nameof(mapDot), mapDot); yield return (nameof(mapNodeShadow), mapNodeShadow); yield return (nameof(mapNodePlate), mapNodePlate);
            yield return (nameof(mapNodeIcon), mapNodeIcon); yield return (nameof(mapNumber), mapNumber); yield return (nameof(mapLola), mapLola);
            yield return (nameof(mapLolaMark), mapLolaMark); yield return (nameof(mapToken), mapToken); yield return (nameof(mapCorner), mapCorner);
        }
    }
}
