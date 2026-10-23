namespace DreamRush.Player
{
    /// <summary>
    /// Coyote time + jump buffer as pure logic (GDD §9.5–9.6). Call <see cref="Tick"/> once per step
    /// before <see cref="TryConsumeJump"/>.
    /// </summary>
    public sealed class JumpAssist
    {
        public float CoyoteTime;
        public float BufferTime;

        float _sinceGrounded = float.PositiveInfinity;
        float _sincePressed = float.PositiveInfinity;
        bool _jumpUsed;

        public JumpAssist(float coyoteTime, float bufferTime)
        {
            CoyoteTime = coyoteTime;
            BufferTime = bufferTime;
        }

        public float TimeSinceGrounded => _sinceGrounded;
        public float TimeSinceJumpPressed => _sincePressed;
        public bool HasBufferedJump => _sincePressed <= BufferTime;
        public bool CanUseCoyote => !_jumpUsed && _sinceGrounded <= CoyoteTime;

        /// <param name="groundedForJump">Grounded and not moving upward (so the take-off frame doesn't re-arm).</param>
        public void Tick(float dt, bool groundedForJump, bool jumpPressed)
        {
            if (groundedForJump)
            {
                _sinceGrounded = 0f;
                _jumpUsed = false;
            }
            else
            {
                _sinceGrounded += dt;
            }

            if (jumpPressed) _sincePressed = 0f;
            else _sincePressed += dt;
        }

        public bool TryConsumeJump()
        {
            if (_sincePressed > BufferTime) return false;
            if (_jumpUsed || _sinceGrounded > CoyoteTime) return false;
            _jumpUsed = true;
            _sincePressed = float.PositiveInfinity;
            _sinceGrounded = float.PositiveInfinity;
            return true;
        }

        public void Reset()
        {
            _sinceGrounded = float.PositiveInfinity;
            _sincePressed = float.PositiveInfinity;
            _jumpUsed = false;
        }
    }
}
