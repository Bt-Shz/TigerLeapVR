using UnityEngine;

public class NoteClickToVFX : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public StarBurst starBurst;   // 你的碎片生成器（场景里那个）
    
    void OnMouseDown()
    {
        // 点击到这个 Note 的 collider 就会触发
        if (starBurst != null)
            starBurst.Play(transform.position);

        Destroy(gameObject);
    }
}
