using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon;
public class NetworkManager : PunBehaviour {

    public int FloorTypeNum;
    public int PlayerTypeNum;

    public Text PlayersNum;
    public GameObject StartBtn;

	public AudioSource mBGMAudioSource;

    void Awake()
    {
        PhotonNetwork.autoJoinLobby = true;
        PhotonNetwork.logLevel = PhotonLogLevel.ErrorsOnly;
        PhotonNetwork.automaticallySyncScene = true;       
    }

    public void Start()
    {
        PhotonNetwork.ConnectUsingSettings("1.0.0");

		mBGMAudioSource.mute = GameBehaviour.isMute();
    }

    public override void OnJoinedLobby()
    {   
        print("on join lobby");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        print("create a room");
        // create a room
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        
        int prev=-1;
        int rand;

        int[] floorSeq=new int[100];
        int idx=0;

        while(idx<floorSeq.Length){
            while (true) {
                rand = Random.Range (0, FloorTypeNum);
                if (rand == prev)
                    continue;
                else{
                    floorSeq[idx]=rand;
                    break;
                }
            }
            
            prev=rand;
            idx+=1;
        }

        int[] playersMaterial=new int[PlayerTypeNum];
        for(int i=0;i<PlayerTypeNum;i++){
            playersMaterial[i]=i;
        }
        for(int i=0;i<PlayerTypeNum;i++){
            rand=Random.Range (0, PlayerTypeNum);
            int tmp=playersMaterial[i];
            playersMaterial[i]=playersMaterial[rand];
            playersMaterial[rand]=tmp;
        }

        options.CustomRoomProperties=new ExitGames.Client.Photon.Hashtable();
        options.CustomRoomProperties.Add("f",floorSeq);
        options.CustomRoomProperties.Add("p",playersMaterial);

        while(!PhotonNetwork.CreateRoom(null, options, TypedLobby.Default)){}
    }

    public override void OnJoinedRoom()
    {
        print("on join room");

        PlayersNum.text=""+PhotonNetwork.playerList.Length;

        if(PhotonNetwork.isMasterClient&&PhotonNetwork.playerList.Length==2){
            StartBtn.SetActive(true);
        }
        else
            StartBtn.SetActive(false);
    }
    
    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        print("on player connected");

        PlayersNum.text=""+PhotonNetwork.playerList.Length;

        if(PhotonNetwork.isMasterClient&&PhotonNetwork.playerList.Length==2){
            StartBtn.SetActive(true);
        }
        else
            StartBtn.SetActive(false);
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        print("on player disconnected");

        PlayersNum.text=""+PhotonNetwork.playerList.Length;
        
        if(PhotonNetwork.isMasterClient&&PhotonNetwork.playerList.Length==2){
            StartBtn.SetActive(true);
        }
        else
            StartBtn.SetActive(false);
    }

    public override void OnDisconnectedFromPhoton()
    {
        SceneManager.LoadScene("Menu_Main");
    }

    public void onStartBattle(){
        PhotonNetwork.room.IsOpen=false;
        PhotonNetwork.LoadLevel("Game_Battle");
    }

    public void onReturn(){
        if(PhotonNetwork.connected)
            PhotonNetwork.Disconnect();
        else
            SceneManager.LoadScene("Menu_Main");
    }
}
