using UnityEngine;

public class LaneClick : MonoBehaviour
{
    [SerializeField] private Camera cam;
    public LayerMask clickMask;     // 只包含 ClickArea 所在层（或用 Default 也行）
    public WhiteboxJudge judge;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (judge == null) judge = FindObjectOfType<WhiteboxJudge>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (cam == null || judge == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, clickMask, QueryTriggerInteraction.Collide))
        {
            var lane = hit.collider.GetComponent<LaneId>();
            if (lane != null)
                judge.TryHit(lane.laneId);
        }
    }
}
