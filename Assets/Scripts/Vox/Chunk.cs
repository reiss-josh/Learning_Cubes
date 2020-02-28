using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Chunk : MonoBehaviour
{
	struct Triangle
	{
#pragma warning disable 649 // disable unassigned variable warning
		public Vector3 a;
		public Vector3 b;
		public Vector3 c;

		public Vector3 this[int i]
		{
			get
			{
				switch (i)
				{
					case 0:
						return a;
					case 1:
						return b;
					default:
						return c;
				}
			}
		}
	}


	public int resolution, size;
	public float threshold;
	public Vector4[] voxels;
	public ComputeShader shader;
	private ComputeBuffer pointsBuffer;
	private ComputeBuffer triCountBuffer;
	private ComputeBuffer triangleBuffer;

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

		voxels = new Vector4[resolution * resSqr];
		float newVal, currMin = 0, currMax = 0;

		//create voxels at all points in chunk
		int i = 0;
		for (int z = 0; z < resolution; z++) {
			for (int y = 0; y < resolution; y++) {
				for (int x = 0; x < resolution; x++, i++) {
					voxels[i] = new Vector4(x+0.5f, y+0.5f, z+0.5f,0)*voxelSize;
					newVal = Density.Sample(ToVector3(voxels[i]) + transform.localPosition);
					voxels[i].w = newVal;
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

	void OnDestroy()
	{
		if (Application.isPlaying)
		{
			ReleaseBuffers();
		}
	}

	void ReleaseBuffers()
	{
		if (triangleBuffer != null)
		{
			triangleBuffer.Release();
			pointsBuffer.Release();
			triCountBuffer.Release();
		}
	}

	private void Triangulate()
	{

		//TriangulateCellRows();
		int numPoints = voxels.Length;
		int maxTriangleCount = numPoints * 5;
		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
		pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

		pointsBuffer.SetData(voxels, 0, 0, numPoints);

		triangleBuffer.SetCounterValue(0);
		shader.SetBuffer(0, "voxels", pointsBuffer);
		shader.SetBuffer(0, "tris", triangleBuffer);
		shader.SetInt("numPointsPerAxis", resolution);
		shader.SetFloat("isoLevel", threshold);

		shader.Dispatch(0, resolution, resolution, resolution);

		// Get number of triangles in the triangle buffer
		ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
		int[] triCountArray = { 0 };
		triCountBuffer.GetData(triCountArray);
		int numTris = triCountArray[0];

		// Get triangle data from shader
		Triangle[] tris = new Triangle[numTris];
		triangleBuffer.GetData(tris, 0, 0, numTris);

		mesh.Clear();

		var vertices = new Vector3[numTris * 3];
		var meshTriangles = new int[numTris * 3];

		for (int i = 0; i < numTris; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				meshTriangles[i * 3 + j] = i * 3 + j;
				vertices[i * 3 + j] = tris[i][j];
			}
		}
		mesh.vertices = vertices;
		mesh.triangles = meshTriangles;

		mesh.RecalculateNormals();
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
			if (voxels[CubeVerts[i]].w > threshold)
			{
				if (i == 2 || i == 6)
					cellType |= t * 2;
				else if (i == 3 || i == 7)
					cellType |= t / 2;
				else
					cellType |= t;
			}
		}

		Vector3 v0pos = ToVector3(voxels[CubeVerts[0]]);
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

		triangles.Add(vertexIndex + 0);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);

		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 0);
	}

	public static Vector3 ToVector3(Vector4 parent)
	{
		return new Vector3(parent.x, parent.y, parent.z);
	}
}