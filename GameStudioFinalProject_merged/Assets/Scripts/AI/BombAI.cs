using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombAI : MonoBehaviour,GameBehaviour.IDirectionHandler {

	public ParticleSystem mExplosion;

	GameBehaviour.Direction mDirection;

	/// <summary>
	/// Awake is called when the script instance is being loaded.
	/// </summary>
	void Awake()
	{

	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnTriggerEnter (Collider other) {
		ParticleSystem explosionInstance = Instantiate (mExplosion) as ParticleSystem;

		explosionInstance.transform.position = transform.position;

		var main = explosionInstance.main;
		main.simulationSpeed = 3f;

		Destroy (explosionInstance.gameObject, 2f);
		Destroy (gameObject);
	}

	public void SetDirection (GameBehaviour.Direction d) {
		mDirection = d;
	}

	public GameBehaviour.Direction getDirection () {
		return mDirection;
	}
}
