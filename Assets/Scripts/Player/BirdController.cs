using FlappyCopy.Core;
using FlappyCopy.Data;
using UnityEngine;

namespace FlappyCopy.Player
{
    /// <summary>
    /// Drives the bird through a Rigidbody2D (explicit gravity + flap impulse), tilts the sprite by
    /// velocity, plays the idle bob and reports the first crash it sees.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class BirdController : MonoBehaviour, ICollisionListener
    {
        [Header("Data")]
        [SerializeField] private PlayerSettings settings;

        [Header("Presentation")]
        [SerializeField, Tooltip("Rotated by the velocity tilt; the root object stays upright.")]
        private Transform visual;

        [Header("Wiring")]
        [SerializeField] private GameController game;

        /// <summary>Distance below the top edge of the frame where the bird counts as out of bounds.</summary>
        private const float CeilingMargin = 0.25f;

        private Rigidbody2D body;
        private float startY;
        private float bobTime;
        private bool hasCrashedCurrentRun;

        public PlayerSettings Settings => settings;
        public bool HasCrashed => hasCrashedCurrentRun;
        public Rigidbody2D Body => body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ConfigureBody();
        }

        private void ConfigureBody()
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.useAutoMass = false;
            body.mass = 1f;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        public void Bind(GameController controller)
        {
            game = controller;
        }

        public void ResetToStart()
        {
            startY = StartY();

            // The start height follows the gap range, so a taller phone (which reveals more world) still starts
            // the bird inside a gap the pipes can offer.
            Vector2 start = new Vector2(settings.StartPosition.x, startY);
            hasCrashedCurrentRun = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.rotation = 0f;
            MoveTo(start);
            bobTime = 0f;
            SetTilt(0f);
        }

        /// <summary>World y the bird starts at (and bobs around).</summary>
        private float StartY()
        {
            return game != null && game.Pipes != null ? game.Pipes.GapCenterRange.x : settings.StartPosition.y;
        }

        public void TickIdle(float deltaTime)
        {
            bobTime += deltaTime * settings.IdleBobSpeed;
            float offset = Mathf.Sin(bobTime * Mathf.PI) * settings.IdleBobAmplitude;
            MoveTo(new Vector2(settings.StartPosition.x, startY + offset));
            SetTilt(Mathf.Cos(bobTime * Mathf.PI) * 6f);
        }

        public void Tick(float deltaTime, bool flapPressed)
        {
            if (flapPressed)
            {
                Flap();
            }

            MoveTo(SimulateStep(transform.position, body.linearVelocity, deltaTime, settings.MaximumFallSpeed));

            // The frame edge is a wall: flying out of the top ends the run.
            if (transform.position.y >= DesignSpace.HalfHeight - CeilingMargin)
            {
                Crash();
            }
        }

        public void Flap()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y = settings.FlapVelocity;
            body.linearVelocity = velocity;
            body.WakeUp();
            SetTilt(settings.MaximumUpAngle);
        }

        public void TickFallen(float deltaTime)
        {
            MoveTo(SimulateStep(transform.position, body.linearVelocity, deltaTime, settings.MaximumFallSpeed * 1.5f));
        }

        /// <summary>Simulated crash: only the state matters, the body keeps falling.</summary>
        public void OnDied()
        {
            hasCrashedCurrentRun = true;
        }

        /// <summary>Physics sends pipe bodies and ground here; the first hit in a run ends the run.</summary>
        public void OnCollisionEntered(Collider2D other, string layerName, Vector2 contactPoint)
        {
            if (layerName == ProjectLayers.PipeName || layerName == ProjectLayers.GroundName)
            {
                Crash();
            }
        }

        /// <summary>Ends the run once: the vertical speed is kept so the bird keeps falling.</summary>
        private void Crash()
        {
            if (game == null || hasCrashedCurrentRun)
            {
                return;
            }

            hasCrashedCurrentRun = true;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            game.NotifyBirdCrashed();
        }

        public void MoveTo(Vector2 position)
        {
            body.position = position;
            transform.position = position;
        }

        /// <summary>
        /// One frame of motion with the settings' gravity. GravityScale is expressed in "g" units
        /// (Physics2D.gravity.y is -9.81) so the tuning stays readable.
        /// </summary>
        private Vector2 SimulateStep(Vector2 position, Vector2 velocity, float deltaTime, float maximumFallSpeed)
        {
            velocity.y = Mathf.Max(velocity.y + (settings.GravityScale * Physics2D.gravity.y * deltaTime), -maximumFallSpeed);
            position.y += velocity.y * deltaTime;
            body.linearVelocity = velocity;
            ApplyTilt(velocity.y, deltaTime * settings.TiltResponsiveness);
            return position;
        }

        private void ApplyTilt(float velocityY, float lerp)
        {
            float target = velocityY >= 0f
                ? settings.MaximumUpAngle
                : Mathf.Lerp(settings.MaximumUpAngle, settings.MaximumDownAngle,
                    Mathf.InverseLerp(0f, settings.MaximumFallSpeed, -velocityY));
            SetTilt(Mathf.LerpAngle(CurrentTilt(), target, Mathf.Clamp01(lerp)));
        }

        private void SetTilt(float angle)
        {
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private float CurrentTilt()
        {
            if (visual == null)
            {
                return 0f;
            }

            float z = visual.localEulerAngles.z;
            return z > 180f ? z - 360f : z;
        }
    }
}
