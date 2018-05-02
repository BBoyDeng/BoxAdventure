using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemAI : MonoBehaviour {

	void Awake () {
	}

	void FixedUpdate () {
		if (gameObject.CompareTag (GameBehaviour.TAG_ITEM_TIME) || gameObject.CompareTag (GameBehaviour.TAG_ITEM_TIME_BIG))
			transform.Rotate (new Vector3 (45f, 30f, 45f) * Time.fixedDeltaTime);
		else if (gameObject.CompareTag (GameBehaviour.TAG_ITEM_DIAMOND)||gameObject.CompareTag (GameBehaviour.TAG_ITEM_DIAMOND_BIG))
			transform.Rotate (new Vector3 (0f, 0f, 30f) * Time.fixedDeltaTime);
		else if (gameObject.CompareTag (GameBehaviour.TAG_ITEM_BATTERY))
			transform.Rotate (new Vector3 (30f, 0f, 0f) * Time.fixedDeltaTime);
	}
}
