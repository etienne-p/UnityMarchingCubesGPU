using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class MarchingCubesGPU : MonoBehaviour
{
	[SerializeField, Range(0, 1.0f)] float isoLevel = .5f;
	[SerializeField, Range(0, .2f)] float deltaGradient = .05f;
	[SerializeField, Range(0, 1.0f)] float threshold = .5f;
	[SerializeField] Vector3 resolution;
	[SerializeField] Shader shader;
	[SerializeField] Color color;

    // this buffer will hold flattened MarchingCubesLookupTables.TRIANGLES
    static ComputeBuffer trianglesLookupTableBuffer;
	// track resolution to regenerate the mesh if needed
    Vector3 latticeResolution = Vector3.zero;
	Material material;

    void OnValidate()
    {
		Check ();
    }

    void OnEnable()
    {
        Check();
    }

    void OnDestroy()
    {
		Dispose ();
    }

	// Exposed for demonstration animation
	public float IsoLevel
	{
		set { 
			isoLevel = Mathf.Clamp01 (value); 
			SetUniforms ();
		}
	}

	public Texture3D VolumetricData
	{
		set 
		{
			CheckMaterial ();
			material.SetTexture("volume", value);
		} 
	}

	[ContextMenu("Force Reset")]
	void ForceReset()
	{
		latticeResolution = Vector3.zero;
		Dispose (true);
		Check ();
	}

	void Dispose(bool forceLookupDispose = false)
	{
		if (material != null) 
		{
			Material.DestroyImmediate (material);
			material = null;
		}
		CheckDisposeLookupTable (forceLookupDispose);
	}

	void CheckMesh()
	{
		if (resolution != latticeResolution)
		{
			latticeResolution = resolution;
			GetComponent<MeshFilter>().mesh = MeshUtil.CreateLatticeUnitVolume(
				(uint)resolution.x, (uint)resolution.y, (uint)resolution.z);
		}
	}

	void SetUniforms()
	{
		material.SetFloat("isoLevel", isoLevel);
		material.SetFloat("threshold", threshold);
		material.SetFloat("deltaGradient", deltaGradient);
		material.SetVector("resolution", resolution);
		material.SetColor("color", color);
	}

    void CheckMaterial()
    {
		if (material == null)
		{
        	material = new Material(shader);
       		material.hideFlags = HideFlags.HideAndDontSave;
        	GetComponent<MeshRenderer>().material = material;
		}
    }

	void Check()
	{
		isoLevel = Mathf.Clamp01(isoLevel);
		resolution.x = Mathf.Clamp(Mathf.Floor(resolution.x), 4, 64);
		resolution.y = Mathf.Clamp(Mathf.Floor(resolution.y), 4, 64);
		resolution.z = Mathf.Clamp(Mathf.Floor(resolution.z), 4, 64);

		CheckMesh ();
		CheckMaterial ();
		CheckUploadLookupTable ();
		SetUniforms ();
	}

    static void CheckUploadLookupTable()
    {
		if (trianglesLookupTableBuffer == null) 
		{
			// we "flatten" MarchingCubesLookupTables.TRIANGLES
			int nTri = MarchingCubesLookupTables.TRIANGLES.GetLength (0) * MarchingCubesLookupTables.TRIANGLES.GetLength (1);
			int[] trianglesLookupTable = new int[nTri];
			for (int i = 0; i < MarchingCubesLookupTables.TRIANGLES.GetLength (0); ++i) {
				for (int j = 0; j < MarchingCubesLookupTables.TRIANGLES.GetLength (1); ++j) {
					trianglesLookupTable [i * MarchingCubesLookupTables.TRIANGLES.GetLength (1) + j] = MarchingCubesLookupTables.TRIANGLES [i, j];
				}
			}
			trianglesLookupTableBuffer = new ComputeBuffer (trianglesLookupTable.Length, Marshal.SizeOf (typeof(int)));
			trianglesLookupTableBuffer.SetData (trianglesLookupTable);
			Shader.SetGlobalBuffer ("trianglesLookupTable", trianglesLookupTableBuffer);
		}
    }

	static void CheckDisposeLookupTable(bool force = false)
	{
		if (force || Object.FindObjectsOfType<MarchingCubesGPU> ().Length == 1) 
		{
			if (trianglesLookupTableBuffer != null) 
			{
				trianglesLookupTableBuffer.Dispose ();
				trianglesLookupTableBuffer = null;
			}
		}
	}
}
