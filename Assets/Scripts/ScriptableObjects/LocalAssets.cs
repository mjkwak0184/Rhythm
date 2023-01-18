using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Local Assets", menuName="Scriptable Object/Local Assets")]
public class LocalAssets:ScriptableObject
{
    [SerializeField]
    public Sprite[] songAttributeLarge = new Sprite[3];
    [SerializeField]
    public Sprite[] songAttributeSmall = new Sprite[3];
    [SerializeField]
    public Sprite[] songAttributeBoost = new Sprite[3];
    [SerializeField]
    public Sprite diamond, sscoin, exp;
    [SerializeField]
    public Sprite buttonPink, buttonPinkFilled, buttonBlackFilled;
    [SerializeField]
    public Sprite[] cardGradeIcons = new Sprite[5];
    [SerializeField]
    public Sprite[] cardFrames = new Sprite[5];
    [SerializeField]
    public Sprite[] cardCovers = new Sprite[5];
    [SerializeField]
    public Sprite rankingBoxMy, rankingBox1, rankingBox2, rankingBox3;
}