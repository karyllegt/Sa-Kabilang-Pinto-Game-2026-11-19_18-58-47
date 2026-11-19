using System;
using Skp.Core;
using UnityEngine;

namespace Skp.Player
{
    public enum PlayerLife { Playing, Dead, GameOver }

    /// <summary>
    /// The player, as a deterministic 60 Hz simulation ported line by line from the prototype's
    /// stepGame() (reference/prototype/index.html), in Unity units with y up.
    /// No MonoBehaviour and no physics engine: tests can step it exactly, and the frame rate
    /// cannot change the result. <see cref="PlayerController"/> drives it in play.
    /// </summary>
    public class PlayerSim
    {
        public const int TickRate = 60;
        public const float Dt = 1f / TickRate;

        public readonly PlayerTuning Tuning;
        public PlayerWorld World;

        // -------- body
        /// <summary>Bottom-left corner of the hitbox.</summary>
        public Vector2 Position;
        /// <summary>Units per second.</summary>
        public Vector2 Velocity;
        public Vector2 Size => Tuning.hitbox;
        public Vector2 Center => Position + Size * 0.5f;
        public Rect Hitbox => new Rect(Position, Size);
        public int Facing = 1;
        public bool Grounded;
        /// <summary>1 = gravity down (Ordinary, Spirit); -1 = gravity up (Inverted).</summary>
        public int GravitySign = 1;

        // -------- timers, in ticks
        public int Coyote, JumpBuffer, DashTicks, DashCooldown, WardTicks, WardCooldown, Invulnerable, VeilTicks;
        public bool JumpCut;
        /// <summary>Extra jumps used this airtime (double jump, M6).</summary>
        public int AirJumps;
        /// <summary>Run cycle phase for the animator.</summary>
        public float Anim;

        // -------- stats
        public int Vitality, MaxVitality;
        public int Flame, MaxFlame;
        /// <summary>Lives left (not used when <see cref="InfiniteLives"/>). Dying with 0 left ends the run.</summary>
        public int Lives;
        public readonly bool InfiniteLives;
        bool _outOfLives;
        public PlayerLife Life = PlayerLife.Playing;
        public int DeathTicks;
        /// <summary>Index of the last lantern lit (respawn point).</summary>
        public int Checkpoint;
        public long Tick;

        readonly bool _story;

        // -------- events (audio, effects, HUD, enemies)
        public event Action Jumped, Dashed, Landed, Died, Respawned, OutOfLives;
        public event Action<Vector2, float> Warded;
        public event Action<int> Hurt, LanternLit;

        public PlayerSim(PlayerTuning tuning, DifficultyMode difficulty, PlayerWorld world)
        {
            Tuning = tuning ? tuning : throw new ArgumentNullException(nameof(tuning));
            World = world ?? throw new ArgumentNullException(nameof(world));
            _story = difficulty.id == DifficultyId.Story;
            MaxVitality = Vitality = difficulty.vitality;
            InfiniteLives = difficulty.InfiniteLives;
            Lives = InfiniteLives ? 0 : difficulty.lives;
            MaxFlame = Flame = tuning.flameMax;
            Checkpoint = 0;
            if (world.Lanterns.Count > 0) PlaceAtLantern(0);
        }

        /// <summary>
        /// Single source of truth for damage immunity. Every damage source goes through
        /// <see cref="TryHurt"/>, which asks this first: post-hit invulnerability, dashing and
        /// (from M6) the Lambong Veil all make Tala untouchable.
        /// </summary>
        public bool CanBeHurt() => Life == PlayerLife.Playing && Invulnerable <= 0 && DashTicks <= 0 && VeilTicks <= 0;

