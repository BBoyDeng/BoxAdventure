using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : GameBehaviour{

	public GameObject PlayerPrefab;
	private IPlayer player;
	public bool isBattle;
	public bool isSimple;

	public ParticleSystem mFatalblowParticle;
	public ParticleSystem mDeadTailParticle;

	public AudioSource mBGMAudioSource;
	public AudioSource mSEAudioSource;

	public AudioSource mFatalSE;
	public AudioClip mPlayingBGM;
	public AudioClip mResultBGM;

	
	private IGameTracker tracker;
	private IUtilityCore utilityCore;


	void Awake () {
print("gm awake");

		GameObject canvas=GameObject.Find("Canvas");

		if(isBattle){
			tracker=canvas.GetComponent<GameControlNetwork>();
			utilityCore=new BattleCore();
		}
			
		else{
			tracker=canvas.GetComponent<GameControl>();
			utilityCore=new NormalCore();
		}
			
		gCubesListener+=handleCubes;
	}

	void OnDisable()
	{
		
		gCubesListener-=handleCubes;
	}

	// Use this for initialization
	void Start () {
print("gm start");
		mBGMAudioSource.mute = isMute();
		mSEAudioSource.mute = isMute();
		mFatalSE.mute = isMute();

		GameObject obj=utilityCore.initPlayer(PlayerPrefab);
		Camera.main.GetComponent<CameraControl>().mPlayer = obj.transform; 

		player=new LocalPlayer(obj);
		print(utilityCore.getPlayerName());

		// 開始遊戲流程
		StartCoroutine (GameProcess ());
	}

	void FixedUpdate () {

		// 當遊戲結束時，切換到結算畫面
		if (tracker.isTimesUp()&&!tracker.isGameResult()) {
			if(mBGMAudioSource.isPlaying)
				mBGMAudioSource.Stop();

			mBGMAudioSource.clip = mResultBGM;
			mBGMAudioSource.Play ();

			player.setEnable(false);
			raiseControlEvent(ControlEvent.OnOver,0);
		}

		if(tracker.testEnterCrazy()){

			raiseFloorEvent(FloorEvent.BONUS);
			raiseFloorEvent(FloorEvent.CLEAR);
			player.fall ();
		}
		else if(tracker.testExitCrazy()){

			raiseFloorEvent(FloorEvent.GENERATE);
			raiseFloorEvent(FloorEvent.CLEAR);
			player.fall ();
		}
	}

	// 遊戲流程，準備→開始
	IEnumerator GameProcess () {
		yield return StartCoroutine (GameReady ());
		yield return StartCoroutine (GameStart ());
	}

	IEnumerator GameReady () {
		player.setActive (false);
		raiseControlEvent(ControlEvent.onReady,0);

		yield return new WaitForSeconds (4f);
	}

	IEnumerator GameStart () {
		if(mBGMAudioSource.isPlaying)
			mBGMAudioSource.Stop();

		mBGMAudioSource.clip = mPlayingBGM;
		mBGMAudioSource.Play ();
		
		player.setActive (true);
		raiseControlEvent(ControlEvent.OnStart,0);
		
		raiseFloorEvent(FloorEvent.GENERATE);

		while (!tracker.isTimesUp()) {
			yield return null;
		}
	}

/* button click listener */
	// 遊戲暫停，暫停所有的控制，且跳出暫停畫面
	public void Pause () {
		mSEAudioSource.Play ();

		player.setEnable(false);
		raiseControlEvent(ControlEvent.OnPause,0);
	}

	// 遊戲繼續，釋放所有的控制，且關閉暫停畫面
	public void Play () {
		mSEAudioSource.Play ();

		player.setEnable(true);
		raiseControlEvent(ControlEvent.OnPlay,0);
	}

	public void FatalBlow () {
		if(isSimple)
			return;
			
		mFatalSE.PlayOneShot(mFatalSE.clip);

		ParticleSystem fatalblowInstance = Instantiate(mFatalblowParticle) as ParticleSystem;
		fatalblowInstance.transform.position = player.getPos();
		Destroy(fatalblowInstance.gameObject, 1f);

		if(!isBattle)
			raiseControlEvent(ControlEvent.DecreaseTime, 10f);

		cubesHandler.destroyAllTail(mDeadTailParticle);
	}

	// 回主選單
	public void onBtnReturn () {
		if(!isBattle)
			playAudioBeforeLoad(mSEAudioSource,mSEAudioSource.clip,"Menu_Main");
		else 
			PhotonNetwork.Disconnect();
			// there is a callback in NetworkListener
			// that will allow us to go back Menu_Main
	}

	// 重新開始
	public void onBtnReplay () {
		if(!isBattle)
			playAudioBeforeLoad(mSEAudioSource,mSEAudioSource.clip,isSimple?"Game_Simple":"Game_Single");

	}

/* public methods */
	public void preSetting(GameObject prefab){
		utilityCore.preSetting(prefab);
	}

	public IGameTracker getGameTracker(){
		return tracker;
	}

	public int getPlayerIdx(){
		return utilityCore.getPlayerIdx();
	}

	public int getNextFloorIdx(int prevIdx,int length){
		return utilityCore.getNextFloorIdx(prevIdx,length);
	}

	public int getLocalPlayerMaterialIdx(int length){
		return utilityCore.getLocalPlayerMaterialIdx(length);
	}

	public int getPlayerMaterialIdx(int otherPlayerIdx){
		return utilityCore.getPlayerMaterialIdx(otherPlayerIdx);
	}

/* about floor cubes */
	private ICubesHandler cubesHandler;
	private void handleCubes(ICubesHandler ich){
		cubesHandler=ich;
	}
	
// IPlayer
	interface IPlayer{
		void setActive(bool isActive);
		void setEnable(bool isEnable);
		void fall();
		Vector3 getPos();

	}

    class LocalPlayer : IPlayer
    {
		private GameObject player;
		private PlayerControl playerControl;

		public LocalPlayer(GameObject player){
			this.player=player;
			playerControl=player.GetComponent<PlayerControl>();
		}

        void IPlayer.fall()
        {
			playerControl.CrazyTimeDrop();
        }

        Vector3 IPlayer.getPos()
        {
            return player.transform.position;
        }

        void IPlayer.setActive(bool isActive)
        {
			player.SetActive(isActive);
        }

        void IPlayer.setEnable(bool isEnable)
        {
			playerControl.enabled=isEnable;
        }
    }

/* utility core */
    interface IUtilityCore
	{
		void preSetting(GameObject prefab);

		GameObject initPlayer(GameObject prefab);

		int getPlayerIdx();
		string getPlayerName();

		int getNextFloorIdx(int prevIdx,int length);

		int getLocalPlayerMaterialIdx(int length);

		int getPlayerMaterialIdx(int otherPlayerIdx);

	}

    class NormalCore : IUtilityCore
    {
		void IUtilityCore.preSetting(GameObject prefab){			

			//DestroyImmediate(prefab.GetComponent<FloorControl>(),true);
			//prefab.AddComponent<FloorListener>();
		}
		GameObject IUtilityCore.initPlayer(GameObject prefab)
        {
            return Instantiate(prefab);
        }

        int IUtilityCore.getPlayerIdx()
        {
            return 0;
        }

        string IUtilityCore.getPlayerName()
        {
			return "Player";
        }

        int IUtilityCore.getNextFloorIdx(int prevIdx,int length)
        {
			int idx;
            while (true) {
				idx = Random.Range (0, length);
				if (idx == prevIdx)
					continue;
				else
					return idx;
			}
        }

        int IUtilityCore.getLocalPlayerMaterialIdx(int length)
        {
            return Random.Range(0,length);
        }

        int IUtilityCore.getPlayerMaterialIdx(int otherPlayerIdx)
        {
            return 0;
        }

    }

	class BattleCore : IUtilityCore
	{
		int playerIdx=-1;
		
		int[] playersMaterial;

		int[] floorSeq;
		int floorIdx=0;


		public BattleCore(){
			floorSeq = PhotonNetwork.room.CustomProperties["f"] as int[];
			playersMaterial = PhotonNetwork.room.CustomProperties["p"] as int[];

			if(PhotonNetwork.isMasterClient)
				playerIdx=0;
			else
				playerIdx=1;
		}

        void IUtilityCore.preSetting(GameObject prefab){
/* 
			DestroyImmediate(prefab.GetComponent<PhotonTransformView>(),true);
			DestroyImmediate(prefab.GetComponent<PhotonView>(),true);
*/
/* 
			PhotonView pv = prefab.AddComponent<PhotonView>() as PhotonView;
			PhotonTransformView ptv = prefab.AddComponent<PhotonTransformView>() as PhotonTransformView;
			ptv.m_PositionModel.SynchronizeEnabled=true;
			ptv.m_RotationModel.SynchronizeEnabled=true;
			ptv.m_ScaleModel.SynchronizeEnabled=true;

			pv.ObservedComponents=new List<Component>();
			pv.ObservedComponents.Add(ptv);
			pv.synchronization=ViewSynchronization.UnreliableOnChange;
*/			
		}
		GameObject IUtilityCore.initPlayer(GameObject prefab)
        {
            return PhotonNetwork.Instantiate(prefab.name,Vector3.up*10, Quaternion.identity, 0);
        }
		int IUtilityCore.getPlayerIdx()
        {
            return playerIdx;
        }
		string IUtilityCore.getPlayerName()
        {
			return "Player "+playerIdx;
        }

		int IUtilityCore.getNextFloorIdx(int prevIdx,int length)
        {
			int ret=floorIdx;
			floorIdx+=1;
			floorIdx%=floorSeq.Length;
			return floorSeq[ret];
        }

		int IUtilityCore.getLocalPlayerMaterialIdx(int length)
        {
            return playersMaterial[playerIdx];
        }

		int IUtilityCore.getPlayerMaterialIdx(int otherPlayerIdx)
        {
            return playersMaterial[otherPlayerIdx];
        }

	}


}

