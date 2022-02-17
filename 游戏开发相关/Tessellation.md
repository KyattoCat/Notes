# 曲面细分

回顾一下GPU渲染流水线：

![GPU流水线](images/UnityShader/GPU流水线.png)

在我们熟知的顶点着色器和片元着色器之间，还有两个可编程控制的阶段：曲面细分和几何着色器。

我在写一个水面波动的效果，然鹅在Unity里一个Plane的顶点数量着实有点少，我希望通过曲面细分使顶点数量变多，实现效果更好一点的顶点动画。（虽然不知道这么做是不是有点浪费性能）。

曲面细分阶段有三个子阶段：

1. Hull
2. Tessellator
3. Domain

其中Tessellator阶段不可控制。下面讲这些个阶段是干嘛用的，怎么写Shader。

## 1. 各个阶段的工作

### 1.1 Vertex Stage

原本的顶点着色器变成了工具人，她只负责传递顶点数据打包给Hull阶段，然后就可以摸鱼了（虽然流水线上她也摸不了鱼）。

### 1.2 Hull Stage

Hull主要函数接收的数据被称为Patch，她是顶点函数输出数据的列表。这些顶点的关系是可以配置的，比如三角形或矩形等（以下默认处理三角形）。同时她还接受一个索引，指定Hull主函数要为哪个顶点输出数据？主函数对每个Patch的每个顶点执行一次？

Hull阶段还存在一个与Hull主函数并行运行的函数，称为PatchConstant函数，这个函数对每个Patch执行一次，这对于计算三角形顶点中的共享数据很有用，同时她需要输出一个TessellationFactor（曲面细分因子），用于控制划分这个三角形的次数。

### 1.3 Tessellator

该阶段不可编程控制。

该阶段获取Hull阶段输出的Patch数据和TessellationFactor来细分每个Patch。

三角形内任意一点可以被描述为三个顶点的加权平均值，而权重则被称为BarycentricCoordinates（重心坐标），权重和为1。除了位置之外，重心坐标还可以计算法线、uv等。

Tessellator生成重心坐标传递给下一阶段。

### 1.4 Domain Stage

Domain函数对细分过的网格上的每个顶点运行，她接收从Hull阶段两个函数输出的Patch数据和曲面细分因子以及Tessellator阶段输出的重心坐标，输出一个顶点的最终数据。

Domain阶段相当于取代了之前顶点-片元着色器模式中的顶点着色器阶段，所以原本在顶点着色器进行的对于顶点的相关操作，比如空间转换等都需要在这里完成。

## 2. 写代码

```cpp
Shader "URP/TessellationShader"
{
    Properties
    {
        _EdgeFactor ("细分因子 x=e0 y=e1 z=e2 w=inside", vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
        }
        Pass
        {
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma target 5.0
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
            float4 _EdgeFactor;
            CBUFFER_END
            
            struct Attributes // Assembler->Vertex
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct TessellationControlPoint // Vertex->Hull and PatchConstant
            {
                float3 positionWS : INTERNALTESSPOS;
                float3 normalWS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct TessellationFactors // PatchConstant->Domain
            {
                float edge[3] : SV_TESSFACTOR;
                float inside : SV_INSIDETESSFACTOR;
            };

            struct Interpolators // Domain->Fragment
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TessellationControlPoint vert(Attributes IN)
            {
                TessellationControlPoint OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs vertexPositionInputs = GetVertexPositionInputs(IN.positionOS);
                VertexNormalInputs vertexNormalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionWS = vertexPositionInputs.positionWS;
                OUT.normalWS = vertexNormalInputs.normalWS;
                return OUT;
            }

            TessellationFactors patchConstantFunction(InputPatch<TessellationControlPoint, 3> patch);
            [domain("tri")] // 指定输入的顶点关系为三角形
            [outputcontrolpoints(3)] // 指定输出3个ControlPoint 与输入对应
            [outputtopology("triangle_cw")] // 指定输出的3个顶点的关系为 三角形顺时针
            [patchconstantfunc("patchConstantFunction")] // 指定并行的PatchConstant函数
            [partitioning("integer")] // 指定分割模式
            TessellationControlPoint hull(
                InputPatch<TessellationControlPoint, 3> patch, // 输入的顶点数据和顶点数量
                uint id : SV_OUTPUTCONTROLPOINTID) // ？ 
            {
                return patch[id];
            }

            TessellationFactors patchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                TessellationFactors f;
                f.edge[0] = _EdgeFactor.x;
                f.edge[1] = _EdgeFactor.y;
                f.edge[2] = _EdgeFactor.z;
                f.inside = _EdgeFactor.w;
                return f;
            }

            #define BARYCENTRIC_INTERPOLATE(fieldName) \
                patch[0].fieldName * barycentricCoordinates.x + \
                patch[1].fieldName * barycentricCoordinates.y + \
                patch[2].fieldName * barycentricCoordinates.z;
            
            [domain("tri")]
            Interpolators domain(
                TessellationFactors factors,
                OutputPatch<TessellationControlPoint, 3> patch,
                float3 barycentricCoordinates : SV_DOMAINLOCATION)
            {
                Interpolators OUT;
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                UNITY_TRANSFER_INSTANCE_ID(patch[0], OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = BARYCENTRIC_INTERPOLATE(positionWS);
                float3 normalWS = BARYCENTRIC_INTERPOLATE(normalWS);
                
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = normalWS;
                OUT.positionWS = positionWS;

                return OUT; 
            }

            float4 frag(Interpolators IN) : SV_TARGET
            {
                return float4(0.7,0.7,0.7,1.0);
            }


            ENDHLSL
        }
    }
}
```

这个就是基础的曲面细分代码，没有做任何的优化，优化以后再写（不过我想手机上压根不能用曲面细分，甚至还要降低面数=-=）。可以通过调整细分因子和分割模式调整分割效果。

