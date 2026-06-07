// URP port of Synty's POLYGON_CustomCharacters (Built-in surface shader).
// Same property names so existing FantasyHero materials carry over unchanged.
// Replicates the mask -> step -> lerp color-region blending; URP forward lighting.
Shader "Synty/CustomCharacters_URP"
{
    Properties
    {
        _Color_Primary("Color_Primary", Color) = (0.2431373,0.4196079,0.6196079,0)
        _Color_Secondary("Color_Secondary", Color) = (0.8196079,0.6431373,0.2980392,0)
        _Color_Leather_Primary("Color_Leather_Primary", Color) = (0.282353,0.2078432,0.1647059,0)
        _Color_Metal_Primary("Color_Metal_Primary", Color) = (0.5960785,0.6117647,0.627451,0)
        _Color_Leather_Secondary("Color_Leather_Secondary", Color) = (0.372549,0.3294118,0.2784314,0)
        _Color_Metal_Dark("Color_Metal_Dark", Color) = (0.1764706,0.1960784,0.2156863,0)
        _Color_Metal_Secondary("Color_Metal_Secondary", Color) = (0.345098,0.3764706,0.3960785,0)
        _Color_Hair("Color_Hair", Color) = (0.2627451,0.2117647,0.1333333,0)
        _Color_Skin("Color_Skin", Color) = (1,0.8000001,0.682353,1)
        _Color_Stubble("Color_Stubble", Color) = (0.8039216,0.7019608,0.6313726,1)
        _Color_Scar("Color_Scar", Color) = (0.9294118,0.6862745,0.5921569,1)
        _Color_BodyArt("Color_BodyArt", Color) = (0.2283196,0.5822246,0.7573529,1)
        _Color_Eyes("Color_Eyes", Color) = (0.2283196,0.5822246,0.7573529,1)
        _Texture("Texture", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.2
        _Emission("Emission", Range(0,8)) = 0
        _BodyArt_Amount("BodyArt_Amount", Range(0,1)) = 0
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionZone("Emission Zone (-1=all,0..6 region)", Float) = -1
        _EmissionEdge("Emission Edge Softness", Range(0,0.49)) = 0.2
        _EmissionPulseSpeed("Emission Pulse Speed", Range(0,12)) = 3
        _EmissionPulseMin("Emission Pulse Min", Range(0,1)) = 0.35
        [HideInInspector]_Mask_01("Mask_01", 2D) = "white" {}
        [HideInInspector]_Mask_02("Mask_02", 2D) = "white" {}
        [HideInInspector]_Mask_03("Mask_03", 2D) = "white" {}
        [HideInInspector]_Mask_04("Mask_04", 2D) = "white" {}
        [HideInInspector]_Mask_05("Mask_05", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float4 _Color_Primary, _Color_Secondary, _Color_Leather_Primary, _Color_Leather_Secondary;
                float4 _Color_Metal_Primary, _Color_Metal_Secondary, _Color_Metal_Dark, _Color_Hair;
                float4 _Color_Skin, _Color_Stubble, _Color_Scar, _Color_BodyArt, _Color_Eyes;
                float _Metallic, _Smoothness, _Emission, _BodyArt_Amount;
                float4 _EmissionColor;
                float _EmissionZone, _EmissionEdge, _EmissionPulseSpeed, _EmissionPulseMin;
            CBUFFER_END

            // global (set via Shader.SetGlobalFloat) so the editor preview can drive the pulse; 0 at runtime -> _Time drives it
            float _GlowTime;

            TEXTURE2D(_Texture);  SAMPLER(sampler_Texture);
            TEXTURE2D(_Mask_01);  SAMPLER(sampler_Mask_01);
            TEXTURE2D(_Mask_02);  SAMPLER(sampler_Mask_02);
            TEXTURE2D(_Mask_03);  SAMPLER(sampler_Mask_03);
            TEXTURE2D(_Mask_04);  SAMPLER(sampler_Mask_04);
            TEXTURE2D(_Mask_05);  SAMPLER(sampler_Mask_05);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 positionWS:TEXCOORD2; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.normalWS    = GetVertexNormalInputs(IN.normalOS).normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _Texture);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half3 albedo = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, uv).rgb;
                half4 m1 = SAMPLE_TEXTURE2D(_Mask_01, sampler_Mask_01, uv);
                half4 m2 = SAMPLE_TEXTURE2D(_Mask_02, sampler_Mask_02, uv);
                half4 m3 = SAMPLE_TEXTURE2D(_Mask_03, sampler_Mask_03, uv);
                half4 m4 = SAMPLE_TEXTURE2D(_Mask_04, sampler_Mask_04, uv);
                half4 m5 = SAMPLE_TEXTURE2D(_Mask_05, sampler_Mask_05, uv);

                albedo = lerp(albedo, _Color_Primary.rgb,          step(m1.r, 0.5));
                albedo = lerp(albedo, _Color_Secondary.rgb,        step(m1.g, 0.5));
                albedo = lerp(albedo, _Color_Leather_Primary.rgb,  step(m4.r, 0.5));
                albedo = lerp(albedo, _Color_Leather_Secondary.rgb,step(m4.g, 0.5));
                albedo = lerp(albedo, _Color_Metal_Primary.rgb,    step(m2.r, 0.5));
                albedo = lerp(albedo, _Color_Metal_Secondary.rgb,  step(m2.g, 0.5));
                albedo = lerp(albedo, _Color_Metal_Dark.rgb,       step(m2.b, 0.5));
                albedo = lerp(albedo, _Color_Hair.rgb,             step(m4.b, 0.5));
                albedo = lerp(albedo, _Color_Skin.rgb,             step(m3.r, 0.5));
                albedo = lerp(albedo, _Color_Stubble.rgb,          step(m3.b, 0.5));
                albedo = lerp(albedo, _Color_Scar.rgb,             step(m3.g, 0.5));
                albedo = lerp(_Color_Eyes.rgb, albedo, m5.r);
                half bodyArtT = lerp(m1.b, 1.0, 1.0 - _BodyArt_Amount);
                albedo = lerp(_Color_BodyArt.rgb, albedo, bodyArtT);

                half3 normalWS = normalize(IN.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 ambient = SampleSH(normalWS);
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = ambient + mainLight.color * ndl * mainLight.shadowAttenuation;

            #ifdef _ADDITIONAL_LIGHTS
                uint addCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < addCount; ++li)
                {
                    Light l = GetAdditionalLight(li, IN.positionWS);
                    lighting += l.color * (saturate(dot(normalWS, l.direction)) * l.distanceAttenuation * l.shadowAttenuation);
                }
            #endif

                half3 col = albedo * lighting;
                // per-zone emission: _EmissionZone picks a color region (-1 = whole, legacy). _EmissionColor tints
                // the glow (black -> emit the albedo). Used for socketed-rune glows on a specific spot (buckle/bands/trim).
                // Soft falloff: smoothstep across the mask threshold (band width = _EmissionEdge) so the glow has a
                // bright core that fades at the zone edge instead of a hard cutout.
                float e = _EmissionEdge;
                float lo = 0.5 - e, hi = 0.5 + e;
                float emitMask;
                int ez = (int)round(_EmissionZone);
                if (ez == 0)      emitMask = 1.0 - smoothstep(lo, hi, m1.r);   // Primary (cloth)
                else if (ez == 1) emitMask = 1.0 - smoothstep(lo, hi, m1.g);   // Secondary
                else if (ez == 2) emitMask = 1.0 - smoothstep(lo, hi, m4.r);   // Leather Primary
                else if (ez == 3) emitMask = 1.0 - smoothstep(lo, hi, m4.g);   // Leather Secondary
                else if (ez == 4) emitMask = 1.0 - smoothstep(lo, hi, m2.r);   // Metal Primary
                else if (ez == 5) emitMask = 1.0 - smoothstep(lo, hi, m2.g);   // Metal Secondary
                else if (ez == 6) emitMask = 1.0 - smoothstep(lo, hi, m2.b);   // Metal Dark
                else              emitMask = (1.0 - m5.r);                      // -1 = whole body (legacy)
                half3 emitCol = (_EmissionColor.r + _EmissionColor.g + _EmissionColor.b) > 0.001 ? _EmissionColor.rgb : albedo;
                // animated pulse: breathe the intensity between _EmissionPulseMin and full over time (speed 0 = steady)
                float t = _Time.y + _GlowTime;
                float osc = _EmissionPulseSpeed > 0.0 ? (0.5 + 0.5 * sin(t * _EmissionPulseSpeed)) : 1.0;
                float pulse = lerp(_EmissionPulseMin, 1.0, osc);
                col += emitMask * emitCol * _Emission * pulse;
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // Writes camera depth so depth-based effects (e.g. the FlatKit outline) include the player.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct DAtt { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DVar { float4 positionHCS:SV_POSITION; };
            DVar DepthVert(DAtt IN){ DVar OUT; UNITY_SETUP_INSTANCE_ID(IN); OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz); return OUT; }
            half DepthFrag(DVar IN):SV_TARGET { return 0; }
            ENDHLSL
        }

        // Writes world-space normals so normals-based outline/SSAO include the player.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct NAtt { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct NVar { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0; };
            NVar DNVert(NAtt IN){ NVar OUT; UNITY_SETUP_INSTANCE_ID(IN); OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz); OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS); return OUT; }
            half4 DNFrag(NVar IN):SV_TARGET { return half4(normalize(IN.normalWS), 0.0); }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
