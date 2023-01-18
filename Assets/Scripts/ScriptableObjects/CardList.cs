using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Card List", menuName="Scriptable Object/List/Card List")]
public class CardList:ScriptableObject
{
    [SerializeField]
    private List<CardData> _cardList;
    [SerializeField]
    private CardData _emptyCard;

    public List<CardData> cardList { get { return this._cardList; } }
    public CardData emptyCard { get { return this._emptyCard; } }
}