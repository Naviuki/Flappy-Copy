using UnityEngine;

namespace FlappyCopy.Data
{
    /// <summary>Tuning values for the bird and for the way the run ramps up in difficulty.</summary>
    [CreateAssetMenu(menuName = "Flappy Copy/Player Settings", fileName = "PlayerSettings")]
    public sealed class PlayerSettings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float gravityScale = 2.4f;
        [SerializeField, Min(0f)] private float flapVelocity = 6.2f;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 14f;

        [Header("Tilt")]
        [SerializeField] private float maximumUpAngle = 32f;
        [SerializeField] private float maximumDownAngle = -75f;
        [SerializeField, Min(1f)] private float tiltResponsiveness = 9f;

        [Header("Start")]
        [SerializeField] private Vector2 startPosition = new(-1.6f, 1.2f);
        [SerializeField, Min(0f)] private float idleBobAmplitude = 0.18f;
        [SerializeField, Min(0f)] private float idleBobSpeed = 1.6f;

        [Header("Runtime Difficulty")]
        [SerializeField, Min(0f)] private float velocityIncreasePerPoint = Core.DifficultyRules.VelocityIncreasePerPoint;
        [SerializeField, Min(0f)] private float maximumVelocityIncrease = Core.DifficultyRules.MaximumVelocityIncrease;
        [SerializeField, Range(0f, 0.95f)] private float gapShrinkPerPoint = Core.DifficultyRules.GapShrinkPerPoint;
        [SerializeField] private float minimumGapMultiplier = Core.DifficultyRules.MinimumGapMultiplier;

        public float GravityScale => gravityScale;
        public float FlapVelocity => flapVelocity;
        public float MaximumFallSpeed => maximumFallSpeed;
        public float MaximumUpAngle => maximumUpAngle;
        public float MaximumDownAngle => maximumDownAngle;
        public float TiltResponsiveness => tiltResponsiveness;
        public Vector2 StartPosition => startPosition;
        public float IdleBobAmplitude => idleBobAmplitude;
        public float IdleBobSpeed => idleBobSpeed;

        /// <summary>Extra pipe speed earned after <paramref name="score"/> points, capped.</summary>
        public float GetVelocityIncrease(int score)
        {
            return Mathf.Min(Mathf.Max(0, score) * velocityIncreasePerPoint, maximumVelocityIncrease);
        }

        /// <summary>Fraction (0.66..1) of the base gap left after <paramref name="score"/> points.</summary>
        public float GetGapMultiplier(int score)
        {
            float multiplier = 1f - (Mathf.Max(0, score) * gapShrinkPerPoint);
            return Mathf.Clamp(multiplier, minimumGapMultiplier, 1f);
        }

        public void Configure(float gravity, float flap, float fallSpeed)
        {
            gravityScale = gravity;
            flapVelocity = flap;
            maximumFallSpeed = fallSpeed;
        }

        /// <summary>Moves the bird's starting height (the x stays where the scene places the bird).</summary>
        public void ConfigureStart(Vector2 start)
        {
            startPosition = start;
        }
    }
}
