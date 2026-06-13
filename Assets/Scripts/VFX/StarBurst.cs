using UnityEngine;

public class StarBurst : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject shardPrefab;

    [Header("Burst Settings")]
    public int count = 10;

    [Tooltip("Initial speed range")]
    public float speedMin = 3.5f;
    public float speedMax = 7.0f;

    [Tooltip("Lifetime in seconds")]
    public float lifeTime = 0.70f;

    [Tooltip("Random size range")]
    public float scaleMin = 0.12f;
    public float scaleMax = 0.42f;

    [Tooltip("Spin (deg/sec) range")]
    public float spinMin = -720f;
    public float spinMax = 720f;

    [Header("Spread Space")]
    [Tooltip("Main spread plane = XY (screen-like). Set true if you want XY as main plane.")]
    public bool spreadOnXY = true;

    [Tooltip("Add some depth variation (Z). 0 = no depth.")]
    public float zSpreadMin = -0.35f;
    public float zSpreadMax = 0.35f;

    [Tooltip("Small initial Z offset for layering")]
    public float zOffsetMin = -0.15f;
    public float zOffsetMax = 0.15f;

    [Header("Vertical Lift (only used when spreadOnXZ=false)")]
    [Tooltip("Extra Y lift if you want shards to jump upward a bit (optional)")]
    public float yLiftMin = 0.0f;
    public float yLiftMax = 0.25f;

    public void Play(Vector3 pos)
    {
        if (shardPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(shardPrefab, pos, Quaternion.identity);

            // Ensure anim exists
            var shard = go.GetComponent<StarShardAnim>();
            if (shard == null) shard = go.AddComponent<StarShardAnim>();

            // 1) Direction: uniform radial in main plane
            // Use polar angle for perfectly even radial distribution
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            Vector3 dir;
            if (spreadOnXY)
            {
                // XY plane spread (screen-like)
                dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                // Add a little upward lift if you want (optional)
                dir.y += Random.Range(yLiftMin, yLiftMax);
            }
            else
            {
                // XZ plane spread (ground-like)
                dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                // Add upward lift
                dir.y = Random.Range(yLiftMin, yLiftMax);
            }

            // 2) Add Z depth component (for spatial feeling)
            dir.z += Random.Range(zSpreadMin, zSpreadMax);

            // Normalize for consistent speed
            dir = dir.normalized;

            float spd = Random.Range(speedMin, speedMax);

            // 3) Assign parameters (random size + spin + lifetime)
            shard.velocity = dir * spd;
            shard.lifeTime = lifeTime;
            shard.spin = Random.Range(spinMin, spinMax);
            shard.startScale = Random.Range(scaleMin, scaleMax);

            // 4) Small initial depth offset for layering
            go.transform.position += new Vector3(0f, 0f, Random.Range(zOffsetMin, zOffsetMax));

            // 5) Important: init after params set (fixes “scale not random” issue)
            shard.Init();
        }
    }
}
