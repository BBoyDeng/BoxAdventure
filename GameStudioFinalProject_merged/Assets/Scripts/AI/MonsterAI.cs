using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour,GameBehaviour.IDirectionHandler {

	public int mMonsterFloorCubeIndex = 87;	// 怪物對應到的地板方塊index

	Transform mPlayer;
	GameBehaviour.IGameTracker tracker;
	GameObject[] mCubes;
	Vector3 mDirection;			// 由怪物到玩家的方向
	GameBehaviour.Direction mStepDirection;		// 怪物每一步前進的方向

	int mMonsterSpeed = 15;
	float mMonsterDelay = 1.2222f;


	private float lastTime;

	void Awake () {
		tracker=GameObject.Find("GameManager").GetComponent<GameManager>().getGameTracker();

		lastTime=Time.time;
	}

	// Use this for initialization
	void Start () {
		GameBehaviour.ICubesHandler ich=transform.parent.GetComponent<FloorControl>();

		mCubes = new GameObject[144];

		for(int i=0;i<144;i++){
			mCubes[i]=ich.getFloorCube(i+1);
		}

		// 怪物AI往玩家方向自動尋路
		StartCoroutine (MoveLoop ());
	}

	void FixedUpdate()
	{
		if(Time.time-lastTime<2)
			return;
		GameObject[] players=GameObject.FindGameObjectsWithTag("Player");
		mPlayer=players[Random.Range(0,players.Length)].transform;
		lastTime=Time.time;
	}

	IEnumerator MoveLoop () {
		if(mPlayer==null)
			mDirection=Vector3.zero;
		else
			mDirection = mPlayer.transform.position - transform.position;

		if (!tracker.isTimerPause () && !tracker.isTimesUp ()) {
			// 怪物移動方式分四步驟，蹲下、起立以及跳躍、墜落
			yield return StartCoroutine (CrouchDown ());
			yield return StartCoroutine (CrouchUp ());
			yield return StartCoroutine (JumpUp ());
			yield return StartCoroutine (JumpDown ());

			// 若怪物踩到地洞，則死亡
			GameObject floorCube = mCubes[mMonsterFloorCubeIndex];
			if (floorCube.CompareTag (GameBehaviour.TAG_HOLE)) {
				Destroy (gameObject);
			}
		}
		// 怪物每次移動後的時間間隔
		yield return StartCoroutine (Delay ());

		StartCoroutine (MoveLoop ());
	}

	// 蹲下動畫
	IEnumerator CrouchDown () {
		for (int i = 0; i < mMonsterSpeed; i++) {
			transform.position = new Vector3 (transform.position.x, transform.position.y - 0.25f / mMonsterSpeed, transform.position.z);
			transform.localScale = new Vector3 (transform.localScale.x + 1f / mMonsterSpeed, transform.localScale.y - 0.5f / mMonsterSpeed, transform.localScale.z + 1f / mMonsterSpeed);
			yield return null;
		}
	}

	// 起立動畫
	IEnumerator CrouchUp () {
		for (int i = 0; i < mMonsterSpeed; i++) {
			transform.position = new Vector3 (transform.position.x, transform.position.y + 0.25f / mMonsterSpeed, transform.position.z);
			transform.localScale = new Vector3 (transform.localScale.x - 1f / mMonsterSpeed, transform.localScale.y + 0.5f / mMonsterSpeed, transform.localScale.z - 1f / mMonsterSpeed);
			yield return null;
		}
	}

	// 跳躍動畫
	IEnumerator JumpUp () {
		if (Mathf.Abs ((int)mDirection.x) >= Mathf.Abs ((int)mDirection.z) && ((int)mDirection.x != 0 || (int)mDirection.z != 0)) {
			if ((int)mDirection.x >= 0) {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex + 1];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x + 0.5f / mMonsterSpeed, transform.position.y + 3f / mMonsterSpeed, transform.position.z);
						transform.localScale = new Vector3 (transform.localScale.x - 0.5f / mMonsterSpeed, transform.localScale.y + 0.2f / mMonsterSpeed, transform.localScale.z - 0.5f / mMonsterSpeed);
						yield return null;
					}
				}
			} else {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex - 1];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x - 0.5f / mMonsterSpeed, transform.position.y + 3f / mMonsterSpeed, transform.position.z);
						transform.localScale = new Vector3 (transform.localScale.x - 0.5f / mMonsterSpeed, transform.localScale.y + 0.2f / mMonsterSpeed, transform.localScale.z - 0.5f / mMonsterSpeed);
						yield return null;
					}
				}
			}
		} else if (Mathf.Abs ((int)mDirection.x) < Mathf.Abs ((int)mDirection.z)) {
			if ((int)mDirection.z >= 0) {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex + 12];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x, transform.position.y + 3f / mMonsterSpeed, transform.position.z + 0.5f / mMonsterSpeed);
						transform.localScale = new Vector3 (transform.localScale.x - 0.5f / mMonsterSpeed, transform.localScale.y + 0.2f / mMonsterSpeed, transform.localScale.z - 0.5f / mMonsterSpeed);
						yield return null;
					}
				}
			} else {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex - 12];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x, transform.position.y + 3f / mMonsterSpeed, transform.position.z - 0.5f / mMonsterSpeed);
						transform.localScale = new Vector3 (transform.localScale.x - 0.5f / mMonsterSpeed, transform.localScale.y + 0.2f / mMonsterSpeed, transform.localScale.z - 0.5f / mMonsterSpeed);
						yield return null;
					}
				}
			} 
		} else {
			for (int i = 0; i < mMonsterSpeed; i++) {
				transform.position = new Vector3 (transform.position.x, transform.position.y + 3f / mMonsterSpeed, transform.position.z);
				transform.localScale = new Vector3 (transform.localScale.x - 0.5f / mMonsterSpeed, transform.localScale.y + 0.2f / mMonsterSpeed, transform.localScale.z - 0.5f / mMonsterSpeed);
				yield return null;
			}
		}
	}

	// 墜落動畫
	IEnumerator JumpDown () {
		if (Mathf.Abs ((int)mDirection.x) >= Mathf.Abs ((int)mDirection.z) && ((int)mDirection.x != 0 || (int)mDirection.z != 0)) {
			if ((int)mDirection.x >= 0) {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex + 1];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x + 0.5f / mMonsterSpeed, transform.position.y - 3f / mMonsterSpeed, transform.position.z);
						transform.localScale = new Vector3 (transform.localScale.x + 0.5f / mMonsterSpeed, transform.localScale.y - 0.2f / mMonsterSpeed, transform.localScale.z + 0.5f / mMonsterSpeed);
						yield return null;
					}
					mMonsterFloorCubeIndex += 1;
					mStepDirection = GameBehaviour.Direction.RIGHT;
				}
			} else {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex - 1];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x - 0.5f / mMonsterSpeed, transform.position.y - 3f / mMonsterSpeed, transform.position.z);
						transform.localScale = new Vector3 (transform.localScale.x + 0.5f / mMonsterSpeed, transform.localScale.y - 0.2f / mMonsterSpeed, transform.localScale.z + 0.5f / mMonsterSpeed);
						yield return null;
					}
					mMonsterFloorCubeIndex -= 1;
					mStepDirection = GameBehaviour.Direction.LEFT;
				}
			}
		} else if (Mathf.Abs ((int)mDirection.x) < Mathf.Abs ((int)mDirection.z)) {
			if ((int)mDirection.z >= 0) {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex + 12];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x, transform.position.y - 3f / mMonsterSpeed, transform.position.z + 0.5f / mMonsterSpeed);
						transform.localScale = new Vector3 (transform.localScale.x + 0.5f / mMonsterSpeed, transform.localScale.y - 0.2f / mMonsterSpeed, transform.localScale.z + 0.5f / mMonsterSpeed);
						yield return null;
					}
					mMonsterFloorCubeIndex += 12;
					mStepDirection = GameBehaviour.Direction.FORWARD;
				}
			} else {
				GameObject floorCube = mCubes[mMonsterFloorCubeIndex - 12];
				if (!floorCube.CompareTag (GameBehaviour.TAG_OBSTACLE)) {
					for (int i = 0; i < mMonsterSpeed; i++) {
						transform.position = new Vector3 (transform.position.x, transform.position.y - 3f / mMonsterSpeed, transform.position.z - 0.5f / mMonsterSpeed);
						transform.localScale = new Vector3 (transform.localScale.x + 0.5f / mMonsterSpeed, transform.localScale.y - 0.2f / mMonsterSpeed, transform.localScale.z + 0.5f / mMonsterSpeed);
						yield return null;
					}
					mMonsterFloorCubeIndex -= 12;
					mStepDirection = GameBehaviour.Direction.BACKWARD;
				}
			}
		} else {
			for (int i = 0; i < mMonsterSpeed; i++) {
				transform.position = new Vector3 (transform.position.x, transform.position.y - 3f / mMonsterSpeed, transform.position.z);
				transform.localScale = new Vector3 (transform.localScale.x + 0.5f / mMonsterSpeed, transform.localScale.y - 0.2f / mMonsterSpeed, transform.localScale.z + 0.5f / mMonsterSpeed);
				yield return null;
			}
		}
	}

	IEnumerator Delay () {
		yield return new WaitForSeconds (mMonsterDelay);
	}

	public GameBehaviour.Direction getDirection () {
		return mStepDirection;
	}


}
