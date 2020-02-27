using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMap : MonoBehaviour
{
	public float threshold = 0.5f;
	public float size = 2f;
	public int voxelResolution = 8;
	public int chunkResolution = 2;
	public int chunkYResolution = 2;
	public Chunk chunkPrefab;

	private Chunk[] chunks;
	private float chunkSize, chunkYSize, voxelSize, halfSize;

	private void Awake()
	{
		halfSize = size * 0.5f;
		chunkSize = size / chunkResolution;
		chunkYSize = size / chunkYResolution;
		voxelSize = chunkSize / voxelResolution;
		chunks = new Chunk[chunkResolution * chunkResolution * chunkResolution];

		for (int i = 0, z = 0; z < chunkResolution; z++) {
			for (int y = 0; y < chunkYResolution; y++) {
				for (int x = 0; x < chunkResolution; x++, i++) {
					CreateChunk(i, x, y, z);
				}
			}
		}
	}

	private void CreateChunk(int i, int x, int y, int z)
	{
		Chunk chunk = Instantiate(chunkPrefab) as Chunk;
		chunk.transform.parent = transform;
		chunk.transform.position = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize, z * chunkSize - halfSize);
		chunk.Initialize(voxelResolution, chunkSize, threshold);
		chunks[i] = chunk;
	}
}