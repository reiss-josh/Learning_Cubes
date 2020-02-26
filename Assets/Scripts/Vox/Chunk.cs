using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Chunk : MonoBehaviour
{
	public int resolution, size;
	public float threshold;
	public float[] vValues;
	public Vector3[] vCoords;

	private int resSqr;
	private float voxelSize, vHSize;
	private static int[][] triTable = Lookup.triangulation;
	private static Vector3[] edgeTable = Lookup.edges;
	private List<Vector3> vertices;
	private List<int> triangles;
	private MeshCollider theMeshCollider;
	private Mesh mesh;
	
	//this function takes some resolution (how many cubes per chunk), and some size (the size of the cubes)
	public void Initialize(int res, float size, float thresh)
	{
		resolution = res;
		threshold = thresh;

		resSqr = (resolution * resolution);
		voxelSize = size / resolution;
		vHSize = voxelSize / 2;

		theMeshCollider = GetComponent<MeshCollider>();
		theMeshCollider.sharedMesh = null;

		vCoords = new Vector3[resolution * resSqr];
		vValues = new float[resolution * resSqr];
		float newVal, currMin = 0, currMax = 0;

		//create voxels at all points in chunk
		for (int i = 0, z = 0; z < resolution; z++) {
			for (int y = 0; y < resolution; y++) {
				for (int x = 0; x < resolution; x++, i++) {
					vCoords[i] = new Vector3(x+0.5f, y+0.5f, z+0.5f)*voxelSize;
					newVal = Density.Sample(vCoords[i] + transform.localPosition);
					vValues[i] = newVal;
					if (newVal < currMin) currMin = newVal;
					else if (newVal > currMax) currMax = newVal;
				}
			}
		}
		//Debug.Log("("+currMin + ", " + currMax+")");

		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "VoxelGrid Mesh";
		vertices = new List<Vector3>();
		triangles = new List<int>();
		Refresh();
	}

	//update all voxels in this chunk
	public void Refresh()
	{
		theMeshCollider.sharedMesh = null;
		Triangulate();
		theMeshCollider.sharedMesh = mesh;
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

	private int[] genDefaultVerts(int i)
	{
		return new int[8] {
			i, //0
			i + 1, //1
			i + resolution, //2
			i + resolution + 1, //3
			i + resSqr, //4
			i + resSqr + 1, //5
			i + resSqr + resolution, //6
			i + resSqr + resolution + 1  //7
		};
	}

	private void TriangulateCellRows() {
		int cells = resolution-1;
		for (int i = 0, z = 0; z < cells; z++) {
			for (int y = 0; y < cells; y++) {
				for (int x = 0; x < cells; x++, i++) {
					int[] passArr = genDefaultVerts(x + y*resolution + z*resSqr);
					TriangulateCell(passArr);
				}
			}
		}
	}

	private void TriangulateCell(int[] CubeVerts)
	{
		int cellType = 0;
		for (int i = 0, t = 1; i < CubeVerts.Length; i++, t = t * 2) {
			if (vValues[CubeVerts[i]] > threshold)
			{
				if (i == 2 || i == 6)
					cellType |= t * 2;
				else if (i == 3 || i == 7)
					cellType |= t / 2;
				else
					cellType |= t;
			}
		}

		Vector3 v0pos = vCoords[CubeVerts[0]];
		int[] stitchEdges = triTable[cellType];
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
}