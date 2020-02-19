using UnityEngine;
using System;

[Serializable]
public class Voxel
{
	public bool state;
	public Vector3 position;

	public Voxel(int x, int y, int z, float size)
	{
		position.x = (x + 0.5f) * size;
		position.y = (y + 0.5f) * size;
		position.z = (z + 0.5f) * size;
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

