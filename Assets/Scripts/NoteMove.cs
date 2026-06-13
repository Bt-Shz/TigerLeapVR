using UnityEngine;

public class NoteMove : MonoBehaviour
{
    public int laneId;            // 0左 1右（生成时设置）
    public float speed = 5f;
    public float despawnZ = -8f;  // 超过这个就销毁（没点就滑走）

    void Update()
    {
        // 这里假设 note 是沿 -Z 向相机靠近；如果你方向相反，把 Vector3.back 改成 forward
        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

        if (transform.position.z < despawnZ)
            Destroy(gameObject);
    }
}
