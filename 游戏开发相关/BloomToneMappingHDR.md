# 小任务

学习Bloom、HDR和Tone Mapping，翻译成中文就是泛光，高动态范围（光照）和色调映射。

## pre. 前置

### pre 1. 降采样

降采样的意思是，降低采样频率。原本对一张图片采样，将会逐个像素获取其颜色。降采样是间隔一定像素取出一个像素，最后把这些取出的像素合并成一张新的图片。

### pre 2. 高斯模糊

高斯模糊的本质是使用一个像素邻域内的像素的加权平均灰度值去代替这个像素。

高斯核一般是5x5或7x7的，标准方差为1的高斯核可以被拆成两个一维的权重，并且是对称的，所以实际上在计算时可以只存3个或5个权重来节省空间。

通过降采样使处理的像素减为原先的$\frac{1}{N}$，可以节省时间。由于模糊消除了图像的细节，降采样同样消除了图像细节，两者可以比较好的结合在一起，不过如果降采样的跨度太大，图像就会变得像素化。

```c#
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GaussianBlurEffect : PostEffectsBase
{
    public Shader gaussianBlurShader;
    private Material gaussianBlurMaterial = null;

    public Material material
    {
        get 
        {
            gaussianBlurMaterial = CheckShaderAndCreateMaterial(gaussianBlurShader, gaussianBlurMaterial);
            return gaussianBlurMaterial;
        }
    }

    [Range(0, 4)]
    public int iterations = 3;
    [Range(0.2f, 3.0f)]
    public float blurSpeed = 0.6f;
    [Range(1, 8)]
    public int downSample = 2;

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if (material != null)
        {
            // 降采样提高效率
            int rtW = src.width / downSample;
            int rtH = src.height / downSample;
            // 高斯模糊需要调用两个Pass 所以需要一块中间缓冲来存储第一个Pass执行后的结果
            RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0);
            // 双线性过滤
            buffer.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, buffer);

            // 迭代
            for (int i = 0; i < iterations; i++)
            {
                material.SetFloat("_BlurSize", 1.0f + i * blurSpeed);

                RenderTexture bufferTemp = RenderTexture.GetTemporary(rtW, rtH, 0);
                
                Graphics.Blit(buffer, bufferTemp, material, 0);

                RenderTexture.ReleaseTemporary(buffer);
                buffer = bufferTemp;
                bufferTemp = RenderTexture.GetTemporary(rtW, rtH, 0);

                Graphics.Blit(buffer, bufferTemp, material, 1);

                RenderTexture.ReleaseTemporary(buffer);
                buffer = bufferTemp;
            }

            Graphics.Blit(buffer, dest);
            RenderTexture.ReleaseTemporary(buffer);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
```

```c
Shader "Custom/GaussianBlur"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }

    SubShader
    {
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        half4 _MainTex_TexelSize;
        float _BlurSize;

        struct v2f
        {
            float4 pos : SV_POSITION;
            half2 uv[5] : TEXCOORD0;
        };

        v2f vertVertical(appdata_img v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);

            half2 uv = v.texcoord;

            o.uv[0] = uv;
            o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
            o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;

            return o;
        }
        
        v2f vertHorizontal(appdata_img v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);

            half2 uv = v.texcoord;

            o.uv[0] = uv;
            o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
            o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;

            return o;
        }

        fixed4 frag(v2f i) : SV_TARGET
        {
            // 权重
            float weight[3] = {0.4026, 0.2442, 0.0545};
            fixed3 sum = tex2D(_MainTex, i.uv[0]).rgb * weight[0];

            for (int it = 1; it < 3; it++)
            {
                sum += tex2D(_MainTex, i.uv[it * 2 - 1]).rgb * weight[it];
                sum += tex2D(_MainTex, i.uv[2 * it]).rgb * weight[it];
            }
            return fixed4(sum, 1.0);
        }
        ENDCG

        pass
        {
            // 定义Pass名称可以再其他Shader中直接使用名称来调用该Pass
            NAME "GAUSSIAN_BLUR_VERTICAL"

            CGPROGRAM
            #pragma vertex vertVertical
            #pragma fragment frag
            ENDCG
        }
        pass
        {
            NAME "GAUSSIAN_BLUR_HORIZONTAL"
            CGPROGRAM
            #pragma vertex vertHorizontal
            #pragma fragment frag
            ENDCG
        }
    }
    Fallback Off
}
```

## 1. HDR

本节参考LearnOpenGLCN。

我们一般用[0, 1]范围内的浮点数表示颜色，而亮度的计算公式（源自UnityShader入门精要）：

$$0.2125R+0.7154G+0.0721B$$

如果在场景中的一块区域内，有多个光源叠加，导致其中某些像素的真实的亮度超过1，但是由于范围只能在1以内，所以这些像素会被约束在1，从而导致场景混合成一片，细节部分将难以分辨。真实世界下这种情况非常常见，太阳就是一个超亮的光源。

![](images/Task/BloomHDRToneMapping/hdr_clamped.png)

HDR原本只是用于摄影上，摄影师对同一场景拍摄不同曝光度的照片，捕捉更大范围的色彩值，然后将这些照片合成为HDR图片，使得细节可见。

由于屏幕只能显示[0, 1]范围内的色彩值，这也导致我们需要将HDR值转换为[0, 1]内的值，也就是LDR（低动态范围）的值，这一过程就是Tone Mapping。

在Unity2019中，默认是开启了HDR的，我把摄像机的HDR开启和关闭时的Bloom效果对比放在下面：

![](/home/mirushii/dev/Notes/游戏开发相关/images/Task/BloomHDRToneMapping/HDR_open.png)

![](/home/mirushii/dev/Notes/游戏开发相关/images/Task/BloomHDRToneMapping/HDR_close.png)

（上图为开启HDR，下图为关闭HDR）

最明显的一点就是，开启HDR后，光晕要比关闭HDR的大。造成这种差异的原因是精度不同。

精度指的是这张图像用于表示颜色的变量的精度（LDR下每个通道8位，HDR下每个通道超过8位，通常为16位），精度越高自然表达的信息越多。

## 2. Tone Mapping

### 2.1 线性映射

最简单但是并不算真正的色调映射，因为她的效果和实际相差的太多了。

在一个整体偏暗，但存在一个高亮光源，线性映射的效果就会把最亮的部分映射到1，把原本偏暗的地方映射到很靠近0的位置。看图比较明显：

![](/home/mirushii/dev/Notes/游戏开发相关/images/Task/BloomHDRToneMapping/linear_tone_mapping_before.jpg)

![](/home/mirushii/dev/Notes/游戏开发相关/images/Task/BloomHDRToneMapping/linear_tone_mapping_after.jpg)

### 2.2 Reinhard

源自2002年的论文，[Photographic Tone Reproduction for Digital Images](https://link.zhihu.com/?target=http%3A//www.cmap.polytechnique.fr/~peyre/cours/x2005signal/hdr_photographic.pdf)。

```c
float3 ReinhardToneMapping(float3 color, float adapted_lum) 
{
    const float MIDDLE_GREY = 1;
    color *= MIDDLE_GREY / adapted_lum;
    return color / (1.0f + color);
}
```

