using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{
	public GameObject voxelPrefab;
	public int resolution;
	public int size;
	public float threshold;
	private int resSqr;

	private static int [][] triTable = Lookup.triangulation;
	private static Vector3[] edgeTable = Lookup.edges;
	public Voxel[] voxels;
	private float voxelSize, gridSize, vHSize;
	private List<Vector3> vertices;
	private List<int> triangles;
	private Material[] voxelMaterials;
	private Mesh mesh;

	//this function takes some resolution (how many cubes per chunk), and some size (the size of the cubes)
	public void Initialize(int resolution, float size, float thresh)
	{
		this.resolution = resolution;
		this.resSqr = (resolution * resolution);
		this.threshold = thresh;
		voxelSize = size / resolution;
		vHSize = voxelSize / 2;
		gridSize = size;

		voxels = new Voxel[resolution * resSqr];
		voxelMaterials = new Material[voxels.Length]; //paint code

		//create voxels at all points in chunk
		for (int i = 0, z = 0; z < resolution; z++) {
			for (int y = 0; y < resolution; y++) {
				for (int x = 0; x < resolution; x++, i++) {
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
		//all of this is just for visualization
		GameObject newVoxel = Instantiate(voxelPrefab) as GameObject; //newVoxel is a 3D Voxel object. These exist purely for visualization.
		newVoxel.transform.parent = transform;
		newVoxel.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
		newVoxel.transform.localScale = Vector3.one * voxelSize * 0.1f;
		voxelMaterials[i] = newVoxel.GetComponent<MeshRenderer>().material; //paintCode;
		
		//this is where the magic happens
		voxels[i] = new Voxel(new Vector3(x, y, z), voxelSize);
	}

	//sets a given voxel to some state
	public void SetVoxel(int x, int y, int z, float input)
	{
		voxels[z * resSqr + y * resolution + x].value += input;
		SetVoxelColors();
	}

	//iterates over all voxels -- for a given state, set a given color
	private void SetVoxelColors() //paint code
	{
		for (int i = 0; i < voxels.Length; i++) {
			voxelMaterials[i].color = selectColor(voxels[i].value);
		}
	}

	//update all voxels in this chunk
	public void Refresh()
	{
		SetVoxelColors();
		Triangulate();
	}

	private void Triangulate()
	{
		vertices.Clear();
		triangles.Clear();
		mesh.Clear();

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
	private void TriangulateCellRows() {
		int cells = resolution - 1;
		for (int i = 0, z = 0; z < cells; z++) {
			for (int y = 0; y < cells; y++) {
				for (int x = 0; x < cells; x++, i++) {
					Voxel[] passArr = genDefaultVerts(x + y*resolution + z*resSqr);
					Debug.Log("checking cell # " + i + ", pos= " + passArr[0].position);
					TriangulateCell(passArr);
				}
			}
		}
	}

	private void TriangulateCell(Voxel[] CubeVerts)
	{
		int cellType = 0;
		for (int i = 0, t = 1; i < CubeVerts.Length; i++, t = t * 2) {
			if (CubeVerts[i].value > threshold)
			{
				if (i == 2 || i == 6)
					cellType |= t * 2;
				else if (i == 3 || i == 7)
					cellType |= t / 2;
				else
					cellType |= t;
			}
			//Debug.Log(i + ", is " + CubeVerts[i].state + "cell, " + cellType);
		}

		Vector3 v0pos = CubeVerts[0].voxT;
		int[] stitchEdges = triTable[cellType];
		//if (cellType > 0) Debug.Log("tri, " + cellType + "orig, " + v0pos);
		//if (cellType > 0) ArrLog(stitchEdges);
		for (int i = 0; i < 15; i+=3) {
			if (stitchEdges[i] < 0) {
				i = 16; break;
			}
			Vector3 edge0 = edgeTable[stitchEdges[i]];
			Vector3 edge1 = edgeTable[stitchEdges[i+1]];
			Vector3 edge2 = edgeTable[stitchEdges[i+2]];
			AddTriangle(v0pos + vHSize * edge0, v0pos + vHSize * edge1, v0pos + vHSize * edge2);
		}
	}

	private void ArrLog<T>(T[] arr)
	{
		string str = "";
		foreach (var elt in arr)
		{
			str += elt;
			str += ", ";
			
		}
		Debug.Log(str);
	}

	private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(c);
		vertices.Add(b);
		vertices.Add(a);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);

		//not sure if next part is necessary -- fairly certain, though
		vertexIndex = vertices.Count;
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	private Color selectColor(float input)
	{
		return new Color(input, input, input);
	}
}