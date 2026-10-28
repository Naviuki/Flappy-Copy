namespace FlappyCopy.Player
{
    /// <summary>Where the bird is in its own little state machine: alive, or falling after a crash.</summary>
    public enum BirdPhase
    {
        Alive = 0,
        Falling = 1
    }

    /// <summary>
    /// Engine-free bird simulation (vertical motion and tilt). Unity's Rigidbody2D in FlappyCopy/Player
    /// integrates the same numbers, this model mirrors them for tests and tools.
    /// </summary>
    public sealed class BirdKinematics
    {
        public BirdKinematics(float gravity, float flapVelocity, float maximumFallSpeed,
            float maximumUpAngle, float maximumDownAngle)
        {
            Gravity = gravity;
            FlapVelocity = flapVelocity;
            MaximumFallSpeed = maximumFallSpeed;
            MaximumUpAngle = maximumUpAngle;
            MaximumDownAngle = maximumDownAngle;
        }

        public float Gravity { get; }
        public float FlapVelocity { get; }
        public float MaximumFallSpeed { get; }
        public float MaximumUpAngle { get; }
        public float MaximumDownAngle { get; }

        public BirdPhase Phase { get; private set; } = BirdPhase.Alive;
        public float Y { get; private set; }
        public float VelocityY { get; private set; }
        public float Tilt { get; private set; }
        public int FlapCount { get; private set; }

        public void PlaceAt(float y)
        {
            Y = y;
            VelocityY = 0f;
            Tilt = 0f;
            Phase = BirdPhase.Alive;
        }

        public void Flap()
        {
            if (Phase != BirdPhase.Alive)
            {
                return;
            }

            VelocityY = FlapVelocity;
            FlapCount++;
            Tilt = MaximumUpAngle;
        }

        /// <summary>Integrates one frame. Returns the new Y.</summary>
        public float Step(float deltaTime, float tiltResponsiveness = 9f)
        {
            if (Phase == BirdPhase.Alive)
            {
                VelocityY -= Gravity * deltaTime;
                if (VelocityY < -MaximumFallSpeed)
                {
                    VelocityY = -MaximumFallSpeed;
                }

                Y += VelocityY * deltaTime;
                float targetTilt = VelocityY >= 0f
                    ? MaximumUpAngle
                    : MaximumUpAngle + (MaximumDownAngle - MaximumUpAngle) * UnityEngine.Mathf.InverseLerp(0f, MaximumFallSpeed, -VelocityY);
                Tilt = UnityEngine.Mathf.LerpUnclamped(Tilt, targetTilt, UnityEngine.Mathf.Clamp01(deltaTime * tiltResponsiveness));
                return Y;
            }

            // Crashing: turn nose down and fall without flapping.
            VelocityY -= Gravity * deltaTime;
            if (VelocityY < -MaximumFallSpeed * 1.5f)
            {
                VelocityY = -MaximumFallSpeed * 1.5f;
            }

            Y += VelocityY * deltaTime;
            Tilt = UnityEngine.Mathf.LerpUnclamped(Tilt, MaximumDownAngle, UnityEngine.Mathf.Clamp01(deltaTime * tiltResponsiveness));
            return Y;
        }

        public void Crash()
        {
            Phase = BirdPhase.Falling;
        }
    }
}
