// OverlayShaderLit — TextureOverlayShader의 URP 2D 조명 적용 버전
// 기존 OverlayShader와 동일한 R채널 마스크 + 월드좌표 UV 오버레이 로직을 유지하면서
// URP 2D Light (Shape Light) 를 받는다.
//
// 사용 방법:
//   1. 이 셰이더를 사용하는 새 Material 생성
//      (예: GroundTilemapMaterial_Lit)
//   2. Tilemap_Base의 Tilemap Renderer → Material 에 교체
//   3. _OverlayTex, _Scale 값은 기존 Material 값 그대로 복사
//
// 주의: URP 2D Renderer에 Renderer2DData가 설정된 경우에만 2D 조명이 활성화됩니다.
Shader "Custom/OverlayShaderLit"
{
    Properties
    {
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _Scale      ("Scale", Float)        = 0.006944444

        // URP 2D 조명 시스템이 런타임에 자동 주입하는 텍스처 (사용자 설정 불필요)
        [HideInInspector] _ShapeLightTexture0 ("", 2D) = "black" {}
        [HideInInspector] _ShapeLightTexture1 ("", 2D) = "black" {}
        [HideInInspector] _ShapeLightTexture2 ("", 2D) = "black" {}
        [HideInInspector] _ShapeLightTexture3 ("", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        // ── Pass 1: 2D Lit 메인 패스 ─────────────────────────────────────────
        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   OverlayVert
            #pragma fragment OverlayFrag

            // 2D 조명 채널 멀티컴파일 (Renderer2DData Shape Light 개수에 대응)
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            // ── 텍스처 선언 ─────────────────────────────────────────────────
            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_OverlayTex); SAMPLER(sampler_OverlayTex);
            float4 _MainTex_ST;
            float  _Scale;

            // ── Shape Light 텍스처 바인딩 ────────────────────────────────────
            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            // ── 정점·보간 구조체 ─────────────────────────────────────────────
            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 lightingUV : TEXCOORD1;  // 스크린 공간 UV (조명 샘플링용)
                float2 worldPos   : TEXCOORD2;  // 월드 XY (오버레이 UV 계산용)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── 정점 셰이더 ──────────────────────────────────────────────────
            Varyings OverlayVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = TransformObjectToWorld(v.positionOS);
                o.positionCS  = TransformWorldToHClip(positionWS);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
                o.color       = v.color;
                o.worldPos    = positionWS.xy;

                // 스크린 공간 UV: ComputeScreenPos → [0,1] 정규화
                float4 screenPos = ComputeScreenPos(o.positionCS);
                o.lightingUV  = screenPos.xy / screenPos.w;

                return o;
            }

            // ── 픽셀 셰이더 ──────────────────────────────────────────────────
            half4 OverlayFrag(Varyings i) : SV_Target
            {
                // 메인 텍스처 (타일 R채널이 마스크 역할)
                half4 mainColor = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // R채널 마스크 → 1이면 오버레이, 0이면 원본
                float mixAmount = floor(mainColor.r);

                // 월드 좌표 기반 오버레이 UV (타일 경계 없는 연속 텍스처)
                float2 overlayUV    = i.worldPos * _Scale;
                half4  overlayColor = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, overlayUV);

                // 마스크 블렌딩
                half4 color = lerp(mainColor, overlayColor, mixAmount);

                // ── URP 2D 조명 적용 ─────────────────────────────────────────
                SurfaceData2D surfaceData;
                InputData2D   inputData;
                InitializeSurfaceData(color.rgb, color.a, surfaceData);
                InitializeInputData(i.uv, i.lightingUV, inputData);

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        // ── Pass 2: 노멀 맵 렌더링 (2D Light Normal Map 지원용) ──────────────
        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #pragma vertex   NormVert
            #pragma fragment NormFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            Varyings NormVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                o.color      = v.color;
                return o;
            }

            // 노멀 맵 없음 → 기본 z방향 노멀(0,0,1) + 알파 패스스루
            half4 NormFrag(Varyings i) : SV_Target
            {
                half4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // rgb = 노멀(월드 공간, 앞면), a = 메인 텍스처 알파
                return half4(0.5h, 0.5h, 1.0h, main.a);
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
