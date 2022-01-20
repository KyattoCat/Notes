# Unity Shader

## 1. 空间

- 模型空间

  以该模型自身中心为原点的坐标系，比如一个prefab点进去，显示的坐标系就是模型空间。

  <img src=".\images\Unity Shader\模型空间.png" alt="模型空间"  />

- 世界空间

  绝对坐标系。

- 观察空间

  其实就是摄像机空间，以摄像机作为原点，但是是右手坐标系，z轴正方向指向屏幕外。

- 裁剪空间

  摄像机有两种投影方式，透视和正交，其视锥体不同。通过投影矩阵可以将观察空间内的点转换到方块形状的裁剪空间。

- 屏幕空间

  

## 2. Unity Shader 基础使用

### 2.1 基础

```c
#pragma vertex vert
#pragma fragment frag
```

这两行编译指令指定了哪个函数包含顶点着色器的代码，哪个函数包含了片元着色器的代码。更加通用的编译指令如下：

```c
#pragma vertex name
#pragma fragment name
```

其中 `name` 为自定义的函数名。

```c
float4 vert(float4 v : POSITION) : SV_POSITION
{
	return mul(UNITY_MATRIX_MVP, v);
	// 在Unity3D 2019中被替换为
	// return UnityObjectToClipPos(v);
	// 函数名很直观 模型到裁剪
}
```

顶点着色器代码，她是逐顶点执行的。函数的输入v是这个顶点的位置，这是通过 `POSITION` 语义指定的。她的返回值是一个在裁剪空间中的位置，这是由 `SV_POSITION` 语义指定的。

`POSITION` 和 `SV_POSITION` 都是 `CG/HLSL` 中的语义，大概就是指定这个值是什么。

```c
float4 frag() : SV_TARGET
{
	return fixed4(1.0, 1.0, 1.0, 1.0);
}
```

片元着色器代码，无输入，返回值指定为 `SV_TARGET`，她告诉渲染器把用户输出的颜色存储到一个渲染目标中？，这里输出到默认的帧缓存中。

本例中返回的是一个表示白色的 `fixed4` 类型的变量。

### 2.2 顶点着色器多属性输入

```c
// 使用结构体定义顶点着色器的输入
struct a2v
{
    // POSITION语义告诉Unity，用模型空间的顶点坐标填充vertex变量
    float4 vertex : POSITION;
    // 用模型空间的法线方向填充normal变量
    float3 normal : NORMAL;
    // 用模型的第一套纹理坐标填充texcoord变量
    float4 texcoord : TEXCOORD0;
};


float4 vert(a2v v) : SV_POSITION
{
	return UnityObjectToClipPos(v.vertex);
}
```

如果想要获取更多的模型数据，那么就需要定义一个结构体作为顶点着色器的参数。

定义结构体时，语义是必须的，因此结构体格式将如下所示：

```c
struct StructName
{
	Type Name : Semantic;
	Type Name : Semantic;
	......
};
```

### 2.3 顶点着色器与片元着色器之间传递信息

```c
// 使用结构体定义顶点着色器的输入
struct a2v
{
    // POSITION语义告诉Unity，用模型空间的顶点坐标填充vertex变量
    float4 vertex : POSITION;
    // 用模型空间的法线方向填充normal变量
    float3 normal : NORMAL;
    // 用模型的第一套纹理坐标填充texcoord变量
    float4 texcoord : TEXCOORD0;
};

// 使用结构体定义顶点着色器的输出
struct a2f
{
    // SV_POSITION表示pos变量内为裁剪空间中的位置
    float4 pos : SV_POSITION;
    // COLOR0表示color可以用于存储颜色
    fixed3 color : COLOR0;
};

a2f vert(a2v v)
{
    // 声明输出结构
    a2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    // v.normal包含顶点的法线方向 将其范围从[-1, 1]映射到[0, 1]
    // 存储到o.color以传递给片元着色器
    o.color = v.normal * 0.5 + fixed3(0.5, 0.5, 0.5);

    return o;
}

float4 frag(a2f i) : SV_TARGET
{
    return fixed4(i.color, 1.0);
}
```

如上所示，定义了一个新结构体`a2f`用于顶点着色器与片元着色器之间传递信息，其中用于传递信息的结构体中必须含有`SV_POSITION`语义指定的变量，否则片元着色器无法获得裁剪空间中的顶点坐标，也就无法将顶点渲染到屏幕上。

