using System;
using UnityEngine;

[CreateAssetMenu(fileName="Card Data", menuName="Scriptable Object/Card/Card Data")]
public class CardData: ScriptableObject
{
    public int collectionId;
    public string collectionName;
    public int maxPower, maxHp;
    public CardAttributeTable attributeTable;
    public Skill.Type skillType;
    public int skillActivateInterval, skillActivateChance, skillActiveDuration, skillPrimaryStat, skillSeconaryStat;
    public bool customCardFrame, updateNeeded;

    private Skill _skill;
    public Skill skill { get { return this._skill; }}

    void OnEnable()
    {
        this._skill = new Skill(this.skillType, skillActivateInterval, skillActivateChance, skillActiveDuration, skillPrimaryStat, skillSeconaryStat);
    }

    public int getAttribute(int memberId){
        return this.attributeTable.getAttribute(memberId);
    }

    public int getPower(int rawlevel){
        if(this.updateNeeded) return 0;
        return this.maxPower - (240 - rawlevel) * 10;
    }

    public int getHp(int rawlevel){
        if(this.updateNeeded)return 0;
        return this.maxHp - (240 - rawlevel) * 5;
    }


    public string getCardImage(int memberId){
        if(this.updateNeeded) return "Card[Iz_clist_emptycard_l_02]";
        else return "Assets/Texture2D/Card/CardImage/" + collectionId + "/" + memberId.ToString() + ".jpg";
    }

    public string getOriginalImage(int memberId){
        if(this.updateNeeded) return "Card[Iz_clist_emptycard_l_02]";
        else return "Assets/Texture2D/Card/Original/" + collectionId + "/" + memberId + ".jpg";
    }

    public static int getGradeFromRawLevel(int rawlevel){
        if(rawlevel >= 121) return 4;
        else if(rawlevel >= 61) return 3;
        else if(rawlevel >= 31) return 2;
        else if(rawlevel >= 11) return 1;
        else return 0;
    }

    public static int getLevelFromRawLevel(int rawlevel){
        if(rawlevel >= 121) return rawlevel - 120;
        else if(rawlevel >= 61) return rawlevel - 60;
        else if(rawlevel >= 31) return rawlevel - 30;
        else if(rawlevel >= 11) return rawlevel - 10;
        else return rawlevel;
    }

    public static (int, int) getGradeLevelFromRawLevel(int rawlevel){
        if(rawlevel >= 121){
            return (4, rawlevel - 120);
        }else if(rawlevel >= 61){
            return (3, rawlevel - 60);
        }else if(rawlevel >= 31){
            return (2, rawlevel - 30);
        }else if(rawlevel >= 11){
            return (1, rawlevel - 10);
        }else{
            return (0, rawlevel);
        }
    }

    private static string[] koreanNames = { "권은비", "미야와키 사쿠라", "강혜원", "최예나", "이채연", "김채원", "김민주", "야부키 나코", "혼다 히토미", "조유리", "안유진", "장원영" };
    private static string[] englishNames = { "Kwon, Eun Bi", "Miyawaki, Sakura", "Kang, Hye Won", "Choi, Ye Na", "Lee, Chae Yeon", "Kim, Chae Won", "Kim, Min Ju", "Yabuki, Nako", "Honda, Hitomi", "Jo, Yu Ri", "An, Yu Jin", "Jang, Won Young" };
    public static string getKoreanName(int memberId){
        return koreanNames[memberId];
    }

    public static string getEnglishName(int memberId){
        return englishNames[memberId];
    }


