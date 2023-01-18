using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using CoreHapticsUnity;

#if UNITY_ANDROID && !UNITY_EDITOR
using Haptics;
#endif


public static class JudgementTime
{
    public static double SPerfect = 0.025;
    public static double Perfect = 0.08;
    public static double Good = 0.2;
    public static double AdjustSyncLimit = 0.3;
}

public class NoteManager : MonoBehaviour
{
    [SerializeField]
    private Camera mainCamera;
    public static NoteManager Instance;
    [SerializeField]
    private AnimationCurve noteMoveCurve, noteScaleCurve;

    [SerializeField]
    private GameObject shortNotePrefab, longNotePrefab;
    [SerializeField]
    public Transform noteParent;

    private float noteSpeed = 1.75f;    // time taken for note to fall to guide
    private float noteSize;

    private List<NoteData> upcomingNoteData = new List<NoteData>();
    private List<Note> activeNotes = new List<Note>();

    private Queue<Note> shortNotePool = new Queue<Note>();
    private Queue<Note> longNotePool = new Queue<Note>();

    public double interpolatedTime = 0;
    private double lastReportedMusicPosition = 0;

    private GameManager gameManager;
    private MusicManager musicManager;
    private JudgeScoreManager judgeScoreManager;
    private AudioManager audioManager;
    private SkillManager skillManager;
    private AnimationManager animationManager;
    private SpriteRenderer[] guideAnimationSprites = new SpriteRenderer[13];
    public List<float> judgeDiffs = new List<float>();
    
    public class LongNoteTouch{
        // List stores all LongNoteTouch objects
        public static List<LongNoteTouch> List = new List<LongNoteTouch>();
        private static WaitForSeconds waitFrameInterval = new WaitForSeconds(0.05f);
        public static bool hapticPlaying = false;

        public int fingerId;
        public string longNoteId;
        public JudgeResult judge;
        public bool isCompleted = false;
        public bool canPlayTapEffect = false;
        public Note nextNote;
        public IEnumerator waitFrame(){
            // yield return null;
            // yield return null;
            yield return waitFrameInterval;
            this.canPlayTapEffect = true;
        }
    }

    private Vector3 pauseButtonPosition;
    private Dictionary<int, int[]> judgeAcceptLanes = new Dictionary<int, int[]>{
        {0, new int[]{0, 1}},
        {1, new int[]{1, 0, 2}},
        {2, new int[]{2, 1, 3}},
        {3, new int[]{3, 2, 4}},
        {4, new int[]{4, 3, 5}},
        {5, new int[]{5, 4, 6}},
        {6, new int[]{6, 5, 7}},
        {7, new int[]{7, 8, 6}},
        {8, new int[]{8, 9, 7}},
        {9, new int[]{9, 10, 8}},
        {10, new int[]{10, 11, 9}},
        {11, new int[]{11, 12, 10}},
        {12, new int[]{12, 11}},
    };

    public int test_judge_before, test_judge_after;

    void Awake()
    {
        
        if(Instance == null) Instance = this;
        else if(Instance == this) Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        test_judge_after = 0;
        test_judge_before = 0;
        gameManager = GameManager.Instance;
        musicManager = MusicManager.Instance;
        judgeScoreManager = JudgeScoreManager.Instance;
        audioManager = AudioManager.Instance;
        skillManager = SkillManager.Instance;
        animationManager = AnimationManager.Instance;

        for(int i = 0; i < guideAnimationSprites.Length; i++){
            guideAnimationSprites[i] = GameObject.Find("GuideAnimation" + i).GetComponent<SpriteRenderer>();
        }

        // Set note size
        noteSize = 1.25f;   // default medium size
        if(Data.saveData.settings_ingameNoteSize == 0){ // small
            noteSize = 1f;
        }else if(Data.saveData.settings_ingameNoteSize == 2){
            noteSize = 1.5f;
        }

        // Set note speed
        noteSpeed = noteSpeed / Data.noteSpeedTable[Data.saveData.settings_noteSpeed];
        // if(Data.saveData.settings_noteSpeed == 0) noteSpeed = noteSpeed * 2f;
        // else if(Data.saveData.settings_noteSpeed == 1) noteSpeed = noteSpeed * 1.333f;
        // else if(Data.saveData.settings_noteSpeed == 3) noteSpeed = noteSpeed * 0.8f;
        // else if(Data.saveData.settings_noteSpeed == 4) noteSpeed = noteSpeed * 0.667f;
        // else if(Data.saveData.settings_noteSpeed == 5) noteSpeed = noteSpeed * 0.5f;

        LongNoteTouch.hapticPlaying = false;
        StartCoroutine(generateNotesCoroutine());
        pauseButtonPosition = gameManager.mainCamera.WorldToScreenPoint(new Vector3(-7.7f, 3.8f, 0));

        #if UNITY_ANDROID && !UNITY_EDITOR
        if(Data.saveData.settings_hapticFeedbackMaster) HapticsAndroid.Initialize();
        #endif

            #if UNITY_EDITOR
            Data.tempData["FUN"] = "";
            #endif
    }

    // Update is called once per frame

    private UnityEngine.InputSystem.EnhancedTouch.Touch update_touch;
    private JudgeResult update_judgeResult;
    private RaycastHit2D[] update_hits;
    private int[] update_lanes;

