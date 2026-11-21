using Skp.Player;

namespace Skp.Audio
{
    /// <summary>
    /// The sounds of play: jump, land, dash, ward, hurt, die, lantern, flame, fragment, letter, door, kill,
    /// shadow stun and crumbling ledges. Skill sounds are in <see cref="SkillCues"/>; boss and level sounds are
    /// hooked by the Level UI. Subscribed once per session, nothing allocated per tick.
    /// </summary>
    public class GameCues
    {
        public const string Jump = "jump", Land = "land", Dash = "dash", Ward = "ward", Hurt = "hurt", Die = "die",
                            Lantern = "lantern", Flame = "flame", Fragment = "shard", Letter = "letter", Door = "door",
                            Locked = "locked", Kill = "kill", Stun = "stun", Crumble = "crumble";

        int _airTicks;

        public GameCues(PlayerSim sim)
        {
            sim.Jumped += () => Sfx.Cue(Jump);
            sim.Landed += () => { if (_airTicks >= 6) Sfx.Cue(Land); };
            sim.Dashed += () => Sfx.Cue(Dash);
            sim.Warded += (_, _) => Sfx.Cue(Ward);
            sim.Hurt += _ => Sfx.Cue(Hurt);
            sim.Died += () => Sfx.Cue(Die);
            sim.LanternLit += k => { if (k > 0) Sfx.Cue(Lantern); };
            sim.ItemCollected += it =>
            {
                if (it.Kind == ItemKind.Flame) Sfx.Cue(Flame);
                else if (it.Kind == ItemKind.Shard) Sfx.Cue(Fragment);
            };
            sim.ClueFound += () => Sfx.Cue(Letter);
            sim.DoorUsed += _ => Sfx.Cue(Door);
            sim.EnemyKilled += _ => Sfx.Cue(Kill);
            sim.ShadowStunned += () => Sfx.Cue(Stun);
            sim.LedgeCrumbled += _ => Sfx.Cue(Crumble);
        }

        /// <summary>Per tick: counts air time so tiny hops and ledge steps do not thud.</summary>
        public void Tick(PlayerSim sim) => _airTicks = sim.Grounded ? 0 : _airTicks + 1;
    }
}
