using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameControl : GameBehaviour, GameBehaviour.IGameTracker{

	public Text mScoreText;
	public Text mResultScoreText;
	public Text mHighestScoreText;
	public Slider mTimerSlider;
	public Slider mFeverSlider;
	public Image mTimerSliderFillImage;
	public Image mFeverSliderFillImage;
	public Image mReadyCountDownImage;
	public AudioSource mAudioSource;
	public AudioClip mReadyCountDownSE;
	public AudioClip mReadyGoSE;

	public Sprite[] mTimeSprites;
	public Sprite[] mReadyCountDowns;

	public float mTimeLimit = 121f;

	float mTimeCountDown;

	int mReadyCountDownIndex = 0;
	int mScore = 0;
	float mTimerSpeed = 1f;
	bool mIsTimerPause = false;
	bool mIsInCrazyTime = false;

	// PlayerPrefs
	string HIGHESTSCORE = "HighestScore";

	
	private GameObject mReadyFrame;
	private GameObject mPlayFrame;
	private GameObject mPauseFrame;
	private GameObject mResultFrame;

	void Awake () {
print("gc awake");
		// 固定手機螢幕為直立式
		Screen.orientation = ScreenOrientation.Portrait;

		// 依照手機螢幕尺寸調整canvas比例
		//CanvasScaler canvasScaler = GetComponent<CanvasScaler> ();
		//canvasScaler.referenceResolution = new Vector2 (Screen.width, Screen.height);

		
		mReadyFrame = transform.Find("ReadyFrame").gameObject;
		mPlayFrame = transform.Find("PlayFrame").gameObject;
		mPauseFrame = transform.Find("PauseFrame").gameObject;
		mResultFrame = transform.Find("ResultFrame").gameObject;

		gControlListener+=handleControlEvent;
	}

	void OnDestroy()
	{
		gControlListener-=handleControlEvent;
	}

	// Use this for initialization
	void Start () {
print("gc start");
		mAudioSource.mute = isMute();

		int index = Random.Range (0, mTimeSprites.Length);
		mTimerSliderFillImage.sprite = mTimeSprites [index];
		mFeverSliderFillImage.sprite = mTimeSprites [(index + 1) % mTimeSprites.Length];

		mTimeCountDown = mTimeLimit;

		Score (0);
	}

	// 得分
	int Score (int point) {
		mScore += point;
		mScoreText.text = mScore.ToString ();
		return mScore;
	}

	// FEVER充電
	void FeverCharge (int point) {
		if (mFeverSlider.value + point >= mFeverSlider.maxValue) {
			mFeverSlider.value = mFeverSlider.maxValue;
		} else {
			mFeverSlider.value += point;
		}
	}

	// 展示結算畫面的分數
	void ShowResultScore () {
		mResultScoreText.text = mScore.ToString ();
		mHighestScoreText.text = PlayerPrefs.GetInt (HIGHESTSCORE, 0).ToString ();
	}

	// 儲存遊戲最高分
	void SetHighestScore () {
		if (mScore > PlayerPrefs.GetInt (HIGHESTSCORE, 0)) {
			PlayerPrefs.SetInt (HIGHESTSCORE, mScore);
		}
	}

	// 開始執行timer倒數
	void TimerPlay () {
		mIsTimerPause = false;

		// 每timerspeed秒鐘，重複呼叫Timer函式，且立即執行
		InvokeRepeating ("Timer", 0f, mTimerSpeed);
	}

	// 暫停執行timer倒數
	void TimerPause () {
		mIsTimerPause = true;

		// 取消重複呼叫Timer函式
		CancelInvoke ("Timer");
	}

	// 加時間，不得超過時間上限
 	void IncreaseTime (float time) {
		CancelInvoke ("Timer");
		if (mTimeCountDown + time >= mTimeLimit) {
			mTimeCountDown = mTimeLimit;
			mTimerSlider.value = mTimeLimit;
		} else { 
			mTimeCountDown += time;
			mTimerSlider.value += time;
		}
		InvokeRepeating ("Timer", 0f, mTimerSpeed);
	}

	// 減時間，不得低於時間下限
	void DecreaseTime (float time) {
		CancelInvoke ("Timer");
		if (mTimeCountDown - time <= 0f) {
			mTimeCountDown = 0f;
			mTimerSlider.value = 0f;
		} else { 
			mTimeCountDown -= time;
			mTimerSlider.value -= time;
		}
		InvokeRepeating ("Timer", 0f, mTimerSpeed);
	}

	// 調整Timer的速度
	void AdjustTimerSpeed (float speed) {
		CancelInvoke ("Timer");
		mTimerSpeed = speed;
		InvokeRepeating ("Timer", 0f, mTimerSpeed);
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

	// 判斷timer時間是否已結束
	bool IsTimesUp () {
		return (mTimeCountDown == 0) ? true : false;
	}

	// 判斷timer時間是否已暫停
	bool IsTimerPause () {
		return (mIsTimerPause) ? true : false;
	}

	// 進入瘋狂時間
	void EnterCrazyTime () {
		TimerPause ();
		mIsInCrazyTime = true;
		InvokeRepeating ("CrazyTime", 0f, 0.2f);
	}

	// 瘋狂時間
	void CrazyTime () {
		mFeverSlider.value--;
	}

	// 離開瘋狂時間
	void ExitCrazyTime () {
		TimerPlay ();
		mIsInCrazyTime = false;
		CancelInvoke ("CrazyTime");
	}

	// 判斷是否已進入瘋狂時間
	bool IsInCrazyTime () {
		return mIsInCrazyTime;
	}

	// 判斷FEVER是否已充電完成
	bool IsFeverFull () {
		return (mFeverSlider.value == mFeverSlider.maxValue) ? true : false;
	}

	// 判斷FEVER是否已放電完成
	bool IsFeverEmpty () {
		return (mFeverSlider.value == mFeverSlider.minValue) ? true : false;
	}

/* handle control event */
	private void handleControlEvent(ControlEvent e, float arg){
		switch(e){
			case ControlEvent.Score:
			
				Score((int)arg);
			break;

			case ControlEvent.AdjustTimerSpeed:
			
            	AdjustTimerSpeed(arg);
			break;

			case ControlEvent.ChargeFever:
			
				FeverCharge((int)arg);
			break;

			case ControlEvent.IncreaseTime:
			
            	IncreaseTime(arg);
			break;

			case ControlEvent.DecreaseTime:
			
				DecreaseTime(arg);
			break;

			case ControlEvent.onReady:

				mReadyFrame.SetActive (true);
				mPlayFrame.SetActive (false);
				mPauseFrame.SetActive (false);
				mResultFrame.SetActive (false);
				ReadyPlay();
			break;

			case ControlEvent.OnStart:
				
				mReadyFrame.SetActive (false);
				mPlayFrame.SetActive (true);
				TimerPlay();
			break;

			case ControlEvent.OnOver:

				mPlayFrame.SetActive (false);
				mResultFrame.SetActive (true);
				
				SetHighestScore ();
				ShowResultScore ();
			break;

			case ControlEvent.OnPlay:
				
				TimerPlay ();
				mPauseFrame.SetActive (false);
			break;

			case ControlEvent.OnPause:
				
				TimerPause ();
				mPauseFrame.SetActive (true);
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
        return IsTimerPause();
    }

    public bool isGameResult()
    {
        return mResultFrame.activeSelf;
    }

    public bool testEnterCrazy()
    {
        if (IsFeverFull ()) {
			EnterCrazyTime ();
			return true;
		}

		return false;
    }

    public bool testExitCrazy()
    {
		if (IsFeverEmpty () && IsInCrazyTime ()) {
			ExitCrazyTime ();
			return true;
		}

		return false;
    }

    public bool isInCrazyTime()
    {
        return IsInCrazyTime();
    }
}
