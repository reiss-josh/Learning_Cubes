using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{
	public GameObject voxelPrefab;
	public int resolution;

	//a voxel does or doesn't exist -- hence, bool array.
	private bool[] voxels;
	private float voxelSize;

	
	private Material[] voxelMaterials; //paint code

	//this function takes some resolution (how many cubes in this chunk), and some size (the size of the cubes)
	public void Initialize(int resolution, float size)
	{
		this.resolution = resolution;
		voxelSize = size / resolution;

		voxels = new bool[resolution * resolution * resolution];

		
		voxelMaterials = new Material[voxels.Length]; //paint code

		//iterate over all x,y,z creating voxels
		for (int i = 0, z = 0; z < resolution; z++)
		{
			for (int y = 0; y < resolution; y++)
			{
				for (int x = 0; x < resolution; x++, i++)
				{
					CreateVoxel(i, x, y, z);
				}
			}
		}

		SetVoxelColors(); //paint code
	}

	//i'm not going to bother explaining this -- check the createVoxel in voxelMap
	private void CreateVoxel(int i, int x, int y, int z)
	{
		GameObject newVoxel = Instantiate(voxelPrefab) as GameObject;
		newVoxel.transform.parent = transform;
		newVoxel.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
		newVoxel.transform.localScale = Vector3.one * voxelSize * 0.1f;
		voxelMaterials[i] = newVoxel.GetComponent<MeshRenderer>().material; //paintCode;
	}

	
	public void SetVoxel(int x, int y, int z, bool state) //paint code
	{
		voxels[y * resolution + z * resolution * resolution + x] = state;
		SetVoxelColors();
	}

	private void SetVoxelColors() //paint code
	{
		for (int i = 0; i < voxels.Length; i++)
		{
			voxelMaterials[i].color = voxels[i] ? Color.black : Color.white;
		}
	}
}
