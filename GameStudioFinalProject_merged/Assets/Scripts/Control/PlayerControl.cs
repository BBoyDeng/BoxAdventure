using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerControl : GameBehaviour {
	private GameManager manager;
	private IGameTracker tracker;

	public ParticleSystem mWalkParticle;

	public AudioClip mPlayerWalkSE;
	public AudioClip mPlayerEatSE;
	public AudioClip mPlayerDropSE;
	public AudioSource mAudioSource;

	Rigidbody mRigidbody;
	BoxCollider mBoxCollider;

	public Material[] mPlayerMaterials;
	int mPlayerMaterialIndex;

	public float mSideLength = 1f; 			// 方塊邊長
	public float mRotationPeriod = 0.3f;	// 旋轉週期
	
	public float mFriction = 1500f;			// 玩家方塊移動時地板的摩擦力

	int mPlayerFloorCubeIndex;	// 玩家方塊對應到的地板方塊index

	float mRadius;				// 重心旋轉軌道半徑長
	float mRotationTime;		// 當前旋轉進度
	Vector3 mStartPosition;		// 滾動起始位置
	Quaternion mFromRotation;		// 起始旋轉角度
	Quaternion mToRotation;			// 結束旋轉角度
	Transform mSceneFireInstance;	// 火-動態生成物件

	int mCountOfRoll = 0;		// 玩家方塊滾動次數

	Vector2 mPrevScreenPos;
	float mPrevScreenTime;
	float mMoveSpeed;

	// Flag
	bool mIsRotate = false;		// 旋轉flag
	bool mIsDrop = false;		// 墜落flag
	bool mIsSlide = false;		// 滑動flag
	bool mIsFire = false;		// 著火flag
	bool mIsBeAttacked = false;	// 遭受攻擊flag
	float mRotateDirectionX = 0f;		// world space X軸移動方向flag
	float mRotateDirectionZ = 0f;		// world space Z軸移動方向flag

	// 遊戲參數
	int PARA_SCORE_HOLE = 20;
	float PARA_TIMER_ITEM_TIME = 10f;
	float PARA_TIMER_ITEM_TIME_BIG = 20f;
	int PARA_SCORE_ITEM_DIAMOND = 4;
	int PARA_SCORE_ITEM_DIAMOND_BIG=8;
	int PARA_FEVER_ITEM_BATTERY = 20;
	int PARA_ROLL_SCENE_FIRE = 10;
	float PARA_TIMER_SCENE_MONSTER = 4f;
	float PARA_TIMER_SCENE_BOMB = 4f;
	float PARA_TIMER_SCENE_FIRE = 0.3f;

	private HashSet<string> mBanCheckers;

	
	private delegate void ItemHandler();
	private Dictionary<string,ItemHandler> mItemHandlers;

	private delegate void EffectHandler(GameObject other);
	private Dictionary<string,EffectHandler> mEffectHandler;

	private delegate IDirectionHandler AttackChecker(GameObject other);
	private Dictionary<string,AttackChecker> mAttackCheckers;

	void Awake () {
print("pc awake");
		manager = GameObject.Find("GameManager").GetComponent<GameManager>();

		// 設定玩家方塊材質
		mPlayerMaterialIndex = manager.getLocalPlayerMaterialIdx(mPlayerMaterials.Length);
		GetComponent<MeshRenderer> ().material = mPlayerMaterials [mPlayerMaterialIndex];

		PhotonView ph = gameObject.GetComponent<PhotonView>();
        if (ph != null)
        {
            if (!ph.isMine && PhotonNetwork.connected)
            {
				if(PhotonNetwork.isMasterClient)
					GetComponent<MeshRenderer> ().material = mPlayerMaterials [manager.getPlayerMaterialIdx(1)];
				else
					GetComponent<MeshRenderer> ().material = mPlayerMaterials [manager.getPlayerMaterialIdx(0)];
					
				DestroyImmediate(gameObject.GetComponent<PlayerControl>(),true);

				return;
            }
        }


		mRigidbody = gameObject.GetComponent<Rigidbody> ();
		mBoxCollider = gameObject.GetComponent<BoxCollider> ();

		// 設定重心旋轉軌道半徑長
		mRadius = mSideLength * Mathf.Sqrt (2f) / 2;

		gCubesListener+=handleCubes;

		mBanCheckers=new HashSet<string>();
		mBanCheckers.Add(TAG_BOUNDARY);
		mBanCheckers.Add(TAG_OBSTACLE);

		mItemHandlers=new Dictionary<string, ItemHandler>();
		mItemHandlers[TAG_ITEM_TIME]=itemTimeHandler;
		mItemHandlers[TAG_ITEM_TIME_BIG]=itemTimeBigHandler;
		mItemHandlers[TAG_ITEM_DIAMOND]=itemDiamondHandler;
		mItemHandlers[TAG_ITEM_DIAMOND_BIG]=itemDiamondBigHandler;
		mItemHandlers[TAG_ITEM_BATTERY]=itemBatteryHandler;
		mItemHandlers[TAG_ITEM_PUZZLE_1]=itemPuzzleHandler;
		mItemHandlers[TAG_ITEM_PUZZLE_2]=itemPuzzleHandler;
		mItemHandlers[TAG_ITEM_PUZZLE_3]=itemPuzzleHandler;
		mItemHandlers[TAG_ITEM_Key]=itemKeyHandler;

		mEffectHandler=new Dictionary<string, EffectHandler>();
		mEffectHandler[TAG_SCENE_ICE]=effectIceHandler;
		mEffectHandler[TAG_SCENE_FIRE]=effectFireHandler;
		mEffectHandler[TAG_SCENE_MONSTER]=effectMonsterHandler;
		mEffectHandler[TAG_SCENE_BOMB]=effectBombHandler;
		mEffectHandler[TAG_SCENE_CHESS]=effectChessHandler;

		mAttackCheckers=new Dictionary<string, AttackChecker>();
		mAttackCheckers[TAG_SCENE_BOMB]=attackBombChecker;
		mAttackCheckers[TAG_SCENE_MONSTER]=attackMonsterChecker;
		mAttackCheckers[TAG_SCENE_CHESS]=attackChessChecker;
	}

	void OnDestroy()
	{
		gCubesListener-=handleCubes;		
	}

	// Use this for initialization
	void Start () {
print ("pc start");

		mAudioSource.mute = isMute();
		tracker=manager.getGameTracker();

		// 設定玩家方塊初始位置
		GameObject floorCube;
		while (true) {
			int floorCubeIndex = Random.Range (1, 145);
			floorCube = cubesHandler.getFloorCube(floorCubeIndex);
			if (floorCube.CompareTag (TAG_UNTAGGED)) {
				mPlayerFloorCubeIndex = floorCubeIndex;
				break;
			}
		}
		transform.position = floorCube.transform.position + Vector3.up;
	}
	
	// Update is called once per frame
	void Update () {
		#if UNITY_EDITOR	
		// PC平台，方便測試
		if (EditorApplication.isPlaying) {
			OnPC ();
		}
		#else
			// Android mobile平台
			OnAndroidMobile	();
		#endif
	}

	void OnPC () {
		float horizontalInputValue = Input.GetAxisRaw ("Horizontal");
		float verticalInputValue = Input.GetAxisRaw ("Vertical");

		if (!mIsDrop && !mIsRotate && !mIsSlide && !mIsBeAttacked) {
			// 根據取得的input值判斷玩家方塊前進的方向，且遇到障礙物與邊界時則無法前進
			if (horizontalInputValue >= 0.25f || horizontalInputValue <= -0.25f || verticalInputValue >= 0.25f || verticalInputValue <= -0.25f) {
				// 橫向移動
				if (Mathf.Abs (horizontalInputValue) > Mathf.Abs (verticalInputValue)) {
					// 往右
					if (horizontalInputValue > 0) {
						GameObject nextFloorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex + 1);
						if (!mBanCheckers.Contains(nextFloorCube.tag)) {
							mRotateDirectionX = 1f;
							
							raiseMissionEvent(TAG_MISSION_RIGHT);
						}
					} else {	// 往左
						GameObject nextFloorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex - 1);
						if (!mBanCheckers.Contains(nextFloorCube.tag)) {
							mRotateDirectionX = -1f;

							raiseMissionEvent(TAG_MISSION_LEFT);
						}
					}
				} else {		// 縱向移動
					// 往前
					if (verticalInputValue > 0) {
						GameObject nextFloorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex + 12);
						if (!mBanCheckers.Contains(nextFloorCube.tag)) {
							mRotateDirectionZ = 1f;

							raiseMissionEvent(TAG_MISSION_FORWARD);
						}
					} else {	// 往後
						GameObject nextFloorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex - 12);
						if (!mBanCheckers.Contains(nextFloorCube.tag)) {
							mRotateDirectionZ = -1f;

							raiseMissionEvent(TAG_MISSION_BACKWARD);
						}
					}
				}

				mStartPosition = transform.position;
				mFromRotation = transform.rotation;
				transform.Rotate (mRotateDirectionZ * 90f, 0f, -mRotateDirectionX * 90f, Space.World);
				mToRotation = transform.rotation;
				transform.rotation = mFromRotation;

				mRotationTime = 0f;
				mIsRotate = true;
			}
		}
	}

	void OnAndroidMobile () {
		if (Input.touchCount <= 0)
			return;

		// 一根手指觸碰螢幕
		if (Input.touchCount == 1) {
			// 鎖定操作條件
			if (!mIsDrop && !mIsRotate && !mIsSlide && !mIsBeAttacked) {

				// 開始觸碰
				if (Input.touches [0].phase == TouchPhase.Began) {
					// 紀錄觸碰位置
					mPrevScreenPos = Input.touches [0].position;
					mPrevScreenTime = Time.time;
				}

				// 手指離開螢幕
				if (Input.touches [0].phase == TouchPhase.Ended) {
					Vector2 newScreenPos = Input.touches [0].position;
					float newScreenTime = Time.time;

					mMoveSpeed = Mathf.Abs (Vector2.Distance (mPrevScreenPos, newScreenPos) / (mPrevScreenTime - newScreenTime));
					mMoveSpeed = 0f;
					int[] coef={1,12}; // [0] 橫向 [1] 縱向
					Vector2[] val=new Vector2[2];
					val[0]=new Vector2(newScreenPos.x,mPrevScreenPos.x);
					val[1]=new Vector2(newScreenPos.y,mPrevScreenPos.y);
					int test=(Mathf.Abs(newScreenPos.x - mPrevScreenPos.x) >= Mathf.Abs(newScreenPos.y - mPrevScreenPos.y) ? 0:1);
					int shift=(int)((val[test].x-(val[test].x+val[test].y)/2)/(Mathf.Abs(val[test].x-val[test].y)/2));
					GameObject nextFloorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex + shift*coef[test]);
					if (!mBanCheckers.Contains(nextFloorCube.tag)) {
						if(test==0){
							mRotateDirectionX = (float)shift;
							if(shift>0)
								raiseMissionEvent(TAG_MISSION_RIGHT);
							else
								raiseMissionEvent(TAG_MISSION_LEFT);
						}
						else{
							mRotateDirectionZ = (float)shift;
							if(shift>0)
								raiseMissionEvent(TAG_MISSION_FORWARD);
							else
								raiseMissionEvent(TAG_MISSION_BACKWARD);
						} 
							
					} 

					// 更新滾動參數
					mStartPosition = transform.position;
					mFromRotation = transform.rotation;
					transform.Rotate (mRotateDirectionZ * 90f, 0f, -mRotateDirectionX * 90f, Space.World);
					mToRotation = transform.rotation;
					transform.rotation = mFromRotation;

					mRotationTime = 0f;
					mIsRotate = true;
				}
			}
		}
	}

	void FixedUpdate () {

		if (mIsRotate) {
			// 利用線性插值取得滾動動作完成比例
			mRotationTime += Time.fixedDeltaTime;
			float ratio = Mathf.Lerp (0f, 1f, mRotationTime / mRotationPeriod);

			// 移動方塊
			float thetaRad = Mathf.Lerp (0f, Mathf.PI / 2f, ratio);
			Vector3 move = new Vector3();
			move.x = mRotateDirectionX * mRadius * (Mathf.Cos (45f * Mathf.Deg2Rad) - Mathf.Cos (45f * Mathf.Deg2Rad + thetaRad));
			move.y = mRadius * (Mathf.Sin (45f * Mathf.Deg2Rad + thetaRad) - Mathf.Sin (45f * Mathf.Deg2Rad));
			move.z = mRotateDirectionZ * mRadius * (Mathf.Cos (45f * Mathf.Deg2Rad) - Mathf.Cos (45f * Mathf.Deg2Rad + thetaRad));
			transform.position = mStartPosition + move;
		
			// 旋轉方塊
			transform.rotation = Quaternion.Lerp (mFromRotation, mToRotation, ratio);

			// 當翻滾結束後
			if (ratio == 1) {
				mAudioSource.PlayOneShot (mPlayerWalkSE);

				// 移動軌跡
				ParticleSystem walkInstance = Instantiate (mWalkParticle) as ParticleSystem;
				walkInstance.gameObject.GetComponent<ParticleSystemRenderer>().material = mPlayerMaterials [mPlayerMaterialIndex];
				walkInstance.transform.position = transform.position - (Vector3.up * 0.5f); 
				var main = walkInstance.main;
				main.simulationSpeed = 2f;
				Destroy (walkInstance.gameObject, 1f);
			
				// floor raise
				if (!tracker.isInCrazyTime() && (mRotateDirectionX != 0f || mRotateDirectionZ != 0f)) {
					if(!manager.isSimple)
						cubesHandler.generateTail(mPlayerFloorCubeIndex);
				}


				// 更新玩家方塊對應到的地板方塊index
				mPlayerFloorCubeIndex += (int)mRotateDirectionX + (int)mRotateDirectionZ * 12;
				List<GameObject> check=new List<GameObject>();
				check.Add(cubesHandler.getFloorCube(mPlayerFloorCubeIndex-1));
				check.Add(cubesHandler.getFloorCube(mPlayerFloorCubeIndex+1));
				check.Add(cubesHandler.getFloorCube(mPlayerFloorCubeIndex-12));
				check.Add(cubesHandler.getFloorCube(mPlayerFloorCubeIndex+12));

				bool isAvailable=false;
				for(int i=0;i<check.Count;i++){
					if(!mBanCheckers.Contains(check[i].tag)){
						isAvailable=true;
						break;
					}
				}

				if(!isAvailable){
					manager.FatalBlow();
				}


				// 判斷滾動方向前方是否可繼續前進
				GameObject nextFloorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex + (int)mRotateDirectionX + (int)mRotateDirectionZ * 12);
				if (!mBanCheckers.Contains(nextFloorCube.tag)) {
					// 玩家方塊移動速度減少，若歸零則停止
					if (mMoveSpeed - mFriction > 0) {
						mMoveSpeed -= mFriction;

						// 更新滾動參數
						mStartPosition = transform.position;
						mFromRotation = transform.rotation;
						transform.Rotate (mRotateDirectionZ * 90f, 0f, -mRotateDirectionX * 90f, Space.World);
						mToRotation = transform.rotation;
						transform.rotation = mFromRotation;

						// 歸零當前旋轉進度
						mRotationTime = 0f;
					} else {
						GameObject floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex);
						if (!floorCube.CompareTag (TAG_SCENE_ICE)) {
							// 重新初始化滾動參數
							mRotateDirectionX = 0f;
							mRotateDirectionZ = 0f;
						}
						mIsRotate = false;
					}
				} else {
					// 重新初始化滾動參數
					mRotateDirectionX = 0f;
					mRotateDirectionZ = 0f;
					mIsRotate = false;
				}

				if (mIsFire) 
					mCountOfRoll++;
			}
		}

		// 場地效果 - 火
		if (mIsFire) {
			mSceneFireInstance.position = transform.position;

			if (mCountOfRoll == PARA_ROLL_SCENE_FIRE) {
				Destroy (mSceneFireInstance.gameObject);
				
				if(manager.isBattle){

				}
				else{
					raiseControlEvent(ControlEvent.AdjustTimerSpeed,1);
				}

				mCountOfRoll = -1;
				mIsFire = false;
			}
		}
			
	}

	void OnTriggerEnter (Collider other) {

		// 前往下一層
		if (other.gameObject.CompareTag (TAG_HOLE) && !mIsDrop) {
			mAudioSource.PlayOneShot(mPlayerDropSE);

			TouchHole ();
			
			// 重新初始化滾動參數
			mRotateDirectionX = 0f;
			mRotateDirectionZ = 0f;
			mIsRotate = false;

			// 產生下一層的地板
			raiseFloorEvent(FloorEvent.GENERATE);

			// 得分
			if(!manager.isBattle)
				raiseControlEvent(ControlEvent.Score,PARA_SCORE_HOLE);
			else
				raiseControlEvent(ControlEvent.Score,1);
		}

		// 觸發場地效果
		// 冰 - 往前滑一格
		// 火 - 著火，加速時間倒數
		// 砲彈 - 遭受攻擊，減少時間
		// 怪物 - 遭受攻擊，減少時間
		else if(mEffectHandler.ContainsKey(other.gameObject.tag) && !mIsDrop){
			mEffectHandler[other.gameObject.tag](other.gameObject);
		}

		// 觸發道具效果
		// 時鐘 - 增加時間
		// 鑽石 - 加分
		// 電池 - FEVER充電
		if(mItemHandlers.ContainsKey(other.gameObject.tag)){
			mAudioSource.PlayOneShot (mPlayerEatSE);

			mItemHandlers[other.gameObject.tag]();
			raiseMissionEvent(other.gameObject.tag);
			
			Destroy (other.gameObject);
		}
	}

	void OnTriggerExit (Collider other) {

		if (other.gameObject.CompareTag (TAG_HOLE)) {
			// 刪除上一層的地板
			raiseFloorEvent(FloorEvent.CLEAR);

			// 離開地洞
			ExitHole ();
		}
	}

	void OnCollisionEnter (Collision other) {

		// 當墜落到地面時 
		if (mIsDrop && other.gameObject.layer == 8) {

			// 校準墜落後的position、rotation
			GameObject floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex);
			Vector3 position = floorCube.transform.position+Vector3.up;
			transform.SetPositionAndRotation (position, floorCube.transform.rotation);

			mRigidbody.velocity = Vector3.zero;
			mRigidbody.angularVelocity = Vector3.zero;

			mRigidbody.useGravity = false;
			mBoxCollider.isTrigger = true;
			mIsDrop = false;
		} 
	}

	// 滑動動畫
	IEnumerator Slide () {
		for (int i = 0; i <= 10; i++) {
			transform.Translate (mRotateDirectionX * 0.1f, 0f, mRotateDirectionZ * 0.1f, Space.World);
			yield return null;
		}

		mPlayerFloorCubeIndex += (int)mRotateDirectionX + (int)mRotateDirectionZ * 12;
		mIsSlide = false;

		
		// 校準位置
		GameObject floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex);
		transform.position = floorCube.transform.position+Vector3.up;

		// 重新初始化滾動參數
		mRotateDirectionX = 0f;
		mRotateDirectionZ = 0f;

		if (mIsDrop) {
			// 產生下一層的地板
			raiseFloorEvent(FloorEvent.GENERATE);

			// 得分
			if(!manager.isBattle)
				raiseControlEvent(ControlEvent.Score,PARA_SCORE_HOLE);
			else
				raiseControlEvent(ControlEvent.Score,1);
		}
	}

	// 遭受攻擊動畫
	IEnumerator BeAttacked (GameObject other) {
		float directionX = 0f;
		float directionZ = 0f;

		if(mAttackCheckers.ContainsKey(other.tag)){
			IDirectionHandler d= mAttackCheckers[other.tag](other);
			if(d.getDirection()==Direction.RIGHT){
				directionX = 1f;
			}
			else if(d.getDirection()==Direction.LEFT){
				directionX = -1f;
			}
			else if(d.getDirection()==Direction.FORWARD){
				directionZ = 1f;
			}
			else if(d.getDirection()==Direction.BACKWARD){
				directionZ = -1f;
			}						
		}

		GameObject floorCube;

		if (mIsRotate) {
			mIsRotate = false;
			mRotateDirectionX = 0f;
			mRotateDirectionZ = 0f;
			mRotationTime = 0f;

			floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex);
			transform.position = floorCube.transform.position+Vector3.up;
			transform.rotation = floorCube.transform.rotation;
		}

		GameObject nextFloorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex + (int)directionX + (int)directionZ * 12);
		if (!mBanCheckers.Contains(nextFloorCube.tag)) {
			yield return StartCoroutine (CollideUp (directionX, directionZ));
			yield return StartCoroutine (CollideDown (directionX, directionZ));

			mPlayerFloorCubeIndex += (int)directionX + (int)directionZ * 12;
		} else {
			yield return StartCoroutine (CollideUp (0f, 0f));
			yield return StartCoroutine (CollideDown (0f, 0f));
		}

	
		floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex);
		transform.position = floorCube.transform.position+Vector3.up;
		transform.rotation = floorCube.transform.rotation;

		mIsBeAttacked = false;
	}

	IEnumerator CollideUp (float x, float z) {
		for (int i = 0; i < 5; i++) {
			transform.Translate (x * 0.1f, 0.05f, z * 0.1f, Space.World);
			yield return null;
		}
	}

	IEnumerator CollideDown (float x, float z) {
		for (int i = 0; i < 5; i++) {
			transform.Translate (x * 0.1f, -0.05f, z * 0.1f, Space.World);
			yield return null;
		}
	}

	// 進入或離開瘋狂時間的墜落
	public void CrazyTimeDrop () {
		// 如果還在滾動中，強制中止
		if (mIsRotate) {
			mIsRotate = false;
			mRotateDirectionX = 0f;
			mRotateDirectionZ = 0f;
			mRotationTime = 0f;
		}

		GameObject floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex);
		transform.position = floorCube.transform.position+Vector3.up*12;
		transform.rotation = floorCube.transform.rotation;

		TouchHole ();
		EnterHole ();
		ExitHole ();
	}

	void TouchHole(){
		mIsDrop = true;
	}
	// 進入地洞
	public void EnterHole () {
		mRigidbody.useGravity = true;
	}

	// 離開地洞
	void ExitHole () {
		// 檢查下一層的地板，調整玩家方塊位置，避免遇上障礙物
		GameObject floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex);
		if (mBanCheckers.Contains(floorCube.tag))
			AvoidObstacle();

		mBoxCollider.isTrigger = false;
	}

	// 調整玩家方塊水平位置，避免遇上障礙物
	void AvoidObstacle () {
		while (true) {
			int index = Random.Range (1, 145);
			GameObject floorCube = cubesHandler.getFloorCube(index);

			if (!mBanCheckers.Contains(floorCube.tag)) {
				Vector3 position = new Vector3 (floorCube.transform.position.x, transform.position.y, floorCube.transform.position.z);
				transform.SetPositionAndRotation (position, floorCube.transform.rotation);

				mPlayerFloorCubeIndex = index;
				break;
			}
		}
	}

