using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NoteType { Short, LongStart, LongMiddle, LongEnd }

public class NoteData
{
    public static Dictionary<int,float> lanePosition = new Dictionary<int,float>{
        {0, -5.88f},
        {1, -4.9f},
        {2, -3.92f},
        {3, -2.94f},
        {4, -1.96f},
        {5, -0.98f},
        {6, 0f},
        {7, 0.98f},
        {8, 1.96f},
        {9, 2.94f},
        {10, 3.92f},
        {11, 4.9f},
        {12, 5.88f}
    };

    public static float getNoteStartXpos(int lane) { return ((float)(lane - 6f)) / 70f; }

    public static float noteStartYpos = 4.38f;

    public int lane;
    public NoteType type;
    public float targetTime;
    public string targetTimeStr = "";
    public string longNoteId;
    public int longNoteOrder;
    public override string ToString()
    {
        string returnStr = "";
        if(type == NoteType.Short) returnStr += "Short, ";
        else if(type == NoteType.LongStart) returnStr += "LongStart, ";
        else if(type == NoteType.LongMiddle) returnStr += "LongMiddle, ";
        else if(type == NoteType.LongEnd) returnStr += "LongEnd, ";
        returnStr += "Lane " + lane.ToString() + ", ";
        returnStr += "Time: " + targetTime.ToString();
        return returnStr;
    }
}


public class Note : MonoBehaviour
{
    public int lane;
    public NoteType type;
    public float targetTime;
    public float initialTime;
    public bool missed;
    public bool tapped;
    public bool isActive;

    private LineRenderer lineRenderer;
    private Coroutine lineRendererUpdate;
    private SpriteRenderer spriteRenderer;
    public string longNoteId;
    public Note nextNote;   // for rendering lines between long notes
    public void Init(NoteData data)
    {
        lane = data.lane;
        type = data.type;
        targetTime = data.targetTime;
        longNoteId = data.longNoteId;

        missed = false;
        tapped = false;

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(1, 1, 1, 1);
        
        // if(type == NoteType.LongStart || type == NoteType.LongMiddle){
        if(type != NoteType.Short){
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startColor = new Color(1, 0.91f, 0.71f, 0.5f);
            lineRenderer.endColor = new Color(1, 0.91f, 0.71f, 0.5f);
            lineRenderer.startWidth = 0;
            lineRenderer.endWidth = 0;
        }
    }

    public void noteMissed()
    {
        missed = true;
        tapped = true;

        spriteRenderer.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        if(type != NoteType.Short){
            lineRenderer.startColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
            lineRenderer.endColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        }

        if(nextNote != null){   // if it's long note, miss all subsequent note
            nextNote.noteMissed();
            GameStat.countMiss += 1;
        }
    }

    IEnumerator renderLine()
    {
        while(true){
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.startWidth = transform.localScale.x * 0.7f;
            if(nextNote != null){
                lineRenderer.SetPosition(1, new Vector3(nextNote.transform.position.x, nextNote.transform.position.y, 1));
                lineRenderer.endWidth = nextNote.transform.localScale.x * 0.7f;
            }else{
                lineRenderer.SetPosition(1, new Vector3(0, NoteData.noteStartYpos, 0));
                lineRenderer.endWidth = 0;
            }
            yield return null;
        }
    }

    void OnEnable()
    {
        if(type == NoteType.LongStart || type == NoteType.LongMiddle){
            lineRendererUpdate = StartCoroutine(renderLine());
        }
    }

    void OnDisable()
    {
        if(lineRendererUpdate != null){
            StopCoroutine(lineRendererUpdate);
            lineRendererUpdate = null;
        }
        isActive = false;
        reset();
    }

    void reset()
    {
        transform.localPosition = Vector3.zero;
        nextNote = null;
        // initialTime = 0;
    }

    public override string ToString()
    {
        string returnStr = "";
        if(type == NoteType.Short) returnStr += "Short, ";
        else if(type == NoteType.LongStart) returnStr += "LongStart, ";
        else if(type == NoteType.LongMiddle) returnStr += "LongMiddle, ";
        else if(type == NoteType.LongEnd) returnStr += "LongEnd, ";
        returnStr += "Lane " + lane.ToString() + ", ";
        returnStr += "Time: " + targetTime.ToString();
        return returnStr;
    }
}
