Shader "Voronoi"
{
    Properties
    {
        [NoScaleOffset] _AlbedoTexture("AlbedoTexture", 2D) = "white" {}
        _AlbedoColor("AlbedoColor", Color) = (1, 1, 1, 1)
        _Opacity("Opacity", Float) = 1
        _Offset("Offset", Float) = 0.5
    }

        HLSLINCLUDE
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"
#define SHADERGRAPH_PREVIEW 1

            CBUFFER_START(UnityPerMaterial)
            float4 _AlbedoColor;
        float _Opacity;
        float _Offset;
        CBUFFER_END
            TEXTURE2D(_AlbedoTexture); SAMPLER(sampler_AlbedoTexture); float4 _AlbedoTexture_TexelSize;

        struct SurfaceDescriptionInputs
        {
            half4 uv0;
        };



        inline float2 Unity_Voronoi_RandomVector_float(float2 UV, float offset)
        {
            
            return 
        }

        void VorFunc(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
        {
            float2 g = floor(UV * CellDensity);
            float2 f = frac(UV * CellDensity);
            float2 res = float2(8.0, 8.0);
            float2 ml = float2(0, 0);
            float2 mv = float2(0, 0);

            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++) {
                    float2 lattice = float2(x, y);

                    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
                    float2 tempUV = frac(sin(mul((lattice + g), m)) * 46839.32);
                    float2 offset = float2(sin(tempUV.y * +AngleOffset) * 0.5 + 0.5, cos(tempUV.x * AngleOffset) * 0.5 + 0.5);

                    float2 v = lattice + offset - f;
                    float d = dot(v, v);

                    if (d < res.x) {
                        res.x = d;
                        res.y = offset.x;
                        mv = v;
                        ml = lattice;
                    }
                }
            }

            Cells = res.y;

            res = float2(8.0, 8.0);
            for (int y = -2; y <= 2; y++) {
                for (int x = -2; x <= 2; x++) {
                    float2 lattice = ml + float2(x, y);
                    
                    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
                    float2 tempUV = frac(sin(mul((lattice + g), m)) * 46839.32);
                    float2 offset = float2(sin(tempUV.y * +AngleOffset) * 0.5 + 0.5, cos(tempUV.x * AngleOffset) * 0.5 + 0.5);

                    float2 v = lattice + offset - f;

                    float2 cellDifference = abs(ml - lattice);
                    if (cellDifference.x + cellDifference.y > 0.1) {
                        float d = dot(0.5 * (mv + v), normalize(v - mv));
                        res.x = min(res.x, d);
                    }
                }
            }

            Out = res.x;
        }

        struct SurfaceDescription
        {
            float Out_3;
        };

        SurfaceDescription PopulateSurfaceData(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Voronoi_F4DAABA6_Out_3;
            float _Voronoi_F4DAABA6_Cells_4;
            Unity_Voronoi_float(IN.uv0.xy, 2, 5, _Voronoi_F4DAABA6_Out_3, _Voronoi_F4DAABA6_Cells_4);
            surface.Out_3 = 0;
            return surface;
        }

        struct GraphVertexInput
        {
            float4 vertex : POSITION;
            float4 texcoord0 : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        GraphVertexInput PopulateVertexData(GraphVertexInput v)
        {
            return v;
        }

        ENDHLSL

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                struct GraphVertexOutput
                {
                    float4 position : POSITION;
                    half4 uv0 : TEXCOORD;

                };

                GraphVertexOutput vert(GraphVertexInput v)
                {
                    v = PopulateVertexData(v);

                    GraphVertexOutput o;
                    float3 positionWS = TransformObjectToWorld(v.vertex);
                    o.position = TransformWorldToHClip(positionWS);
                    float4 uv0 = v.texcoord0;
                    o.uv0 = uv0;

                    return o;
                }

                float4 frag(GraphVertexOutput IN) : SV_Target
                {
                    float4 uv0 = IN.uv0;

                    SurfaceDescriptionInputs surfaceInput = (SurfaceDescriptionInputs)0;
                    surfaceInput.uv0 = uv0;

                    SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
                    return all(isfinite(surf.Out_3)) ? half4(surf.Out_3, surf.Out_3, surf.Out_3, 1.0) : float4(1.0f, 0.0f, 1.0f, 1.0f);

                }
                ENDHLSL
            }
        }
}
