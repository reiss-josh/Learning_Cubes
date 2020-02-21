using UnityEngine;
using System;

[Serializable]
public class Voxel
{
	public float value;
	public Vector3 position;
	public Vector3 voxT;

	public Voxel(Vector3 pos, float size, float val = -1)
	{
		position = pos;
		voxT.x = (pos.x + 0.5f) * size;
		voxT.y = (pos.y + 0.5f) * size;
		voxT.z = (pos.z + 0.5f) * size;
		if(val == -1)
			value = UnityEngine.Random.Range(0.0f, 1.0f);
	}

	//empty fallback initializer
	public Voxel() { }

	/*
	 * the following fxns could probably be turned into one big fxn
	 * call it BecomeDummyOf(Voxel voxel, float offset, Vector3 xyz)
	 * if xyz = (1,0,0) then do x dummy;
	 *			(0,1,0) is y dummy;
	 *			(1,1,1) is xyzdummy; etc
	 * just build everything out of switch()
	 */
}