        /// <summary>Deals damage if <see cref="CanBeHurt"/>. <paramref name="fromX"/> is the source's x (null = push backwards).</summary>
        public bool TryHurt(int amount, float? fromX)
        {
            if (!CanBeHurt()) return false;
            Vitality -= amount;
            Invulnerable = PlayerTuning.Ticks(_story ? Tuning.invulnerableTimeStory : Tuning.invulnerableTime);
            Velocity.y = GravitySign * Tuning.knockbackUp;
            Velocity.x = fromX.HasValue ? (Center.x < fromX.Value ? -Tuning.knockbackSide : Tuning.knockbackSide)
                                        : -Facing * Tuning.knockbackSide;
            Grounded = false;
            Hurt?.Invoke(amount);
            if (Vitality <= 0) { Vitality = 0; Die(); }
            return true;
        }

        public void Die()
        {
            if (Life != PlayerLife.Playing) return;
            Life = PlayerLife.Dead;
            DeathTicks = PlayerTuning.Ticks(Tuning.deathDelay);
            if (!InfiniteLives) { Lives--; if (Lives < 0) { Lives = 0; _outOfLives = true; } }
            Died?.Invoke();
        }

        /// <summary>Back at the last lit lantern with full vitality and at least some flame.</summary>
        public void Respawn()
        {
            PlaceAtLantern(Checkpoint);
            Velocity = Vector2.zero;
            Coyote = JumpBuffer = DashTicks = DashCooldown = WardTicks = WardCooldown = VeilTicks = 0;
            AirJumps = 0; JumpCut = false; Anim = 0; Facing = 1; GravitySign = 1;
            Invulnerable = PlayerTuning.Ticks(Tuning.respawnInvulnerableTime);
            Vitality = MaxVitality;
            Flame = Mathf.Max(Flame, Tuning.respawnMinFlame);
            Life = PlayerLife.Playing;
            Respawned?.Invoke();
        }

        void PlaceAtLantern(int i)
        {
            var l = World.Lanterns[Mathf.Clamp(i, 0, World.Lanterns.Count - 1)];
            Position = new Vector2(l.x - Size.x * 0.5f, l.y);
            Grounded = true;
        }

