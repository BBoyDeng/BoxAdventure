using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {

	public Transform mPlayer;
	public Transform mThirdPersonPerspective;	// 第三人稱視角

	float mDoubleTouchCurrDis;	// 當前雙指觸控間距
	float mDoubleTouchPrevDis;	// 過去雙指觸控間距

	bool mIsZoom = false;	// 是否縮放
	
	// Update is called once per frame
	void Update () {
		if (Input.touchCount == 2 && (Input.GetTouch (0).phase == TouchPhase.Moved || Input.GetTouch (1).phase == TouchPhase.Moved)) {
			Touch touch1 = Input.GetTouch (0);
			Touch touch2 = Input.GetTouch (1);

			mDoubleTouchCurrDis = Vector2.Distance (touch1.position, touch2.position);

			if (!mIsZoom) {
				mDoubleTouchPrevDis = mDoubleTouchCurrDis;
				mIsZoom = true;
			}

			float distance = mDoubleTouchCurrDis - mDoubleTouchPrevDis;

			if (transform.position.z < -1.5f && transform.position.z > -15f) {
				transform.Translate (new Vector3 (0f, 0f, distance * Time.deltaTime));
			}

			mDoubleTouchPrevDis = mDoubleTouchCurrDis;
		} else {
			mDoubleTouchCurrDis = 0f;
			mDoubleTouchPrevDis = 0f;
			mIsZoom = false;
		}
	}

	void FixedUpdate () {
		mThirdPersonPerspective.position =Vector3.Lerp(mThirdPersonPerspective.position,mPlayer.position,0.1f);
	}
}
