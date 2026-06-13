using UnityEngine;

public class StarShardAnim : MonoBehaviour
{
    [Header("Input from StarBurst")]
    public Vector3 velocity;
    public float lifeTime = 0.70f;
    public float spin = 360f;         // deg/sec
    public float startScale = 0.22f;

    [Header("Motion Feel")]
    [Tooltip("Velocity damping per frame (closer to 1 = less braking, flies further)")]
    [Range(0.85f, 1.00f)]
    public float dampingPerFrame = 0.985f;

    [Tooltip("Optional: shrink slightly over lifetime (1 = keep size, 0 = shrink to 0)")]
    [Range(0.0f, 1.0f)]
    public float endScaleFactor = 0.85f;

    [Tooltip("Face camera for 2D look (Sprite/Quad).")]
    public bool faceCamera = false;

    private float t;
    private SpriteRenderer sr;
    private Color c;
    private bool inited;

    public void Init()
    {
        if (inited) return;
        inited = true;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            // If your shardPrefab is not SpriteRenderer (e.g., Quad + MeshRenderer),
            // you can swap to MeshRenderer fading; for now we hard-stop to avoid null spam.
            Debug.LogWarning($"StarShardAnim: No SpriteRenderer on {name}. Add SpriteRenderer to shardPrefab.");
            Destroy(gameObject);
            return;
        }

        c = sr.color;
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        if (!inited) Init();
        if (sr == null) return;

        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / lifeTime);

        // Move & rotate
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(0f, 0f, spin * Time.deltaTime);

        // Damping (keep near 1 to let it fly outward)
        velocity *= dampingPerFrame;

        // Fade out
        c.a = 1f - p;
        sr.color = c;

        // Slight shrink (optional, keeps it feeling “dissolve”)
        float s = Mathf.Lerp(1f, endScaleFactor, p);
        transform.localScale = Vector3.one * (startScale * s);

        // Optional: billboard
        if (faceCamera && Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }

        if (t >= lifeTime) Destroy(gameObject);
    }
}
