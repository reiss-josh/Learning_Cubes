using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{
	public GameObject voxelPrefab;
	public VoxelGrid xNeighbor, yNeighbor, zNeighbor, xyNeighbor, xzNeighbor, yzNeighbor, xyzNeighbor;
	public int resolution;
	private int resSqr;

	private static int [,] triTable = Lookup.triangulation;
	private Voxel[] voxels;
	private float voxelSize, gridSize;
	private List<Vector3> vertices;
	private List<int> triangles;
	private Voxel dummyX, dummyY, dummyZ, dummyT;
	private Material[] voxelMaterials;
	private Mesh mesh;

	//this function takes some resolution (how many cubes per chunk), and some size (the size of the cubes)
	public void Initialize(int resolution, float size)
	{
		this.resolution = resolution;
		this.resSqr = (resolution * resolution);
		voxelSize = size / resolution;
		gridSize = size;

		voxels = new Voxel[resolution * resSqr];
		voxelMaterials = new Material[voxels.Length]; //paint code

		dummyX = new Voxel();
		dummyY = new Voxel();
		dummyZ = new Voxel();
		dummyT = new Voxel();

		//create voxels at all points in chunk
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

		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "VoxelGrid Mesh";
		vertices = new List<Vector3>();
		triangles = new List<int>();
		Refresh();
	}

	//creates a voxel at a given (x,y,z)
	private void CreateVoxel(int i, int x, int y, int z)
	{
		GameObject newVoxel = Instantiate(voxelPrefab) as GameObject;
		newVoxel.transform.parent = transform;
		newVoxel.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
		newVoxel.transform.localScale = Vector3.one * voxelSize * 0.1f;
		voxelMaterials[i] = newVoxel.GetComponent<MeshRenderer>().material; //paintCode;
		voxels[i] = new Voxel(x, y, z, voxelSize);
	}

	//sets a given voxel to some state
	public void SetVoxel(int x, int y, int z, bool state)
	{
		voxels[z * resSqr + y * resolution + x].state = state;
		SetVoxelColors();
	}

	//iterates over all voxels -- for a given state, set a given color
	private void SetVoxelColors() //paint code
	{
		for (int i = 0; i < voxels.Length; i++)
		{
			voxelMaterials[i].color = voxels[i].state ? Color.black : Color.white;
		}
	}

	//update all voxels in this chunk
	public void Refresh()
	{
		SetVoxelColors();
		Triangulate();
	}

	//edit for 3d
	private void Triangulate()
	{
		vertices.Clear();
		triangles.Clear();
		mesh.Clear();

		/* handle chunk gaps
		 * 
		if (xNeighbor != null)
			{ dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize); }
		if (yNeighbor != null)
			{ TriangulateGapRow(); }
		if (zNeighbor != null)
			{
				//do something here;
			}
		*/
		
		TriangulateCellRows();

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
	}

	private Voxel[] genDefaultVerts(int i)
	{
		return new Voxel[] {voxels[i], //0
							voxels[i + 1], //1
							voxels[i + resolution], //2
							voxels[i + resolution + 1], //3
							voxels[i + resSqr], //4
							voxels[i + resSqr + 1], //5
							voxels[i + resSqr + resolution], //6
							voxels[i + resSqr + resolution + 1]}; //7
	}

	//edit for 3d
	private void TriangulateCellRows()
	{
		int cells = resolution - 1;
		for (int i = 0, y = 0; y < cells; y++, i++)
		{
			Voxel[] passArr = genDefaultVerts(i);
			for (int x = 0; x < cells; x++, i++)
			{
				TriangulateCell(passArr);
			}
			if (xNeighbor != null)
			{
				//handle chunk gaps
				//TriangulateGapCell(i, passArr);
			}
		}
	}

	/* handle chunk gaps -- edit for 3d

	private void TriangulateGapCell(int i, Voxel[] passArr)
	{
		Voxel dummySwap = dummyT;
		dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
		dummyT = dummyX;
		dummyX = dummySwap;
		passArr[1] = dummyT;
		passArr[3] = dummyX;
		//prob need to do something to 4-7
		TriangulateCell(passArr);
	}

	//edit for 3d
	private void TriangulateGapRow()
	{
		dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
		int cells = resolution - 1;
		int offset = cells * resolution;

		for (int x = 0; x < cells; x++)
		{
			Voxel dummySwap = dummyT;
			dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
			dummyT = dummyY;
			dummyY = dummySwap;
			Voxel[] passArr = genDefaultVerts(x + offset);
			passArr[2] = dummyT;
			passArr[3] = dummyY;
			// prob need to do something to 4-7
			TriangulateCell(passArr);
		}

		if (xNeighbor != null)
		{
			dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
			Voxel[] passArr = genDefaultVerts(voxels.Length - 1);
			passArr[1] = dummyX;
			passArr[2] = dummyY;
			passArr[3] = dummyT;
			TriangulateCell(passArr);
		}
	}
	*/

	//edit for 3d
	private void TriangulateCell(Voxel[] CubeVerts)
	{
		Voxel a = CubeVerts[0];
		Voxel b = CubeVerts[1];
		Voxel c = CubeVerts[2];
		Voxel d = CubeVerts[3];
	
		int cellType = 0;
		for(int i = 0, t = 1; i < CubeVerts.Length; i++, t = t*2)
		{
			if (CubeVerts[i].state)
				cellType |= t;
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