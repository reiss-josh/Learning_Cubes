using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMap : MonoBehaviour
{
	public float threshold = 0.5f;	//threshold for points in space
	public float voxelSize = 2f;			//size of a voxel across each axis
	public int voxelResolution = 8; //num voxels per chunk, per axis
	public int chunkResolution = 2; //num chunks per axis
	public Chunk chunkPrefab;       //prefab for chunk

	public ComputeShader shader;			//shader??
	public ComputeShader densityShader;		//density shader -- generates "w" for each voxel
	private ComputeBuffer pointsBuffer;		//stores all points in space
	private ComputeBuffer triCountBuffer;	//keeps count of number of tris in a chunk
	private ComputeBuffer triangleBuffer;   //stores actual tris in a chunk

	private int numPoints;                  //stores number of points
	private int maxTriangleCount;           //determines max # of triangles in a chunk
	private int chunkResSqr, voxelResSqr;   //res^2
	private static int[][] triTable = Lookup.triangulation;
	private static Vector3[] edgeTable = Lookup.edges;

	private Chunk[] chunks;	//array of chunks
	private float chunkSize, voxelHalfSize;

	private System.Diagnostics.Stopwatch st;
	private System.Diagnostics.Stopwatch st2;
	//setup variables for chunk generation
	private void Awake()
	{
		st = new System.Diagnostics.Stopwatch();
		st2 = new System.Diagnostics.Stopwatch();
		chunkResSqr = chunkResolution * chunkResolution;
		voxelResSqr = voxelResolution * voxelResolution;
		voxelHalfSize = voxelSize * 0.5f;	 //used to find midpoint of a chunk
		chunkSize = voxelSize * voxelResolution;      //size of a chunk, per axis
		Debug.Log(chunkSize);
		Debug.Log(voxelSize);
		chunks = new Chunk[chunkResolution * chunkResolution * chunkResolution];

		GenerateChunks();
	}

	//create all chunks in mesh
	private void GenerateChunks()
	{
		float sumGood = 0;
		float sumGarb = 0;
		//i is current chunk ID
		//x,y,z is integer chunk position in world mesh
		for (int i = 0, z = 0; z < chunkResolution; z++) {
			for (int y = 0; y < chunkResolution; y++) {
				for (int x = 0; x < chunkResolution; x++, i++) {
					st2.Start();
					CreateChunk(i, x, y, z);
					st2.Stop();
					sumGood += st2.ElapsedMilliseconds;
					sumGarb += st.ElapsedMilliseconds;
					st2.Reset();
					st.Reset();
				}
			}
		}
		sumGood -= sumGarb;
		Debug.Log(string.Format("voxels took {0} ms to complete", sumGood));
		Debug.Log(string.Format("garbage took {0} ms to complete", sumGarb));
	}

	//generate a single chunk
	//takes as input a chunk id and x,y,z position in world mesh
	private void CreateChunk(int i, int x, int y, int z)
	{
		Chunk chunk = Instantiate(chunkPrefab) as Chunk;
		chunks[i] = chunk;

		chunk.transform.parent = transform;
		chunk.transform.position = new Vector3(x*chunkSize-x*voxelSize, y*chunkSize-y*voxelSize, z*chunkSize-z*voxelSize); //placeholder -- assumes only one chunk

		chunk.Initialize(x,y,z);
		Triangulate(i);
	}

	private int chunkIDFromCoord(int x, int y, int z)
	{
		return x + (y * chunkResolution) + (z * chunkResSqr);
	}

	private void Triangulate(int iD)
	{
		Chunk currChunk = chunks[iD];
		currChunk.chunkMeshCollider.sharedMesh = null;
		BuildBuffers();
		densityShader.SetBuffer(0, "voxels", pointsBuffer);
		densityShader.SetInt("voxRes", voxelResolution);
		//densityShader.SetFloat("voxMid", voxelResolution * 0.5f); //EXPECTED TO BE AN INTEGER
		//densityShader.SetFloat("chunkMid", chunkResolution * 0.5f); //EXPECTED TO BE AN INTEGER
		densityShader.SetFloat("tfx", currChunk.transform.position.x*10);
		densityShader.SetFloat("tfy", currChunk.transform.position.y*10);
		densityShader.SetFloat("tfz", currChunk.transform.position.z*10);
		densityShader.SetFloat("vS", voxelSize * 10);
		densityShader.Dispatch(0, voxelResolution, voxelResolution, voxelResolution);

		st.Start();
		//look inside the points!
		Vector4[] beep = new Vector4[voxelResolution * voxelResSqr];
		pointsBuffer.GetData(beep);

		for (int i = 0; i < beep.Length; i++)
		{
			Debug.Log(beep[i].w);
			var newVal = Density.Sample(ToVector3(beep[i])/100+currChunk.transform.localPosition/100);
			beep[i].w = newVal;
			//Debug.Log(newVal);

			//GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			//cube.transform.position = ToVector3(beep[i]);
			//cube.transform.localScale = new Vector3(voxelSize*0.5f, voxelSize*0.5f, voxelSize*0.5f);
		}
		pointsBuffer.SetData(beep, 0, 0, numPoints);
		st.Stop();
		

		triangleBuffer.SetCounterValue(0);
		shader.SetBuffer(0, "voxels", pointsBuffer);
		shader.SetBuffer(0, "tris", triangleBuffer);
		shader.SetInt("resolution", voxelResolution);
		shader.SetFloat("isoLevel", threshold);
		shader.SetFloat("vHSize", voxelHalfSize);

		shader.Dispatch(0, voxelResolution, voxelResolution, voxelResolution);

		// Get number of triangles in the triangle buffer
		ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
		int[] triCountArray = { 0 };
		triCountBuffer.GetData(triCountArray);
		int numTris = triCountArray[0];

		// Get triangle data from shader
		Triangle[] tris = new Triangle[numTris];
		triangleBuffer.GetData(tris, 0, 0, numTris);

		currChunk.chunkMesh.Clear();

		var vertices = new Vector3[numTris * 3];
		var chunkMeshTriangles = new int[numTris * 3];

		for (int i = 0; i < numTris; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				chunkMeshTriangles[i * 3 + j] = i * 3 + j;
				vertices[i * 3 + j] = tris[i][j];
			}
		}
		currChunk.chunkMesh.vertices = vertices;
		currChunk.chunkMesh.triangles = chunkMeshTriangles;

		currChunk.chunkMesh.RecalculateNormals();
		currChunk.chunkMeshCollider.sharedMesh = currChunk.chunkMesh;
		ReleaseBuffers();
	}

	public static Vector3 ToVector3(Vector4 parent)
	{
		return new Vector3(parent.x, parent.y, parent.z);
	}

	void BuildBuffers()
	{
		numPoints = voxelResolution * voxelResSqr;
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

	struct Triangle
	{
		#pragma warning disable 649 // disable unassigned variable warning
		public Vector3 a; //position of first vert
		public Vector3 b;
		public Vector3 c;
		public Vector3 this[int i] {
			get {
				switch (i) {
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
}