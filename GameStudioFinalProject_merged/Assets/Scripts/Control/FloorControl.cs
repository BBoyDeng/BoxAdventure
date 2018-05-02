using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorControl : GameBehaviour, GameBehaviour.ICubesHandler {


	//names are cube_1, cube_2, ..., cube_144
	private GameObject[] mCubes;
	
	private Dictionary<int,string> tails;

	public int mCubeNum = 144;
	public int mHoleNum = 3;
	public bool isBonus = false;
	private bool hasHole=false;
	

	void Awake()
	{
		tails=new Dictionary<int, string>();
		mCubes=new GameObject[mCubeNum];
		Transform cubes = transform.Find("GameObject");
		for(int i=0;i<mCubeNum;i++){
			mCubes[i]=cubes.GetChild(i).gameObject;
		}

		print(gameObject.name);
		updateFloorCubes(this);
	}


	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {
		
	}
	public void setFixedItems(GameObject prefab, int amount){
		for(int i=0;i<amount;i++){
			GameObject obj=(Instantiate (prefab) as GameObject);

			GameObject floorCube;
			while (true) {
				int floorCubeIndex = Random.Range (0, mCubeNum);
				floorCube = getFloorCube(floorCubeIndex+1);
				if (!floorCube.CompareTag (TAG_BOUNDARY) && !floorCube.CompareTag (TAG_OBSTACLE) && !floorCube.CompareTag (TAG_HOLE))
					break;
			}
			obj.transform.position = floorCube.transform.position + new Vector3 (0f, 1f, 0f);
			obj.transform.parent = transform;
		}
			
	}
	
	public void setItems(GameObject[] prefabs){
		// 隨機動態生成道具
		int rand=Random.Range(1,3);
		for (int i = 0; i < rand; i++) {
			GameObject obj = Instantiate (prefabs [Random.Range (0, prefabs.Length)]) as GameObject;

			GameObject floorCube;
			while (true) {
				int floorCubeIndex = Random.Range (0, mCubeNum);
				floorCube = getFloorCube(floorCubeIndex+1);
				if (!floorCube.CompareTag (TAG_BOUNDARY) && !floorCube.CompareTag (TAG_OBSTACLE) && !floorCube.CompareTag (TAG_HOLE))
					break;
			}

			obj.transform.position = floorCube.transform.position + new Vector3 (0f, 1f, 0f);
			obj.transform.parent = transform;
		}
	}

	public void setBonusItems(GameObject[] prefabs){
		// 動態生成獎勵道具
		int index = 0;
		for (int floorCubeIndex = 0; floorCubeIndex < mCubeNum; floorCubeIndex++) {
			GameObject floorCube = getFloorCube(floorCubeIndex+1);

			if (floorCube.CompareTag (TAG_BOUNDARY))
				continue;

			GameObject obj = Instantiate (prefabs [Random.Range (0, prefabs.Length)]) as GameObject;
			obj.transform.position = floorCube.transform.position + new Vector3 (0f, 1f, 0f);
			obj.transform.parent = transform;

			index++;
		}
	}


	public void setFloorHole(ParticleSystem ps){
		if(isBonus)
			return;
		
		if(hasHole)
			return;

		hasHole=true;


		// 隨機動態生成地洞
		int countOfHole = 0;
		while (countOfHole < mHoleNum) {
			int holeIndex = Random.Range (0, mCubeNum);

			GameObject hole=getFloorCube(holeIndex+1);

			if (hole.CompareTag (TAG_BOUNDARY) || hole.CompareTag (TAG_OBSTACLE)||hole.CompareTag(TAG_HOLE))
				continue;


			hole.tag = TAG_HOLE;

			ParticleSystem holeInstance = Instantiate(ps) as ParticleSystem;
			holeInstance.transform.parent = transform;
			holeInstance.transform.position = hole.transform.position;

			// 檢查地洞方塊是否有子物件(e.g. 火)，若有則將active設成false
			if (hole.transform.childCount != 0) {
				GameObject child = hole.transform.GetChild (0).gameObject;
				child.SetActive (false);
			}
			hole.GetComponent<MeshRenderer> ().enabled = false;
			countOfHole++;
		}
	}

	public void setFloorTimerHole(ParticleSystem ps){
		if(isBonus)
			return;
		
		if(hasHole)
			return;

		hasHole=true;

		Dictionary<int,string> timerHoles=new Dictionary<int, string>();
		List<ParticleSystem> pss=new List<ParticleSystem>();

		// 隨機動態生成地洞
		int countOfHole = 0;
		while (countOfHole < mHoleNum) {
			int holeIndex = Random.Range (0, mCubeNum);

			GameObject hole=getFloorCube(holeIndex+1);

			if (hole.CompareTag (TAG_BOUNDARY) || hole.CompareTag (TAG_OBSTACLE)||hole.CompareTag(TAG_HOLE))
				continue;

			timerHoles[holeIndex]=hole.tag;
			hole.tag = TAG_HOLE;

			ParticleSystem holeInstance = Instantiate(ps) as ParticleSystem;
			holeInstance.transform.parent = transform;
			holeInstance.transform.position = hole.transform.position;

			pss.Add(holeInstance);

			// 檢查地洞方塊是否有子物件(e.g. 火)，若有則將active設成false
			if (hole.transform.childCount != 0) {
				GameObject child = hole.transform.GetChild (0).gameObject;
				child.SetActive (false);
			}
			hole.GetComponent<MeshRenderer> ().enabled = false;
			countOfHole++;
		}

		StartCoroutine(HoleTimer(pss,timerHoles));
	}

	IEnumerator HoleTimer (List<ParticleSystem> pss, Dictionary<int, string> ths) {
		yield return new WaitForSeconds(6f);

		foreach(var pair in ths){
			mCubes[pair.Key].tag=pair.Value;
			mCubes[pair.Key].GetComponent<MeshRenderer>().enabled=true;
			
			if (mCubes[pair.Key].transform.childCount != 0) {
				GameObject child = mCubes[pair.Key].transform.GetChild (0).gameObject;
				child.SetActive (true);
			}
		}

		foreach(var ps in pss){
			Destroy(ps.gameObject);
		}

		hasHole=false;
		raiseFloorEvent(FloorEvent.CLOSE);
	}

	void Tail (int index) {
		TailGoDown ();
		TailGoUp (index);
	}

	void AllTailDestroy (ParticleSystem ps) {
		foreach(var pair in tails){
			Transform childFloorCube = mCubes[pair.Key].transform.Find (mCubes[pair.Key].name + "(Clone)");
			mCubes[pair.Key].tag = pair.Value;
			
			ParticleSystem deadTailInstance = Instantiate(ps) as ParticleSystem;
			deadTailInstance.transform.position = mCubes[pair.Key].transform.position + Vector3.up * 0.5f;

			Destroy (deadTailInstance.gameObject, 1f);
			Destroy (childFloorCube.gameObject);
		}
		tails.Clear();
	}

	void TailGoUp (int index) {
		tails[index]=mCubes[index].tag;
		mCubes[index].tag=TAG_OBSTACLE;

		GameObject floorCubeInstance = Instantiate (mCubes[index]);
		floorCubeInstance.transform.parent = mCubes[index].transform;
		floorCubeInstance.transform.position = mCubes[index].transform.position;

		StartCoroutine (GoUp (floorCubeInstance));
	}

	void TailGoDown () {
		int[] idxs=new int[tails.Count];
		tails.Keys.CopyTo(idxs,0);

		for(int i=0;i<idxs.Length;i++){
			Transform childFloorCube = mCubes[idxs[i]].transform.Find (mCubes[idxs[i]].name + "(Clone)");
			StartCoroutine (GoDown (idxs[i], childFloorCube.gameObject));
		}
	}

	IEnumerator GoUp (GameObject cube) {
		for (int i = 0; i < 5; i++) {
			if(cube!=null)
				cube.transform.Translate (0f, 0.1f, 0f, Space.World);
			yield return null;
		}
	}

	IEnumerator GoDown (int index, GameObject childCube) {
		if (childCube.transform.position.y - transform.position.y < 0.1f) {

			mCubes[index].tag = tails[index];
			tails.Remove(index);

			Destroy (childCube);
			yield return null;
		} else {
			childCube.transform.Translate (0f, -0.1f, 0f, Space.World);
			yield return null;
		}
	}
	
/* interface implement */	
	// num is from 1 to 144
	// idx is from 0 to 143	
	public GameObject getFloorCube(int num){
		int idx=num-1;

		return mCubes[idx];
	}
    public void generateTail(int num)
    {
		int idx=num-1;
        Tail(idx);
    }

    public void destroyAllTail(ParticleSystem ps)
    {
        AllTailDestroy(ps);
    }
}