顶点着色器是逐顶点调用的，而片元着色器是逐片元调用的，因此片元着色器中的输入实际上是把顶点着色器的输出进行插值后得到的结果？

### 2.4 使用属性（Properties块）

通过属性，我们可以随时调整材质的效果，参数需要写在Properties语义块中。

```c
Properties
{
    // 声明一个Color类型的属性
    _Color ("Color Hint", Color) = (1.0, 1.0, 1.0, 1.0)
}
```

同时，我们需要在CG代码块中添加与其相匹配的变量（同名且同类型），匹配关系见该节[附表](#ShaderLab中属性的类型和CG中变量之间的匹配关系)

````c
// 在Properties中声明的属性也需要在CG代码块中定义一个名称和类型都相同的变量
fixed4 _Color;
````

为了显示效果，修改片元着色器代码：

```c
float4 frag(a2f i) : SV_TARGET
{
    fixed3 c = i.color;
    c *= _Color.rgb;

    return fixed4(c, 1.0);
}
```

### 2.5 Unity Shader 内置文件

Unity中存在一些有用的类似头文件的文件，其后缀名为`.cginc`

常用的文件：

- UnityCG.cginc，包含经常使用的函数、宏和结构体等。
- UnityShaderVariables.cginc，包含很多内置的全局变量，如UNITY_MATRIX_MVP等。
- Lighting.cginc，包含了各种内置的光照模型，如果Shader文件是Surface Shader的话会自动包含。
- HLSLSupport.cginc，自动包含，用于跨平台编译。

### 2.6 Unity CG/HLSL语义

语义实际上就是一个赋给Shader输入和输出的字符串，这个字符串表达了这个参数的含义。

个人理解就是，这个变量本身存储了什么Shader流水线是不关心的，我们可以通过语义自行决定这个变量的用途。

而Unity为了方便模型数据的传输，对一些语义进行了特殊的规定。

比如`TEXCOORD0`作为顶点着色器的参数语义输入时，Unity会把模型的第一组纹理坐标填充进去。

但比如当`TEXCOORD0`作为顶点着色器的输出时则没有了特殊含义。

在Dx10之后，有一种新的语义类型出现，即系统数值语义（`system-value semantics`，以下简称SV语义），对，`SV_`开头的`SV_POSITION`就是系统数值语义。

SV语义在渲染流水线中是有特殊含义的，被这些语义修饰的变量不可以被随意修改，因为流水线需要使用这些变量来完成特定的目的。

在某些平台上，必须使用SV语义才能让Shader正常工作，所以我们应该尽量使用SV语义对变量进行修饰，让Shader有更好的跨平台性。

### 2.A 附表

<span id="常见矩阵及其用法">常见矩阵及其用法如下表（可能会被`Unity Shader`自动升级为其他的函数实现）：</span>

|        变量名        | 描述                                                         |
| :------------------: | :----------------------------------------------------------- |
|  `UNITY_MATRIX_MVP`  | 当前的模型·观察·投影矩阵，用于将顶点/矢量从模型空间变换到裁剪空间 |
|  `UNITY_MATRIX_MV`   | 当前的模型·观察矩阵，用于将顶点/矢量从模型空间变换到观察空间 |
|   `UNITY_MATRIX_V`   | 当前的观察矩阵，用于将顶点/矢量从世界空间变换到观察空间      |
|   `UNITY_MATRIX_P`   | 当前的投影矩阵，用于将顶点/矢量从观察空间变换到裁剪空间      |
|  `UNITY_MATRIX_VP`   | 当前的观察·投影矩阵，用于将顶点/矢量从世界空间变换到裁剪空间 |
| `UNITY_MATRIX_T_MV`  | `UNITY_MATRIX_MV`的转置矩阵                                  |
| `UNITY_MATRIX_IT_MV` | `UNITY_MATRIX_MV`的逆转置矩阵，用于将法线从模型空间变换到观察空间 |
|   `_Object2World`    | 当前的模型矩阵，用于将顶点/矢量从模型空间变换到世界空间      |
|   `_World2Object`    | `_Object2World`的逆矩阵，用于将顶点/矢量从世界空间变换到模型空间 |

<span id="ShaderLab中属性的类型和CG中变量之间的匹配关系">ShaderLab中属性的类型和CG中变量之间的匹配关系：</span>

| ShaderLab属性类型 | CG变量类型            |
| ----------------- | --------------------- |
| Color, Vector     | float4, half4, fixed4 |
| Range, Float      | float, half, fixed    |
| 2D                | sampler2D             |
| Cube              | samplerCube           |
| 3D                | sampler3D             |

<span id="UnityCG.cginc中常用的结构体">UnityCG.cginc中常用的结构体：</span>

| 名称           | 描述           | 包含的变量                                       |
| -------------- | -------------- | ------------------------------------------------ |
| `appdata_base` | 顶点着色器输入 | 顶点位置，顶点法线，第一组纹理坐标               |
| `appdata_tan`  | 顶点着色器输入 | 顶点位置，顶点切线，顶点法线，第一组纹理坐标     |
| `appdata_full` | 顶点着色器输入 | 顶点位置，顶点切线，顶点法线，四组或更多纹理坐标 |
| `appdata_img`  | 顶点着色器输入 | 顶点位置，第一组纹理坐标                         |
| `v2f_img`      | 顶点着色器输出 | 裁剪空间中的位置，纹理坐标                       |

应用阶段传递给顶点着色器时Unity支持的常用语义：

| 语义        | 描述                                                         |
| ----------- | ------------------------------------------------------------ |
| `POSITION`  | 模型空间中的顶点位置，通常是float4类型                       |
| `NORMAL`    | 顶点法线，通常是float3类型                                   |
| `TANGENT`   | 顶点切线，通常是float4类型                                   |
| `TEXCOORDn` | TEXCOORD0表示第一组纹理坐标，以此类推，通常是float2或float4类型 |
| `COLOR`     | 顶点颜色，通常是fixed4或float4类型                           |

从顶点着色器传递到片元着色器时Unity支持的常用语义：

| 语义          | 描述                                                         |
| ------------- | ------------------------------------------------------------ |
| `SV_POSITION` | 裁剪空间中的顶点坐标，结构体中必须包含一个使用该语义修饰的变量。等同于Dx9中的POSITION，但最好还是用这个 |
| `COLOR0`      | 通常用于输出第一组顶点颜色                                   |
| `COLOR1`      | 通常用于输出第二组顶点颜色                                   |
| `TEXCOORDn`   | 通常用于输出纹理坐标                                         |

片元着色器输出时Unity支持的常用语义：

- `SV_TARGET`，输出值将会存储到渲染目标中（render target）

## 3. Unity基础光照

### 3.1 标准光照模型

标准光照模型把进入到摄像机内的光线分为四个部分：

1. 自发光（emissive），当给定一个方向时，一个表面本身会向该方向发射多少辐射量。
2. 高光反射（specular），当光线从光源照射到模型表面时，该表面会在完全镜面反射方向散射多少辐射量。
3. 漫反射（diffuse），当光线从光源照射到模型表面时，该表面会在每个方向散射多少辐射量。
4. 环境光（ambient），用于描述所有其他间接光照。间接光照指的是，在进入摄像机之前，光经过了不止一次的物体反射。

### 3.2 Unity中的环境光和自发光

在Shader中，只需要使用Unity的内置变量`UNITY_LIGHTMODEL_AMBIENT`就可以获取环境光，而大多数物体是没有自发光的，如果要计算自发光也很简单，只要在片元着色器输出最后的颜色之前，将自发光颜色添加上去即可。

<img src=".\images\Unity Shader\环境光设置.png" alt="环境光" style="zoom: 80%;" />

### 3.A 附表

<span id="UnityGC.cginc中常用函数">UnityGC.cginc中常用函数：</span>

| 函数名                                         | 描述                                                         |
| ---------------------------------------------- | ------------------------------------------------------------ |
| `float3 WorldSpaceViewDir(float4 v)`           | 输入一个模型空间中的顶点位置，返回世界空间中从该点到摄像机的观察方向 |
| `float3 UnityWorldSpaceViewDir(float4 v)`      | 输入一个世界空间中的顶点位置，返回世界空间中从该点到摄像机的观察方向 |
| `float3 ObjSpaceViewDir(float4 v)`             | 输入一个模型空间中的顶点位置，返回模型空间中从该点到摄像机的观察方向 |
| `float3 WorldSpaceLightDir(float4 v)`          | **仅用于前向渲染**。输入一个模型空间中的顶点位置，返回世界空间中从该点到光源的观察方向，没有被归一化 |
| `float3 UnityWorldSpaceLightDir(float4 v)`     | **仅用于前向渲染**。输入一个世界空间中的顶点位置，返回世界空间中从该点到光源的观察方向，没有被归一化 |
| `float3 ObjSpaceLightDir(float4 v)`            | **仅用于前向渲染**。输入一个模型空间中的顶点位置，返回模型空间中从该点到光源的观察方向，没有被归一化 |
| `float3 Shader4PointLights(...)`               | **仅用于前向渲染**。计算四个点光源的光照，她的参数是已经打包进矢量的光照数据，通常如`unity_4LightPosX0`等，见高级光照[附表](#前向渲染可以使用的内置光照变量)。前向渲染通常会使用这个函数计算逐顶点光照 |
| `float3 UnityObjectToWorldNormal(float3 norm)` | 把法线方向从模型空间转换到世界空间中                         |
| `float3 UnityObjectToWorldDir(in float3 dir)`  | 把方向矢量从模型空间转换到世界空间中                         |
| `float3 UnityWorldToObjectDir(float3 dir)`     | 把方向矢量从世界空间转换到模型空间中                         |

## 4. 基础纹理

### 4.1 基础

纹理的最初目的就是使用一张图片来控制模型的外观。使用纹理映射技术就可以把一张图黏在模型表面，逐纹素（其实就是像素啦）地控制模型的颜色。

建模时会利用纹理展开技术把纹理映射坐标存储在每个顶点上，纹理映射坐标定义了该顶点在纹理中对应的2D坐标，通常该坐标使用一个二维变量表示(u, v)。其中u表示横向，v表示纵向，因此纹理映射坐标也被称为UV坐标。

尽管纹理的大小可以是多样的，但最终通常被归一化到[0, 1]范围内。但需要注意的是，纹理采样坐标不一定在[0, 1]范围内，采样的效果取决于纹理的平铺模式。比如重复或者是钳制等。

在Unity中，UV坐标符合OpenGL标准，即原点在左下角。

### 4.2 在Shader中使用纹理时需要注意的点

首先在Properties块中声明纹理属性，类型为2D，默认值为Unity内置白色图片：

```c
Properties
{
	......
	_MainTex ("Main Tex", 2D) = "white" {}
	......
}
```

在CG代码块中，除了定义一个与纹理匹配的变量外，还需要定义一个变量，该变量名必须为`[纹理名]_ST`：

```c
sampler2D _MainTex;
float4 _MainTex_ST;
```

这个变量用于存储纹理的缩放和平移值，ST分别为缩放和平移的首字母，其中xy分量存储缩放值，zw分量存储平移值。

### 4.3 纹理的属性

在Unity中导入一张纹理资源后，可以在Inspector面板上调整属性。

![](.\images\Unity Shader\纹理属性.png)

需要关注的是拼接模式和过滤模式，拼接模式决定了当纹理采样坐标超过[0, 1]范围后如何进行采样，过滤模式决定当纹理被拉伸时使用的滤波方式，其中有点、双线性和三线性三种，越靠后滤波效果越好。

当我们需要纹理缩小时，最常使用的方法就是mipmapping技术，她将原纹理提前用滤波处理得到很多更小的图像，形成了一个图像金字塔，每一层都是对上一层图像降采样的结果，这样可以在游戏中快速获取结果像素，缺点是会多占用内存空间，通常比原本多33%。

导入纹理时，纹理的长宽应该是2的次幂，否则会占用更多内存且GPU读取该纹理的速度也会下降？

### 4.4 纹理的作用

显而易见的，直接贴在模型表面是一种用途，这里主要介绍别的

- 凹凸映射

  凹凸映射的目的是使用一张纹理来修改模型表面的法线，以为模型提供更多的细节，这种方法可以让模型**看起来**凹凸不平。

  有两种方式可以用来进行凹凸映射：

  1. 高度映射。使用一张高度纹理来模拟表面位移，获得修改后的法线值。
  2. 法线映射。使用一张法线纹理直接存储表面法线。法线方向分量范围[-1, 1]，通常会将法线方向映射到像素分量[0, 1]上，才能将法线信息存储在纹理上。

  方向是相对于坐标空间来说的，因此法线方向也有不同坐标空间下的纹理。

  一种是模型空间的法线纹理，一种是切线空间的法线纹理。通常使用切线空间的法线纹理。

  切线空间的法线纹理具有一些模型空间的法线纹理没有的优点：

  1. 自由度高。模型空间下记录的是绝对法线信息，仅用于创建她时的那个模型，不能用于其他模型。
  2. 可以进行UV动画。原因同上，切线空间下允许凹凸移动的效果。
  3. 可重用。一个砖块六个面仅需要一张切线空间下纹理即可。
  4. 可压缩。切线空间下法线纹理法线方向的Z方向总是正方向，因此可以仅存储XY分量，通过XY分量计算Z分量，所以可以压缩。

- 渐变纹理

  嗯哼，可以自由控制模型的漫反射光照。

- 遮罩纹理

  遮罩纹理可以让我们保护某些区域免于某些修改。

  采样获得遮罩纹理的纹素值，然后使用其中某个（或某几个）通道的值来与某种表面属性相乘，这样当该通道的值为0时，可以保护表面不受该属性的影响。

  比如说，有一个红砖块的纹理，我希望让砖块之间的沟槽不反射高光，我就可以使用砖块纹理的灰度图（因为正好沟槽部分在灰度图里比较黑）当作遮罩，从而避免沟槽的部分受到高光反射的影响。

## 5. 高级光照

### 5.1 渲染路径

渲染路径是什么？我理解为，我给Unity一个信息，让Unity将一些特定光源的信息存储到内置属性里，我才能正确的调用。如果不指定渲染路径，有可能我们的计算结果就是错误的（因为用到的属性是错误的）。LightMode标签用于设置渲染路径，具体见[附表](#LightMode标签支持的渲染路径设置选项)。

在Unity3D 2019里已经找不到书上对应的渲染路径设置了，官方文档说的`Edit -> Project Settings -> Graphics`中我也没看到渲染路径，摄像机上的设置倒是还有，等之后再查一查。

#### 5.1.1 前向渲染路径

最常用。

每进行一次完整的前向渲染，我们需要渲染该对象的渲染图元，并计算两个缓冲区的信息：颜色缓冲区和深度缓冲区。我们利用深度缓冲区来决定一个片元是否可见，如果可见就更新颜色缓冲区中的颜色值。

以下伪代码描述了前向渲染路径的大致过程：

```c
Pass
{
	for (each promitive in this model)
	{
        for (each fragment covered by this primitive)
        {
            if (failed in depth test)
            {
                // 未通过深度测试则该片元不可见 不做更新
                discard;
            }
            else
            {
                // 若该片元可见 则进行光照计算
                float4 color = Shading(materialInfo, pos, normal, lightDir, viewDir);
            	// 并更新帧缓冲
                writeFrameBuffer(fragment, color);
            }
		}
	}
}
```

对于每个逐像素光源，都需要进行上述的一次完整渲染，所以当一个物体在多个逐像素光源的影响范围内，那么这个物体就需要执行多个Pass，并混合多个Pass执行的结果颜色。如果场景中有N个物体，每个物体受M个光源影响，那么就需要计算N*M个Pass，计算量比较大，所以渲染引擎一般会限制每个物体受逐像素光源影响的数目。

在Unity中，前向渲染路径有三种处理光照的方式：

1. 逐顶点处理
2. 逐像素处理
3. 球谐函数（SH）处理

决定一个光源使用哪种处理模式取决于光源的类型和渲染模式。类型指的是平行光或者其他类型的光源，渲染模式指的是她是否是重要的，重要的光源将会被当作逐像素光源处理，如图所示：

![光源渲染模式](.\images\Unity Shader\光源渲染模式.png)

在前向渲染中，当我们渲染一个物体时，Unity会根据场景中各个光源的设置以及这些光源对物体的影响程度对这些光源进行一个重要度排序，其中一定数目的光源会被当做逐像素光源处理，最多有4个光源被逐顶点处理，剩下的按SH方式处理。Unity对光源的判断规则如下：

- 场景中最亮的平行光总是按逐像素处理
- 渲染模式被设为不重要，会按逐顶点或SH方式处理
- 渲染模式被设为重要，会按逐像素处理
- 根据以上规则设置的逐像素光源数量如果小于质量设置中的逐像素光源数量，则会有更多的光源被当作逐像素处理（根据排序结果？）

前向渲染的两种Pass的设置和**通常的**光照计算如图所示：

![前向渲染的两种Pass](.\images\Unity Shader\前向渲染的两种Pass.png)

需要注意的是除了设置标签以外，还需要添加相应的编译指令`#pragma multi_compile_fwdbase`等。

在Additional Pass的渲染设置中，我们还开启和设置了混合模式，这是因为通常我们希望每个Additional Pass执行的光照结果可以和上一次的结果叠加从而形成多光源影响的效果，通常选择的是`Blend One One`。

对于前向渲染来说，通常Unity Shader会定义一个Base Pass和一个Additional Pass。Base Pass仅会执行一次，Additional Pass则会根据该物体的其他逐像素光源的数目被多次调用。

前向渲染可以使用的内置光照变量见[附表](#前向渲染可以使用的内置光照变量)，同时还可以回头看一下基础光照那一节的[附表](#UnityGC.cginc中常用函数)仅用于前向渲染的函数。

#### 5.1.2 顶点照明渲染路径

顶点照明渲染路径是对硬件配置要求最低，性能最高，相应的效果最差的渲染路径，她不支持那些逐像素才能得到的效果，比如阴影、法线映射、高精度的高光反射等。

她实际上是前向渲染的一个子集，但如果使用这种渲染路径，那么Unity将不会自动填充那些逐像素光照变量。在当前Unity版本，这个渲染路径已经被作为一个遗留的渲染路径，在之后与之相关的设定可能会被移除。

顶点照明渲染路径我估计不会去使用了，其中可以使用的内置变量和内置函数就不列出来了，在这里截个图：

<img src=".\images\Unity Shader\顶点照明渲染路径可以使用的内置.png" style="zoom: 80%;" />

#### 5.1.3 延迟渲染路径

古老但仍有用武之地。

前向渲染路径的最大问题就是当场景内包含大量的实时光源时，渲染性能会急速下降，因为我们需要为场景内的每个物体执行多个Pass来计算光照结果，但每执行一次Pass我们都需要重新渲染一次物体，实际上执行了很多次的重复计算。

延迟渲染除了前向渲染用到的颜色和深度缓冲区，还会使用额外的缓冲区，这些缓冲区被统称为G缓冲。

G缓冲存储了我们所关心的表面（通常指的是里摄像机最近的表面）的其他信息，例如该表面的法线、位置、用于光照计算的材质属性等。

延迟渲染主要包含两个Pass，第一个Pass中不进行任何光照计算，仅计算哪些片元时可见的，这主要是通过深度缓冲来实现的。当发现某个片元是可见的就把她的相关信息存储到G缓冲区中。第二个Pass中，我们利用G缓冲区的各个片元信息进行真正的光照计算。

以下伪代码描述了延迟渲染路径的大致过程：

```c
Pass 1
{
	// 第一个Pass不进行真正光照计算 仅仅把可见片元信息存储到G缓冲区中
    for (each primitive in this model)
    {
        for (each fragment covered by this primitive)
        {
            if (failed in depth test)
            {
                discard;
            }
            else
            {
                writeGBuffer(materialInfo, pos, nnormal, lightDir, viewDir);
            }
        }
    }
}

Pass 2
{
    for (each pixell in the screen)
    {
        if (the pixel is valid)
        {
            // 若该像素有效 则读取G缓冲区内相关信息
            readBuffer(pixel, materialInfo, pos, normal, lightDir, viewDir);
            // 根据读取的信息进行光照计算
            float4 color = Shading(materialInfo, pos, normal, lightDir, viewDir);
            // 并更新帧缓冲
            writeFrameBuffer(fragment, color);
        }
    }
}
```

延迟渲染使用的Pass通常就是两个，和场景内光源数目无关，因此延迟渲染的效率不取决于场景的复杂度，而取决于屏幕的大小，因为我们所需要的信息都存储在缓冲区中，而缓冲区的大小和可视范围有关？

对于延迟渲染路径来说，她最适合在场景中光源数目很多且在使用前向渲染时会造成性能瓶颈的情况下使用，而且延迟渲染中的每个光源都可以当作逐像素光源处理。

但，延迟渲染路径有一些缺点：

- 不支持真正的抗锯齿功能
- 不能处理半透明物体
- 对显卡有一定要求，显卡必须支持MRT、Shader Mode 3.0及以上、深度渲染纹理以及双面模板缓冲。

当使用延迟渲染时，Unity要求我们提供两个Pass：

第一个Pass用于渲染G缓冲。这个Pass中我们会把漫反射颜色、高光反射颜色、平滑度、法线、自发光和深度等信息渲染到屏幕的G缓冲区。对于每个物体，这个Pass只会执行一次。

第二个Pass用于计算真正的光照，默认情况下仅可以使用Unity内置的Standard光照模型。

默认的G缓冲区包含以下几个渲染纹理（注意，不同Unity版本这些渲染纹理存储的内容会有所不同，以下为Unity 2019f4）：

- RT0，ARGB32格式，漫反射颜色（RGB）和遮挡（A）
- RT1，ARGB32格式，高光反射颜色（RGB）和粗糙度（A）
- RT2，ARGB2101010格式：世界空间法线（RGB）
- RT3，ARGB2101010（非HDR）或ARGBHalf（HDR）格式，自发光+光照？+光照映射+反射探针
- 深度+模板缓冲

延迟渲染路径可以使用的内置变量见[附表](#延迟渲染路径可以使用的内置变量)

### 5.2 Unity的光源类型

Unity一共支持4种光源类型：

1. 平行光（定向光）
2. 点光源
3. 聚光灯
4. 区域光（面光源）

#### 5.2.1 光源类型的影响

最常用的光源属性有：

- 光源的位置
- 光源的方向（更准确的说是，到某点的方向）
- 光源的颜色
- 强度
- 衰减（到某点的衰减）

以下对三种光源类型进行分类讨论（区域光不在讨论范围内）：

1. 平行光：她的几何定义是最简单的。平行光可以照亮的范围是没有限制的，通常作为类似太阳的角色出现。平行光的几何属性只有方向，且光照强度不会随距离衰减。
2. 点光源：她的照亮空间是由空间中的一个球体定义的，她表示由一个点发出的，向所有方向延申的光。点光源是有位置属性的，同时光照强度是会随距离衰减的，衰减值可以由一个函数定义。
3. 聚光灯：她的照亮空间是一个空间内的锥形区域定义的，是由一个点出发，向特定方向延申的光。她即有位置属性，也有旋转属性，还有衰减属性。

#### 5.2.2 如何在Shader中处理不同的光源类型

```c
Shader "Custom/ForwardRendering"
{
    Properties
    {
        _Diffuse ("Diffuse", Color) = (1, 1, 1, 1)
        _Specular ("Specular", Color) = (1, 1, 1, 1)
        _Gloss ("Gloss", Range(8.0, 256)) = 20
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }


            CGPROGRAM
            
            #pragma multi_compile_fwdbase

            #pragma vertex vert
            #pragma fragment frag

            #include "Lighting.cginc"

            fixed4 _Diffuse;
            fixed4 _Specular;
            float _Gloss;

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(a2v v)
            {
                v2f o;
                // 坐标空间转换
				o.pos = UnityObjectToClipPos(v.vertex);

                // 将模型法线转换到世界坐标下 法线是33矩阵 所以截取WorldToObject的前三行列
                o.worldNormal = mul(v.normal, (float3x3)unity_WorldToObject);

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                // 环境光
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;
                fixed3 worldNormal = normalize(i.worldNormal);
                // 世界坐标下的光源方向 _WorldSpaceLightPos0仅适用于世界有且仅有一个平行光源的情况
                fixed3 worldLight = normalize(_WorldSpaceLightPos0.xyz);

                // fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * saturate(dot(worldNormal, worldLight));
                fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(worldNormal, worldLight));


                fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                fixed3 halfDir = normalize(worldLight + viewDir);

                fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);

                // 这个Pass仅执行一次 且光源类型一定是平行光 平行光没有衰减
                fixed atten = 1.0;
                fixed3 color = ambient + (diffuse + specular) * atten;


                return fixed4(color, 1.0);
            }
            ENDCG

        }

        Pass
        {
            Tags {"LightMode"="ForwardAdd"}
            Blend One One

            CGPROGRAM
            #pragma multi_compile_fwdadd

            #pragma vertex vert
            #pragma fragment frag

            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _Diffuse;
            fixed4 _Specular;
            float _Gloss;

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(a2v v)
            {
                v2f o;
                // 坐标空间转换
				o.pos = UnityObjectToClipPos(v.vertex);

                // 将模型法线转换到世界坐标下 法线是33矩阵 所以截取WorldToObject的前三行列
                o.worldNormal = mul(v.normal, (float3x3)unity_WorldToObject);

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                // 环境光
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;
                fixed3 worldNormal = normalize(i.worldNormal);
// 判断当前处理的逐像素光源类型 非定向光还需要进行向量减法获取光源方向
#ifdef USING_DIRECTIONAL_LIGHT
                fixed3 worldLight = normalize(_WorldSpaceLightPos0.xyz);
#else
                fixed3 worldLight = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
#endif
                // fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * saturate(dot(worldNormal, worldLight));
                fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(worldNormal, worldLight));


                fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                fixed3 halfDir = normalize(worldLight + viewDir);

                fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);

#ifdef USING_DIRECTIONAL_LIGHT
                fixed atten = 1.0;
#else
                float3 lightCoord = mul(unity_WorldToLight, float4(i.worldPos, 1)).xyz;
                fixed atten = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).UNITY_ATTEN_CHANNEL;
#endif
                fixed3 color = ambient + (diffuse + specular) * atten;


                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Specular"
}

```

每一步执行的效果可以通过帧调试器查看，帧调试器打开位置如下图所示：

<img src=".\images\Unity Shader\帧调试器窗口打开位置.png" alt="帧调试器打开位置" style="zoom:67%;" />

通过帧调试器可以看出，Base Pass只执行了一次，而Additional Pass执行了两次，且顺序为红点光源到绿点光源，这是由于Unity对光源的重要程度进行排序的结果，但我们并不知道Unity具体是如何排序的。

![帧调试结果](.\images\Unity Shader\帧调试结果.png)

### 5.A 附表

<span id="LightMode标签支持的渲染路径设置选项">LightMode标签支持的渲染路径设置选项：</span>

| 标签名                         | 描述                                                         |
| ------------------------------ | ------------------------------------------------------------ |
| Always                         | 不管使用哪种渲染路径，该Pass总是会被渲染，但不会计算任何光照 |
| ForwardBase                    | 用于**前向渲染**。该Pass会计算环境光，最重要的平行光，逐顶点/SH光源和Lightmaps |
| ForwardAdd                     | 用于**前向渲染**。计算额外的逐像素光源，每个Pass对应一个光源 |
| Deferred                       | 用于**延迟渲染**。该Pass会渲染G缓冲                          |
| ShadowCaster                   | 把物体的深度信息渲染到阴影映射纹理（shadowmap）或一张深度纹理中 |
| PrepassBase                    | 用于**遗留的延迟渲染**。渲染法线和高光反射的指数部分？       |
| PrepassFinal                   | 用于**遗留的延迟渲染**。该Pass通过合并纹理了、光照和自发光来渲染得到最后的颜色 |
| Vertex、VertexLMRGBM和VertexLM | 用于**遗留的顶点照明渲染**                                   |

<span id="前向渲染可以使用的内置光照变量">前向渲染可以使用的内置光照变量：</span>

| 名称                                                      | 类型     | 描述                                                         |
| --------------------------------------------------------- | -------- | ------------------------------------------------------------ |
| `_LightColor0`                                            | float4   | 该Pass处理的**逐像素光源**颜色                               |
| `_WorldSpaceLightPos0`                                    | float4   | xyz分量为该Pass处理的**逐像素光源**的世界位置，w分量为0时为平行光，为1时为其他光源。 |
| `_LightMatrix0`                                           | float4x4 | 从世界空间到光源空间的变换矩阵。可以用于采样cookie和光强衰减纹理？ |
| `unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0` | float4   | **仅用于Base Pass**。前四个非重要的点光源在世界空间中的位置  |
| `unity_4LightAtten0`                                      | float4   | **仅用于Base Pass**。前四个非重要的点光源的衰减因子          |
| `unity_LightColor`                                        | half4[4] | **仅用于Base Pass**。前四个非重要的点光源的颜色              |

<span id="延迟渲染路径可以使用的内置变量">延迟渲染路径可以使用的内置变量：</span>

| 名称            | 类型     | 描述                                                         |
| --------------- | -------- | ------------------------------------------------------------ |
| `_LightColor`   | float4   | 光源颜色                                                     |
| `_LightMatrix0` | float4x4 | 从世界空间到光源空间的变换矩阵。可以用于采样cookie和光强衰减纹理? |

