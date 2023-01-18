using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Card Attribute Table", menuName="Scriptable Object/Card/Card Attribute Table")]
public class CardAttributeTable:ScriptableObject
{
    public enum CardAttribute { Flower, Snow, Moon }

    public CardAttribute _1Eunbi, _2Sakura, _3Hyewon, _4Yena, _5Chaeyeon, _6Chaewon, _7Minju, _8Nako, _9Hitomi, _10Yuri, _11Yujin, _12Wonyoung;

    private int AttributeToInt(CardAttributeTable.CardAttribute attr)
    {
        if(attr == CardAttributeTable.CardAttribute.Flower) return 0;
        else if(attr == CardAttributeTable.CardAttribute.Snow) return 1;
        else if(attr == CardAttributeTable.CardAttribute.Moon) return 2;
        else return -1;
    }

    public int getAttribute(int memberId){
        if(memberId == 0) return AttributeToInt(_1Eunbi);
        else if(memberId == 1) return AttributeToInt(_2Sakura);
        else if(memberId == 2) return AttributeToInt(_3Hyewon);
        else if(memberId == 3) return AttributeToInt(_4Yena);
        else if(memberId == 4) return AttributeToInt(_5Chaeyeon);
        else if(memberId == 5) return AttributeToInt(_6Chaewon);
        else if(memberId == 6) return AttributeToInt(_7Minju);
        else if(memberId == 7) return AttributeToInt(_8Nako);
        else if(memberId == 8) return AttributeToInt(_9Hitomi);
        else if(memberId == 9) return AttributeToInt(_10Yuri);
        else if(memberId == 10) return AttributeToInt(_11Yujin);
        else if(memberId == 11) return AttributeToInt(_12Wonyoung);
        else return -1;
    }
}