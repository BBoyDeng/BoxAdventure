using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : GameBehaviour {

	public bool isOpening;

	public GameObject mMainManuFrame;
	public GameObject mSettingFrame;
	public AudioSource mBGMAudioSource;
	public AudioSource mSEAudioSource;


	void Awake () {
		// 固定手機螢幕為直立式
		Screen.orientation = ScreenOrientation.Portrait;
		checkMute();

		gMuteListener+=handleMute;
	}

	void OnDestroy()
	{
		gMuteListener-=handleMute;
	}

	// Use this for initialization
	void Start () {
		
		mBGMAudioSource.mute = isMute();
		mSEAudioSource.mute = isMute();
	}
	
	// Update is called once per frame
	void Update () {
		if(isOpening)
			if(Input.touchCount > 0 || Input.GetMouseButtonDown (0)) {
				changeScene(SCENE_MENU_MAIN);
			}
	}

	public void OpenSettingFrame () {
		mSEAudioSource.Play ();

		mSettingFrame.SetActive (true);
		mMainManuFrame.SetActive (false);
	}

	public void ExitSettingFrame () {
		mSEAudioSource.Play ();

		mSettingFrame.SetActive (false);
		mMainManuFrame.SetActive (true);
	}

	public void changeScene(string scene){
		if(!isMute())
			playAudioBeforeLoad(mSEAudioSource, mSEAudioSource.clip, scene);
		else
			SceneManager.LoadScene(scene);
	}

	public void setMute(bool isMute){
		GameBehaviour.changeMute(isMute);
	}

	protected void handleMute(bool isMute){
		mSEAudioSource.Play ();
		
		mBGMAudioSource.mute = isMute;
		mSEAudioSource.mute = isMute;
	}

}
