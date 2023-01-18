using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardAlbumSceneManager : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{
    [SerializeField]
    private GameObject cardListItem;
    [SerializeField]
    private LoopVerticalScrollRect cardScrollRect;
    private int[,] cardLevels;
    private List<int> cardList = new List<int>();
    private Stack<Transform> itemPool = new Stack<Transform>();

    void Start()
    {
        if(Data.userData.cards.Length == 0){

        }else{
            // initialize cardLevels
            int collection_size = ((Data.userData.cards.Length - 1) / 24) + 1;
            cardLevels = new int[12, collection_size];
            // parse card ownership
            for(int i = 0; i < Data.userData.cards.Length / 2; i++){
                cardLevels[i % 12, i / 12] = Data.readBitfieldData(Data.userData.cards, 2, i);
            }
        }



        cardScrollRect.prefabSource = this;
        cardScrollRect.dataSource = this;
        
    }


    #region LoopScrollPrefabSource
    public GameObject GetObject(int index)
    {
        if(itemPool.Count == 0)
        {
            GameObject obj = Instantiate(cardListItem);
            obj.transform.localScale = new Vector2(0.83f, 0.83f);
            return obj;
        }
        // otherwise activate cell from pool
        Transform candidate = itemPool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // return cell to pool
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        itemPool.Push(trans);
    }
    #endregion


    #region LoopScrollDataSource
    public void ProvideData(Transform trans, int index)
    {
        CardListItem cell = trans.gameObject.GetComponent<CardListItem>();
        cell.reset();
        cell.Init(cardList[index] / 12, cardList[index] % 12);
    }
    #endregion
}
