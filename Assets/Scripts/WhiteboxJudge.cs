using UnityEngine;

public class WhiteboxJudge : MonoBehaviour
{
    public StarBurst starBurst;

    // 0=左 1=右：判定区当前可命中的note
    private NoteMove[] candidates = new NoteMove[2];

    public void Register(NoteMove note)
    {
        if (note == null) return;
        if (note.laneId < 0 || note.laneId > 1) return;
        candidates[note.laneId] = note;
    }

    public void Unregister(NoteMove note)
    {
        if (note == null) return;
        if (note.laneId < 0 || note.laneId > 1) return;

        if (candidates[note.laneId] == note)
            candidates[note.laneId] = null;
    }

    public void TryHit(int laneId)
    {
        if (laneId < 0 || laneId > 1) return;

        var note = candidates[laneId];
        if (note == null) return; // 你要求：点不到就啥也不发生

        if (starBurst != null) starBurst.Play(note.transform.position);

        Destroy(note.gameObject);
        candidates[laneId] = null;
    }
}
