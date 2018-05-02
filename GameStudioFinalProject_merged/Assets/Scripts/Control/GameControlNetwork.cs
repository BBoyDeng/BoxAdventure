using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameControlNetwork : GameBehaviour, GameBehaviour.IGameTracker{

	public GameManager manager;
	public GameObject mReturn;
	public Text[] mScoreTexts;
	public Image[] mPlayerIcons;
	public Sprite[] mPlayerSprites;

	public Image mResultImage;
	public Sprite[] mResultSprites;

	public Text[] mResultTexts;


	public Slider mTimerSlider;
	public Image mTimerSliderFillImage;
	public Image mReadyCountDownImage;
	public Sprite[] mTimeSprites;
	public Sprite[] mReadyCountDowns;
	public float mTimeLimit = 121f;


	public AudioSource mAudioSource;
	public AudioClip mReadyCountDownSE;
	public AudioClip mReadyGoSE;


	private int localIdx;
	int[] scorePos;

	float mTimeCountDown;

	int mReadyCountDownIndex = 0;
	int mScore = 0;
	float mTimerSpeed = 1f;

	private GameObject mReadyFrame;
	private GameObject mPlayFrame;
	private GameObject mResultFrame;


	private Dictionary<int,int> playerScores;
	private bool[] arePlayerEnd;


	void Awake () {
		// 固定手機螢幕為直立式
		Screen.orientation = ScreenOrientation.Portrait;

		scorePos=new int[mPlayerIcons.Length];
		for(int i=0;i<scorePos.Length;i++){
			scorePos[i]=i;
		}
		
		mReadyFrame = transform.Find("ReadyFrame").gameObject;
		mPlayFrame = transform.Find("PlayFrame").gameObject;
		mResultFrame = transform.Find("ResultFrame").gameObject;

		playerScores=new Dictionary<int, int>();
		arePlayerEnd=new bool[2];
		arePlayerEnd[0]=false;
		arePlayerEnd[1]=false;
		
		gControlListener+=handleControlEvent;
	}

	void OnDestroy()
	{
		gControlListener-=handleControlEvent;
	}

	// Use this for initialization
	void Start () {
		localIdx=manager.getPlayerIdx();
		scorePos[localIdx]=0;

		for(int i=0;i<localIdx&&i<scorePos.Length;i++){
			scorePos[i]++;
		}

		Dictionary<int,int> dict=new Dictionary<int, int>();
		for(int i=0;i<scorePos.Length;i++){
			dict[scorePos[i]]=i;
		}

		for(int i=0;i<mPlayerIcons.Length;i++){
			mPlayerIcons[i].sprite=mPlayerSprites[dict[i]];
		}


		mAudioSource.mute = isMute();

		int index = Random.Range (0, mTimeSprites.Length);
		mTimerSliderFillImage.sprite = mTimeSprites [index];

		mTimeCountDown = mTimeLimit;

		Score (0);
		for(int i=0;i<mScoreTexts.Length;i++){
			mScoreTexts[i].text = "0";
		}
	}

	// 得分
	void Score (int point) {
		mScore += point;
		if(mScore<0)
			mScore=0;
		mScoreTexts[0].text = mScore.ToString ();

		playerScores[localIdx]=mScore;

		PhotonNetwork.RaiseEvent(
			NetCode.ScoreCode,
			new object[]{"GCN",localIdx,mScore}, true,
			new RaiseEventOptions()
			{
				Receivers = ReceiverGroup.Others
			}
		);	
	}

	void setScore(int playerIdx,int score){
		mScoreTexts[scorePos[playerIdx]].text = score.ToString ();
	}
		
	// 開始執行timer倒數
	void TimerPlay () {

		// 每timerspeed秒鐘，重複呼叫Timer函式，且立即執行
		InvokeRepeating ("Timer", 0f, mTimerSpeed);
	}

	// 判斷timer時間是否已結束
	bool IsTimesUp () {
		return (mTimeCountDown == 0) ? true : false;
	}

	// 倒數計時器
	void Timer () {
		if (!IsTimesUp ()) {
			mTimeCountDown -= 1;
			mTimerSlider.value = mTimeCountDown;
		} else {
			CancelInvoke ("Timer");
		}
	}

	// 執行開場倒數
	void ReadyPlay () {
		InvokeRepeating ("ReadyCountDown", 0f, 1f);
	}

	// 開場倒數
	void ReadyCountDown () {
		if (mReadyCountDownIndex < 3) {
			mAudioSource.PlayOneShot (mReadyCountDownSE);
		} else {
			mAudioSource.PlayOneShot (mReadyGoSE);
		}

		mReadyCountDownImage.sprite = mReadyCountDowns [mReadyCountDownIndex];
		mReadyCountDownIndex++;

		if (mReadyCountDownIndex == 4)
			CancelInvoke ("ReadyCountDown");
	}


	void setResult(){
		mReturn.SetActive(true);

		for(int i=0;i<mResultTexts.Length;i++){
			if(playerScores.ContainsKey(i)){
				mResultTexts[i].text=""+playerScores[i];
			}	
			else
				mResultTexts[i].text="0";
		}

		mResultTexts[localIdx].fontSize=200;

		int mine=(int)playerScores[localIdx];

		foreach (KeyValuePair<int,int> pair in playerScores)
        {
        	if(pair.Value>mine){
				mResultImage.sprite=mResultSprites[1];
				return;
			}
        }

		if(playerScores[0]==playerScores[1])
			mResultImage.sprite=mResultSprites[2];
		else
			mResultImage.sprite=mResultSprites[0];
	}

/* handle control event */
	private void handleControlEvent(ControlEvent e, float arg){
		switch(e){
			case ControlEvent.Score:
			
				Score((int)arg);
			break;

			case ControlEvent.onReady:

				mReadyFrame.SetActive (true);
				mPlayFrame.SetActive (false);
				mResultFrame.SetActive(false);
				ReadyPlay ();
			break;

			case ControlEvent.OnStart:
					
				mReadyFrame.SetActive (false);
				mPlayFrame.SetActive (true);
				TimerPlay ();
			break;

			case ControlEvent.OnOver:

				mPlayFrame.SetActive (false);
				mResultFrame.SetActive(true);	
				
				PhotonNetwork.RaiseEvent(
					NetCode.EndCode,
					new object[]{"GCN",localIdx}, true,
					new RaiseEventOptions()
					{
						Receivers = ReceiverGroup.All
					}
				);
			break;
		}
	}

/* implement game tracker */
    public bool isTimesUp()
    {
        return IsTimesUp();
    }

    public bool isTimerPause()
    {
        return false;
    }

    public bool isGameResult()
    {
        return mResultFrame.activeSelf;
    }

    public bool testEnterCrazy()
    {
		return false;
    }

    public bool testExitCrazy()
    {
		return false;
    }

	public bool isInCrazyTime()
    {
		return false;
    }

/* photon event */
	private class NetCode
	{
		public readonly static byte ScoreCode=0;
		
		public readonly static byte EndCode=1;
	}

	void OnEnable()
	{
		PhotonNetwork.OnEventCall += this.OnEvent;
	}
	void OnDisable()
	{
		PhotonNetwork.OnEventCall -= this.OnEvent;
	}

	void OnEvent(byte eventcode, object content, int senderid)
	{
		object[] info=content as object[];
		string claz=info[0] as string;

		if(claz!="GCN")
			return;

		if(eventcode==NetCode.ScoreCode){
			int other=(int)info[1];
			int score=(int)info[2];

			playerScores[other]=score;
			setScore(other,score);
		}
		else if(eventcode==NetCode.EndCode){
			int player=(int)info[1];
			arePlayerEnd[player]=true;

			print("receive end code from player "+player);

			if(arePlayerEnd[0]&&arePlayerEnd[1]){
				setResult();
			}
		}
	}


}
