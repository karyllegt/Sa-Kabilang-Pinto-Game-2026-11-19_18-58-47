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
        float _stepDistance;
        int _step;
        bool _inWater;

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
            sim.DimensionChanged += _ => Sfx.Cue("dimension");
        }

        /// <summary>Per tick: counts air time so tiny hops and ledge steps do not thud.</summary>
        public void Tick(PlayerSim sim)
        {
            _airTicks = sim.Grounded ? 0 : _airTicks + 1;
            bool inWater = sim.Life == PlayerLife.Playing && sim.World.Flood != null && sim.Position.y < sim.World.Flood.Y;
            if (inWater && !_inWater) Sfx.Cue("splash");
            _inWater = inWater;
            if (!sim.Grounded || sim.Life != PlayerLife.Playing || sim.DashTicks > 0 ||
                inWater)
            { _stepDistance = 0; return; }
            // Distance, rather than wall-clock time: no footsteps while standing still, paused or airborne.
            // Six run frames at six ticks each have two foot plants: 0.3 s between steps at full speed.
            _stepDistance += UnityEngine.Mathf.Abs(sim.Velocity.x) * PlayerSim.Dt;
            float stride = UnityEngine.Mathf.Max(0.25f, sim.Tuning.runSpeed * 0.3f);
            if (_stepDistance < stride) return;
            _stepDistance %= stride;
            Sfx.Cue((_step++ & 1) == 0 ? "step_left" : "step_right");
        }
    }
}