        /// <summary>One 60 Hz step. Order matches the prototype's stepGame().</summary>
        public void Step(PlayerInputFrame input)
        {
            if (Life == PlayerLife.GameOver) return;
            if (Life == PlayerLife.Dead)
            {
                if (--DeathTicks <= 0)
                {
                    if (_outOfLives) { Life = PlayerLife.GameOver; OutOfLives?.Invoke(); }
                    else Respawn();
                }
                return;
            }

            var t = Tuning;
            int gs = GravitySign;
            Tick++;

            // timers
            if (VeilTicks > 0) VeilTicks--;
            if (Invulnerable > 0) Invulnerable--;
            if (DashCooldown > 0) DashCooldown--;
            if (WardCooldown > 0) WardCooldown--;
            if (WardTicks > 0) WardTicks--;
            int regen = Mathf.Max(1, PlayerTuning.Ticks(t.flameRegenInterval));
            if (Tick % regen == 0 && Flame < MaxFlame) Flame = Mathf.Min(MaxFlame, Flame + 1);

            int ax = input.MoveX;
            if (input.JumpPressed) JumpBuffer = PlayerTuning.Ticks(t.jumpBufferTime); else if (JumpBuffer > 0) JumpBuffer--;
            if (Grounded) Coyote = PlayerTuning.Ticks(t.coyoteTime); else if (Coyote > 0) Coyote--;

            // dash
            if (input.DashPressed && DashCooldown == 0 && Flame >= t.dashFlameCost && DashTicks == 0)
            {
                DashTicks = PlayerTuning.Ticks(t.dashDuration);
                DashCooldown = PlayerTuning.Ticks(t.dashCooldown);
                Flame -= t.dashFlameCost;
                if (ax != 0) Facing = ax;
                Velocity.y = 0;
                Dashed?.Invoke();
            }

            // ward
            if (input.WardPressed && WardCooldown == 0)
            {
                WardTicks = PlayerTuning.Ticks(t.wardDuration);
                WardCooldown = PlayerTuning.Ticks(t.wardCooldown);
                Warded?.Invoke(Center, t.wardRadius);
            }

            if (DashTicks > 0)
            {
                DashTicks--;
                Velocity.x = Facing * t.dashSpeed;
                Velocity.y = 0;
            }
            else
            {
                float acc = (Grounded ? t.groundAccel : t.airAccel) * Dt;
                float max = t.runSpeed;
                if (ax != 0)
                {
                    float lim = Mathf.Max(max, Mathf.Abs(Velocity.x));
                    Velocity.x = Mathf.Clamp(Velocity.x + ax * acc, -lim, lim);
                    if (Mathf.Abs(Velocity.x) > max) Velocity.x *= t.overspeedDecayPerTick;
                    Facing = ax;
                }
                else
                {
                    Velocity.x *= Grounded ? t.groundFrictionPerTick : t.airFrictionPerTick;
                    if (Mathf.Abs(Velocity.x) < t.stopSpeed) Velocity.x = 0;
                }

                // y up: gravity pulls toward -gs
                Velocity.y = Mathf.Clamp(Velocity.y - gs * t.gravity * Dt, -t.terminalFall, t.terminalFall);

                if (JumpBuffer > 0 && Coyote > 0)
                {
                    Velocity.y = gs * t.jumpSpeed;
                    JumpBuffer = 0; Coyote = 0; Grounded = false; JumpCut = false;
                    Jumped?.Invoke();
                }

                if (!input.JumpHeld && !JumpCut && Velocity.y * gs > 0)
                {
                    Velocity.y *= t.jumpCutFactor;
                    JumpCut = true;
                }
            }

            // move x, clamp to the level
            Position.x += Velocity.x * Dt;
            var b = World.Bounds;
            if (Position.x < b.xMin) { Position.x = b.xMin; Velocity.x = 0; }
            if (Position.x > b.xMax - Size.x) { Position.x = b.xMax - Size.x; Velocity.x = 0; }

            // move y, land on one-way ledges
            bool wasGrounded = Grounded;
            float prevBottom = Position.y, prevTop = Position.y + Size.y;
            Position.y += Velocity.y * Dt;
            Grounded = false;
            float tol = t.landingTolerance;
            foreach (var l in World.Ledges)
            {
                if (Position.x + Size.x <= l.xMin || Position.x >= l.xMax) continue;
                if (gs > 0)
                {
                    if (Velocity.y <= 0 && prevBottom >= l.yMax - tol && Position.y <= l.yMax)
                    { Position.y = l.yMax; Velocity.y = 0; Grounded = true; }
                }
                else
                {
                    float top = Position.y + Size.y;
                    if (Velocity.y >= 0 && prevTop <= l.yMin + tol && top >= l.yMin)
                    { Position.y = l.yMin - Size.y; Velocity.y = 0; Grounded = true; }
                }
            }
            if (Grounded) { AirJumps = 0; JumpCut = false; if (!wasGrounded) Landed?.Invoke(); }
            if (Grounded && Mathf.Abs(Velocity.x) * Dt * PlayerTuning.PxPerUnit > 0.4f)
                Anim += Mathf.Abs(Velocity.x) * Dt * PlayerTuning.PxPerUnit * 0.11f;
            else if (!Grounded) Anim = 0;

            // lanterns (checkpoints)
            for (int k = 0; k < World.Lanterns.Count; k++)
            {
                var l = World.Lanterns[k];
                if (Mathf.Abs(Center.x - l.x) < t.lanternReachX && Mathf.Abs(Position.y - l.y) < t.lanternReachY && gs > 0 && k > Checkpoint)
                {
                    Checkpoint = k;
                    Vitality = Mathf.Min(MaxVitality, Vitality + 1);
                    Flame = Mathf.Min(MaxFlame, Flame + t.lanternFlame);
                    LanternLit?.Invoke(k);
                }
            }

            // hazards (1 px inset, as in the prototype)
            const float inset = 1f / 16f;
            var hb = Hitbox;
            foreach (var h in World.Hazards)
            {
                var r = new Rect(h.x + inset, h.y + inset, h.width - 2 * inset, h.height - 2 * inset);
                if (hb.Overlaps(r)) TryHurt(1, h.center.x);
            }
        }
    }
}