/* about item */
	private void itemTimeHandler(){
		if(manager.isBattle){

		}
		else{
			raiseControlEvent(ControlEvent.IncreaseTime,PARA_TIMER_ITEM_TIME);
		}
	}

	private void itemTimeBigHandler(){
		if(manager.isBattle){
			
		}
		else{
			raiseControlEvent(ControlEvent.IncreaseTime,PARA_TIMER_ITEM_TIME_BIG);
		}
	}

	private void itemDiamondHandler(){
		if(manager.isBattle){
			
		}
		else{
			raiseControlEvent(ControlEvent.Score,PARA_SCORE_ITEM_DIAMOND);
		}
	}

	private void itemDiamondBigHandler(){
		if(manager.isBattle){
			
		}
		else{
			raiseControlEvent(ControlEvent.Score,PARA_SCORE_ITEM_DIAMOND_BIG);
		}
	}

	private void itemBatteryHandler(){
		if(manager.isBattle){
			
		}
		else{
			raiseControlEvent(ControlEvent.ChargeFever,PARA_FEVER_ITEM_BATTERY);
		}
	}

	private void itemPuzzleHandler(){
		if(manager.isBattle){
			
		}
		else{
			
		}
	}

	private void itemKeyHandler(){
		if(manager.isBattle){
			
		}
		else{
			
		}
	}

