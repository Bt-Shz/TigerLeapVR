using UnityEngine;

public class JudgeZoneTrigger : MonoBehaviour
{
    public WhiteboxJudge judge;

    private void OnTriggerEnter(Collider other)
    {
        var note = other.GetComponent<NoteMove>();
        if (note != null) judge.Register(note);
    }

    private void OnTriggerExit(Collider other)
    {
        var note = other.GetComponent<NoteMove>();
        if (note != null) judge.Unregister(note);
    }
}
