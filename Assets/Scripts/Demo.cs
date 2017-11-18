using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarchingCubesGPU))]
public class Demo : MonoBehaviour 
{
	[SerializeField] int volumetricDataWidth;
	[SerializeField] int volumetricDataHeight;
	[SerializeField] int volumetricDataDepth;
	[SerializeField] float isoMin;
	[SerializeField] float isoMax;
	[SerializeField] float animationSpeed;
	Texture3D volumetricData;
	MarchingCubesGPU marchingCubesGPU;

	void OnEnable()
	{
		marchingCubesGPU = GetComponent<MarchingCubesGPU> ();
		CheckVolumetricData ();
	}

	void OnValidate()
	{
		marchingCubesGPU = GetComponent<MarchingCubesGPU> ();
		CheckVolumetricData ();
	}

	void Update()
	{
		marchingCubesGPU.IsoLevel = Mathf.Lerp(isoMin, isoMax, Mathf.Repeat (Time.time * animationSpeed, 1.0f));
	}

	void CheckVolumetricData()
	{
		volumetricDataWidth = (int)Mathf.Clamp (volumetricDataWidth, 2, 256);
		volumetricDataHeight = (int)Mathf.Clamp (volumetricDataHeight, 2, 256);
		volumetricDataDepth = (int)Mathf.Clamp (volumetricDataDepth, 2, 256);

		if (
				volumetricData == null ||
				volumetricData.width != volumetricDataWidth ||
				volumetricData.height != volumetricDataHeight ||
				volumetricData.depth != volumetricDataDepth
		) {
			ReleaseVolumetricData ();
			volumetricData = CreateVolumePerlin (volumetricDataWidth, volumetricDataHeight, volumetricDataDepth);
			marchingCubesGPU.VolumetricData = volumetricData;
		}
	}

	void OnDestroy()
	{
		ReleaseVolumetricData ();
	}

	void ReleaseVolumetricData()
	{
		if (volumetricData != null)
		{
			marchingCubesGPU.VolumetricData = null;
			Texture3D.DestroyImmediate(volumetricData);
		}
	}

	static Texture3D CreateVolumePerlin(int width, int height, int depth)
	{
		Texture3D t3d = new Texture3D(width, height, depth, TextureFormat.RGB24, true);
		Color[] colors = new Color[width * height * depth];
		for (int z = 0; z < depth; ++z)
		{
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					Vector3 position = new Vector3(
						(float)x / (float)(width - 1),
						(float)y / (float)(height - 1),
						(float)z / (float)(depth - 1));
					float dCenter = (position - new Vector3(.5f, .5f, .5f)).magnitude;
					const float noiseScale = 4.0f;
					position *= noiseScale;
					position += Vector3.one * Time.time * .1f;
					float n0 = Mathf.PerlinNoise(position.x, position.y);
					float n1 = Mathf.PerlinNoise(position.y, position.z);
					float v = n0 * n1;
					colors[z * width * height + y * width + x] = Color.white * v;
				}
			}
		}
		t3d.SetPixels(colors);
		t3d.Apply();
		return t3d;
	}
}