    void Update()
    {
        // animationManager.tapEffectQueue.Enqueue((true, Random.Range(0f, 2000f)));
        // Debug.Log("Update: " + Time.realtimeSinceStartup);
        if(gameManager.gameState == GameState.Playing){

            if(interpolatedTime == 0){
                lastReportedMusicPosition = musicManager.songPosition;
                interpolatedTime = musicManager.songPosition;
            }

            // float diff_ = (float)musicManager.songPosition - (float)musicManager.smoothSongPosition;
            // if(Mathf.Abs(diff_) > 1) Debug.Log(Mathf.Abs(diff_));
            
            if(musicManager.songPosition - lastReportedMusicPosition > 0.006){
                
                interpolatedTime = musicManager.songPosition;
                lastReportedMusicPosition = interpolatedTime;
            }else{
                interpolatedTime += Time.deltaTime;
            }
            
            // move notes downwards and destroy missed notes
            moveNotes();

            #if RHYTHMIZ_TEST
            if(Data.tempData.ContainsKey("FUN")){
                if(UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count != 0){
                    update_touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                    if(update_touch.screenPosition.x < pauseButtonPosition.x && update_touch.screenPosition.y > pauseButtonPosition.y){
                        gameManager.pauseGame();
                        return;
                    }
                }
                for(int i = 0; i < LongNoteTouch.List.Count; i++){
                    if(LongNoteTouch.List[i].nextNote != null){
                        if(interpolatedTime + 0.018 > LongNoteTouch.List[i].nextNote.targetTime){
                            test_handleLongJudge(LongNoteTouch.List[i]);
                        }
                    }
                }
                for(int i = 0; i < activeNotes.Count; i++){
                    if(activeNotes[i].type != NoteType.Short && activeNotes[i].type != NoteType.LongStart || activeNotes[i].tapped) continue;
                    if(interpolatedTime + 0.018 > activeNotes[i].targetTime){
                        test_handleJudge(i, JudgeResult.SPerfect, Random.Range(System.Int32.MinValue, System.Int32.MaxValue));
                    }else{
                        break;
                    }
                }
                return;
            }
            #endif


            // 터치 갯수만큼 판정을 위해 큐에서 꺼내오기
            for(int i = 0; i < UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count; i++){
                update_touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[i];
                Vector2 position = mainCamera.ScreenToWorldPoint(update_touch.screenPosition);
                update_hits = Physics2D.RaycastAll(position, Vector2.zero);
                // Debug.Log("Touch #" + touch.touchId + ", Status: " + touch.phase + ", Hits: " + hits.Length);
                if(update_hits.Length == 0) continue;  // Touch didn't hit the right box collider

                // If Pause button is touched
                if(update_hits[0].collider.CompareTag("PauseButton")){
                    gameManager.pauseGame();
                    continue;
                }

                // Get an array of lanes
                update_lanes = judgeAcceptLanes[int.Parse(update_hits[0].collider.gameObject.name)];
                if(update_touch.phase == UnityEngine.InputSystem.TouchPhase.Began){
                    judgeNote(update_lanes, update_touch.finger.index, update_touch.startTime);
                }else if(update_touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || update_touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary){  // touch moved/hold
                    // if active long note equals current touch finger id
                    for(int j = 0; j < LongNoteTouch.List.Count; j++){
                        if(update_touch.finger.index == LongNoteTouch.List[j].fingerId){
                            if(LongNoteTouch.List[j].canPlayTapEffect){
                                animationManager.tapEffectQueue.Enqueue((true, position.x));
                                LongNoteTouch.List[j].canPlayTapEffect = false;
                                GameStat.combo++;
                                animationManager.shouldUpdateCombo = true;
                                StartCoroutine(LongNoteTouch.List[j].waitFrame());
                            }
                            animationManager.playJudgeEffect(LongNoteTouch.List[j].judge, shouldPlayEffectImmediately: false);
                            // run long note judge if next note time is within judgement range
                            if(LongNoteTouch.List[j].nextNote != null){
                                if(Mathf.Abs((float)interpolatedTime - LongNoteTouch.List[j].nextNote.targetTime) < JudgementTime.Good)
                                    judgeLongNote(update_lanes, LongNoteTouch.List[j]);
                            }
                        }
                    }
                }else if(update_touch.phase == UnityEngine.InputSystem.TouchPhase.Ended){
                    // long note ended, remove long note
                    for(int j = 0; j < LongNoteTouch.List.Count; j++){
                        if(update_touch.finger.index == LongNoteTouch.List[j].fingerId){
                            LongNoteTouch.List.RemoveAt(j);
                            // Stop vibrating if all long note has ended
                            #if UNITY_IOS
                            if(LongNoteTouch.List.Count == 0 && LongNoteTouch.hapticPlaying){
                                Task.Run(delegate {CoreHapticsUnityProxy.StopKeepEngine();});
                                LongNoteTouch.hapticPlaying = false;
                            }
                            #endif
                            break;
                        }
                    }
                }
            }

            #if UNITY_EDITOR
            if(Mouse.current.leftButton.isPressed){
                Vector2 position = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.zero);
                if(hits.Length > 0){
                    if(hits[0].collider.gameObject.name == "Pause"){
                        gameManager.pauseGame();
                    }else{
                        int[] lanes = new int[hits.Length];
                        for(int j = 0; j < hits.Length; j++){
                            int.TryParse(hits[j].collider.gameObject.name, out lanes[j]);
                        }
                        judgeNote(lanes, 0, 0);
                    }
                }

                
            }
            #endif
        }
    }

    public void reset()
    {
        // Debug.Log("Reset");
        for(int i = 0; i < activeNotes.Count; i++){
            deactivateNote(activeNotes[i]); // reset notes and free them back to pool
        }
        activeNotes = new List<Note>();
        upcomingNoteData = new List<NoteData>();
        LongNoteTouch.List = new List<LongNoteTouch>();
        judgeDiffs = new List<float>();

        interpolatedTime = 0;
    }

    public void poolNotes()
    {
        // Pool note objects
        const int shortnote_pool_size = 40;
        const int longnote_pool_size = 50;
        GameObject noteObj;
        Note note;
        for(int i = 0; i < shortnote_pool_size; i++){
            noteObj = Instantiate(shortNotePrefab);
            noteObj.SetActive(false);
            note = noteObj.GetComponent<Note>();
            note.transform.parent = noteParent;
            shortNotePool.Enqueue(note);
        }
        for(int i = 0; i < longnote_pool_size; i++){
            noteObj = Instantiate(longNotePrefab);
            noteObj.SetActive(false);
            note = noteObj.GetComponent<Note>();
            note.transform.parent = noteParent;
            longNotePool.Enqueue(note);
        }
    }

    public void loadNotes(string[] list)
    {
        test_judge_after = 0; test_judge_before = 0;
        NoteData data;
        bool shouldFlipNotes = false;
        if(Data.saveData.settings_flipNotes == 1){
            // 50% chance to flip notes
            if(Random.Range(0, 2) == 1){
                shouldFlipNotes = true;
            }
        }else if(Data.saveData.settings_flipNotes == 2){
            // always flip notes
            shouldFlipNotes = true;
        }

        if(GameManager.Instance.isAdjustSync) shouldFlipNotes = false;

        if(shouldFlipNotes){
            Data.tempData["noteFlipped"] = "YES";
        }else{
            if(Data.tempData.ContainsKey("noteFlipped")){
                Data.tempData.Remove("noteFlipped");
            }
        }

        try{
            for(int i = 0; i < list.Length; i++){
                string[] dat = list[i].Split(";");
                if(list[i].Length == 0) continue;
                else if(list[i][0] == '#'){
                    continue;   // commented line
                }else if(dat[0] == "s"){
                    string[] main = dat[1].Split(",");
                    data = new NoteData();
                    if(shouldFlipNotes){
                        data.lane = 12 - int.Parse(main[0]);
                    }else{
                        data.lane = int.Parse(main[0]);
                    }
                    data.targetTimeStr = main[1];
                    data.targetTime = float.Parse(main[1], CultureInfo.InvariantCulture);
                    data.type = NoteType.Short;
                    upcomingNoteData.Add(data);
                }else if(dat[0] == "l"){
                    string longNoteId = System.Guid.NewGuid().ToString();
                    for(int j = 1; j < dat.Length; j++){
                        string[] main = dat[j].Split(",");
                        data = new NoteData();
                        if(shouldFlipNotes){
                            data.lane = 12 - int.Parse(main[0]);
                        }else{
                            data.lane = int.Parse(main[0]);
                        }
                        data.longNoteId = longNoteId;
                        data.longNoteOrder = j;
                        data.targetTimeStr = main[1];
                        data.targetTime = float.Parse(main[1], CultureInfo.InvariantCulture);
                        if(j == 1) data.type = NoteType.LongStart;
                        else if(j == dat.Length - 1) data.type = NoteType.LongEnd;
                        else data.type = NoteType.LongMiddle;
                        upcomingNoteData.Add(data);
                    }
                }
            }
        }catch{
            UIManager.Instance.loadSceneAsync("SelectMusicScene", delegate {
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("채보를 불러오는데 실패하였습니다.\n채보를 확인해 주세요.", "Failed to load the note file.")));
            });
            return;
        }

        // sort note by target time
        upcomingNoteData.Sort((x, y) => x.targetTime.CompareTo(y.targetTime));

        // Randomize notes
        #if UNITY_EDITOR
        shuffleNotes();
        #else
        if(Data.tempData.ContainsKey("random") && !GameManager.Instance.isAdjustSync) shuffleNotes();
        #endif
        GameStat.totalNoteCount = upcomingNoteData.Count;
    }

    // completely randomize notes
    private void shuffleNotes()
    {
        Dictionary<int, int> generateLaneMap(int domainL, int domainR, int rangeL = -1, int rangeR = -1, bool sort = false, bool reverse = false){
            Dictionary<int, int> map = new Dictionary<int, int>();
            if(rangeR - rangeL == domainR - domainL || rangeL == -1 || rangeR == -1){   // if domain and range size is same
                int temp;
                int[] mapArr = new int[domainR - domainL + 1];
                for(int i = 0; i < mapArr.Length; i++){
                    if(rangeL == -1 || rangeR == -1) mapArr[i] = domainL + i;
                    else mapArr[i] = rangeL + i;
                }

                // Randomly sort array
                if(!sort){
                    for(int i = 0; i < mapArr.Length - 1; i++){
                        int swapIndex = Random.Range(i + 1, mapArr.Length);
                        temp = mapArr[i];
                        mapArr[i] = mapArr[swapIndex];
                        mapArr[swapIndex] = temp;
                    }
                }

                if(reverse) System.Array.Reverse(mapArr);

                // map to dictionary
                for(int i = 0; i < mapArr.Length; i++){
                    map[domainL + i] = mapArr[i];
                }
            }else{  // domain and range has different size
                if(!sort){
                    for(int i = domainL; i <= domainR; i++){
                        map[i] = Random.Range(rangeL, rangeR + 1);
                    }
                }else{
                    List<int> rangeArr = new List<int>();
                    for(int i = rangeL; i <= rangeR; i++){
                        rangeArr.Add(i);
                    }
                    int sizeDiff = (rangeR - rangeL) - (domainR - domainL);
                    if(sizeDiff > 0){   // range is larger than domain, so remove numbers
                        for(int i = 0; i < sizeDiff; i++){
                            rangeArr.RemoveAt(Random.Range(0, rangeArr.Count));
                        }
                    }else if(sizeDiff < 0){ // range is smaller than domain, add numbers
                        for(int i = 0; i < Mathf.Abs(sizeDiff); i++){
                            rangeArr.Add(Random.Range(rangeL, rangeR + 1));
                        }
                        // new numbers added, so sort
                        rangeArr.Sort();
                    }
                    if(reverse) rangeArr.Reverse();
                    // apply to map
                    for(int i = domainL; i <= domainR; i++){
                        map[i] = rangeArr[i - domainL];
                    }
                }
            }
            return map;
        }

        // Dictionary<int, int> m = generateLaneMap(0, 6);
        // foreach(KeyValuePair<int, int> k in m) Debug.Log(k.Key + " - " + k.Value);
        // Debug.Log("===");
        // m = generateLaneMap(0, 6, 6, 12);
        // foreach(KeyValuePair<int, int> k in m) Debug.Log(k.Key + " - " + k.Value);
        // Debug.Log("===");
        // m = generateLaneMap(0, 12, 0, 6);
        // foreach(KeyValuePair<int, int> k in m) Debug.Log(k.Key + " - " + k.Value);
        // Debug.Log("===");
        // return;

        #if UNITY_EDITOR
        // Code to display original notes in the editor
        HashSet<int> inited = new HashSet<int>();
        NoteData data = upcomingNoteData[0];
        for(int i = 0; i < upcomingNoteData.Count; i++){
            if(inited.Contains(i)) continue;
            GameObject noteObj;
            if(upcomingNoteData[i].type == NoteType.Short) noteObj = Instantiate(shortNotePrefab);
            else noteObj = Instantiate(longNotePrefab);
            noteObj.GetComponent<Note>().Init(upcomingNoteData[i]);
            noteObj.transform.localPosition = new Vector2(upcomingNoteData[i].lane - 20, upcomingNoteData[i].targetTime * 16);
            inited.Add(i);
            if(upcomingNoteData[i].type == NoteType.LongStart){
                for(int j = i + 1; j < upcomingNoteData.Count; j++){
                    if(upcomingNoteData[i].longNoteId == upcomingNoteData[j].longNoteId){
                        GameObject noteObj2;
                        if(upcomingNoteData[j].type == NoteType.Short) noteObj2 = Instantiate(shortNotePrefab);
                        else noteObj2 = Instantiate(longNotePrefab);
                        Note notec = noteObj2.GetComponent<Note>();
                        notec.Init(upcomingNoteData[j]);
                        noteObj2.transform.localPosition = new Vector2(upcomingNoteData[j].lane - 20, upcomingNoteData[j].targetTime * 16);
                        if(upcomingNoteData[j].type != NoteType.LongEnd) noteObj.GetComponent<Note>().nextNote = notec;
                        noteObj = noteObj2;
                        inited.Add(j);
                    }
                }
            }

        }
        #endif

        // Begin randomizing
        HashSet<int> processedNoteAtIndex = new HashSet<int>();
        NoteData current;
        int boundaryL, boundaryR, currentLaneAvg, currentLaneAvgCount, altLaneAvg, altLaneAvgCount;
        bool isLeft;
        string currentLongNoteId;
        float longNoteEndTime;

        for(int i = 0; i < upcomingNoteData.Count; i++){
            // identify all long notes
            if(upcomingNoteData[i].type == NoteType.LongStart){
                current = upcomingNoteData[i];
                currentLongNoteId = current.longNoteId;

                // calculate whether the long note is on the left hand or right hand
                currentLaneAvg = current.lane; currentLaneAvgCount = 0;
                longNoteEndTime = current.targetTime;
                boundaryL = current.lane; boundaryR = current.lane;
                currentLaneAvgCount++;

                for(int j = i + 1; j < upcomingNoteData.Count; j++){
                    if(currentLongNoteId == upcomingNoteData[j].longNoteId){
                        currentLaneAvg += upcomingNoteData[j].lane;
                        currentLaneAvgCount++;
                        // store boundary lanes
                        if(upcomingNoteData[j].lane < boundaryL) boundaryL = upcomingNoteData[j].lane;
                        if(upcomingNoteData[j].lane > boundaryR) boundaryR = upcomingNoteData[j].lane;

                        if(upcomingNoteData[j].type == NoteType.LongEnd){
                            longNoteEndTime = upcomingNoteData[j].targetTime;
                            break;
                        }
                    }
                }

                // obtained lane avg and long note end time
                // check if there are other notes while long note is active
                altLaneAvg = 0; altLaneAvgCount = 0;
                for(int j = i - 1 < 0 ? 0 : i - 1; j < upcomingNoteData.Count; j++){
                    if(current.longNoteId == upcomingNoteData[j].longNoteId) continue;
                    if(upcomingNoteData[j].targetTime < current.targetTime) continue;
                    else if(upcomingNoteData[j].targetTime <= longNoteEndTime){
                        // falls in between long note
                        altLaneAvg += upcomingNoteData[j].lane;
                        altLaneAvgCount++;
                    }else break;
                }

                // whether the selected lane is left is determined by average lane value
                // if alt lane is empty, the middle line (6) is used to determine left/right
                isLeft = (float)currentLaneAvg / (float)currentLaneAvgCount <= (altLaneAvgCount == 0 ? 6 : (float)altLaneAvg / (float)altLaneAvgCount);
                // Debug.Log("Cur.avg: " + currentLaneAvg + ", Cur.avg.c: " + currentLaneAvgCount + ", alt.avg: " + altLaneAvg + ", alt.avg.c: " + altLaneAvgCount);
                // if the original long note lane range is small, allow it to become bigger
                int old_boundaryL = boundaryL, old_boundaryR = boundaryR;
                // Debug.Log("Old: " + old_boundaryL + " ; " + old_boundaryR);
                // Randomize long note range a bit
                if(boundaryR - boundaryL < 4){
                    float rand = Random.value;
                    if(rand < 0.25f) boundaryL += Random.Range(-1, 2);
                    else if(rand < 0.5f) boundaryR += Random.Range(-1, 2);
                    if(boundaryL < 0) boundaryL = 0;
                    else if(boundaryL > 12) boundaryL = 12;
                    if(boundaryR < 0) boundaryR = 0;
                    else if(boundaryR > 12) boundaryR = 12;
                }

                // generate mapping for long note
                Dictionary<int, int> map;
                if(isLeft) map = generateLaneMap(old_boundaryL, old_boundaryR, 0, boundaryR, sort: true, reverse: Random.value > 0.5f);
                else map = generateLaneMap(old_boundaryL, old_boundaryR, boundaryL, 12, sort: true, reverse: Random.value > 0.5f);
                int newBoundary = isLeft ? 0 : 12;  // if isLeft, 
                for(int j = i; j < upcomingNoteData.Count; j++){
                    if(currentLongNoteId == upcomingNoteData[j].longNoteId){
                        // randomize
                        // Debug.Log("Lane: " + upcomingNoteData[j] + ", isLeft: " + isLeft + ", BoundaryL: " + boundaryL + ", Boundary R: " + boundaryR);
                        upcomingNoteData[j].lane = map[upcomingNoteData[j].lane];
                        // Debug.Log("Lane: " + upcomingNoteData[j].lane);
                        // update new boundary
                        if(isLeft && upcomingNoteData[j].lane > newBoundary) newBoundary = upcomingNoteData[j].lane;
                        else if(!isLeft && upcomingNoteData[j].lane < newBoundary) newBoundary = upcomingNoteData[j].lane;
                        processedNoteAtIndex.Add(j);
                        if(upcomingNoteData[j].type == NoteType.LongEnd) break;
                    }
                }

                // if alt lane exists, check it and replace
                // replace only short notes since long notes will be adapted automatically after loop ends
                if(altLaneAvgCount == 0) continue;  // skip if there's no notes in alt lane
                // Debug.Log("Current: " + current + ", isLeft: " + isLeft + ", newBound: " + newBoundary);

                // if(isLeft){
                //     // map = generateLaneMap(newB)
                //     if(newBoundary < 4) map = generateLaneMap(0, 12, 6, 12, sort: true, reverse: Random.value > 0.5f);
                //     if(newBoundary > 9) map = generateLaneMap(0, 12, 12, 12, sort: true, reverse: Random.value > 0.5f);
                //     else map = generateLaneMap(0, 12, newBoundary + 3, 12);  // different hand, so create map for right hand
                // }else{
                //     if(newBoundary > 8) map = generateLaneMap(0, 12, 0, 6, sort: true, reverse: Random.value > 0.5f);
                //     if(newBoundary < 3) map = generateLaneMap(0, 12, 0, 0, sort: true, reverse: Random.value > 0.5f);
                //     else map = generateLaneMap(0, 12, 0, newBoundary - 3);
                // }

                for(int j = i - 5 < 0 ? 0 : i - 5; j < upcomingNoteData.Count; j++){
                    if(processedNoteAtIndex.Contains(j)) continue;
                    else if(upcomingNoteData[j].targetTime < current.targetTime) continue;
                    else if(upcomingNoteData[j].targetTime <= longNoteEndTime){
                        // if note is short, move
                        if(upcomingNoteData[j].type == NoteType.Short){
                            if(isLeft) upcomingNoteData[j].lane = Random.Range(newBoundary + 3 > 12 ? 12 : newBoundary + 3, 13);
                            else upcomingNoteData[j].lane = Random.Range(0, newBoundary - 3 < 0 ? 0 : newBoundary - 3);
                            // if(map.ContainsKey(upcomingNoteData[j].lane)){
                            //     upcomingNoteData[j].lane = map[upcomingNoteData[j].lane];
                            // }else{
                            //     // somehow the mapping key doesn't contain lane info
                            //     if(isLeft){
                            //         // check new boundary -- make sure lane doesn't go beyond 0, 12 when boundary exceeds one side
                            //         if(newBoundary <= 2) upcomingNoteData[j].lane = 0;
                            //         else upcomingNoteData[j].lane = Random.Range(0, newBoundary - 2);
                            //     }else{
                            //         if(newBoundary >= 10) upcomingNoteData[j].lane = 12;
                            //         else upcomingNoteData[j].lane = Random.Range(newBoundary + 3, 13);
                            //     }
                            // }
                            processedNoteAtIndex.Add(j);
                        }
                    }else break;    // done with the long note
                }
            }
        }

        // Only short notes are remaining
        for(int i = 0; i < upcomingNoteData.Count; i++){
            if(processedNoteAtIndex.Contains(i)) continue;
            current = upcomingNoteData[i];
            NoteData alt = null;
            int altIndex = -1;
            for(int j = i - 1 < 0 ? 0 : i - 1; j < upcomingNoteData.Count; j++){
                if(current.targetTimeStr == upcomingNoteData[j].targetTimeStr && current.lane != upcomingNoteData[j].lane){
                    if(!processedNoteAtIndex.Contains(j)){
                        // two short note exists at the same time, and the alt note has not been processed yet
                        alt = upcomingNoteData[j];
                        altIndex = j;
                        break;
                    }
                }else if(upcomingNoteData[j].targetTime > current.targetTime) break;    // no short note with same time exists
            }

            if(alt != null && altIndex >= 0){
                // randomize both note
                isLeft = current.lane < alt.lane;
                // Debug.Log("Alt exists: " + alt + ", isLeft: " + isLeft);
                if(isLeft){
                    current.lane = Random.Range(0, 6);
                    alt.lane = Random.Range(current.lane + 4 < 6 ? current.lane + 4 : 6, 13);
                }else{
                    alt.lane = Random.Range(0, 6);
                    current.lane = Random.Range(alt.lane + 4 < 6 ? alt.lane + 4 : 6, 13);
                }
                processedNoteAtIndex.Add(i);
                processedNoteAtIndex.Add(altIndex);
            }else{
                // randomize single note
                current.lane = Random.Range(current.lane - 3 < 0 ? 0 : current.lane - 3, current.lane + 3 > 12 ? 13 : current.lane + 3);
                processedNoteAtIndex.Add(i);
            }
        }
        // Done

        #if UNITY_EDITOR

        inited = new HashSet<int>();
        data = upcomingNoteData[0];
        for(int i = 0; i < upcomingNoteData.Count; i++){
            if(inited.Contains(i)) continue;
            GameObject noteObj;
            if(upcomingNoteData[i].type == NoteType.Short) noteObj = Instantiate(shortNotePrefab);
            else noteObj = Instantiate(longNotePrefab);
            noteObj.GetComponent<Note>().Init(upcomingNoteData[i]);
            noteObj.transform.localPosition = new Vector2(upcomingNoteData[i].lane, upcomingNoteData[i].targetTime * 16);
            inited.Add(i);
            if(upcomingNoteData[i].type == NoteType.LongStart){
                for(int j = i + 1; j < upcomingNoteData.Count; j++){
                    if(upcomingNoteData[i].longNoteId == upcomingNoteData[j].longNoteId){
                        GameObject noteObj2;
                        if(upcomingNoteData[j].type == NoteType.Short) noteObj2 = Instantiate(shortNotePrefab);
                        else noteObj2 = Instantiate(longNotePrefab);
                        Note notec = noteObj2.GetComponent<Note>();
                        notec.Init(upcomingNoteData[j]);
                        noteObj2.transform.localPosition = new Vector2(upcomingNoteData[j].lane, upcomingNoteData[j].targetTime * 16);
                        if(upcomingNoteData[j].type != NoteType.LongEnd) noteObj.GetComponent<Note>().nextNote = notec;
                        noteObj = noteObj2;
                        inited.Add(j);
                    }
                }
            }

        }
        #endif
    }

    private Note activateNote_note;
    Note activateNote(NoteData data){
        
        if(data.type == NoteType.Short) activateNote_note = shortNotePool.Dequeue();
        else activateNote_note = longNotePool.Dequeue();

        activateNote_note.Init(data);
        activeNotes.Add(activateNote_note);
        activateNote_note.gameObject.SetActive(false);
        activateNote_note.initialTime = activateNote_note.targetTime - noteSpeed;
        activateNote_note.transform.localPosition = new Vector3(NoteData.getNoteStartXpos(activateNote_note.lane), NoteData.noteStartYpos, 0.01f * data.longNoteOrder);
        activateNote_note.transform.localScale = new Vector2(0, 0);
        return activateNote_note;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Note deactivateNote(Note note){
        note.gameObject.SetActive(false);
        if(note.type == NoteType.Short) shortNotePool.Enqueue(note);
        else longNotePool.Enqueue(note);
        return note;
    }

    private WaitForSeconds generateNoteInterval = new WaitForSeconds(0.07f);
    IEnumerator generateNotesCoroutine()
    {
        while(true){
            while(gameManager.gameState != GameState.Playing) yield return null;
            generateNotes();
            yield return generateNoteInterval;
        }
    }

    private Note generateNotes_note, generateNotes_nextNote;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void generateNotes(){   // Generate notes that should appear soon
        if(upcomingNoteData.Count == 0) return;
        if(upcomingNoteData[0].targetTime - interpolatedTime < noteSpeed + 0.25){
            generateNotes_note = activateNote(upcomingNoteData[0]);
            upcomingNoteData.RemoveAt(0);

            // set up long notes
            if(generateNotes_note.type == NoteType.LongStart){
                int upcomingNoteDataCount = upcomingNoteData.Count;
                for(int i = 0; i < upcomingNoteDataCount; i++){
                    if(generateNotes_note.longNoteId == upcomingNoteData[i].longNoteId){
                        // initialize next notes
                        Note generateNotes_nextNote = activateNote(upcomingNoteData[i]);
                        generateNotes_note.nextNote = generateNotes_nextNote;
                        generateNotes_note = generateNotes_nextNote;
                        upcomingNoteData.RemoveAt(i);
                        i--;
                        upcomingNoteDataCount--;
                        if(generateNotes_nextNote.type == NoteType.LongEnd) break;
                    }
                }
            }
            
            generateNotes();    // check if different note has same target time
        }
    }

    private Note moveNotes_note;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void moveNotes()
    {
        int activeNotesCount = activeNotes.Count;
        float videoSyncAdjustedTime, percentage, adjusted, xpos, ypos = 0, rawDiff, guideAnimationTime;

        for(int i = 0; i < guideAnimationSprites.Length; i++){
            guideAnimationSprites[i].color = Color.clear;
        }

        for(int i = 0; i < activeNotesCount; i++){  // for each active note
            // calculate Y position (guide ypos = -3.6)
            moveNotes_note = activeNotes[i];
            // get the progress percentage

            // float timeSinceStart = (float)(Time.realtimeSinceStartupAsDouble - playStartTime);
            videoSyncAdjustedTime = (float)musicManager.smoothSongPosition + Data.saveData.videoSyncProfiles[Data.saveData.audioSyncSelected];
            // float videoSyncAdjustedTime = (float)interpolatedTime + Data.saveData.videoSync;
            percentage = (videoSyncAdjustedTime - moveNotes_note.initialTime) / (moveNotes_note.targetTime - moveNotes_note.initialTime);
            
            if(percentage < 0) continue;


            if(!moveNotes_note.isActive){
                moveNotes_note.gameObject.SetActive(true);
                moveNotes_note.isActive = true;
            }

            // float adjusted = Mathf.Pow(percentage, 2.9f) - 0.2f * Mathf.Pow(percentage, 2f) + 0.2f * percentage;     // ORIGINAL
            // float adjusted = 1.1f * Mathf.Pow(percentage, 3) - 0.3f * Mathf.Pow(percentage, 2) + 0.2f * percentage;
            // float adjusted = 0.8f * Mathf.Pow(percentage, 3.5f) + 0.2f * Mathf.Pow(percentage, 0.9f);
            // if(Data.tempData.ContainsKey("NewInput")){
            //     adjusted = noteMoveCurve.Evaluate(percentage);
            // }else{
            // adjusted = 0.8f * Mathf.Pow(percentage, 3.7f) + 0.2f * percentage;
            adjusted = 0.8f * Mathf.Pow(percentage, 3.5f) + 0.2f * percentage;
            // }
            
            xpos = Mathf.LerpUnclamped(NoteData.getNoteStartXpos(moveNotes_note.lane), NoteData.lanePosition[moveNotes_note.lane], adjusted);
            ypos = (float)(NoteData.noteStartYpos - (3.67 + NoteData.noteStartYpos) * adjusted);
            if(moveNotes_note.type == NoteType.LongMiddle || moveNotes_note.type == NoteType.LongEnd){
                moveNotes_note.transform.localScale = new Vector2(adjusted * noteSize * 0.9f, adjusted * noteSize * 0.9f);
            }else{
                moveNotes_note.transform.localScale = new Vector2(adjusted * noteSize, adjusted * noteSize);
            }

            moveNotes_note.transform.localPosition = new Vector3(xpos, ypos, moveNotes_note.transform.localPosition.z);
            // note.transform.localScale = new Vector2(sizeAdjust * 1.2f, sizeAdjust * 1.2f);
            
            // check if note has not been tapped and went below the Good judgement time
            // rawdiff > 0 => Tapped late, rawdiff < 0 => Tapped early
            rawDiff = (float)interpolatedTime - moveNotes_note.targetTime;
            if(rawDiff > (gameManager.isAdjustSync ? JudgementTime.AdjustSyncLimit : JudgementTime.Good) && !moveNotes_note.tapped){
                moveNotes_note.noteMissed();
                judgeScoreManager.handleJudge(JudgeResult.Miss);
                animationManager.playJudgeEffect(JudgeResult.Miss, true);
                animationManager.shouldUpdateCombo = true;

                if(moveNotes_note.type == NoteType.LongMiddle || moveNotes_note.type == NoteType.LongEnd){
                    // remove from LongNoteTouch list
                    for(int j = 0; j < LongNoteTouch.List.Count; j++){
                        if(LongNoteTouch.List[j].longNoteId == moveNotes_note.longNoteId){
                            LongNoteTouch.List.RemoveAt(j);
                            // Stop vibrating if all long note has ended
                            #if UNITY_IOS
                            if(LongNoteTouch.List.Count == 0 && LongNoteTouch.hapticPlaying){
                                Task.Run(delegate {CoreHapticsUnityProxy.StopKeepEngine();});
                                LongNoteTouch.hapticPlaying = false;
                            }
                            #endif
                            break;
                        }
                    }
                }
            }

            // guide animation
            guideAnimationTime = moveNotes_note.targetTime - videoSyncAdjustedTime;
            if(guideAnimationTime > 0 && guideAnimationTime < 0.6f){
                if(guideAnimationSprites[moveNotes_note.lane].color.a == 0 && (moveNotes_note.type == NoteType.Short || moveNotes_note.type == NoteType.LongStart) && !moveNotes_note.tapped){
                    guideAnimationSprites[moveNotes_note.lane].color = new Color(1f, 1f, 1f, 1 - ((guideAnimationTime) / 0.6f));
                }
            }

            if(ypos < -6.5 && interpolatedTime > moveNotes_note.targetTime + JudgementTime.AdjustSyncLimit){    // Note missed
                // if it is long note, wait until long note has ended
                if(moveNotes_note.type == NoteType.Short){
                    // Destroy notes
                    // note.gameObject.SetActive(false);
                    deactivateNote(moveNotes_note);
                    activeNotes.RemoveAt(i);
                    i--;
                    activeNotesCount--;
                }else if(moveNotes_note.nextNote != null){
                    if(moveNotes_note.nextNote.transform.localPosition.y < -6.5){
                        deactivateNote(moveNotes_note);
                        activeNotes.RemoveAt(i);
                        i--;
                        activeNotesCount--;
                    }
                }else if(moveNotes_note.type == NoteType.LongEnd){
                    for(int j = 0; j < activeNotesCount; j++){
                        if(activeNotes[j].longNoteId == moveNotes_note.longNoteId){
                            deactivateNote(activeNotes[j]);
                            activeNotes.RemoveAt(j);
                            j--;
                            i--;
                            activeNotesCount--;
                        }
                    }
                }             
            }
        }        
    }

    #if RHYTHMIZ_TEST
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void test_handleJudge(int activeNoteIndex, JudgeResult result, int fingerId)
    {
        // if note is .LongStart, start long note and save
        if(activeNotes[activeNoteIndex].type == NoteType.LongStart){
            judgeNote_touch = new LongNoteTouch();
            judgeNote_touch.judge = result;
            judgeNote_touch.fingerId = fingerId;
            judgeNote_touch.longNoteId = activeNotes[activeNoteIndex].longNoteId;
            judgeNote_touch.nextNote = activeNotes[activeNoteIndex].nextNote;
            LongNoteTouch.List.Add(judgeNote_touch);
            
            // Long note started, start vibrating
            #if UNITY_IOS
            if(LongNoteTouch.List.Count > 0 && !LongNoteTouch.hapticPlaying && Data.saveData.settings_hapticFeedbackMaster){
                Task.Run(delegate {CoreHapticsUnityProxy.PlayContinuous(0.3f, 0.06f, 30f); });
                LongNoteTouch.hapticPlaying = true;
            }
            #endif
            StartCoroutine(judgeNote_touch.waitFrame());
        }

        // Play Animations
        animationManager.tapEffectQueue.Enqueue((activeNotes[activeNoteIndex].type != NoteType.Short, NoteData.lanePosition[activeNotes[activeNoteIndex].lane]));
        animationManager.playJudgeEffect(result, true);
        GameStat.combo++;
        animationManager.shouldUpdateCombo = true;

        judgeScoreManager.handleJudge(judgeNote_result, isLongNote: activeNotes[activeNoteIndex].type != NoteType.Short);
        activeNotes[activeNoteIndex].tapped = true;   // mark the note as scored

        // VIBRATE
        if(Data.saveData.settings_hapticFeedbackMaster){
            if(activeNotes[activeNoteIndex].type == NoteType.Short || activeNotes[activeNoteIndex].type == NoteType.LongStart){
                #if UNITY_ANDROID && !UNITY_EDITOR
                HapticsAndroid.VibratePredefined(HapticsAndroid.PredefinedEffect.EFFECT_CLICK);
                #elif UNITY_IOS && !UNITY_EDITOR
                Task.Run(delegate {CoreHapticsUnityProxy.PlayTransient(0.6f, 1f); });
                #endif
            }
        }




        // destroy only short notes, long notes contain trails, so don't destroy
        if(activeNotes[activeNoteIndex].type == NoteType.Short){
            deactivateNote(activeNotes[activeNoteIndex]);
            activeNotes.RemoveAt(activeNoteIndex);
        }
    }


    IEnumerator removeLongNoteTouch(LongNoteTouch touch){
        yield return longNoteTouchRemoveDelay;
        LongNoteTouch.List.Remove(touch);
        // Stop vibrating if all long note has ended
        #if UNITY_IOS
        if(LongNoteTouch.List.Count == 0 && LongNoteTouch.hapticPlaying){
            Task.Run(delegate {CoreHapticsUnityProxy.StopKeepEngine();});
            LongNoteTouch.hapticPlaying = false;
        }
        #endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void test_handleLongJudge(LongNoteTouch touch)
    {
        Note note = touch.nextNote;
        if(note.type == NoteType.LongMiddle || note.type == NoteType.LongEnd){
            // judge accepted, mark as judged
            judgeScoreManager.handleJudge(touch.judge, isLongNote: true);
            note.tapped = true;
            if(note.type == NoteType.LongMiddle && Data.saveData.settings_hapticFeedbackMaster){
                StartCoroutine(playTransientHaptic(0));
            }
            // if it is the last note, remove from LongNoteTouch.List
            if(note.type == NoteType.LongEnd){
                
                StartCoroutine(removeLongNoteTouch(touch));
            }

            touch.nextNote = note.nextNote;

            // judge next note if it is also within judgement time
            if(note.nextNote != null){
                if(Mathf.Abs((float)interpolatedTime - note.nextNote.targetTime) < JudgementTime.Good){
                    test_handleLongJudge(touch);
                }
            }
        }
    }
    #endif



    private JudgeResult judgeNote_result;
    private LongNoteTouch judgeNote_touch;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void judgeNote(int[] lanes, int fingerId, double tapTime)
    {
        if(activeNotes.Count <= 0){ // if empty, play tap sound
            if(Data.saveData.settings_ingameSoundEffect) audioManager.playClip(SoundEffects.liveTap);
            return;
        }
        for(int i = 0; i < activeNotes.Count; i++){
            if(System.Array.IndexOf(lanes, activeNotes[i].lane) != -1){ // same lane
                if(activeNotes[i].tapped) continue;
                float tapFrameDiff = (float)(musicManager.fixedRealTimeSinceStartupAsDouble - tapTime);
                float rawDiff = (float)interpolatedTime - activeNotes[i].targetTime - tapFrameDiff;
                // rawdiff < 0 => tapped early/, rawdiff > 0 => tapped late
                // float tapFrameDiff = (float)(Time.realtimeSinceStartupAsDouble - tapTime);
                // // Debug.Log(tapFrameDiff);
                // float rawDiff = (float)interpolatedTime - activeNotes[i].targetTime - tapFrameDiff;
                float timeDiff = Mathf.Abs(rawDiff);

                if(activeNotes[i].type == NoteType.LongMiddle || activeNotes[i].type == NoteType.LongEnd){
                    // when you tap long note after you raised your finger
                    if(timeDiff < JudgementTime.Good){
                        judgeNote_result = JudgeResult.Good;
                    }else{
                        continue;
                    }
                }else{
                    if(timeDiff < JudgementTime.SPerfect * skillManager.sPerfectRangeMultiplier){
                        judgeNote_result = JudgeResult.SPerfect;
                    }else if(timeDiff < JudgementTime.Perfect){
                        judgeNote_result = JudgeResult.Perfect;
                    }else if(timeDiff < JudgementTime.Good){
                        judgeNote_result = JudgeResult.Good;
                    }else{  // blank tap
                        if(gameManager.isAdjustSync && timeDiff < JudgementTime.AdjustSyncLimit){
                            judgeNote_result = JudgeResult.Good;
                        }else{
                            continue;
                        }
                        
                    }
                }
                // if note is .LongStart, start long note and save
                if(activeNotes[i].type == NoteType.LongStart){
                    judgeNote_touch = new LongNoteTouch();
                    judgeNote_touch.judge = judgeNote_result;
                    judgeNote_touch.fingerId = fingerId;
                    judgeNote_touch.longNoteId = activeNotes[i].longNoteId;
                    judgeNote_touch.nextNote = activeNotes[i].nextNote;
                    LongNoteTouch.List.Add(judgeNote_touch);
                    
                    // Long note started, start vibrating
                    #if UNITY_IOS
                    if(LongNoteTouch.List.Count > 0 && !LongNoteTouch.hapticPlaying && Data.saveData.settings_hapticFeedbackMaster){
                        Task.Run(delegate {CoreHapticsUnityProxy.PlayContinuous(0.3f, 0.06f, 30f); });
                        LongNoteTouch.hapticPlaying = true;
                    }
                    #endif
                    StartCoroutine(judgeNote_touch.waitFrame());
                }

                // Play Animations
                animationManager.tapEffectQueue.Enqueue((activeNotes[i].type != NoteType.Short, NoteData.lanePosition[activeNotes[i].lane]));
                animationManager.playJudgeEffect(judgeNote_result, true);
                GameStat.combo++;
                animationManager.shouldUpdateCombo = true;

                judgeScoreManager.handleJudge(judgeNote_result, isLongNote: activeNotes[i].type != NoteType.Short);
                activeNotes[i].tapped = true;   // mark the note as scored

                // VIBRATE
                if(Data.saveData.settings_hapticFeedbackMaster){
                    if(activeNotes[i].type == NoteType.Short || activeNotes[i].type == NoteType.LongStart){
                        #if UNITY_ANDROID && !UNITY_EDITOR
                        HapticsAndroid.VibratePredefined(HapticsAndroid.PredefinedEffect.EFFECT_CLICK);
                        #elif UNITY_IOS && !UNITY_EDITOR
                        Task.Run(delegate {CoreHapticsUnityProxy.PlayTransient(0.6f, 1f); });
                        #endif
                    }
                }




                // destroy only short notes, long notes contain trails, so don't destroy
                if(activeNotes[i].type == NoteType.Short){
                    deactivateNote(activeNotes[i]);
                    activeNotes.RemoveAt(i);
                }


                
                if(gameManager.isAdjustSync){   // Add adjust sync data
                    judgeDiffs.Add(rawDiff);
                }
                
                #if RHYTHMIZ_TEST
                if(judgeNote_result == JudgeResult.Perfect || judgeNote_result == JudgeResult.Good){
                    if(rawDiff > 0) test_judge_after += 1;
                    else test_judge_before += 1;
                }
                if(!gameManager.isAdjustSync) judgeDiffs.Add(rawDiff);
                gameManager.TEST_syncDiffSum += rawDiff;
                gameManager.TEST_syncTapCount++;
                #endif

                return;
            }
        }
        // haven't triggered anything, play tap sound
        
        if(Data.saveData.settings_ingameSoundEffect) audioManager.playClip(SoundEffects.liveTap);
    }

    private WaitForSeconds longNoteTouchRemoveDelay = new WaitForSeconds(0.03f);
    private IEnumerator playTransientHaptic(float rawDiff){
        if(rawDiff < 0) yield return new WaitForSeconds(-rawDiff);
        #if UNITY_ANDROID && !UNITY_EDITOR
        HapticsAndroid.VibratePredefined(HapticsAndroid.PredefinedEffect.EFFECT_CLICK);
        #elif UNITY_IOS && !UNITY_EDITOR
        Task.Run(delegate {CoreHapticsUnityProxy.PlayTransient(0.6f, 1f); });
        #endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void judgeLongNote(int[] lanes, LongNoteTouch touch)
    {   // method only called during touch move/hold
        // for all active notes, check if longNoteId matches other active notes
        for(int i = 0; i < activeNotes.Count; i++){
            // if activeNote doesn't match long note, skip
            if(activeNotes[i].longNoteId != touch.longNoteId) continue;
            if(activeNotes[i] != touch.nextNote) continue;  // skip if the note is not next note
            if(activeNotes[i].tapped) continue; // skip if it is already judged
            // 
            // 
            if(System.Array.IndexOf(lanes, activeNotes[i].lane) != -1){
                float rawDiff = (float)interpolatedTime - activeNotes[i].targetTime;
                // if the note in current lane is a long note that is not .LongStart
                if(activeNotes[i].type == NoteType.LongMiddle || activeNotes[i].type == NoteType.LongEnd){
                    // check if you're within "Good" time window
                    if(Mathf.Abs(rawDiff) < JudgementTime.Good){
                        // judge accepted, mark as judged
                        judgeScoreManager.handleJudge(touch.judge, isLongNote: true);
                        touch.nextNote = activeNotes[i].nextNote;
                        activeNotes[i].tapped = true;
                        if(activeNotes[i].type == NoteType.LongMiddle && Data.saveData.settings_hapticFeedbackMaster){
                            StartCoroutine(playTransientHaptic(rawDiff));
                        }
                        // if it is the last note, remove from LongNoteTouch.List
                        if(activeNotes[i].type == NoteType.LongEnd){
                            IEnumerator remove(LongNoteTouch touch){
                                yield return longNoteTouchRemoveDelay;
                                LongNoteTouch.List.Remove(touch);
                                // Stop vibrating if all long note has ended
                                #if UNITY_IOS
                                if(LongNoteTouch.List.Count == 0 && LongNoteTouch.hapticPlaying){
                                    Task.Run(delegate {CoreHapticsUnityProxy.StopKeepEngine();});
                                    LongNoteTouch.hapticPlaying = false;
                                }
                                #endif
                            }
                            touch.isCompleted = true;
                            StartCoroutine(remove(touch));
                        }

                        // judge next note if it is also within judgement time
                        if(activeNotes[i].nextNote != null){
                            if(Mathf.Abs((float)interpolatedTime - activeNotes[i].nextNote.targetTime) < JudgementTime.Good){
                                judgeLongNote(lanes, touch);
                            }
                        }
                        return;
                    }
                }
            }
        }
    }

    
}