    public override string ToString()
    {
        return "Card Collection #" + this.collectionId + " <" + this.collectionName + "> Max.Power: " + this.maxPower + ", Max.HP: " + this.maxHp;
    }
    // Damage Cut:  %i초마다 %c%확률로 %d초동안 데미지 %p% 감소
    // Damage Cut+: %i초마다 %c%확률로 %d초동안 데미지 %p% 감소, S-PERFECT 판정범위 %s% 증가
    // Score Up:    %i초마다 %c%확률로 %d초동안 S-PERFECT 점수 %p% 상승
    // Score Up+:   %i초마다 %c%확률로 %d초동안 S-PERFECT 및 PERFECT 점수 %p% 상승
    // Score Up++:  %i초마다 %c%확률로 %d초동안 S-PERFECT 점수 %p% 상승, PERFECT 점수 %s% 상승
    // Rave Bonus:  %i초마다 %c%확률로 %d초동안 RAVE 보너스 %p% 증가
    // Rave Bonus+: %i초마다 %c%확률로 %d초동안 RAVE 보너스 %p% 증가, RAVE 게이지 상승속도 %s% 증가

    /*
    public static Dictionary<int, CardData> List = new Dictionary<int, CardData>(){
        { 0, new CardData(0, "Birthday Collection Part 1", 6666, 2500,
            AttributeFlower,
            new Skill(Skill.Type.DamageCut, 10, 33, 10, 50, 15)  )
        },
        { 1, new CardData(1, "Party Dress Collection Part 1", 6666, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 1}, {2, 2}, {3, 0}, {4, 1}, {5, 2}, {6, 0}, {7, 1}, {8, 2}, {9, 0}, {10, 1}, {11, 2}},
            new Skill(Skill.Type.DamageCut, 5, 50, 10, 1, 30)  )
        },
        { 2, new CardData(2, "Party Dress Collection Part 2", 6077, 2000,
            AttributeMoon,
            new Skill(Skill.Type.RaveBonus, 10, 10, 12, 82, 45)  )
        },
        { 3, new CardData(3, "Room Wear Collection Part 1", 6026, 2000,
            AttributeIce,
            new Skill(Skill.Type.RaveBonus, 8, 20, 8, 65, 30)  )
        },
        { 4, new CardData(4, "Room Wear Collection Part 2", 6666, 2000,
            AttributeMoon,
            new Skill(Skill.Type.DamageCut, 8, 28, 8, 30, 20)  )
        },
        { 5, new CardData(5, "Room Wear Collection Part 3", 6113, 2000,
            AttributeFlower,
            new Skill(Skill.Type.ScoreUp, 8, 20, 8, 35, 0)  )
        },
        { 6, new CardData(6, "Sporty Style Collection Part 1", 6113, 2000,
            AttributeIce,
            new Skill(Skill.Type.ScoreUp, 8, 20, 8, 35, 0)  )
        },
        { 7, new CardData(7, "Sporty Style Collection Part 2", 6026, 2000,
            AttributeMoon,
            new Skill(Skill.Type.RaveBonus, 8, 20, 8, 65, 30)  )
        },
        { 8, new CardData(8, "Sporty Style Collection Part 3", 6666, 2000,
            AttributeFlower,
            new Skill(Skill.Type.DamageCut, 8, 28, 8, 30, 20)  )
        },
        { 9, new CardData(9, "Winter Style Collection Part 1", 6666, 2000,
            AttributeIce,
            new Skill(Skill.Type.DamageCut, 8, 28, 8, 30, 20)  )
        },
        { 10, new CardData(10, "Winter Style Collection Part 2", 6113, 2000,
            AttributeMoon,
            new Skill(Skill.Type.ScoreUp, 8, 20, 8, 35, 0)  )
        },
        { 11, new CardData(11, "Winter Style Collection Part 3", 6026, 2000,
            AttributeFlower,
            new Skill(Skill.Type.RaveBonus, 8, 20, 8, 65, 30)  )
        },
        { 12, new CardData(12, "Live Photo Collection Part 1", 6013, 2000,
            AttributeFlower,
            new Skill(Skill.Type.ScoreUp, 9, 17, 10, 35, 35)  )
        },
        { 13, new CardData(13, "Live Photo Collection Part 2", 6041, 2000,
            AttributeIce,
            new Skill(Skill.Type.ScoreUp, 10, 14, 10, 38, 38)  )
        },
        { 14, new CardData(14, "Live Photo Collection Part 3", 6009, 2000,
            AttributeMoon,
            new Skill(Skill.Type.ScoreUp, 8, 12, 8, 40, 40)  )
        },
        { 15, new CardData(15, "SUKI TO IWASETAI Collection Part 1", 6033, 2000,
            AttributeIce,
            new Skill(Skill.Type.ScoreUp, 10, 8, 12, 45, 45)  )
        },
        { 16, new CardData(16, "SUKI TO IWASETAI Collection Part 2", 5985, 2000,
            AttributeFlower,
            new Skill(Skill.Type.RaveBonus, 5, 5, 10, 110, 0)  )
        },
        { 17, new CardData(17, "Buenos Aires Collection Part 1", 5951, 2000,
            AttributeFlower,
            new Skill(Skill.Type.RaveBonus, 10, 9, 12, 90, 50)  )
        },
        { 18, new CardData(18, "Buenos Aires Collection Part 2", 5956, 2000,
            AttributeIce,
            new Skill(Skill.Type.RaveBonus, 10, 8, 12, 94, 50)  )
        },
        { 19, new CardData(19, "Buenos Aires Collection Part 3", 5962, 2000,
            AttributeMoon,
            new Skill(Skill.Type.RaveBonus, 10, 7, 12, 98, 50)  )
        },
        { 20, new CardData(20, "EYES ON ME Collection Part 1", 6125, 2000,
            AttributeMoon,
            new Skill(Skill.Type.ScoreUp, 10, 25, 9, 26, 42)  )
        },
        { 21, new CardData(21, "EYES ON ME Collection Part 2", 6119, 2000,
            AttributeIce,
            new Skill(Skill.Type.ScoreUp, 12, 16, 12, 32, 48)  )
        },
        { 22, new CardData(22, "Vampire Style Collection Part 1", 6030, 2000,
            AttributeMoon,
            new Skill(Skill.Type.RaveBonus, 10, 25, 10, 62, 15)  )
        },
        { 23, new CardData(23, "Vampire Style Collection Part 2", 6023, 2000,
            AttributeIce,
            new Skill(Skill.Type.RaveBonus, 10, 30, 10, 65, 0)  )
        },
        { 24, new CardData(24, "Cute Style Collection Part 1", 6047, 2000,
            AttributeFlower,
            new Skill(Skill.Type.ScoreUp, 10, 25, 10, 28, 28)  )
        },
        { 25, new CardData(25, "Cute Style Collection Part 2", 6012, 2000,
            AttributeIce,
            new Skill(Skill.Type.RaveBonus, 10, 25, 10, 60, 25)  )
        },
        { 26, new CardData(26, "Red Dress Collection Part 1", 6021, 2000,
            AttributeFlower,
            new Skill(Skill.Type.ScoreUp, 10, 9, 12, 44, 44)  )
        },
        { 27, new CardData(27, "Red Dress Collection Part 2", 6045, 2000,
            AttributeMoon,
            new Skill(Skill.Type.ScoreUp, 10, 7, 12, 46, 46)  )
        },
        { 28, new CardData(28, "Classy Style Collection Part 1", 6055, 2000,
            AttributeIce,
            new Skill(Skill.Type.RaveBonus, 10, 33, 10, 53, 20)  )
        },
        { 29, new CardData(29, "Classy Style Collection Part 2", 6013, 2000,
            AttributeMoon,
            new Skill(Skill.Type.ScoreUp, 9, 25, 10, 27, 27)  )
        },
        { 30, new CardData(30, "Marine Style Collection Part 1", 6012, 2000,
            AttributeMoon,
            new Skill(Skill.Type.RaveBonus, 9, 20, 9, 65, 30)  )
        },
        { 31, new CardData(31, "Marine Style Collection Part 2", 6021, 2000,
            AttributeFlower,
            new Skill(Skill.Type.RaveBonus, 9, 18, 9, 66, 35)  )
        },
        { 32, new CardData(32, "Casual Style Collection Part 1", 6350, 2000,
            AttributeFlower,
            new Skill(Skill.Type.ScoreUp, 10, 30, 9, 23, 23)  )
        },
        { 33, new CardData(33, "Casual Style Collection Part 2", 6075, 2000,
            AttributeMoon,
            new Skill(Skill.Type.ScoreUp, 10, 22, 9, 28, 50)  )
        },
        { 34, new CardData(34, "Resort Style Collection Part 1", 6038, 2000,
            AttributeFlower,
            new Skill(Skill.Type.RaveBonus, 10, 25, 10, 60, 25)  )
        },
        { 35, new CardData(35, "Resort Style Collection Part 2", 6061, 2000,
            AttributeIce,
            new Skill(Skill.Type.ScoreUp, 10, 25, 9, 30, 30)  )
        },
        { 36, new CardData(36, "Halloween Style Collection Part 1", 6036, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 2}, {2, 2}, {3, 1}, {4, 0}, {5, 1}, {6, 2}, {7, 0}, {8, 0}, {9, 0}, {10, 2}, {11, 1}},
            new Skill(Skill.Type.ScoreUp, 8, 25, 7, 32, 32)  )
        },
        { 37, new CardData(37, "Halloween Style Collection Part 2", 6043, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 0}, {2, 0}, {3, 2}, {4, 1}, {5, 2}, {6, 0}, {7, 1}, {8, 1}, {9, 1}, {10, 0}, {11, 2}},
            new Skill(Skill.Type.RaveBonus, 8, 25, 8, 72, 0)  )
        },
        { 38, new CardData(38, "Celebration Dress Collection Part 1", 5923, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 1}, {2, 1}, {3, 0}, {4, 2}, {5, 0}, {6, 1}, {7, 2}, {8, 2}, {9, 2}, {10, 1}, {11, 0}},
            new Skill(Skill.Type.ScoreUp, 8, 10, 10, 45, 45)  )
        },
        { 39, new CardData(39, "Celebration Dress Collection Part 2", 6000, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 2}, {2, 2}, {3, 1}, {4, 0}, {5, 1}, {6, 2}, {7, 0}, {8, 0}, {9, 0}, {10, 2}, {11, 1}},
            new Skill(Skill.Type.RaveBonus, 8, 12, 10, 80, 40)  )
        },
        { 40, new CardData(40, "Autumn Style Collection Part 1", 5986, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 2}, {2, 2}, {3, 2}, {4, 2}, {5, 2}, {6, 0}, {7, 0}, {8, 0}, {9, 0}, {10, 0}, {11, 0}},
            new Skill(Skill.Type.ScoreUp, 5, 50, 5, 20, 20)  )
        },
        { 41, new CardData(41, "Christmas Style Collection Part 1", 6027, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 1}, {2, 1}, {3, 0}, {4, 2}, {5, 0}, {6, 1}, {7, 2}, {8, 2}, {9, 2}, {10, 1}, {11, 0}},
            new Skill(Skill.Type.RaveBonus, 7, 25, 7, 60, 25), customCardFrame: true  )
        },
        { 42, new CardData(42, "Christmas Style Collection Part 2", 5934, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 2}, {2, 2}, {3, 1}, {4, 0}, {5, 1}, {6, 2}, {7, 0}, {8, 0}, {9, 0}, {10, 2}, {11, 1}},
            new Skill(Skill.Type.ScoreUp, 7, 25, 7, 33, 33), customCardFrame: true  )
        },
        { 43, new CardData(43, "Black Dress Collection Part 1", 5893, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 0}, {2, 0}, {3, 2}, {4, 1}, {5, 2}, {6, 0}, {7, 1}, {8, 1}, {9, 1}, {10, 0}, {11, 2}},
            new Skill(Skill.Type.ScoreUp, 5, 5, 10, 50, 50)  )
        },
        { 44, new CardData(44, "Winter Date Collection Part 1", 6054, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 1}, {2, 1}, {3, 0}, {4, 2}, {5, 0}, {6, 1}, {7, 2}, {8, 2}, {9, 2}, {10, 1}, {11, 0}},
            new Skill(Skill.Type.RaveBonus, 8, 33, 8, 53, 20)  )
        },
        { 45, new CardData(45, "Valentine's Day Collection Part 1", 5650, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 2}, {2, 2}, {3, 1}, {4, 0}, {5, 1}, {6, 2}, {7, 0}, {8, 0}, {9, 0}, {10, 2}, {11, 1}},
            new Skill(Skill.Type.ScoreUp, 8, 33, 8, 30, 30), customCardFrame: true  )
        },
        { 46, new CardData(46, "Twelve Collection Part 1", 5547, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0}, {6, 1}, {7, 1}, {8, 1}, {9, 1}, {10, 1}, {11, 1}},
            new Skill(Skill.Type.ScoreUp, 12, 24, 12, 36, 36), customCardFrame: true  )
        },
        { 47, new CardData(47, "White Day Collection Part 1", 6030, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 0}, {2, 0}, {3, 2}, {4, 1}, {5, 2}, {6, 0}, {7, 1}, {8, 1}, {9, 1}, {10, 0}, {11, 2}},
            new Skill(Skill.Type.RaveBonus, 8, 50, 8, 42, 15), customCardFrame: true  )
        },
        { 48, new CardData(48, "Oneiric Theater Collection Part 1", 5984, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 2}, {2, 2}, {3, 2}, {4, 2}, {5, 2}, {6, 0}, {7, 0}, {8, 0}, {9, 0}, {10, 0}, {11, 0}},
            new Skill(Skill.Type.RaveBonus, 5, 8, 8, 115, 0), customCardFrame: true  )
        },
        { 49, new CardData(49, "Oneiric Theater Collection Part 2", 6056, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0}, {6, 1}, {7, 1}, {8, 1}, {9, 1}, {10, 1}, {11, 1}},
            new Skill(Skill.Type.ScoreUp, 5, 3, 10, 55, 55), customCardFrame: true  )
        },
        { 50, new CardData(50, "Colorful Style Collection Part 1", 5981, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 1}, {2, 2}, {3, 0}, {4, 1}, {5, 2}, {6, 0}, {7, 1}, {8, 2}, {9, 0}, {10, 1}, {11, 2}},
            new Skill(Skill.Type.RaveBonus, 1, 1, 5, 200, 0), customCardFrame:true )
        },
        { 51, new CardData(51, "Rose Collection Part 1", 5702, 2000,
            AttributeFlower,
            new Skill(Skill.Type.ScoreUp, 8, 15, 12, 37, 37), customCardFrame: true )    
        },
        { 52, new CardData(52, "Rose Collection Part 2", 5693, 2000,
            AttributeFlower,
            new Skill(Skill.Type.RaveBonus, 8, 15, 12, 75, 40), customCardFrame: true )    
        },
        { 53, new CardData(53, "Violeta Collection Part 1", 5491, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 0}, {2, 1}, {3, 2}, {4, 0}, {5, 1}, {6, 2}, {7, 0}, {8, 1}, {9, 2}, {10, 0}, {11, 1}},
            new Skill(Skill.Type.ScoreUp, 2, 20, 5, 30, 30), customCardFrame: true )    
        },
        { 54, new CardData(54, "Violeta Collection Part 2", 6285, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 2}, {2, 0}, {3, 1}, {4, 2}, {5, 0}, {6, 1}, {7, 2}, {8, 0}, {9, 1}, {10, 2}, {11, 0}},
            new Skill(Skill.Type.RaveBonus, 10, 30, 10, 38, 80), customCardFrame:true )    
        },
        { 55, new CardData(55, "Sapphire Collection Part 1", 5649, 2000,
            AttributeIce,
            new Skill(Skill.Type.ScoreUp, 10, 22, 14, 31, 31), customCardFrame: true )    
        },
        { 56, new CardData(56, "Sapphire Collection Part 2", 5654, 2000,
            AttributeIce,
            new Skill(Skill.Type.RaveBonus, 10, 22, 14, 65, 25), customCardFrame: true )    
        },
        { 57, new CardData(57, "Blooming Style Collection Part 1", 6086, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 0}, {2, 2}, {3, 1}, {4, 0}, {5, 2}, {6, 1}, {7, 0}, {8, 2}, {9, 1}, {10, 0}, {11, 2}},
            new Skill(Skill.Type.RaveBonus, 10, 30, 9, 55, 25), customCardFrame: true )    
        },
        { 58, new CardData(58, "Blooming Style Collection Part 2", 5856, 2000,
            AttributeMoon,
            new Skill(Skill.Type.ScoreUp, 17, 18, 17, 34, 34) )    
        },
        { 59, new CardData(59, "Blooming Style Collection Part 3", 5910, 2000,
            AttributeMoon,
            new Skill(Skill.Type.RaveBonus, 17, 18, 17, 66, 35), customCardFrame: true )    
        },
        { 60, new CardData(60, "Blooming Style Collection Part 4", 5922, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 2}, {2, 0}, {3, 1}, {4, 2}, {5, 0}, {6, 1}, {7, 2}, {8, 0}, {9, 1}, {10, 2}, {11, 0}},
            new Skill(Skill.Type.ScoreUp, 1, 1, 4, 100, 100), customCardFrame:true )    
        },
        { 61, new CardData(61, "Diary Collection Part 1", 6048, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 2}, {2, 2}, {3, 2}, {4, 2}, {5, 2}, {6, 1}, {7, 1}, {8, 1}, {9, 1}, {10, 1}, {11, 1}},
            new Skill(Skill.Type.ScoreUp, 8, 25, 8, 28, 28) )    
        },
        { 62, new CardData(62, "Diary Collection Part 2", 6007, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 1}, {2, 0}, {3, 2}, {4, 1}, {5, 0}, {6, 2}, {7, 1}, {8, 0}, {9, 2}, {10, 1}, {11, 0}},
            new Skill(Skill.Type.RaveBonus, 5, 50, 5, 45, 10) )    
        },
        { 63, new CardData(63, "Oneiric Collection Part 1", 6030, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 0}, {2, 2}, {3, 1}, {4, 0}, {5, 2}, {6, 1}, {7, 0}, {8, 2}, {9, 1}, {10, 0}, {11, 2}},
            new Skill(Skill.Type.ScoreUp, 25, 25, 25, 25, 25) )    
        },
        { 64, new CardData(64, "Oneiric Collection Part 2", 6065, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 2}, {2, 2}, {3, 1}, {4, 0}, {5, 1}, {6, 2}, {7, 0}, {8, 0}, {9, 0}, {10, 2}, {11, 1}},
            new Skill(Skill.Type.RaveBonus, 8, 50, 8, 35, 60) )    
        },
        { 65, new CardData(65, "One-reeler / Act IV Collection Part I", 5701, 2000,
            new Dictionary<int, int>(){{0, 0}, {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0}, {6, 2}, {7, 2}, {8, 2}, {9, 2}, {10, 2}, {11, 2}},
            new Skill(Skill.Type.RaveBonus, 5, 15, 10, 65, 30), customCardFrame:true )
        },
        { 66, new CardData(66, "One-reeler / Act IV Collection Part II", 5642, 2000,
            new Dictionary<int, int>(){{0, 2}, {1, 0}, {2, 1}, {3, 2}, {4, 0}, {5, 1}, {6, 2}, {7, 0}, {8, 1}, {9, 2}, {10, 0}, {11, 1}},
            new Skill(Skill.Type.RaveBonus, 6, 24, 8, 75, 20), customCardFrame:true ) 
        },
        { 67, new CardData(67, "One-reeler / Act IV Collection Part III", 5659, 2000,
            new Dictionary<int, int>(){{0, 1}, {1, 1}, {2, 1}, {3, 1}, {4, 1}, {5, 1}, {6, 0}, {7, 0}, {8, 0}, {9, 0}, {10, 0}, {11, 0}},
            new Skill(Skill.Type.ScoreUp, 8, 30, 12, 27, 27), customCardFrame:true )    
        }
    };

    */
    // new Dictionary<int, int>(){{0, }, {1, }, {2, }, {3, }, {4, }, {5, }, {6, }, {7, }, {8, }, {9, }, {10, }, {11, }},
}

