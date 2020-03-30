﻿using UnityEngine;
using System;

[Serializable]
public class Pixel
{
	public bool state;
	public Vector2 position, xEdgePosition, yEdgePosition; //edit for 3d

	//edit for 3d
	public Pixel(int x, int y, float size)
	{
		position.x = (x + 0.5f) * size;
		position.y = (y + 0.5f) * size;

		xEdgePosition = position;
		xEdgePosition.x += size * 0.5f;
		yEdgePosition = position;
		yEdgePosition.y += size * 0.5f;
	}

	public Pixel() { }

	//edit for 3d
	public void BecomeXDummyOf(Pixel pixel, float offset)
	{
		state = pixel.state;
		position = pixel.position;
		xEdgePosition = pixel.xEdgePosition;
		yEdgePosition = pixel.yEdgePosition;
		position.x += offset;
		xEdgePosition.x += offset;
		yEdgePosition.x += offset;
	}

	//edit for 3d
	public void BecomeYDummyOf(Pixel pixel, float offset)
	{
		state = pixel.state;
		position = pixel.position;
		xEdgePosition = pixel.xEdgePosition;
		yEdgePosition = pixel.yEdgePosition;
		position.y += offset;
		xEdgePosition.y += offset;
		yEdgePosition.y += offset;
	}

	//edit for 3d
	public void BecomeXYDummyOf(Pixel pixel, float offset)
	{
		state = pixel.state;
		position = pixel.position;
		xEdgePosition = pixel.xEdgePosition;
		yEdgePosition = pixel.yEdgePosition;
		position.x += offset;
		position.y += offset;
		xEdgePosition.x += offset;
		xEdgePosition.y += offset;
		yEdgePosition.x += offset;
		yEdgePosition.y += offset;
	}
}

