using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMap : MonoBehaviour
{
	public float size = 2f;
	public int voxelResolution = 8;
	public int chunkResolution = 2;
	public VoxelGrid voxelGridPrefab;

	//store an array of VoxelGrids -- these are our chunks
	private VoxelGrid[] chunks;
	private float chunkSize, voxelSize, halfSize;

	private void Awake()
	{
		halfSize = size * 0.5f;
		chunkSize = size / chunkResolution;
		voxelSize = chunkSize / voxelResolution;
		chunks = new VoxelGrid[chunkResolution * chunkResolution * chunkResolution];

		for (int i = 0, z = 0; z < chunkResolution; z++)
		{
			for (int y = 0; y < chunkResolution; y++)
			{
				for (int x = 0; x < chunkResolution; x++, i++)
				{
					CreateChunk(i, x, y, z);
				}
			}
		}
		BoxCollider box = gameObject.AddComponent<BoxCollider>(); //this collider is being used for the draw raycast -- will that be a problem??
		box.size = new Vector3(size, size, size); //edit for 3d
	}

	private void Update()
	{
		//color with lmouse
		if (Input.GetMouseButton(0))
		{
			RaycastHit hitInfo;
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))
			{
				if (hitInfo.collider.gameObject == gameObject)
				{
					EditVoxels(transform.InverseTransformPoint(hitInfo.point));
				}
			}
		}
		//update all chunks with space
		if (Input.GetKeyDown("space"))
		{
			for (int i = 0; i < chunks.Length; i++)
			{
				chunks[i].Refresh();
			}
		}
	}

	//edit for 3d -- done?
	private void EditVoxels(Vector3 point)
	{
		int voxelX = (int)((point.x + halfSize) / voxelSize);
		int voxelY = (int)((point.y + halfSize) / voxelSize);
		int voxelZ = (int)((point.z + halfSize) / voxelSize);
		int chunkX = voxelX / voxelResolution;
		int chunkY = voxelY / voxelResolution;
		int chunkZ = voxelZ / voxelResolution;
		Debug.Log(voxelX + ", " + voxelY + ", " + voxelZ + " in chunk " + chunkX + ", " + chunkY + ", " + chunkZ);
		voxelX -= chunkX * voxelResolution;
		voxelY -= chunkY * voxelResolution;
		voxelZ -= chunkZ * voxelResolution;
		chunks[chunkZ * chunkResolution * chunkResolution + chunkY * chunkResolution + chunkX].SetVoxel(voxelX, voxelY, voxelZ, true);
	}

	//edit for 3d
	//takes an int i (number of chunks created thus far), and an (x,y) point
	//we instantiate the grid prefab, then set its position relative to the voxelMap
	private void CreateChunk(int i, int x, int y, int z)
	{
		VoxelGrid chunk = Instantiate(voxelGridPrefab) as VoxelGrid;
		chunk.Initialize(voxelResolution, chunkSize);
		chunk.transform.parent = transform;
		chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize, z * chunkSize - halfSize);
		chunks[i] = chunk;
		if (x > 0)
		{
			chunks[i - 1].xNeighbor = chunk;
		}
		if (y > 0)
		{
			chunks[i - chunkResolution].yNeighbor = chunk;
			if (x > 0)
			{
				chunks[i - chunkResolution - 1].xyNeighbor = chunk;
			}
		}
		if (z > 0)
		{
			int hmm = chunkResolution; //i'm not sure what this needs to be
			chunks[i - hmm].zNeighbor = chunk;

			if (y > 0)
			{
				chunks[i - hmm - chunkResolution].yzNeighbor = chunk;
				if (x > 0)
				{
					chunks[i - hmm - chunkResolution - 1].xyzNeighbor = chunk;
				}
			}
			if (x > 0)
			{
				chunks[i - hmm - - 1].xzNeighbor = chunk;
			}
		}
	}
}