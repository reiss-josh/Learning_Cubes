﻿using System.Collections;
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
	}

	private void Update()
	{
		//update all chunks with space
		if (Input.GetKeyDown("space")) {
			for (int i = 0; i < chunks.Length; i++) {
				chunks[i].Refresh();
			}
		}
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