﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelMap : MonoBehaviour
{

	public float size = 2f;
	public int pixelResolution = 8;
	public int chunkResolution = 2;
	public PixelGrid pixelGridPrefab;

	//store an array of PixelGrids -- these are our chunks
	private PixelGrid[] chunks;
	private float chunkSize, pixelSize, halfSize;

	private void Awake()
	{
		halfSize = size * 0.5f;
		chunkSize = size / chunkResolution;
		pixelSize = chunkSize / pixelResolution;

		//edit for 3d
		chunks = new PixelGrid[chunkResolution * chunkResolution];
		for (int i = 0, y = 0; y < chunkResolution; y++)
		{
			for (int x = 0; x < chunkResolution; x++, i++)
			{
				CreateChunk(i, x, y);
			}
		}
		BoxCollider box = gameObject.AddComponent<BoxCollider>();
		box.size = new Vector3(size, size); //edit for 3d
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
					EditPixels(transform.InverseTransformPoint(hitInfo.point));
				}
			}
		}
		//update all chunks with space
		if (Input.GetKeyDown("space"))
		{
			for(int i = 0; i < chunks.Length; i++)
			{
				chunks[i].Refresh();
			}
		}
	}

	//edit for 3d
	private void EditPixels(Vector3 point)
	{
		int pixelX = (int)((point.x + halfSize) / pixelSize);
		int pixelY = (int)((point.y + halfSize) / pixelSize);
		int chunkX = pixelX / pixelResolution;
		int chunkY = pixelY / pixelResolution;
		Debug.Log(pixelX + ", " + pixelY + " in chunk " + chunkX + ", " + chunkY);
		pixelX -= chunkX * pixelResolution;
		pixelY -= chunkY * pixelResolution;
		chunks[chunkY * chunkResolution + chunkX].SetPixel(pixelX, pixelY, true);
	}

	//edit for 3d
	//takes an int i (number of chunks created thus far), and an (x,y) point
	//we instantiate the grid prefab, then set its position relative to the pixelMap
	private void CreateChunk(int i, int x, int y)
	{
		PixelGrid chunk = Instantiate(pixelGridPrefab) as PixelGrid;
		chunk.Initialize(pixelResolution, chunkSize);
		chunk.transform.parent = transform;
		chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize);
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
	}
}