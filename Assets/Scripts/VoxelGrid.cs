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

		//this generates an array of size resolution^2
		//would want res^3 in 3D
		voxels = new bool[resolution * resolution];

		
		voxelMaterials = new Material[voxels.Length]; //paint code

		//iterate over all x,y creating voxels
		for (int i = 0, y = 0; y < resolution; y++)
		{
			for (int x = 0; x < resolution; x++, i++)
			{
				CreateVoxel(i, x, y);
			}
		}

		SetVoxelColors(); //paint code
	}

	//i'm not going to bother explaining this -- check the createVoxel in voxelMap
	private void CreateVoxel(int i, int x, int y)
	{
		GameObject newVoxel = Instantiate(voxelPrefab) as GameObject;
		newVoxel.transform.parent = transform;
		newVoxel.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, -0.01f);
		newVoxel.transform.localScale = Vector3.one * voxelSize * 0.1f;
		voxelMaterials[i] = newVoxel.GetComponent<MeshRenderer>().material; //paintCode;
	}

	
	public void SetVoxel(int x, int y, bool state) //paint code
	{
		voxels[y * resolution + x] = state;
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
