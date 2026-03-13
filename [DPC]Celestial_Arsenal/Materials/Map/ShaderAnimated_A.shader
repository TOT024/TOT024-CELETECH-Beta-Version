Shader "CMC/ShaderAnimated_A"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 0.9)) = 0.55

        _ColorA ("Color A", Color) = (0.2, 0.6, 1.0, 1)
        _ColorB ("Color B", Color) = (0.8, 0.2, 0.6, 1)
        
        // 柏林噪声参数
        _NoiseScale ("Noise Scale", Range(1, 20)) = 5.0
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.5
        
        // 电流参数
        _CurrentDensity ("Current Density", Range(1, 8)) = 3.0
        _CurrentWidth ("Current Width", Range(0.05, 0.5)) = 0.15
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 0.4
        
        _Intensity ("Intensity", Range(0, 3)) = 1.5
        
        // 泛光参数
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.8      // 泛光强度
        _GlowRange ("Glow Range", Range(0.01, 0.3)) = 0.1         // 泛光范围
        _GlowFalloff ("Glow Falloff", Range(1, 3)) = 2.0          // 泛光衰减
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;              // 原始UV（用于Alpha测试）
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;         // 世界空间位置
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _Cutoff;
            float4 _ColorA;
            float4 _ColorB;
            float _NoiseScale;
            float _NoiseSpeed;
            float _CurrentDensity;
            float _CurrentWidth;
            float _FlowSpeed;
            float _Intensity;
            float _GlowIntensity;
            float _GlowRange;
            float _GlowFalloff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // 计算世界空间位置
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = worldPos;
                
                return o;
            }

            float SampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            // 简单的伪随机函数
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            // 2D噪声函数（基于随机插值）
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 分形布朗运动（多层噪声叠加，模拟柏林噪声）
            float fbm(float2 st, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(st * frequency);
                    st *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            // 计算电流值（核心函数）- 使用世界空间坐标
            float CalculateCurrent(float3 worldPos, float time, out float3 color)
            {
                // 使用世界空间的XZ平面作为基础坐标
                float2 uv = worldPos.xz * 0.5; // 添加缩放因子避免过密
                
                // 基础UV，添加流动
                float2 flowUV = uv * _NoiseScale;
                flowUV.x += time * _FlowSpeed;
                flowUV.y += time * _FlowSpeed * 0.3;
                
                // 生成柏林噪声（使用4层分形布朗运动）
                float perlin1 = fbm(flowUV, 4);
                float perlin2 = fbm(flowUV * 2.0 + time * 0.5, 3);
                float perlin3 = fbm(flowUV * 0.5 - time * 0.3, 3);
                
                // 组合多层噪声
                float combined = (perlin1 * 0.7 + perlin2 * 0.3 + perlin3 * 0.2);
                
                // Remap到-1到1
                float n = combined * 2.0 - 1.0;
                
                // 取绝对值
                float absN = abs(n);
                
                // One Minus - 得到电流纹理
                float current = 1.0 - absN;
                
                // 增强密度
                current = pow(current, _CurrentDensity);
                
                // 应用宽度（阈值处理）
                float threshold = 1.0 - _CurrentWidth;
                float currentMask = max(0, current - threshold);
                if (currentMask > 0)
                {
                    currentMask = currentMask / (1.0 - threshold);
                }
                
                // 动态颜色混合（基于世界坐标和时间）
                float colorBlend = sin(time * 3 + worldPos.x * 2 + worldPos.z * 2) * 0.5 + 0.5;
                color = lerp(_ColorA.rgb, _ColorB.rgb, colorBlend);
                
                return currentMask;
            }

            // 计算泛光 - 使用预定义的采样点，避免循环嵌套
            float CalculateGlow(float3 worldPos, float time, float currentMask)
            {
                if (_GlowIntensity < 0.01) return 0;
                
                float glow = 0;
                
                // 预定义的采样偏移量（世界空间）
                const float2 offsets[12] = {
                    // 4个主要方向
                    float2(1,0), float2(-1,0), float2(0,1), float2(0,-1),
                    // 4个对角线方向
                    float2(0.7,0.7), float2(-0.7,0.7), float2(0.7,-0.7), float2(-0.7,-0.7),
                    // 4个额外的方向，增加采样密度
                    float2(0.5,0.2), float2(-0.5,0.2), float2(0.2,0.5), float2(0.2,-0.5)
                };
                
                // 对应的距离权重（近似值）
                const float distances[12] = {
                    1.0, 1.0, 1.0, 1.0,
                    0.98, 0.98, 0.98, 0.98,
                    0.54, 0.54, 0.54, 0.54
                };
                
                // 世界空间中的采样范围
                float worldGlowRange = _GlowRange * 2.0; // 转换到世界空间
                
                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    // 计算采样位置
                    float3 samplePos = worldPos;
                    samplePos.xz += offsets[i] * worldGlowRange * 0.5;
                    
                    float3 sampleColor;
                    float sampleCurrent = CalculateCurrent(samplePos, time, sampleColor);
                    
                    if (sampleCurrent > 0.01)
                    {
                        float dist = distances[i] * worldGlowRange;
                        float falloff = 1.0 - pow(dist / worldGlowRange, _GlowFalloff);
                        glow += sampleCurrent * falloff * 0.15; // 减小贡献度避免过亮
                    }
                }
                
                return min(glow, 1.0) * _GlowIntensity;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float centerAlpha = SampleAlpha(i.uv);
                
                if (centerAlpha < _Cutoff)
                    discard;
                
                float time = _Time.y * _NoiseSpeed;
                
                // 计算当前像素的电流（使用世界空间坐标）
                float3 currentColor;
                float currentMask = CalculateCurrent(i.worldPos, time, currentColor);
                
                // 计算泛光（使用世界空间坐标）
                float glow = CalculateGlow(i.worldPos, time, currentMask);
                
                // 组合电流和泛光
                float3 finalColor = 0;
                
                // 如果有电流，显示电流
                if (currentMask > 0.01)
                {
                    finalColor += currentColor * currentMask * _Intensity;
                }
                
                // 添加泛光
                if (glow > 0.01)
                {
                    float3 glowColor = lerp(_ColorA.rgb, _ColorB.rgb, 0.5);
                    finalColor += glowColor * glow * 0.8;
                }
                
                // 如果什么都没有，不显示
                if (length(finalColor) < 0.01)
                    discard;
                
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}