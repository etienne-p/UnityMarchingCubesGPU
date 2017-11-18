using UnityEngine;
using System.Collections;

public class MeshUtil
{
	public static Mesh CreateLatticeUnitVolume(uint xRes, uint yRes, uint zRes)
    {
        Mesh mesh = new Mesh(); 
        Vector3[] vertices = new Vector3[xRes * yRes * zRes];

        for (int z = 0; z != zRes; ++z)
        {
            for (int y = 0; y != yRes; ++y)
            {
                for (int x = 0; x != xRes; ++x)
                {
                    vertices[z * yRes * xRes + y * xRes + x] = new Vector3(
                        (float)x / (float)(xRes - 1),
                        (float)y / (float)(yRes - 1),
                        (float)z / (float)(zRes - 1)
					);
                }
            }
        }

        mesh.vertices = vertices;

        int[] indices = new int[vertices.Length];

        for (int i = 0; i != indices.Length; i++)
        {
            indices[i] = i;
        }

        mesh.SetIndices(indices, MeshTopology.Points, 0);

        return mesh;
    } 
}
