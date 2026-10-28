using UnityEngine;

namespace FlappyCopy.Data
{
    /// <summary>Spawn rhythm, scrolling speed and the world's parallax layers.</summary>
    [CreateAssetMenu(menuName = "Flappy Copy/World Settings", fileName = "WorldSettings")]
    public sealed class WorldSettings : ScriptableObject
    {
        [Header("Pipes")]
        [SerializeField, Min(0.1f)] private float initialPipeSpeed = 4.2f;
        [SerializeField, Min(0.1f)] private float pipeSpeed = 3.2f;
        [SerializeField, Min(0.1f)] private float pipeSpacing = 5.2f;
        [SerializeField, Min(0.1f)] private float spawnOffsetFromRightEdge = 1.4f;

        [Header("Ground")]
        [SerializeField, Min(0.1f)] private float groundScrollSpeed = 3.2f;

        [Header("Sky")]
        [SerializeField] private Color skyColor = new(0.42f, 0.77f, 0.94f, 1f);
        [SerializeField] private Color cloudColor = new(0.96f, 0.98f, 1f, 1f);
        [SerializeField, Range(0f, 0.6f)] private float cloudOpacity = 0.45f;

        [Header("Scenery")]
        [SerializeField] private Color skylineColor = new(0.52f, 0.82f, 0.72f, 1f);
        [SerializeField] private Color houseColor = new(0.80f, 0.92f, 0.76f, 1f);
        [SerializeField] private Color bushColor = new(0.40f, 0.72f, 0.33f, 1f);

        [Header("Ground Strips")]
        [SerializeField] private Color grassColor = new(0.52f, 0.82f, 0.26f, 1f);
        [SerializeField] private Color soilColor = new(0.84f, 0.72f, 0.48f, 1f);
        [SerializeField] private Color stripeColor = new(0.76f, 0.62f, 0.38f, 1f);

        [Header("Bird")]
        [SerializeField] private Color birdColor = new(0.99f, 0.83f, 0.15f, 1f);
        [SerializeField] private Color wingColor = new(0.98f, 0.71f, 0.10f, 1f);

        public float InitialPipeSpeed => initialPipeSpeed;
        public float PipeSpeed => pipeSpeed;
        public float PipeSpacing => pipeSpacing;
        public float SpawnOffsetFromRightEdge => spawnOffsetFromRightEdge;
        public float GroundScrollSpeed => groundScrollSpeed;
        public Color SkyColor => skyColor;
        public Color CloudColor => cloudColor;
        public float CloudOpacity => cloudOpacity;
        public Color SkylineColor => skylineColor;
        public Color HouseColor => houseColor;
        public Color BushColor => bushColor;
        public Color GrassColor => grassColor;
        public Color SoilColor => soilColor;
        public Color StripeColor => stripeColor;
        public Color BirdColor => birdColor;
        public Color WingColor => wingColor;
    }
}
