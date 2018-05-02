using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CannonAI : MonoBehaviour {

	public GameObject mBomb;
	public Transform mStar;
	public Transform mCannon;

	public float mFireForce = 500f;		// 發射砲彈的力道
	public float mFireRate = 1.5f;		// 發射砲彈的時間間隔
	public float mNextFire = 0f;

	GameBehaviour.IGameTracker tracker;
	GameObject mBombInstance;
	Vector3 mBombDirection;

	void Awake () {
		tracker=GameObject.Find("GameManager").GetComponent<GameManager>().getGameTracker();
	}

	// Use this for initialization
	void Start () {
		mBombDirection = mStar.position - mCannon.position;
	}

	void FixedUpdate () {
		if (!tracker.isTimerPause () && !tracker.isTimesUp ()) {
			// 發射砲彈
			if (mBombInstance == null && Time.time >= mNextFire) {
				mBombInstance = Instantiate (mBomb) as GameObject;
				mBombInstance.transform.parent = transform;
				mBombInstance.transform.position = mStar.position;
				mBombInstance.GetComponent<Rigidbody> ().AddForce (mBombDirection * mFireForce);

				if (mBombDirection.x > 0f)
					mBombInstance.GetComponent<BombAI> ().SetDirection (GameBehaviour.Direction.RIGHT);
				else if (mBombDirection.x < 0f)
					mBombInstance.GetComponent<BombAI> ().SetDirection (GameBehaviour.Direction.LEFT);
				else if (mBombDirection.z > 0f)
					mBombInstance.GetComponent<BombAI> ().SetDirection (GameBehaviour.Direction.FORWARD);
				else if (mBombDirection.z < 0f)
					mBombInstance.GetComponent<BombAI> ().SetDirection (GameBehaviour.Direction.BACKWARD);
			

				mNextFire = Time.time + mFireRate;
			}
		}
	}

}
