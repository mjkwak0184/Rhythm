using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniJSON;

public class ChallengeRankingHistoryView : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{
    [SerializeField]
    private LoopVerticalScrollRect scrollRect;

    [SerializeField]
    private GameObject cellPrefab;

    private Stack<Transform> itemPool = new Stack<Transform>();

    private List<int> weekList = new List<int>();
    public int selectedWeek;
    public Action<int> loadWeek;
    public bool initialized = false;

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        #endif

        scrollRect.onValueChanged.AddListener(smoothScroll);
        scrollRect.prefabSource = this;
        scrollRect.dataSource = this;
    }
    void OnDestroy()
    {
        UIManager.Instance.ScrollRectSmoothScrollClear(scrollRect);
    }
    private void smoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(scrollRect);
    }

    public void Init(int earliestWeekNumber, int currentWeekNumber)
    {
        if(this.initialized) return;

        for(int i = currentWeekNumber; i >= earliestWeekNumber; i--){
            weekList.Add(i);
        }
        this.selectedWeek = currentWeekNumber;
        scrollRect.totalCount = weekList.Count;
        scrollRect.RefillCells();
        this.initialized = true;
    }

    #region LoopScrollPrefabSource

    public GameObject GetObject(int index)
    {
        if(itemPool.Count == 0)
        {
            return Instantiate(cellPrefab);
        }
        // otherwise activate cell from pool
        Transform candidate = itemPool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // return cell to pool
        Debug.Log("Return");
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        itemPool.Push(trans);
    }

    #endregion


    #region LoopScrollDataSource
    public void ProvideData(Transform trans, int index)
    {
        ChallengeRankingHistoryViewCell cell = trans.gameObject.GetComponent<ChallengeRankingHistoryViewCell>();
        cell.Init(weekList[index]);
        cell.setSelected(weekList[index] == selectedWeek);
        cell.button.onClick.RemoveAllListeners();
        
        cell.button.onClick.AddListener( delegate {
            GameObject obj = GameObject.Find("ChallengeWeek_" + this.selectedWeek.ToString());
            if(obj != null){
                ChallengeRankingHistoryViewCell previous = obj.GetComponent<ChallengeRankingHistoryViewCell>();
                if(previous != null) previous.setSelected(false);
            }
            this.selectedWeek = cell.weekNumber;
            cell.setSelected(true);
            loadWeek(cell.weekNumber);
        } );
    }
    #endregion
}
