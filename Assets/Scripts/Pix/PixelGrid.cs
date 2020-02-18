using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PixelGrid : MonoBehaviour
{
	public GameObject pixelPrefab;
	public PixelGrid xNeighbor, yNeighbor, xyNeighbor; //edit for 3d
	public int resolution;

	private Pixel[] pixels;
	private float pixelSize, gridSize;
	private List<Vector3> vertices;
	private List<int> triangles;
	private Pixel dummyX, dummyY, dummyT; //edit for 3d
	private Material[] pixelMaterials;
	private Mesh mesh;

	
	//this function takes some resolution (how many cubes per chunk), and some size (the size of the cubes)
	public void Initialize(int resolution, float size)
	{
		this.resolution = resolution;
		pixelSize = size / resolution;
		gridSize = size;

		//this generates an array of size resolution^2
		//would want res^3 in 3D
		pixels = new Pixel[resolution * resolution];
		pixelMaterials = new Material[pixels.Length]; //paint code

		dummyX = new Pixel();
		dummyY = new Pixel();
		dummyT = new Pixel();

		//edit for 3d
		//create pixels at all points in chunk
		for (int i = 0, y = 0; y < resolution; y++)
		{
			for (int x = 0; x < resolution; x++, i++)
			{
				CreatePixel(i, x, y);
			}
		}

		SetPixelColors(); //paint code

		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "PixelGrid Mesh";
		vertices = new List<Vector3>();
		triangles = new List<int>();
		Refresh();
	}

	//edit for 3d
	//creates a pixel at a given (x,y)
	private void CreatePixel(int i, int x, int y)
	{
		GameObject newPixel = Instantiate(pixelPrefab) as GameObject;
		newPixel.transform.parent = transform;
		newPixel.transform.localPosition = new Vector3((x + 0.5f) * pixelSize, (y + 0.5f) * pixelSize, -0.01f);
		newPixel.transform.localScale = Vector3.one * pixelSize * 0.1f;
		pixelMaterials[i] = newPixel.GetComponent<MeshRenderer>().material; //paintCode;
		pixels[i] = new Pixel(x, y, pixelSize);
	}

	//edit for 3d
	//sets a given pixel to some state
	public void SetPixel(int x, int y, bool state)
	{
		pixels[y * resolution + x].state = state;
		SetPixelColors();
	}

	//iterates over all pixels -- for a given state, set a given color
	private void SetPixelColors() //paint code
	{
		for (int i = 0; i < pixels.Length; i++)
		{
			pixelMaterials[i].color = pixels[i].state ? Color.black : Color.white;
		}
	}

	//update all pixels in this chunk
	public void Refresh()
	{
		SetPixelColors();
		Triangulate();
	}

	//edit for 3d
	private void Triangulate()
	{
		vertices.Clear();
		triangles.Clear();
		mesh.Clear();

		if (xNeighbor != null)
			{dummyX.BecomeXDummyOf(xNeighbor.pixels[0], gridSize);}
		if (yNeighbor != null)
			{TriangulateGapRow();}

		TriangulateCellRows();

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
	}

	//edit for 3d
	private void TriangulateCellRows()
	{
		int cells = resolution - 1;
		for (int i = 0, y = 0; y < cells; y++, i++)
		{
			for (int x = 0; x < cells; x++, i++)
			{
				TriangulateCell(
					pixels[i],
					pixels[i + 1],
					pixels[i + resolution],
					pixels[i + resolution + 1]);
			}
			if (xNeighbor != null)
			{
				TriangulateGapCell(i);
			}
		}
	}

	//edit for 3d
	private void TriangulateGapCell(int i)
	{
		Pixel dummySwap = dummyT;
		dummySwap.BecomeXDummyOf(xNeighbor.pixels[i + 1], gridSize);
		dummyT = dummyX;
		dummyX = dummySwap;
		TriangulateCell(pixels[i], dummyT, pixels[i + resolution], dummyX);
	}

	//edit for 3d
	private void TriangulateGapRow()
	{
		dummyY.BecomeYDummyOf(yNeighbor.pixels[0], gridSize);
		int cells = resolution - 1;
		int offset = cells * resolution;

		for (int x = 0; x < cells; x++)
		{
			Pixel dummySwap = dummyT;
			dummySwap.BecomeYDummyOf(yNeighbor.pixels[x + 1], gridSize);
			dummyT = dummyY;
			dummyY = dummySwap;
			TriangulateCell(pixels[x + offset], pixels[x + offset + 1], dummyT, dummyY);
		}

		if (xNeighbor != null)
		{
			dummyT.BecomeXYDummyOf(xyNeighbor.pixels[0], gridSize);
			TriangulateCell(pixels[pixels.Length - 1], dummyX, dummyY, dummyT);
		}
	}

	//edit for 3d
	private void TriangulateCell(Pixel a, Pixel b, Pixel c, Pixel d)
	{
		int cellType = 0;
		if (a.state)
		{
			cellType |= 1;
		}
		if (b.state)
		{
			cellType |= 2;
		}
		if (c.state)
		{
			cellType |= 4;
		}
		if (d.state)
		{
			cellType |= 8;
		}

		switch (cellType)
		{
			case 0:
				return;
			case 1:
				AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
				break;
			case 2:
				AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
				break;
			case 4:
				AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
				break;
			case 8:
				AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
				break;
			case 3:
				AddQuad(a.position, a.yEdgePosition, b.yEdgePosition, b.position);
				break;
			case 5:
				AddQuad(a.position, c.position, c.xEdgePosition, a.xEdgePosition);
				break;
			case 10:
				AddQuad(a.xEdgePosition, c.xEdgePosition, d.position, b.position);
				break;
			case 12:
				AddQuad(a.yEdgePosition, c.position, d.position, b.yEdgePosition);
				break;
			case 15:
				AddQuad(a.position, c.position, d.position, b.position);
				break;
			case 7:
				AddPentagon(a.position, c.position, c.xEdgePosition, b.yEdgePosition, b.position);
				break;
			case 11:
				AddPentagon(b.position, a.position, a.yEdgePosition, c.xEdgePosition, d.position);
				break;
			case 13:
				AddPentagon(c.position, d.position, b.yEdgePosition, a.xEdgePosition, a.position);
				break;
			case 14:
				AddPentagon(d.position, b.position, a.xEdgePosition, a.yEdgePosition, c.position);
				break;
			case 6:
				AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
				AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
				break;
			case 9:
				AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
				AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
				break;
		}
	}

	//edit for 3d
	private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	//edit for 3d
	private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		vertices.Add(d);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	//edit for 3d
	private void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		vertices.Add(d);
		vertices.Add(e);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 3);
		triangles.Add(vertexIndex + 4);
	}
}