﻿using System.Collections;
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
	public ComputeShader densityShader;
	private ComputeBuffer pointsBuffer;
	private ComputeBuffer triCountBuffer;
	private ComputeBuffer triangleBuffer;

	private int numPoints;
	private int maxTriangleCount;
	private int resSqr;
	private float voxelSize, vHSize;
	private static int[][] triTable = Lookup.triangulation;
	private static Vector3[] edgeTable = Lookup.edges;
	private List<Vector3> vertices;
	private List<int> triangles;
	private MeshCollider theMeshCollider;
	private Mesh mesh;

	private System.Diagnostics.Stopwatch st;
	//this function takes some resolution (how many cubes per chunk), and some size (the size of the cubes)
	public void Initialize(int res, float size, float thresh)
	{
		st = new System.Diagnostics.Stopwatch();
		vertices = new List<Vector3>();
		triangles = new List<int>();

		resolution = res;
		threshold = thresh;

		resSqr = (resolution * resolution);
		voxelSize = size / resolution;
		vHSize = voxelSize / 2;

		theMeshCollider = GetComponent<MeshCollider>();
		theMeshCollider.sharedMesh = null;
		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "VoxelGrid Mesh";

		BuildBuffers();

		//create voxels at all points in chunk -- this ought to be converted to a shader
		st.Start();
		voxels = new Vector4[resolution * resSqr];
		float newVal, currMin = 0, currMax = 0;
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
		st.Stop();
		Debug.Log(string.Format("voxels took {0} ms to complete", st.ElapsedMilliseconds));
		st.Reset();

		//do the actual marching
		st.Start();
		Refresh();
		st.Stop();
		Debug.Log(string.Format("tris took {0} ms to complete", st.ElapsedMilliseconds));
		st.Reset();
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
	
	void BuildBuffers()
	{
		numPoints = resolution * resSqr;
		maxTriangleCount = numPoints * 5 * 2;
		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
		pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
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
		densityShader.SetBuffer(0, "voxels", pointsBuffer);
		densityShader.SetInt("resolution", resolution);
		densityShader.SetFloat("tfx", transform.localPosition.x);
		densityShader.SetFloat("tfy", transform.localPosition.y);
		densityShader.SetFloat("tfz", transform.localPosition.z);
		densityShader.SetFloat("vS", voxelSize);
		densityShader.Dispatch(0, resolution, resolution, resolution);

		//pointsBuffer.SetData(voxels, 0, 0, numPoints);

		triangleBuffer.SetCounterValue(0);
		shader.SetBuffer(0, "voxels", pointsBuffer);
		shader.SetBuffer(0, "tris", triangleBuffer);
		shader.SetInt("resolution", resolution);
		shader.SetFloat("isoLevel", threshold);
		shader.SetFloat("vHSize", vHSize);

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