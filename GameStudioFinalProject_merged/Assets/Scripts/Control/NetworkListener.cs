using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon;

public class NetworkListener : PunBehaviour {

	public GameObject mPauseFrame;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
  {
		mPauseFrame.SetActive(true);	
  }

  public override void OnDisconnectedFromPhoton()
  {
		print("photon disconnected");
		SceneManager.LoadScene("Menu_Main");
  }
}
