using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessAI : MonoBehaviour, GameBehaviour.IDirectionHandler {

	public bool[] mOkDirection = new bool[4];

	public int mChessFloorCubeIndex = 87;
	public float mDelay = 1.5f;

	GameBehaviour.IGameTracker tracker;

	GameObject[] mCubes;

	string tmpTag;

	int mDirection;
	GameBehaviour.Direction mStepDirection;

	float mDirX = 0;
	float mDirZ = 0;


	void Awake () {
		tracker=GameObject.Find("GameManager").GetComponent<GameManager>().getGameTracker();
		
	}

	// Use this for initialization
	void Start () {
		GameBehaviour.ICubesHandler ich=transform.parent.GetComponent<FloorControl>();

		mCubes = new GameObject[144];

		for(int i=0;i<144;i++){
			mCubes[i]=ich.getFloorCube(i+1);
		}

		while (true) {
			mDirection = Random.Range (0, 4);
			if (mOkDirection [mDirection])
				break;
		}

		if (mDirection == 0) {
			mDirX = 1f;
			mStepDirection = GameBehaviour.Direction.RIGHT;
		} else if (mDirection == 1) {
			mDirX = -1f;
			mStepDirection = GameBehaviour.Direction.LEFT;
		}  else if (mDirection == 2) {
			mDirZ = 1f;
			mStepDirection = GameBehaviour.Direction.FORWARD;
		}  else if (mDirection == 3) { 
			mDirZ = -1f;
			mStepDirection = GameBehaviour.Direction.BACKWARD;
		} 

		GameObject nextFloorCube = mCubes[mChessFloorCubeIndex + (int)mDirX + (int)mDirZ * 12  -1];
		if (!nextFloorCube.CompareTag (GameBehaviour.TAG_HOLE)) {

			StartCoroutine (MoveLoop ());
		}
	}

	IEnumerator MoveLoop () {
		if (!tracker.isTimerPause () && !tracker.isTimesUp ()) {
			yield return StartCoroutine (Go ());
			yield return StartCoroutine (Delay ());
			yield return StartCoroutine (Back ());
		}
			
		yield return StartCoroutine (Delay ());

		GameObject nextFloorCube = mCubes[mChessFloorCubeIndex + (int)mDirX + (int)mDirZ * 12  -1];
		if (!nextFloorCube.CompareTag (GameBehaviour.TAG_HOLE)) {
			StartCoroutine (MoveLoop ());
		}
	}

	IEnumerator Go () {

		mCubes[mChessFloorCubeIndex-1].tag = GameBehaviour.TAG_UNTAGGED;
		mChessFloorCubeIndex += (int)mDirX + (int)mDirZ * 12;
		
		mCubes[mChessFloorCubeIndex-1].tag = GameBehaviour.TAG_OBSTACLE;

		for (int i = 0; i < 10; i++) {
			transform.Translate (mDirX * 0.1f, 0f, mDirZ * 0.1f);
			yield return null;
		}
			
		mDirX *= -1;
		mDirZ *= -1;

		if (mDirection == 0)
			mStepDirection = GameBehaviour.Direction.LEFT;
		else if (mDirection == 1) 
			mStepDirection = GameBehaviour.Direction.RIGHT;
		else if (mDirection == 2) 
			mStepDirection = GameBehaviour.Direction.BACKWARD;
		else if (mDirection == 3) 
			mStepDirection = GameBehaviour.Direction.FORWARD;
		
	}

	IEnumerator Back () {
		mCubes[mChessFloorCubeIndex-1].tag = GameBehaviour.TAG_UNTAGGED;
		mChessFloorCubeIndex += (int)mDirX + (int)mDirZ * 12;
		mCubes[mChessFloorCubeIndex-1].tag = GameBehaviour.TAG_OBSTACLE;

		for (int i = 0; i < 10; i++) {
			transform.Translate (mDirX * 0.1f, 0f, mDirZ * 0.1f);
			yield return null;
		}

		mDirX *= -1;
		mDirZ *= -1;

		if (mDirection == 0)
			mStepDirection = GameBehaviour.Direction.RIGHT;
		else if (mDirection == 1)
			mStepDirection = GameBehaviour.Direction.LEFT;
		else if (mDirection == 2)
			mStepDirection = GameBehaviour.Direction.FORWARD;
		else if (mDirection == 3)
			mStepDirection = GameBehaviour.Direction.BACKWARD;
		
	}

	IEnumerator Delay () {
		yield return new WaitForSeconds (mDelay);
	}

	public GameBehaviour.Direction getDirection () {
		return mStepDirection;
	}

}
