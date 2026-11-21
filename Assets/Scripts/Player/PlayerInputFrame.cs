namespace Skp.Player
{
    /// <summary>
    /// Input for one 60 Hz simulation tick. "Pressed" flags are true only on the tick that
    /// consumes a press (see <see cref="PlayerInputLatch"/>), "held" flags reflect the current state.
    /// </summary>
    public struct PlayerInputFrame
    {
        /// <summary>-1, 0 or 1 (the prototype's movement is digital).</summary>
        public int MoveX;
        public bool JumpHeld;
        public bool JumpPressed;
        public bool DashPressed;
        public bool WardPressed;
        public bool VeilPressed;
        public bool BoltPressed;
        public bool InteractPressed;

        public static int Digital(float axis) => axis > 0.5f ? 1 : axis < -0.5f ? -1 : 0;
    }

    /// <summary>
    /// Collects presses that happen between ticks so none are lost when a frame runs zero ticks
    /// (high frame rates) and none repeat when a frame runs several ticks (low frame rates).
    /// </summary>
    public class PlayerInputLatch
    {
        PlayerInputFrame _pending;

        /// <summary>Call once per rendered frame with that frame's input.</summary>
        public void Collect(PlayerInputFrame frame)
        {
            _pending.MoveX = frame.MoveX;
            _pending.JumpHeld = frame.JumpHeld;
            _pending.JumpPressed |= frame.JumpPressed;
            _pending.DashPressed |= frame.DashPressed;
            _pending.WardPressed |= frame.WardPressed;
            _pending.VeilPressed |= frame.VeilPressed;
            _pending.BoltPressed |= frame.BoltPressed;
            _pending.InteractPressed |= frame.InteractPressed;
        }

        /// <summary>Input for the next tick; presses are handed out once.</summary>
        public PlayerInputFrame Consume()
        {
            var f = _pending;
            _pending.JumpPressed = _pending.DashPressed = _pending.WardPressed = false;
            _pending.VeilPressed = _pending.BoltPressed = _pending.InteractPressed = false;
            return f;
        }

        public void Clear() => _pending = default;
    }

    /// <summary>Fixed 60 Hz clock: how many simulation ticks a frame of length dt owes.</summary>
    public class FixedTickClock
    {
        public const int MaxTicksPerFrame = 8; // after a long stall, drop time instead of spiralling

        double _acc;

        /// <summary>0..1, how far the current time sits between the last two ticks (for drawing).</summary>
        public float Alpha => (float)(_acc * PlayerSim.TickRate);

        /// <summary>Drops any accumulated time (after a pause or dialog).</summary>
        public void Reset() => _acc = 0;

        public int Advance(float dt)
        {
            _acc += dt;
            int n = (int)(_acc * PlayerSim.TickRate + 1e-6);
            if (n > MaxTicksPerFrame) { n = MaxTicksPerFrame; _acc = 0; return n; }
            _acc -= n / (double)PlayerSim.TickRate;
            if (_acc < 0) _acc = 0;
            return n;
        }
    }
}
