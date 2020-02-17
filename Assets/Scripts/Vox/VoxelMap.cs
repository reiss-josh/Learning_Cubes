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

		//create an array of chunkRes * chunkRes (x * y)
		//iterate over x,y coordinates, spawning chunks.
		//for resolution n, this generates n^2 chunks.
		//it'd be n^3 in 3d
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

		//this is code for painting the voxels
		BoxCollider box = gameObject.AddComponent<BoxCollider>();
		box.size = new Vector3(size, size);
	}

	//broken for 3d
	//in update, we check if a raycast from the mouse hits the voxels
	private void Update()
	{
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
		if (Input.GetKeyDown("space"))
		{
			StartCoroutine(SetAllChnk());
		}
	}

	//debug function that sets one cube in each chunk to black
	public IEnumerator SetAllChnk(int xv = 0, int yv = 0, int zv = 0)
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		for (int z = 0; z < chunkResolution; z++)
		{
			for (int y = 0; y < chunkResolution; y++)
			{
				for (int x = 0; x < chunkResolution; x++)
				{
					chunks[x + y * chunkResolution + (z * chunkResolution * chunkResolution)].SetVoxel(xv,yv,zv,true);
					yield return wait;
				}
			}
		}
	}

	//editVoxel actually sets the voxel and recolors it
	//fairly certain this doesn't work right in 3d
	private void EditVoxels(Vector3 point)
	{
		int voxelX = (int)((point.x + halfSize) / voxelSize);
		int voxelY = (int)((point.y + halfSize) / voxelSize);
		int voxelZ = (int)((point.z + halfSize) / voxelSize);
		int chunkX = voxelX / voxelResolution;
		int chunkY = voxelY / voxelResolution;
		int chunkZ = voxelZ / voxelResolution;
		Debug.Log(voxelX + ", " + voxelY + ", " + voxelZ + "in chunk " + chunkX + ", " + chunkY);
		voxelX -= chunkX * voxelResolution;
		voxelY -= chunkY * voxelResolution;
		voxelZ -= chunkZ * voxelResolution;
		chunks[(chunkZ * chunkResolution * chunkResolution) + (chunkY * chunkResolution) + chunkX].SetVoxel(voxelX, voxelY, voxelZ, true);
	}

	//here we actually create the chunk.
	//this function takes an i (number of chunks created thus far), and an (x,y,z) point
	//we instantiate the grid prefab, then set its position relative to the voxelMap
	private void CreateChunk(int i, int x, int y, int z)
	{
		VoxelGrid chunk = Instantiate(voxelGridPrefab) as VoxelGrid;
		chunk.Initialize(voxelResolution, chunkSize);
		chunk.transform.parent = transform;
		chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize, z * chunkSize - halfSize);
		chunks[i] = chunk;
	}
}