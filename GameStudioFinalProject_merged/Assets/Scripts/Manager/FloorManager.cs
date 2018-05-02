using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloorManager : GameBehaviour {

	public GameManager manager;
	private FloorControl floorControl;
	private Dictionary<string,GameObject> itemMap;

	public GameObject[] mFloor;
	public GameObject mBonusFloor;

	public GameObject mItemKey;
	public GameObject mItmeTime;
	public GameObject mItemTimeBig;
	public GameObject mItemDiamond;
	public GameObject mItemDiamondBig;
	public GameObject mItemBattery;

	public GameObject mItemPuzzle_1;
	public GameObject mItemPuzzle_2;
	public GameObject mItemPuzzle_3;

	public ParticleSystem mHoleParticle;

	public AudioSource mHoleTimerSE;


	public float mHeight = 10f;

	GameObject[] mFloorInstance = new GameObject[2];


	int mCurrFloorIndex;
	int mCountOfFloor = 0;
	int mPrevFloorIndex = -1;
	int mUnuseFloorIndex = 0;	// 當前非使用中地板的index，0 or 1

	private Dictionary<string,Sprite> missionItemMap;
	private string[] missionItemType;

	private Dictionary<string,Sprite> missionMoveMap;
	private string[] missionMoveType;

	private Dictionary<string,Sprite[]> missionPuzzleMap;
	private string[] missionPuzzleType;

	public Image[] mMissionImage;
	public Text[] mMissionText;

	public Sprite mMissionTime;
	public Sprite mMissionTimeBig;
	public Sprite mMissionBattery;
	public Sprite mMissionDiamond;

	public Sprite mMissionRight;
	public Sprite mMissionLeft;
	public Sprite mMissionForward;
	public Sprite mMissionBackward;

	public Image mMissionPuzzleImage;
	public Sprite[] mMissionPuzzle1;
	public Sprite[] mMissionPuzzle2;
	public Sprite[] mMissionPuzzle3;


	private List<IMission> missions;
	private List<string> missionSet;

	void Awake()
	{
print("fm awake");
		gFloorListener+=handleFloorEvent;
		gMissionListener+=handleMissionEvent;

		itemMap=new Dictionary<string, GameObject>();
		itemMap[TAG_ITEM_TIME]=mItmeTime;
		itemMap[TAG_ITEM_TIME_BIG]=mItemTimeBig;
		itemMap[TAG_ITEM_BATTERY]=mItemBattery;
		itemMap[TAG_ITEM_DIAMOND]=mItemDiamond;
		itemMap[TAG_ITEM_PUZZLE_1]=mItemPuzzle_1;
		itemMap[TAG_ITEM_PUZZLE_2]=mItemPuzzle_2;
		itemMap[TAG_ITEM_PUZZLE_3]=mItemPuzzle_3;

		missionItemMap=new Dictionary<string, Sprite>();
		missionItemMap[TAG_ITEM_TIME]=mMissionTime;
		missionItemMap[TAG_ITEM_TIME_BIG]=mMissionTimeBig;
		missionItemMap[TAG_ITEM_DIAMOND]=mMissionDiamond;
		missionItemMap[TAG_ITEM_BATTERY]=mMissionBattery;

		missionItemType=new string[missionItemMap.Count];
		Dictionary<string, Sprite>.KeyCollection items=missionItemMap.Keys;
		items.CopyTo(missionItemType,0);

		missionMoveMap=new Dictionary<string, Sprite>();
		missionMoveMap[TAG_MISSION_RIGHT]=mMissionRight;
		missionMoveMap[TAG_MISSION_LEFT]=mMissionLeft;
		missionMoveMap[TAG_MISSION_FORWARD]=mMissionForward;
		missionMoveMap[TAG_MISSION_BACKWARD]=mMissionBackward;

		missionMoveType=new string[missionMoveMap.Count];
		Dictionary<string, Sprite>.KeyCollection moves=missionMoveMap.Keys;
		moves.CopyTo(missionMoveType,0);

		missionPuzzleMap=new Dictionary<string, Sprite[]>();
		missionPuzzleMap[TAG_ITEM_PUZZLE_1]=mMissionPuzzle1;
		missionPuzzleMap[TAG_ITEM_PUZZLE_2]=mMissionPuzzle2;
		missionPuzzleMap[TAG_ITEM_PUZZLE_3]=mMissionPuzzle3;

		missionPuzzleType=new string[missionPuzzleMap.Count];
		Dictionary<string, Sprite[]>.KeyCollection puzzles=missionPuzzleMap.Keys;
		puzzles.CopyTo(missionPuzzleType,0);

		missions=new List<IMission>();
		missionSet=new List<string>();
		missionSet.Add(TAG_ITEM_TIME);
		missionSet.Add(TAG_ITEM_TIME_BIG);
		missionSet.Add(TAG_ITEM_DIAMOND);
		missionSet.Add(TAG_ITEM_BATTERY);
		missionSet.Add(TAG_MISSION_RIGHT);
		missionSet.Add(TAG_MISSION_LEFT);
		missionSet.Add(TAG_MISSION_FORWARD);
		missionSet.Add(TAG_MISSION_BACKWARD);
	}
	void OnDestroy()
	{
		gFloorListener-=handleFloorEvent;
		gMissionListener-=handleMissionEvent;	
	}

	// Use this for initialization
	void Start () {
print("fm start");
		if(manager.isBattle)
			mHoleTimerSE.mute=isMute();

		for(int i=0;i<mFloor.Length;i++){
			manager.preSetting(mFloor[i]);
		} 			
	}

	void FloorGenerate () {
		// 清除所有任務設定
		if(!manager.isBattle){

			for(int i=0;i<mMissionImage.Length;i++)
				mMissionImage[i].enabled=false;
			for(int i=0;i<mMissionText.Length;i++)
				mMissionText[i].text="";
			mMissionPuzzleImage.enabled=false;

			missions.Clear();
		}
		
		// 取得下一層地板，不會與上一層相同
		mCurrFloorIndex=manager.getNextFloorIdx(mPrevFloorIndex,mFloor.Length);
		mFloorInstance [mUnuseFloorIndex] = Instantiate(mFloor [mCurrFloorIndex]) as GameObject;

		// 調整新地板的Y軸座標
		mFloorInstance [mUnuseFloorIndex].transform.position 
			= new Vector3 (mFloorInstance [mUnuseFloorIndex].transform.position.x,
						mFloor [mCurrFloorIndex].transform.position.y - mCountOfFloor * mHeight,
						mFloorInstance [mUnuseFloorIndex].transform.position.z);


		floorControl = mFloorInstance [mUnuseFloorIndex].GetComponent<FloorControl>();
		if(manager.isBattle){
			floorControl.setFixedItems(mItemKey,1);	
		}
		else
		{
			int num=mCountOfFloor/2;
			bool puzzle=false;
			if(num>5){
				puzzle=(Random.Range(0,3)==0?true:false);
				num=3;
			}
			else if(num>3){
				num=3;
			}

			if(puzzle){
				int select=Random.Range(0,missionPuzzleType.Length);
				string type=missionPuzzleType[select];
				missions.Add(new MissionPuzzle(type,mMissionPuzzleImage,missionPuzzleMap[type]));
				mMissionPuzzleImage.enabled=true;
				
				floorControl.setFixedItems(itemMap[type],missionPuzzleMap[type].Length-1);
			}
			else{
				HashSet<string> tmp=new HashSet<string>();
				for(int i=0;i<num;i++){
					int select=Random.Range(0,missionSet.Count);
					while(tmp.Contains(missionSet[select])){
						select=Random.Range(0,missionSet.Count);
					}
					tmp.Add(missionSet[select]);

					string type=missionSet[select];
					if(missionItemMap.ContainsKey(type)){
						int need=Random.Range(1,3);
						missions.Add(new MissionItem(type,need,mMissionText[i]));
						mMissionImage[i].enabled=true;
						mMissionImage[i].sprite=missionItemMap[type];
						
						floorControl.setFixedItems(itemMap[type],need);
					}
					else if(missionMoveMap.ContainsKey(type)){
						int need=Random.Range(2,5);
						missions.Add(new MissionItem(type,need,mMissionText[i]));
						mMissionImage[i].enabled=true;
						mMissionImage[i].sprite=missionMoveMap[type];
					}
				}
			}

			floorControl.setItems(new GameObject[]{mItmeTime,mItemTimeBig,mItemDiamond,mItemBattery});	
		}

		// 更新參數
		mCountOfFloor++;
		mUnuseFloorIndex = (mUnuseFloorIndex == 0) ? 1 : 0;
		mPrevFloorIndex = mCurrFloorIndex;
	}

	void FloorClear () {            
		// 動態刪除上一層地板
		if (mFloorInstance [mUnuseFloorIndex] != null)
			Destroy (mFloorInstance [mUnuseFloorIndex]);
	}

	void BonusFloorGenerate () {
		// 清除所有任務設定
		if(!manager.isBattle){

			for(int i=0;i<mMissionImage.Length;i++)
				mMissionImage[i].enabled=false;
			for(int i=0;i<mMissionText.Length;i++)
				mMissionText[i].text="";
			mMissionPuzzleImage.enabled=false;

			missions.Clear();
		}

		// 動態生成獎勵地板
		mFloorInstance [mUnuseFloorIndex] = Instantiate (mBonusFloor) as GameObject;

		// 調整獎勵地板的Y軸座標
		mFloorInstance [mUnuseFloorIndex].transform.position 
			= new Vector3 (mFloorInstance [mUnuseFloorIndex].transform.position.x,
						mBonusFloor.transform.position.y - mCountOfFloor * mHeight,
						mFloorInstance [mUnuseFloorIndex].transform.position.z);

		floorControl= mFloorInstance [mUnuseFloorIndex].GetComponent<FloorControl>();
		floorControl.setBonusItems(new GameObject[]{mItemDiamond,mItemDiamondBig});

		// 更新參數
		mCountOfFloor++;
		mUnuseFloorIndex = (mUnuseFloorIndex == 0) ? 1 : 0;
		mPrevFloorIndex = mCurrFloorIndex;
	}	 

/* handle events */
	private void handleFloorEvent(FloorEvent e){
		switch(e){
			case FloorEvent.GENERATE:
				FloorGenerate();
			break;
			case FloorEvent.CLEAR:
				FloorClear();
			break;
			case FloorEvent.BONUS:
				BonusFloorGenerate();
			break;
			case FloorEvent.CLOSE:
				floorControl.setFixedItems(mItemKey,1);
			break;
		}
	}

	private void handleMissionEvent(string mission){
		for(int i=0;i<missions.Count;i++){
			if(!missions[i].isFinish())
				missions[i].check(mission);
		}

		for(int i=0;i<missions.Count;i++){
			if(!missions[i].isFinish())
				return;
		}


		if(!manager.isBattle)
			floorControl.setFloorHole(mHoleParticle);
		else if(mission==TAG_ITEM_Key){
			mHoleTimerSE.PlayOneShot(mHoleTimerSE.clip);
			floorControl.setFloorTimerHole(mHoleParticle);
		}

	}

/* mission types */
    private class MissionMove : GameBehaviour.IMission
    {
		private string mission;
		private int times;
		private Text text;

		public MissionMove(string mission,int times,Text text){
			this.mission=mission;
			this.times=times;
			this.text=text;
			text.text=""+times;
		}

        bool IMission.isFinish()
        {
			if(times<=0)
				return true;
			return false;
        }

        void IMission.check(string mission)
        {
			if(mission==this.mission){
				this.times--;	
				text.text=""+times;
			}
        }

    }

    private class MissionItem : GameBehaviour.IMission
    {		
		private string mission;
		private int times;
		private Text text;

		public MissionItem(string mission,int times,Text text){
			this.mission=mission;
			this.times=times;
			this.text=text;
			text.text=""+times;
		}
        bool IMission.isFinish()
        {
 			if(times<=0)
				return true;
			return false;
        }

        void IMission.check(string mission)
        {
			if(mission==this.mission){
				this.times--;
				text.text=""+times;
			}
        }
    }

    private class MissionPuzzle : GameBehaviour.IMission
    {
		private string mission;
		private int idx;
		private Image image;
		private Sprite[] sprites;

		public MissionPuzzle(string mission,Image image, Sprite[] sprites){

			this.mission=mission;
			this.image=image;
			this.sprites=sprites;

			this.idx=0;
			this.image.sprite=sprites[idx];
		}
        
		void IMission.check(string mission)
        {
        	if(mission==this.mission){
				idx++;
				image.sprite=sprites[idx];
			}
		}

        bool IMission.isFinish()
        {
			if(idx==sprites.Length-1)
				return true;
			return false;
        }
    }
}
