Shader"Custom/WhiteOverlay" // 1. 셰이더 이름 정의
{
    Properties // 2. 프로퍼티 블록.
    { // 인스펙터 창에서 사용자가 직접 값을 조절할 수 있는 변수들을 선언하는 곳
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Blend ("Blend", Range(0,1)) = 0
        _Alpha ("Alpha", Range(0,1)) = 1
    }
    SubShader // 3. 서브셰이더 블록. 실제 렌더링 로직이 담기는 부분
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"} // 4. 태그
        Blend SrcAlpha OneMinusSrcAlpha // 5. 블렌딩 모드
        Cull Off // 6. 컬링. 폴리곤의 어느 쪽 면을 그릴지 결정. Off는 앞면 뒷면 모두 그리기
        Lighting Off // 7. 라이팅. 씬의 라이트(조명)에 영향을 받을지 결정
        ZWrite Off // 8. Z-버퍼 쓰기. 깊이 버퍼(Z-buffer)에 자신의 깊이 값을 쓸지 말지 결정

        Pass // 패스 블록
        {
            CGPROGRAM // 10. CG/HLSL 코드 시작
                #pragma vertex vert       // 11. 버텍스 셰이더 함수 지정
                #pragma fragment frag     // 12. 프래그먼트(픽셀) 셰이더 함수 지정
                #include "UnityCG.cginc" // 13. 유니티 기본 함수 포함

                // 14. 변수 선언
                sampler2D _MainTex;
                float _Blend;
                float _Alpha;
                float4 _MainTex_ST; // 텍스처 타일링/오프셋 정보

                // 15. 입력 구조체
                struct appdata_t
                {   
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                };

                // 16. 정점 -> 픽셀 전달 구조체
                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                // 17. 버텍스 셰이더
                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    return o;
                }

                // 18. 프래그먼트 셰이더
                fixed4 frag(v2f i) : SV_Target
                {
                    // 1. 텍스처에서 원본 픽셀 색상 가져오기
                    fixed4 col = tex2D(_MainTex, i.uv);
    
                    // 2. 흰색으로 덮어씌우기 (Flash White의 핵심)
                    col.rgb = lerp(col.rgb, 1.0, _Blend); // White 보간
    
                    // 3. 최종 투명도 조절
                    col.a *= _Alpha;
    
                    return col;
                }
            ENDCG // 19. CG/HLSL 코드 끝
        }
    }
}
