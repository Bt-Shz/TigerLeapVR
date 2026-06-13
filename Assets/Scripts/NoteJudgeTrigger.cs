using UnityEngine;

public class NoteJudgeTrigger : MonoBehaviour
{
    public WhiteboxJudge judge;
    private NoteMove note;

    void Awake()
    {
        note = GetComponent<NoteMove>();
    }

    void Start()
    {
        if (judge == null) judge = Object.FindObjectOfType<WhiteboxJudge>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("JudgeZone"))
            judge.Register(note);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("JudgeZone"))
            judge.Unregister(note);
    }
}
