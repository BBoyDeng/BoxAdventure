using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameBehaviour : MonoBehaviour {
    private static bool mute;

/* listener about mute */
/* push mode */
/* handle this if current scene is allowed to changeMute */
    public delegate void MuteListener(bool isMute);
    protected static MuteListener gMuteListener;

    
/* methods about mute */
/* handle MuteListener if current scene can changeMute */
    private readonly static string KEY_MUTE="key_mute";
    
    protected static void checkMute(){
        int val=PlayerPrefs.GetInt(KEY_MUTE,0);
        mute=(val==1?true:false);
    }

    protected static void changeMute(bool isMute){
        PlayerPrefs.SetInt(KEY_MUTE,isMute?1:0);
        mute=isMute;

        if(gMuteListener!=null)
            gMuteListener(isMute);
    }

    public static bool isMute(){
        return mute;
    }

/* handle about play SE before load scene */
    protected void playAudioBeforeLoad(AudioSource source, AudioClip clip, string scene)
    {
        float len = clip.length;
        source.PlayOneShot(clip);

        StartCoroutine(DelayWork(len, scene));
    }
 
    private IEnumerator DelayWork(float len, string scene)
    {
        yield return new WaitForSeconds(len);
        SceneManager.LoadScene(scene);
    }

/* interface about AI */
    public enum Direction
    {
        RIGHT, LEFT, FORWARD, BACKWARD
    }
    public interface IDirectionHandler
    {
        Direction getDirection();
    }

/* interface about cubes */
    public interface ICubesHandler{
        
        // num is from 1 to 144
	    // idx is from 0 to 143
        GameObject getFloorCube(int num);
        void generateTail(int num);
        void destroyAllTail(ParticleSystem ps);
    }

/* listener about floor cubes */
/* push mode */
    public delegate void CubesListener(ICubesHandler ich);
    protected static CubesListener gCubesListener;
    protected static void updateFloorCubes(ICubesHandler ich){
        if(gCubesListener!=null){
            gCubesListener(ich);
        }
    }

/* listener about floor */
/* push mode / event */
    public enum FloorEvent{
        GENERATE,CLEAR,BONUS,CLOSE
    }
    public delegate void FloorListener(FloorEvent e);
    protected static FloorListener gFloorListener;

    protected static void raiseFloorEvent(FloorEvent e){
        if(gFloorListener!=null)
            gFloorListener(e);
    }

/* listener about game control */
/* push mode / event */
    public enum ControlEvent{
        OnPlay,OnPause,onReady,OnStart,OnOver,
        Score,AdjustTimerSpeed,IncreaseTime,DecreaseTime,ChargeFever
    }

    public delegate void ControlListener(ControlEvent e, float arg);
    protected static ControlListener gControlListener;

    protected static void raiseControlEvent(ControlEvent e, float arg){
        if(gControlListener!=null)
            gControlListener(e,arg);
    }

/* interface about game track */
    public interface IGameTracker{
        bool isTimesUp();
		bool isTimerPause();
        bool isGameResult();
		bool testEnterCrazy();
		bool testExitCrazy();
        bool isInCrazyTime();
    }


/* listener about mission */
/* push mode / event */
    public delegate void MissionListener(string mission);
    protected static MissionListener gMissionListener;

    protected static void raiseMissionEvent(string mission){
        if(gMissionListener!=null)
            gMissionListener(mission);
    }

/* interface about mission */
    public interface IMission{
        void check(string mission);
        bool isFinish();
    }

/* names about scenes */
	public readonly static string SCENE_MENU_OPENING = "Menu_Opening";
	public readonly static string SCENE_MENU_MAIN = "Menu_Main";
    public readonly static string SCENE_GAME_SINGLE = "Game_Single";
    public readonly static string SCENE_BATTLE_LOAD="Game_Battle_Load";
    public readonly static string SCENE_BATTLE="Game_Battle";
	
/* tags about floor cubes */
	public readonly static string TAG_UNTAGGED = "Untagged";
    public readonly static string TAG_BOUNDARY = "Boundary";
    public readonly static string TAG_OBSTACLE = "Obstacle";
	public readonly static string TAG_HOLE = "Hole";
	public readonly static string TAG_SCENE_ICE = "SceneIce";
	public readonly static string TAG_SCENE_FIRE = "SceneFire";
	public readonly static string TAG_SCENE_BOMB = "SceneBomb";
	public readonly static string TAG_SCENE_MONSTER = "SceneMonster";
	public readonly static string TAG_SCENE_CHESS = "SceneChess";

/* tags about items */
    public readonly static string TAG_ITEM_Key = "ItemKey";
	public readonly static string TAG_ITEM_TIME = "ItemTime";
	public readonly static string TAG_ITEM_TIME_BIG = "ItemBigTime";
	public readonly static string TAG_ITEM_DIAMOND = "ItemDiamond";
    public readonly static string TAG_ITEM_DIAMOND_BIG = "ItemBigDiamond";
	public readonly static string TAG_ITEM_BATTERY = "ItemBattery";
    public readonly static string TAG_ITEM_PUZZLE_1="ItemPuzzle1";
    public readonly static string TAG_ITEM_PUZZLE_2="ItemPuzzle2";
    public readonly static string TAG_ITEM_PUZZLE_3="ItemPuzzle3";

/* tags about mission */
    public readonly static string TAG_MISSION_RIGHT="MissionRight";
    public readonly static string TAG_MISSION_LEFT="MissionLeft";
    public readonly static string TAG_MISSION_FORWARD="MissionForward";
    public readonly static string TAG_MISSION_BACKWARD="MissionBackward";
}

