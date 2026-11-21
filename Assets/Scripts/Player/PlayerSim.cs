using System;
using Skp.Core;
using System.Collections.Generic;
using Skp.Dimensions;
using Skp.Skills;
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
        /// <summary>Skill numbers (M6). Defaults when no asset is given.</summary>
        public SkillTuning SkillTuning = SkillTuning.Default;
        /// <summary>The learned skills, asked every tick (so a skill learned mid-level works at once).</summary>
        public ISkillSet Skills = SkillSet.None;
        public bool Has(string id) => Skills != null && Skills.Has(id);

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
        /// <summary>The dimension Tala is in. Only objects whose mask contains it are solid, harmful or usable.</summary>
        public Dim Dimension { get; private set; } = Dim.Ordinary;
        /// <summary>1 = gravity down (Ordinary, Spirit); -1 = gravity up (Inverted).</summary>
        public int GravitySign => DimensionRules.GravitySign(Dimension);
        /// <summary>The Anino shadow that follows Tala's path in the Spirit dimension.</summary>
        public readonly ShadowChaser Shadow;
        /// <summary>Index into World.Doors of the usable door Tala stands at, or -1.</summary>
        public int NearDoor = -1;

        // -------- timers, in ticks
        public int Coyote, JumpBuffer, DashTicks, DashCooldown, WardTicks, WardCooldown, Invulnerable, VeilTicks;
        /// <summary>Lambong Veil and Liwanag Bolt cooldowns, in ticks (counted from activation).</summary>
        public int VeilCooldown, BoltCooldown;
        public bool JumpCut;
        /// <summary>Extra jumps used this airtime (Double Jump).</summary>
        public int AirJumps;
        /// <summary>Pakpak Glide is holding the fall this tick.</summary>
        public bool Gliding;
        /// <summary>Liwanag Bolts in flight.</summary>
        public readonly List<Bolt> Bolts = new List<Bolt>();
        /// <summary>The last damage source that went through <see cref="TryHurt"/> (dealt or blocked).</summary>
        public DamageSource LastDamageSource { get; private set; }
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

        // -------- level progress (prototype G.score, G.shards, G.clue, G.kills, G.time)
        public int Score, Shards, Kills;
        public bool HasClue;
        /// <summary>The ledge Tala stood on at the end of the last tick (moving ledges carry her, crumbling ones start to fall).</summary>
        public Ledge Standing;
        /// <summary>Enemy speed multiplier: difficulty speed x (1 + 0.02 per level).</summary>
        public float EnemySpeedMult = 1f;

        readonly bool _story;

        // -------- events (audio, effects, HUD, enemies)
        public event Action Jumped, Dashed, Landed, Died, Respawned, OutOfLives;
        public event Action<Vector2, float> Warded;
        public event Action<int> Hurt, LanternLit;
        public event Action<Dim> DimensionChanged;
        public event Action<Door> DoorUsed;
        /// <summary>Interact at the exit door; the level decides whether it is locked (M4).</summary>
        public event Action<Door> ExitRequested;
        public event Action ShadowStunned;
        public event Action<Item> ItemCollected;
        public event Action ClueFound;
        public event Action<Enemy> EnemyKilled;
        public event Action<Ledge> LedgeCrumbled;
        /// <summary>Double Jump used.</summary>
        public event Action AirJumped;
        public event Action VeilStarted;
        /// <summary>Bolts fired (1, or 3 with Triple Bolt).</summary>
        public event Action<int> BoltFired;
        public event Action<Vector2, BoltTarget> BoltHit;
        /// <summary>Bolt pressed with too little flame.</summary>
        public event Action NotEnoughFlame;
        /// <summary>A hit that <see cref="CanBeHurt"/> refused while the veil was up.</summary>
        public event Action<DamageSource> VeilBlocked;
        /// <summary>The Bellkeeper's bolt test (M7): returns true when the bolt struck it (it takes damage only while stunned).</summary>
        public Func<Vector2, bool> BoltHitsBoss;

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
            Shadow = new ShadowChaser(difficulty.chaseFrames);
            if (world.Lanterns.Count > 0) PlaceAtLantern(0);
        }

        /// <summary>
        /// Single source of truth for damage immunity. Every damage source (spikes, roots, thorns, creatures,
        /// the shadow, water, the Bellkeeper) goes through <see cref="TryHurt"/>, which asks this first:
        /// post-hit invulnerability, dashing and the Lambong Veil all make Tala untouchable.
        /// </summary>
        public bool CanBeHurt() => Life == PlayerLife.Playing && Invulnerable <= 0 && DashTicks <= 0 && VeilTicks <= 0;

        /// <summary>The Lambong Veil is up.</summary>
        public bool Veiled => VeilTicks > 0;

        /// <summary>Deals damage if <see cref="CanBeHurt"/>. <paramref name="fromX"/> is the source's x (null = push backwards).</summary>
        public bool TryHurt(int amount, float? fromX, DamageSource source = DamageSource.Other)
        {
            LastDamageSource = source;
            if (!CanBeHurt())
            {
                if (VeilTicks > 0 && Life == PlayerLife.Playing) VeilBlocked?.Invoke(source);
                return false;
            }
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
            VeilCooldown = BoltCooldown = 0; Gliding = false; Bolts.Clear();
            AirJumps = 0; JumpCut = false; Anim = 0; Facing = 1;
            // No dimension state survives death: back to the Waking World, no trail, no stun.
            SetDimension(Dim.Ordinary);
            Shadow.Clear();
            Shadow.ClearStun();
            NearDoor = -1;
            Invulnerable = PlayerTuning.Ticks(Tuning.respawnInvulnerableTime);
            Vitality = MaxVitality;
            Flame = Mathf.Max(Flame, Tuning.respawnMinFlame);
            Life = PlayerLife.Playing;
            Respawned?.Invoke();
        }

        /// <summary>Level start or Continue: stand at lantern <paramref name="checkpoint"/> with 60 ticks of grace (prototype startLevel).</summary>
        public void BeginAt(int checkpoint)
        {
            Checkpoint = Mathf.Clamp(checkpoint, 0, Mathf.Max(0, World.Lanterns.Count - 1));
            PlaceAtLantern(Checkpoint);
            Velocity = Vector2.zero;
            Invulnerable = 60;
        }

        /// <summary>Switches dimension; the shadow forgets its trail (prototype setDim).</summary>
        public void SetDimension(Dim d)
        {
            if (d == Dimension) return;
            Dimension = d;
            Shadow.Clear();
            DimensionChanged?.Invoke(d);
        }

        /// <summary>Shadow lag in ticks (difficulty chase frames, shortened per level by the DifficultyCurve).</summary>
        public void SetChaseDelay(int ticks) => Shadow.SetDelay(ticks);

        /// <summary>Stun the shadow (ward 190 ticks, Liwanag Bolt 140 ticks).</summary>
        public bool StunShadow(int ticks)
        {
            if (Dimension != Dim.Spirit || !Shadow.Stun(ticks)) return false;
            ShadowStunned?.Invoke();
            return true;
        }

        public Rect ShadowRect => new Rect(Shadow.Current.Position, Size);

        public void UseDoor(Door d)
        {
            if (d.Kind == DoorKind.Exit) { ExitRequested?.Invoke(d); return; }
            Shadow.Clear();
            if (d.To.HasValue)
            {
                Position = new Vector2(d.To.Value.x - Size.x * 0.5f, d.To.Value.y);
                Velocity = Vector2.zero;
            }
            SetDimension(d.Target);
            DoorUsed?.Invoke(d);
        }

        /// <summary>Prototype item pickup: flame +15 (score 10), fragment: max flame +5 up to 160 and full flame (score 200), letter (score 150).</summary>
        void CollectItems()
        {
            var hb = Hitbox;
            foreach (var it in World.Items)
            {
                if (it.Got || !DimensionRules.Active(it.Mask, Dimension) || !hb.Overlaps(it.Rect)) continue;
                it.Got = true;
                switch (it.Kind)
                {
                    case ItemKind.Flame:
                        Flame = Mathf.Min(MaxFlame, Flame + 15); Score += 10; break;
                    case ItemKind.Shard:
                        Shards++; Score += 200;
                        MaxFlame = Mathf.Min(Tuning.flameCap, MaxFlame + Tuning.flamePerFragment);
                        Flame = MaxFlame; break;
                    case ItemKind.Clue:
                        HasClue = true; Score += 150;
                        foreach (var o in World.Items) if (o.Kind == ItemKind.Clue) o.Got = true;
                        break;
                }
                ItemCollected?.Invoke(it);
                if (it.Kind == ItemKind.Clue) ClueFound?.Invoke();
            }
        }

        /// <summary>Prototype updEnemies() for the walking creatures (Anino, tikbalang). Manananggal flight is M7.</summary>
        void UpdateEnemies()
        {
            const float px = 1f / 16f;
            foreach (var e in World.Enemies)
            {
                if (e.Dead || !DimensionRules.Active(e.Mask, Dimension)) continue;
                if (e.Stun > 0) e.Stun--;
                if (e.Kind == EnemyKind.Anino || e.Kind == EnemyKind.Tikbalang)
                {
                    if (e.Stun == 0)
                    {
                        float sp = e.Speed * EnemySpeedMult;
                        if (e.Kind == EnemyKind.Tikbalang)
                        {
                            bool same = Mathf.Abs(Position.y - e.Position.y) < 14 * px;
                            float dx = Center.x - e.Center.x;
                            if (same && Mathf.Abs(dx) < 90 * px && Position.x > e.MinX - 20 * px && Position.x < e.MaxX + 20 * px)
                            { e.Dir = dx < 0 ? -1 : 1; sp = 1.5f * EnemySpeedMult; e.Charging = true; }
                            else e.Charging = false;
                        }
                        e.Position.x += e.Dir * sp * px;
                        if (e.Position.x < e.MinX) { e.Position.x = e.MinX; e.Dir = 1; }
                        if (e.Position.x + e.Size.x > e.MaxX) { e.Position.x = e.MaxX - e.Size.x; e.Dir = -1; }
                    }
                }
                if (e.Stun == 0 && Hitbox.Overlaps(e.ContactRect)) TryHurt(1, e.Center.x, DamageSource.Enemy);
            }
        }

        /// <summary>Prototype wardPulse(): within the radius, an Anino dies; others lose 1 hp and are stunned 170 ticks.</summary>
        void WardCreatures(float radius)
        {
            foreach (var e in World.Enemies)
            {
                if (e.Dead || !DimensionRules.Active(e.Mask, Dimension)) continue;
                if ((e.Center - Center).sqrMagnitude >= radius * radius) continue;
                if (e.Kind == EnemyKind.Anino) KillEnemy(e);
                else { e.Hp--; if (e.Hp <= 0) KillEnemy(e); else e.Stun = 170; }
            }
        }

        public void KillEnemy(Enemy e)
        {
            if (e.Dead) return;
            e.Dead = true;
            Kills++;
            Score += 50;
            EnemyKilled?.Invoke(e);
        }

        // ------------------------------------------------------------ skills

        /// <summary>A skill was just learned (letter): Heart Charm adds a heart now; Double Jump is ready at once.</summary>
        public void OnSkillLearned(string id)
        {
            var st = SkillTuning ?? SkillTuning.Default;
            if (id == SkillIds.Heart)
            {
                MaxVitality += st.heartBonus;
                Vitality = Mathf.Min(MaxVitality, Vitality + st.heartBonus);
            }
            if (id == SkillIds.DoubleJump) AirJumps = 0;
        }

        /// <summary>Prototype fireBolt(): from 6 px ahead of Tala's center, one ray or a fan of three.</summary>
        void FireBolt(bool spread, SkillTuning st)
        {
            float a = st.tripleBoltAngle * Mathf.Deg2Rad;
            int n = spread ? 3 : 1;
            for (int i = 0; i < n; i++)
            {
                float ang = spread ? (i - 1) * a : 0f;
                Bolts.Add(new Bolt
                {
                    Position = Center + new Vector2(Facing * st.boltSpawnAhead, 0),
                    // prototype vy = sin(a) * speed with y down; y is up here
                    Velocity = new Vector2(Mathf.Cos(ang) * Facing * st.boltSpeed, -Mathf.Sin(ang) * st.boltSpeed),
                    Life = PlayerTuning.Ticks(st.boltLifetime),
                });
            }
            BoltFired?.Invoke(n);
        }

        /// <summary>
        /// Prototype updBolts(): move, then the first thing struck stops the bolt. Ledges of this dimension stop it;
        /// an Anino dies; other creatures lose 1 hp and are stunned; roots and thorn walls burn; the Spirit shadow is
        /// stunned; the Bellkeeper is hit (M7 decides the damage).
        /// </summary>
        void UpdateBolts()
        {
            var st = SkillTuning ?? SkillTuning.Default;
            const float px = 1f / 16f;
            for (int i = Bolts.Count - 1; i >= 0; i--)
            {
                var b = Bolts[i];
                b.Position += b.Velocity * Dt;
                var p = b.Position;
                BoltTarget? hit = null;
                if (--b.Life <= 0) hit = BoltTarget.Expired;

                if (hit == null)
                    foreach (var pl in World.Ledges)
                    {
                        if (!DimensionRules.Active(pl.Mask, Dimension) || pl.IsGone) continue;
                        var r = pl.Rect; // prototype: inside x, and between 2 px below the top and 1 px above the bottom
                        if (p.x > r.xMin && p.x < r.xMax && p.y < r.yMax - 2 * px && p.y > r.yMin + px) { hit = BoltTarget.Ledge; break; }
                    }

                if (hit == null)
                    foreach (var e in World.Enemies)
                    {
                        if (e.Dead || !DimensionRules.Active(e.Mask, Dimension)) continue;
                        if (p.x > e.Position.x - 2 * px && p.x < e.Position.x + e.Size.x + 2 * px && p.y > e.Position.y - 2 * px && p.y < e.Position.y + e.Size.y + 2 * px)
                        {
                            if (e.Kind == EnemyKind.Anino) { KillEnemy(e); hit = BoltTarget.Anino; }
                            else
                            {
                                e.Hp--;
                                if (e.Hp <= 0) KillEnemy(e); else e.Stun = PlayerTuning.Ticks(st.boltCreatureStun);
                                hit = BoltTarget.Creature;
                            }
                            break;
                        }
                    }

                if (hit == null)
                    foreach (var h in World.Hazards)
                    {
                        if ((h.Kind != HazardKind.Root && h.Kind != HazardKind.Thorns) || !DimensionRules.Active(h.Mask, Dimension) || h.BurnTicks > 0) continue;
                        var r = h.Rect;
                        if (p.x > r.xMin - 3 * px && p.x < r.xMax + 3 * px && p.y > r.yMin - 3 * px && p.y < r.yMax + 3 * px)
                        {
                            bool thorns = h.Kind == HazardKind.Thorns;
                            h.BurnTicks = PlayerTuning.Ticks(thorns ? st.thornBurnTime : st.rootBurnTime);
                            Score += st.burnScore;
                            hit = thorns ? BoltTarget.Thorns : BoltTarget.Root;
                            break;
                        }
                    }

                if (hit == null && Dimension == Dim.Spirit && Shadow.Active && Shadow.StunTicks == 0)
                {
                    var c = ShadowRect.center;
                    if (Mathf.Abs(p.x - c.x) < 8 * px && Mathf.Abs(p.y - c.y) < 10 * px && StunShadow(PlayerTuning.Ticks(st.boltShadowStun)))
                        hit = BoltTarget.Shadow;
                }

                if (hit == null && BoltHitsBoss != null && BoltHitsBoss(p)) hit = BoltTarget.Boss;

                if (hit != null)
                {
                    Bolts.RemoveAt(i);
                    BoltHit?.Invoke(p, hit.Value);
                }
            }
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

            // moving and crumbling ledges (prototype: start of stepGame)
            foreach (var pl in World.Ledges)
            {
                if (pl.Kind == LedgeKind.Moving)
                {
                    float nx = pl.BaseX + Mathf.Sin(Tick * pl.Speed + pl.Phase) * pl.Amplitude;
                    pl.DeltaX = nx - pl.Rect.x;
                    pl.Rect.x = nx;
                }
                else if (pl.Kind == LedgeKind.Crumble)
                {
                    if (pl.DeadTicks > 0) { pl.DeadTicks--; if (pl.DeadTicks == 0) pl.CrumbleTimer = 0; }
                    else if (pl.CrumbleTimer > 0)
                    {
                        pl.CrumbleTimer--;
                        if (pl.CrumbleTimer == 0) { pl.DeadTicks = 220; LedgeCrumbled?.Invoke(pl); }
                    }
                }
            }
            if (Standing != null && Standing.Kind == LedgeKind.Moving) Position.x += Standing.DeltaX;

            // timers
            if (VeilTicks > 0) VeilTicks--;
            if (VeilCooldown > 0) VeilCooldown--;
            if (BoltCooldown > 0) BoltCooldown--;
            if (Invulnerable > 0) Invulnerable--;
            if (DashCooldown > 0) DashCooldown--;
            if (WardCooldown > 0) WardCooldown--;
            if (WardTicks > 0) WardTicks--;
            Shadow.TickStun();
            int regen = Mathf.Max(1, PlayerTuning.Ticks(t.flameRegenInterval));
            if (Tick % regen == 0 && Flame < MaxFlame) Flame = Mathf.Min(MaxFlame, Flame + 1);

            int ax = input.MoveX;
            if (input.JumpPressed) JumpBuffer = PlayerTuning.Ticks(t.jumpBufferTime); else if (JumpBuffer > 0) JumpBuffer--;
            if (Grounded) Coyote = PlayerTuning.Ticks(t.coyoteTime); else if (Coyote > 0) Coyote--;

            var st = SkillTuning ?? SkillTuning.Default;

            // dash (Swift Dash: cheaper and quicker to recharge)
            bool swift = Has(SkillIds.Swift);
            int dashCost = swift ? st.swiftDashFlameCost : t.dashFlameCost;
            if (input.DashPressed && DashCooldown == 0 && Flame >= dashCost && DashTicks == 0)
            {
                DashTicks = PlayerTuning.Ticks(t.dashDuration);
                DashCooldown = PlayerTuning.Ticks(swift ? st.swiftDashCooldown : t.dashCooldown);
                Flame -= dashCost;
                if (ax != 0) Facing = ax;
                Velocity.y = 0;
                Dashed?.Invoke();
            }

            // Lambong Veil (Greater Veil: longer, quicker to recharge)
            if (input.VeilPressed && Has(SkillIds.Veil) && VeilCooldown == 0)
            {
                bool big = Has(SkillIds.GreaterVeil);
                VeilTicks = PlayerTuning.Ticks(big ? st.greaterVeilDuration : st.veilDuration);
                VeilCooldown = PlayerTuning.Ticks(big ? st.greaterVeilCooldown : st.veilCooldown);
                VeilStarted?.Invoke();
            }

            // Liwanag Bolt (Triple Bolt: three rays)
            if (input.BoltPressed && Has(SkillIds.Bolt) && BoltCooldown == 0)
            {
                bool spread = Has(SkillIds.TripleBolt);
                int cost = spread ? st.tripleBoltFlameCost : st.boltFlameCost;
                if (Flame >= cost)
                {
                    Flame -= cost;
                    BoltCooldown = PlayerTuning.Ticks(spread ? st.tripleBoltCooldown : st.boltCooldown);
                    FireBolt(spread, st);
                }
                else NotEnoughFlame?.Invoke();
            }

            // ward
            if (input.WardPressed && WardCooldown == 0)
            {
                WardTicks = PlayerTuning.Ticks(t.wardDuration);
                WardCooldown = PlayerTuning.Ticks(t.wardCooldown);
                Warded?.Invoke(Center, t.wardRadius);
                WardCreatures(t.wardRadius);
                if (Dimension == Dim.Spirit && Shadow.Active && (ShadowRect.center - Center).sqrMagnitude < t.shadowWardReach * t.shadowWardReach)
                    StunShadow(PlayerTuning.Ticks(t.shadowWardStun));
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
                // Double Jump: a fresh press in mid-air, never on a ground or coyote jump frame, once per airtime
                else if (input.JumpPressed && Has(SkillIds.DoubleJump) && !Grounded && Coyote == 0 && AirJumps < st.airJumps)
                {
                    AirJumps++;
                    Velocity.y = gs * st.doubleJumpSpeed;
                    JumpCut = false; JumpBuffer = 0;
                    AirJumped?.Invoke();
                }

                // Pakpak Glide: holding jump while falling caps the fall (mirrored in Inverted gravity)
                Gliding = false;
                if (Has(SkillIds.Glide) && input.JumpHeld && !Grounded && -Velocity.y * gs > st.glideFallSpeed)
                {
                    Velocity.y = -gs * st.glideFallSpeed;
                    Gliding = true;
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
            Standing = null;
            float tol = t.landingTolerance;
            foreach (var ml in World.Ledges)
            {
                if (!DimensionRules.Active(ml.Mask, Dimension) || ml.IsGone) continue;
                var l = ml.Rect;
                if (Position.x + Size.x <= l.xMin || Position.x >= l.xMax) continue;
                if (gs > 0)
                {
                    if (Velocity.y <= 0 && prevBottom >= l.yMax - tol && Position.y <= l.yMax)
                    { Position.y = l.yMax; Velocity.y = 0; Grounded = true; Standing = ml; }
                }
                else
                {
                    float top = Position.y + Size.y;
                    if (Velocity.y >= 0 && prevTop <= l.yMin + tol && top >= l.yMin)
                    { Position.y = l.yMin - Size.y; Velocity.y = 0; Grounded = true; Standing = ml; }
                }
            }
            if (Grounded)
            {
                AirJumps = 0; JumpCut = false; Gliding = false;
                if (!wasGrounded) Landed?.Invoke();
                if (Standing != null && Standing.Kind == LedgeKind.Crumble && Standing.CrumbleTimer == 0) Standing.CrumbleTimer = 42;
            }
            if (Grounded && Mathf.Abs(Velocity.x) * Dt * PlayerTuning.PxPerUnit > 0.4f)
                Anim += Mathf.Abs(Velocity.x) * Dt * PlayerTuning.PxPerUnit * 0.11f;
            else if (!Grounded) Anim = 0;

            // the Anino shadow replays the path in the Spirit dimension
            if (Dimension == Dim.Spirit)
            {
                Shadow.Record(Position, Facing, Anim);
                if (Shadow.CanHurt && Hitbox.Overlaps(ShadowRect)) TryHurt(1, ShadowRect.center.x, DamageSource.Shadow);
            }

            // lanterns (checkpoints)
            for (int k = 0; k < World.Lanterns.Count; k++)
            {
                var l = World.Lanterns[k];
                if (Dimension == Dim.Inverted || Mathf.Abs(Center.x - l.x) >= t.lanternReachX || Mathf.Abs(Position.y - l.y) >= t.lanternReachY) continue;
                if (k > Checkpoint)
                {
                    Checkpoint = k;
                    Score += 100;
                    Vitality = Mathf.Min(MaxVitality, Vitality + 1);
                    Flame = Mathf.Min(MaxFlame, Flame + t.lanternFlame);
                    LanternLit?.Invoke(k);
                }
                // a lantern always brings Tala back to the Waking World
                SetDimension(Dim.Ordinary);
            }

            // doors: usable when touching (4 px margin) and in this dimension
            NearDoor = -1;
            const float m4 = 4f / 16f;
            var reach = new Rect(Position.x - m4, Position.y - m4, Size.x + 2 * m4, Size.y + 2 * m4);
            for (int k = 0; k < World.Doors.Count; k++)
            {
                var d = World.Doors[k];
                if (DimensionRules.Active(d.Mask, Dimension) && reach.Overlaps(d.Rect)) { NearDoor = k; break; }
            }
            if (NearDoor >= 0 && input.InteractPressed) UseDoor(World.Doors[NearDoor]);

            CollectItems();
            UpdateEnemies();
            UpdateBolts();

            // hazards (1 px inset, as in the prototype)
            const float inset = 1f / 16f;
            var hb = Hitbox;
            foreach (var hz in World.Hazards)
            {
                if (!DimensionRules.Active(hz.Mask, Dimension)) continue;
                var h = hz.Rect;
                Rect r;
                switch (hz.Kind)
                {
                    case HazardKind.Thorns:
                        if (hz.BurnTicks > 0) { hz.BurnTicks--; continue; }
                        r = new Rect(h.x + 2 * inset, h.y, h.width - 4 * inset, h.height);
                        break;
                    case HazardKind.Root:
                        if (hz.BurnTicks > 0) { hz.BurnTicks--; continue; }
                        if (!hz.RootStriking(Tick)) continue;
                        r = h;
                        break;
                    default:
                        r = new Rect(h.x + inset, h.y + inset, h.width - 2 * inset, h.height - 2 * inset);
                        break;
                }
                if (hb.Overlaps(r)) TryHurt(1, h.center.x, hz.Kind == HazardKind.Root ? DamageSource.Root : hz.Kind == HazardKind.Thorns ? DamageSource.Thorns : DamageSource.Spike);
            }
        }
    }
}
