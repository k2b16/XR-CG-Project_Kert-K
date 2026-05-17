
Shader "Custom/InteractiveWater"
{
    Properties
    {
        _HeightTex          ("Height Map (RG)",     2D)         = "black" {}
        _HeightScale        ("Height Scale",        Range(0,5)) = 1.0
        _NormalStrength     ("Normal Strength",     Range(0,5)) = 1.5

        _ShallowColor       ("Shallow Color",       Color)      = (0.30, 0.70, 0.85, 1)
        _DeepColor          ("Deep Color",          Color)      = (0.03, 0.15, 0.30, 1)

        _RefractionStrength ("Refraction Strength", Range(0, 0.2)) = 0.04
        _WaterTintDensity   ("Tint Density",        Range(0, 5))   = 1.0

        _Smoothness         ("Smoothness",          Range(0,1))    = 0.95
        _SpecPower          ("Specular Power",      Range(8,512))  = 200
        _SpecIntensity      ("Specular Intensity",  Range(0,5))    = 0.6
        _FresnelPower       ("Fresnel Power",       Range(1,8))    = 5.0

        [IntRange] _DebugMode ("Debug Mode (0=off)", Range(0,5))   = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull   Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_HeightTex);
            SAMPLER(sampler_HeightTex);
            float4 _HeightTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float  _HeightScale;
                float  _NormalStrength;
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _RefractionStrength;
                float  _WaterTintDensity;
                float  _Smoothness;
                float  _SpecPower;
                float  _SpecIntensity;
                float  _FresnelPower;
                float  _DebugMode;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 viewPosVS  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float SampleHeight(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_HeightTex, sampler_HeightTex, uv, 0).y;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float h = SampleHeight(IN.uv);
                float3 displacedOS = IN.positionOS.xyz;
                displacedOS.y += h * _HeightScale;

                OUT.positionWS = TransformObjectToWorld(displacedOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.viewPosVS  = TransformWorldToView(OUT.positionWS);
                OUT.uv         = IN.uv;

                float texel = _HeightTex_TexelSize.x;
                float hL = SampleHeight(IN.uv + float2(-texel, 0));
                float hR = SampleHeight(IN.uv + float2( texel, 0));
                float hD = SampleHeight(IN.uv + float2(0, -texel));
                float hU = SampleHeight(IN.uv + float2(0,  texel));

                float3 normalOS;
                normalOS.x = (hL - hR) * _HeightScale * _NormalStrength;
                normalOS.z = (hD - hU) * _HeightScale * _NormalStrength;
                normalOS.y = 1.0;
                normalOS = normalize(normalOS);

                OUT.normalWS = TransformObjectToWorldNormal(normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float NdotV = saturate(dot(N, V));

                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float2 refractUV = saturate(screenUV + N.xz * _RefractionStrength);

                half3 sceneBehind = SampleSceneColor(refractUV);

                float sceneEyeDepth   = LinearEyeDepth(SampleSceneDepth(refractUV), _ZBufferParams);
                float surfaceEyeDepth = -IN.viewPosVS.z;
                float waterDepth = max(0.0, sceneEyeDepth - surfaceEyeDepth);

                // ==== DEBUG MODES ====
                if (_DebugMode > 0.5 && _DebugMode < 1.5)
                {
                    return half4(sceneBehind, 1);
                }
                if (_DebugMode > 1.5 && _DebugMode < 2.5)
                {
                    float d = saturate(waterDepth / 5.0);
                    return half4(d, d, d, 1);
                }
                if (_DebugMode > 2.5 && _DebugMode < 3.5)
                {
                    float fresnel = pow(1.0 - NdotV, _FresnelPower);
                    return half4(fresnel, fresnel, fresnel, 1);
                }
                if (_DebugMode > 3.5 && _DebugMode < 4.5)
                {
                    return half4(N * 0.5 + 0.5, 1);
                }
                if (_DebugMode > 4.5)
                {
                    return half4(sceneBehind, 1);
                }

                float absorption = 1.0 - exp(-waterDepth * _WaterTintDensity);
                half3 tintColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, absorption);
                half3 underwater = sceneBehind * tintColor;

                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                float3 R = reflect(-V, N);
                half3 envRefl = GlossyEnvironmentReflection(R, IN.positionWS, 1.0 - _Smoothness, 1.0);

                half3 col = lerp(underwater, envRefl, fresnel);

                Light mainLight = GetMainLight();
                float3 H = normalize(mainLight.direction + V);
                float NdotH = saturate(dot(N, H));
                float spec = pow(NdotH, _SpecPower) * _SpecIntensity * _Smoothness;
                col += spec * mainLight.color;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
