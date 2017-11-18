Shader "Custom/MarchingCubes"
{
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		Cull Off
		
		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#include "UnityCG.cginc"

			#define PI 3.14159265359
			#define SAMPLE_VOLUME(position) (tex3Dlod(volume, float4((position), 0)))

			float deltaGradient;
			float isoLevel;
			float threshold;
			float3 resolution;
			float4 color;
			sampler3D volume;

			StructuredBuffer<int> trianglesLookupTable;

			inline float3 interpolateVertex(float isolevel, float3 p1, float3 p2, float valp1, float valp2)
			{
				return lerp(p1, p2, (isoLevel - valp1) / (valp2 - valp1));
			}

			struct v2g
			{
				float4 position : POSITION;
			};

			struct g2f
			{
				float4 position : POSITION;
				// used to compute gradient
				float3 normal : NORMAL;
			};

			v2g vert(appdata_base v)
			{
				v2g output;
				output.position = v.vertex;
				return output;
			}

			float3 compute_gradient(float3 position)
			{
				float3 gradient = float3(
					SAMPLE_VOLUME(position + float3(deltaGradient, 0, 0)).r - SAMPLE_VOLUME(position + float3(-deltaGradient, 0, 0)).r,
					SAMPLE_VOLUME(position + float3(0, deltaGradient, 0)).r - SAMPLE_VOLUME(position + float3(0, -deltaGradient, 0)).r,
					SAMPLE_VOLUME(position + float3(0, 0, deltaGradient)).r - SAMPLE_VOLUME(position + float3(0, 0, -deltaGradient)).r);

				// averaging gradient
				for (int j = 0; j < 4; ++j)
				{
					float f = j + 2;
					gradient += float3(
						SAMPLE_VOLUME(position + float3(deltaGradient * f, 0, 0)).r - SAMPLE_VOLUME(position + float3(-deltaGradient * f, 0, 0)).r,
						SAMPLE_VOLUME(position + float3(0, deltaGradient * f, 0)).r - SAMPLE_VOLUME(position + float3(0, -deltaGradient * f, 0)).r,
						SAMPLE_VOLUME(position + float3(0, 0, deltaGradient * f)).r - SAMPLE_VOLUME(position + float3(0, 0, -deltaGradient * f)).r);
				}
				return normalize(gradient);
			}

			[maxvertexcount(15)]
			void geom(point v2g p[1], inout TriangleStream<g2f> OutputStream)
			{
				float3 normalizedCellPosition = p[0].position.xyz;
				// TODO: could be cached
				float3 d = float3(1.0 / (resolution.x - 1.0), 1.0 / (resolution.y - 1.0), 1.0 / (resolution.z - 1.0));

				const float3 normalizedCellVertices[8] =
				{
					normalizedCellPosition + float3(0,   0,   0),
					normalizedCellPosition + float3(d.x,   0,   0),
					normalizedCellPosition + float3(d.x, d.y,   0),
					normalizedCellPosition + float3(0, d.y,   0),

					normalizedCellPosition + float3(0,   0, d.z),
					normalizedCellPosition + float3(d.x,   0, d.z),
					normalizedCellPosition + float3(d.x, d.y, d.z),
					normalizedCellPosition + float3(0, d.y, d.z),
				};

				float cellValues[8] =
				{
					SAMPLE_VOLUME(normalizedCellVertices[0]).r,
					SAMPLE_VOLUME(normalizedCellVertices[1]).r,
					SAMPLE_VOLUME(normalizedCellVertices[2]).r,
					SAMPLE_VOLUME(normalizedCellVertices[3]).r,
					SAMPLE_VOLUME(normalizedCellVertices[4]).r,
					SAMPLE_VOLUME(normalizedCellVertices[5]).r,
					SAMPLE_VOLUME(normalizedCellVertices[6]).r,
					SAMPLE_VOLUME(normalizedCellVertices[7]).r
				};

				float3 interpolatedVertices[12] =
				{
					interpolateVertex(isoLevel, normalizedCellVertices[0], normalizedCellVertices[1], cellValues[0], cellValues[1]),
					interpolateVertex(isoLevel, normalizedCellVertices[1], normalizedCellVertices[2], cellValues[1], cellValues[2]),
					interpolateVertex(isoLevel, normalizedCellVertices[2], normalizedCellVertices[3], cellValues[2], cellValues[3]),
					interpolateVertex(isoLevel, normalizedCellVertices[3], normalizedCellVertices[0], cellValues[3], cellValues[0]),
					interpolateVertex(isoLevel, normalizedCellVertices[4], normalizedCellVertices[5], cellValues[4], cellValues[5]),
					interpolateVertex(isoLevel, normalizedCellVertices[5], normalizedCellVertices[6], cellValues[5], cellValues[6]),
					interpolateVertex(isoLevel, normalizedCellVertices[6], normalizedCellVertices[7], cellValues[6], cellValues[7]),
					interpolateVertex(isoLevel, normalizedCellVertices[7], normalizedCellVertices[4], cellValues[7], cellValues[4]),
					interpolateVertex(isoLevel, normalizedCellVertices[0], normalizedCellVertices[4], cellValues[0], cellValues[4]),
					interpolateVertex(isoLevel, normalizedCellVertices[1], normalizedCellVertices[5], cellValues[1], cellValues[5]),
					interpolateVertex(isoLevel, normalizedCellVertices[2], normalizedCellVertices[6], cellValues[2], cellValues[6]),
					interpolateVertex(isoLevel, normalizedCellVertices[3], normalizedCellVertices[7], cellValues[3], cellValues[7])
				};

				int cubeIndex =
					(cellValues[7] < isoLevel) * 128 +
					(cellValues[6] < isoLevel) * 64 +
					(cellValues[5] < isoLevel) * 32 +
					(cellValues[4] < isoLevel) * 16 +
					(cellValues[3] < isoLevel) * 8 +
					(cellValues[2] < isoLevel) * 4 +
					(cellValues[1] < isoLevel) * 2 +
					(cellValues[0] < isoLevel) * 1;

				for (int i = 0; trianglesLookupTable[cubeIndex * 16 + i] != -1; i += 3)
				{
					float3 p0 = interpolatedVertices[trianglesLookupTable[cubeIndex * 16 + i]];
					float3 p1 = interpolatedVertices[trianglesLookupTable[cubeIndex * 16 + i + 1]];
					float3 p2 = interpolatedVertices[trianglesLookupTable[cubeIndex * 16 + i + 2]];

					// Compute gradient (and deduce normal)


					g2f a = (g2f)0;
					a.position = UnityObjectToClipPos(float4(p0, 1));
					a.normal = mul(unity_ObjectToWorld, float4(compute_gradient(p0), 0));
					OutputStream.Append(a);

					g2f b = (g2f)0;
					b.position = UnityObjectToClipPos(float4(p1, 1));
					b.normal = mul(unity_ObjectToWorld, float4(compute_gradient(p1), 0));
					OutputStream.Append(b);

					g2f c = (g2f)0;
					c.position = UnityObjectToClipPos(float4(p2, 1));
					c.normal = mul(unity_ObjectToWorld, float4(compute_gradient(p2), 0));
					OutputStream.Append(c);
				}
			}

			float4 frag(g2f i) : COLOR
			{
				// we expect directional light at the moment
				float diffuse = max(.0, dot(i.normal, normalize(float4(-_WorldSpaceLightPos0.xyz, 0))));
				float4 eyeDir = float4(_WorldSpaceCameraPos, 0) - i.position;
				float specular = .0 * pow(max(.0, dot(normalize(i.normal), -normalize(eyeDir))), 12);

				float ambiant = .5;

				float factor = ambiant + diffuse + specular;

				return color * factor;
			}
			ENDCG
		}
	}
}
