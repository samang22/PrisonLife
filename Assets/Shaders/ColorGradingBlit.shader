// =============================================================================
// ColorGradingBlit
// =============================================================================

Shader "Hidden/PrisonLife/ColorGradingBlit"
{
    // -------------------------------------------------------------------------
    // Properties
    //   머티리얼 인스펙터에 노출되는 파라미터 정의. 이름은 아래 HLSL의 float _이름과 동일해야 한다.
    //   형식:  _Shader변수 ("인스펙터 라벨", 타입) = 기본값
    // -------------------------------------------------------------------------
    Properties
    {
        // 0.5를 중심으로 벌리거나 모음. 1 = 그대로, 1보다 크면 대비 증가
        _Contrast ("Contrast", Range(0, 2)) = 1
        // 0 = 완전 흑백(채도 0), 1 = 원본, 1보다 크면 채도 과다
        _Saturation ("Saturation", Range(0, 2)) = 1
        // 전체를 밝게/어둡게(작은 델타). 0 = 변화 없음
        _Lift ("Lift", Range(-0.2, 0.2)) = 0
    }

    // 하나의 "그리기용 세트". 여러 SubShader를 쓰면 (예: PC용/모바일용) 플랫폼에 맞는 것이 선택된다. 여기서는 1개만 사용.
    SubShader
    {
        // URP(Universal)에서만 쓰인다는 표시. Built-in 씬이면 이 SubShader는 스킵될 수 있음
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // 풀스크린 Blit: 깊이 버퍼와 관계없이 항상 그리기, 깊이 쓰기 끄기, 뒤집힌 면(백페이스)도 그리기
        // (화면을 덮는 사각형/삼각형이 카메라 앞에 정확히 붙기 위한 설정)
        ZTest Always ZWrite Off Cull Off

        // Blit는 보통 Pass 하나로 충분. 여기 Pass의 결과가 프레임버퍼(또는 Blit 대상 RT)에 쓰인다.
        Pass
        {
            // 유니티 프레이머/디버그에서 Pass 이름으로 구분할 때 쓰는 라벨(선택)
            Name "ColorGrading"
            HLSLPROGRAM
            // vert: 버텍스 셰이더 함수명, frag: 픽셀(프래그먼트) 셰이더 함수명. 이름은 아래 Varyings Vert / Frag와 일치
            #pragma vertex Vert
            #pragma fragment Frag

            // URP 공통 매크로 (행렬, half 타입, TEXTURE2D_X 등). URP/Blit 조합에 필수에 가깝다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Blitter.BlitCameraTexture( ... material ) 호출 시, Unity가 "소스 화면"을 이 텍스처로 바인딩한다.
            // TEXTURE2D_X: 싱글/스테레오/XR 등에 맞는 샘플링 매크로를 쓰기 위한 URP 권장 선언
            // 이름 _BlitTexture 는 Blit 경로에서 기대하는 바인딩과 맞출 것
            TEXTURE2D_X(_BlitTexture);
            // 위 텍스처에 대한 샘플러(필터/클램프). sampler_ 접두 + 텍스처명 규칙
            SAMPLER(sampler_BlitTexture);

            // Blitter.BlitCameraTexture(…, material, pass)는 매 드로우마다 _BlitTexture / _BlitScaleBias 를 설정함 (Core RP Blitter API).
            // scaleBias: xy=소스 RT 내 UV 스케일, zw=바이어스(문서: ZW는 텍스처 오프셋)
            float4 _BlitScaleBias;

            // Properties 블록의 세 변수와 1:1 (유니티가 머티리얼에서 이 값을 GPU 상수로 넘긴다)
            float _Contrast;
            float _Saturation;
            float _Lift;

            // vertex -> fragment로 넘기는 보간용 데이터. 여기서는 클립공간 위치 + UV만 사용
            struct Varyings
            {
                // SV_POSITION: 클립공간(스크린에 투영된 좌표). GPU가 래스터에 사용
                float4 positionCS : SV_POSITION;
                // 0번 텍스처 좌표. Frag에서 샘플링에 사용
                float2 texcoord     : TEXCOORD0;
            };

            // Blit.hlsl Vert 와 동일: Common.hlsl GetFullScreenTriangle* + _BlitScaleBias
            // (0~2 범위 UV에 *0.5를 셰이더에서 임의로 넣으면 Blitter가 주는 _BlitScaleBias와 이중이 되어 1/4 샘플 등으로 깨짐)
            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(vertexID);
                o.texcoord = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return o;
            }

            // SV_Target: 이 픽셀에 쓰일 최종 색(RGBA). 알파는 소스(Blit 텍스처) 그대로 둔다(보정은 RGB만)
            half4 Frag(Varyings i) : SV_Target
            {
                // RGBA 전부 샘플. 투명 UI/블렌딩 씬이면 a가 1이 아닐 수 있음
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.texcoord);

                // 대비: 0.5(중간 회색)을 기준으로 대비. 수식: out = (in - 0.5) * contrast + 0.5
                c.rgb = ((c.rgb - 0.5h) * (half)_Contrast) + 0.5h;
                c.rgb = max(c.rgb, 0.0h);

                // 채도: Rec.709 루마(휘도) weight로 명암 l을 구한 뒤, 원색과 lerp. sat=0이면 회색만, 1이면 원본
                // dot 계수: sRGB/선형이 아닌 "대략 눈에 맞는" 밝기 합(자주 쓰는 0.2126, 0.7152, 0.0722 근사)
                half l = dot(c.rgb, half3(0.2126729h, 0.7151522h, 0.0721750h));
                c.rgb = lerp(half3(l, l, l), c.rgb, (half)_Saturation);

                // 리프트: RGB 전부에 동일한 덧셈(전체를 살짝 밝게/어둡게)
                c.rgb += (half)_Lift;

                return c;
            }
            ENDHLSL
        }
    }
}