/* about scene effect */
	private void effectIceHandler(GameObject other){
		if(!mIsSlide) {
			mIsRotate = false;

			GameObject floorCube = cubesHandler.getFloorCube(mPlayerFloorCubeIndex + (int)mRotateDirectionX + (int)mRotateDirectionZ * 12);

			if (!mBanCheckers.Contains(floorCube.tag)) {
				if (floorCube.CompareTag (TAG_HOLE)) {
					mIsDrop = true;
				}

				mIsSlide = true;
				StartCoroutine (Slide ());
			}
		}
	}

	private void effectFireHandler(GameObject other){
		if (!mIsFire) 
		{
			mIsFire = true;

			mSceneFireInstance = Instantiate (other.transform.GetChild (0).gameObject).transform;
			mSceneFireInstance.position = transform.position;
			mSceneFireInstance.localScale = new Vector3 (0.8f, 0.8f, 0.8f);

			if(manager.isBattle){

			}
			else{
				raiseControlEvent(ControlEvent.AdjustTimerSpeed,PARA_TIMER_SCENE_FIRE);
			}
			
		}
	}

	private void effectMonsterHandler(GameObject other){
		if (!mIsBeAttacked && !tracker.isTimerPause () && !tracker.isTimesUp ()) {
			mIsBeAttacked = true;
			
			if(manager.isBattle){

			}
			else{
				raiseControlEvent(ControlEvent.DecreaseTime,PARA_TIMER_SCENE_MONSTER);
			}

			StartCoroutine (BeAttacked (other));
		}
	}

	private void effectBombHandler(GameObject other){
		if (!mIsBeAttacked && !tracker.isTimerPause () && !tracker.isTimesUp ()) {
			mIsBeAttacked = true;
			
			if(manager.isBattle){

			}
			else{
				raiseControlEvent(ControlEvent.DecreaseTime,PARA_TIMER_SCENE_BOMB);
			}

			StartCoroutine (BeAttacked (other));
		}
	}

	private void effectChessHandler(GameObject other){
		if (!mIsBeAttacked && !tracker.isTimerPause () && !tracker.isTimesUp ()) {
			mIsBeAttacked = true;
			StartCoroutine (BeAttacked (other));
		}
	}

/* about attack */
	private IDirectionHandler attackBombChecker(GameObject other){
		return other.GetComponent<BombAI>();
	}

	private IDirectionHandler attackMonsterChecker(GameObject other){
		return other.GetComponent<MonsterAI>();
	}

	private IDirectionHandler attackChessChecker(GameObject other){
		return other.GetComponent<ChessAI>();
	}

/* about floor cubes */
	private ICubesHandler cubesHandler;
	private bool isStart=true;
	private void handleCubes(ICubesHandler ich){
		cubesHandler=ich;

		if(isStart){
			isStart=false;
			return;
		}
		EnterHole();
	}
}
