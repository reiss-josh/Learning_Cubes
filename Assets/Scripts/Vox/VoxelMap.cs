using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMap : MonoBehaviour
{
	public float threshold = 0.5f;
	public float size = 2f;
	public int voxelResolution = 8;
	public int chunkResolution = 2;
	public VoxelGrid voxelGridPrefab;

	private VoxelGrid[] chunks;
	private float chunkSize, voxelSize, halfSize;

	private void Awake()
	{
		halfSize = size * 0.5f;
		chunkSize = size / chunkResolution;
		voxelSize = chunkSize / voxelResolution;
		chunks = new VoxelGrid[chunkResolution * chunkResolution * chunkResolution];

		for (int i = 0, z = 0; z < chunkResolution; z++) {
			for (int y = 0; y < chunkResolution; y++) {
				for (int x = 0; x < chunkResolution; x++, i++) {
					CreateChunk(i, x, y, z);
				}
			}
		}
		BoxCollider box = gameObject.AddComponent<BoxCollider>(); //this collider is being used for the draw raycast -- will that be a problem??
		box.size = new Vector3(size, size, size);
	}

	private void Update()
	{
		//color with mouse
		if (Input.GetMouseButton(0)) {
			PerfEdit(0.1f);
		}
		if (Input.GetMouseButton(1)) {
			PerfEdit(-0.1f);
		}
		//update all chunks with space
		if (Input.GetKeyDown("space")) {
			for (int i = 0; i < chunks.Length; i++) {
				chunks[i].Refresh();
			}
		}
	}

	private void PerfEdit(float stateChange)
	{
		RaycastHit hitInfo;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)) {
			if (hitInfo.collider.gameObject == gameObject) {
				EditVoxels(transform.InverseTransformPoint(hitInfo.point), stateChange);
			}
		}
	}

	private void EditVoxels(Vector3 point, float stateChange)
	{
		Vector3 pointEdit = (point + (Vector3.one * halfSize)) / voxelSize;
		Vector3Int voxelXYZ = new Vector3Int((int)pointEdit.x, (int)pointEdit.y, (int)pointEdit.z);

		//i have no idea why i need this next bit
		for (int i = 0; i < 3; i++) {
			if (point[i] >= 1.0)
				voxelXYZ[i] -= 1;
		}
		Vector3Int chunkXYZ = new Vector3Int (voxelXYZ.x / voxelResolution, voxelXYZ.y/voxelResolution, voxelXYZ.z/voxelResolution);
		chunkXYZ.z /= voxelResolution;
		Debug.Log(voxelXYZ + " in chunk " + chunkXYZ);
		voxelXYZ -= chunkXYZ * voxelResolution;
		chunks[chunkXYZ.z * (chunkResolution * chunkResolution) + chunkXYZ.y * chunkResolution + chunkXYZ.x].SetVoxel(voxelXYZ.x, voxelXYZ.y, voxelXYZ.z, stateChange);
	}

	private void CreateChunk(int i, int x, int y, int z)
	{
		VoxelGrid chunk = Instantiate(voxelGridPrefab) as VoxelGrid;
		chunk.Initialize(voxelResolution, chunkSize, threshold);
		chunk.transform.parent = transform;
		chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize, z * chunkSize - halfSize);
		chunks[i] = chunk;
	}
}