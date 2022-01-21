# Unity Shader

## 1. 基础

### 1.1 渲染流水线

渲染流水线的工作任务在于，由一个三维场景出发，生成（或者说渲染）一张二维图像。

渲染流程分为3个阶段：

1. 应用阶段
2. 几何阶段
3. 光栅化阶段

其中每个阶段本身也是一个流水线系统。

#### 1.1.1 应用阶段

应用阶段开发者主要有三个任务：

1. 准备好场景数据（摄像机位置、视锥体、模型、光源等）
2. 进行粗粒度剔除（剔除不可见物体）
3. 设置每个模型的渲染状态（材质、纹理、使用的Shader等）

这一阶段最重要的就是输出渲染所需要的几何信息，即渲染图元（rendering primitives）。

#### 1.1.2 几何阶段

几何阶段用于处理所有和我们要绘制的几何相关的事情。例如决定要绘制的图元是什么（点、线、三角面等），怎样绘制她们，在哪里绘制她们。这一阶段通常在GPU上执行。

几何阶段负责和每个渲染图元打交道，进行逐顶点、逐多边形的操作。这个阶段的一个重要任务就是把顶点左边转换到屏幕空间中，再交给光栅器处理。

这一阶段的输出是顶点在屏幕空间的二位坐标、每个顶点的深度值、着色等相关信息。

#### 1.1.3 光栅化阶段

通过上一阶段产生的数据来生成屏幕上的像素，渲染出最终的图像。

这一阶段决定每个渲染图元中的哪些像素应该被绘制，她需要对上一阶段产生的逐顶点数据进行插值（纹理坐标、顶点颜色等），然后进行逐像素处理。

### 1.2 GPU流水线

试一下mermaid画流程图：

````mermaid
graph LR;
顶点数据-->顶点着色器;
subgraph 几何阶段;
顶点着色器-->曲面细分着色器;
曲面细分着色器-->几何着色器;
几何着色器-->裁剪;
裁剪-->屏幕映射;
end
````

````mermaid
graph LR
屏幕映射-->三角形设置;
subgraph 光栅化阶段
三角形设置-->三角形遍历;
三角形遍历-->片元着色器;
片元着色器-->逐片元操作;
end
逐片元操作-->屏幕图像
````

书上的截图：

![GPU流水线](images/Unity Shader/GPU流水线.png)

上图中绿色表示完全可编程控制，黄色表示可配置不可编程，蓝色表示由GPU固定实现。实线框表示必须由开发人员编程实现，虚线框表示可选的。

#### 1.2.1 顶点着色器

顶点着色器是流水线的第一个阶段，她的输入来自CPU。顶点着色器的处理单位是顶点，也就是说输入的每个顶点都会调用一次顶点着色器。顶点着色器本身无法创建或销毁任何顶点，也不知道顶点与顶点之间的关系，正因如此GPU可以利用不相关性进行并行处理，这一阶段的处理速度会很快。

顶点着色器需要完成的工作有：

- 顶点坐标变换（代码中表现为`UnityObjectToClipPos(v.vertex)`）
- 逐顶点光照

#### 1.2.2 裁剪

由于游戏场景非常大，摄像机的事业范围很可能不会覆盖所有物体，所以那些不在摄像机视野范围内的物体不需要被处理，裁剪阶段因此被提出。

一个图元与摄像机视野的关系有三种：

1. 完全在视野内
2. 部分在视野内
3. 完全在视野外

部分在视野内的图元将会被裁剪，在视野内外交界处形成新的顶点来代替旧的顶点。

顶点着色器将顶点坐标转换到了裁剪坐标，再由硬件做透视除法后得到归一化的设备坐标（NDC），Unity使用的NDC的z轴范围为[-1, 1]。

由NDC坐标可以确定该顶点是否在一个小立方体内，设备只需要将图元裁剪到立方体内即可。

#### 1.2.3 屏幕映射

NDC坐标仍然是三维的坐标，屏幕映射的作用就是将图元的xy分量转换到屏幕坐标下。

屏幕映射不会对输入的z分量做任何处理，而是将屏幕空间下的坐标和z分量一起传递到光栅化阶段，屏幕空间坐标和z坐标组成的坐标系叫做窗口坐标系。

#### 1.2.4 三角形设置

从此进入光栅化阶段。该阶段的输入是上阶段输出的窗口坐标信息、法线方向、视角方向等。光栅化有两个最重要的目标：计算每个图元覆盖了哪些像素，以及为这些像素计算她们的颜色。

这一阶段将上一阶段输出的三角顶点转换为三角边的表示方式，为下一阶段做准备。

#### 1.2.5 三角形遍历

这个阶段会检查每个像素是否被一个三角网格覆盖，如果被覆盖则生成一个**片元**。找到哪些像素被三角网格覆盖的过程就叫做三角形遍历，也被称为扫描变换。

该阶段获取上一阶段传递的边界信息，对三角形覆盖区域做插值计算，输出片元序列。

片元并不是真正意义上的像素，而是包含了计算像素颜色的各种信息，如深度信息、法线、纹理坐标等。

#### 1.2.6 片元着色器

片元着色器是另一个非常重要的可编程着色器阶段。

之前的光栅化阶段并不会影响屏幕上每个像素的颜色值，而是会产生一系列数据信息，用来表述一个三角网格是如何覆盖每个像素的。而每个片元就负责存储这一系列的数据。

片元着色器的输入是上一阶段对顶点信息进行插值的结果，她的输出是一个或多个颜色值。

该阶段可以完成很多重要的渲染技术，如纹理采样等，之后再展开。

#### 1.2.7 逐片元操作

这是GPU流水线的最后一个阶段。

该阶段的主要任务：

- 测试片元的可见性（深度测试、模板测试等）
- 如果一个片元通过了所有测试，则把这个片元的颜色值和已经存储在颜色缓冲区中的颜色进行混合。

### 1.3 坐标空间

- 模型空间

  以该模型自身中心为原点的坐标系，比如一个prefab点进去，显示的坐标系就是模型空间。

  ![1](images/Unity Shader/1.png)

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

![环境光设置](images/Unity Shader/环境光设置.png)

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

![纹理属性](images\Unity Shader\纹理属性.png)

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

### 4.A 一些疑问

为什么纹理长宽要是2的次幂，引用一下知乎@文礼的回答：

> 作者：文礼
> 链接：https://www.zhihu.com/question/376921536/answer/1063272336
> 来源：知乎
> 著作权归作者所有。商业转载请联系作者获得授权，非商业转载请注明出处。
>
> 这个问题其实与GPU的寻址方式（地址对齐要求）有关，并非是图形API层面的限制。
>
> GPU作为一种专用的绘图硬件（当然现在也在逐渐泛用化），其高速运作的能力来自于对于特殊目的定制的特殊硬件模块。其中，实现贴图参照的模块就是这么一种特殊的硬件模块。
>
> 在CPU当中，当要参照一张贴图的时候，你需要在程序当中给出计算地址的公式。比如，当你要访问一张宽度为w高度为h的以行优先顺序存储的贴图当中的坐标为（x，y）的贴图的时候，就需要用y*w+x来计算这个地址。这个计算公式是以代码的形式给出的，对于CPU来说，它只是将这张贴图作为一个连续的内存空间处理而已。
>
> 但是GPU不同。GPU当中完成对贴图访问（寻址以及获取数据）的模块是以硬件形式存在的。它是不可编程的，你只能从它所提供的几种方式当中选一种。这是第一个因素。
>
> 其次，GPU当中进行的是大量的并行计算。这就意味着GPU经常会需要对贴图的不同位置进行同时参照。而且，这种参照往往是需要截取贴图当中一小块面积的内容，也就是是一种2D甚至是3D形状的参照，按CPU那种1D线性的方式保存贴图的话，会使得要参照的那一小部分地址不连续，从而影响高速缓存的命中率，降低速度。
>
> 所以，贴图在GPU的内存（显存）当中，一般都不是以行优先或者列优先方式存储的，而是以“块”为单位存储的。就好像一块一块马赛克组成的墙面那样，每个马赛克对应的像素在内存上是连续存储的。这是第二个因素。
>
> 此外，为了尽可能节约内存开销和传输带宽，贴图一般都会以一种合适的压缩格式存储。而常用的压缩格式，如BC或者DXT，都是将贴图分块进行压缩。这同样隐含了贴图必须是这些“块”的整数倍这样一个条件。这是第三个因素。
>
> 上述几个因素（硬件寻址+贴图在内存上的特殊排布方式+压缩算法要求）决定了一款GPU在设计的时候就会将这个“块”的最小尺寸固定下来。有的GPU是2x2像素，有的是4x4像素，还有的是8x8像素。所以当贴图的尺寸不是这些“块”的整数倍的时候，当贴图被传送到GPU内存（显存）的时候，就会被拉伸或者在四周（一般是右侧和下侧）填充无用数据，使其成为这些“块”的整数倍。（称为pitch）
>
> 这个情况是GPU硬件的要求，与使用何种图形API无关。但是诸如OpenGL这种抽象等级较高的图形API在易用性和可控性之间选择了易用性，也就是尽力隐藏这些细节，在其内部为你完成必要的拉伸或者pitch的操作，从而使得你觉得好像它支持非二次幂的纹理。

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

![光源渲染模式](images\Unity Shader\光源渲染模式.png)

在前向渲染中，当我们渲染一个物体时，Unity会根据场景中各个光源的设置以及这些光源对物体的影响程度对这些光源进行一个重要度排序，其中一定数目的光源会被当做逐像素光源处理，最多有4个光源被逐顶点处理，剩下的按SH方式处理。Unity对光源的判断规则如下：

- 场景中最亮的平行光总是按逐像素处理
- 渲染模式被设为不重要，会按逐顶点或SH方式处理
- 渲染模式被设为重要，会按逐像素处理
- 根据以上规则设置的逐像素光源数量如果小于质量设置中的逐像素光源数量，则会有更多的光源被当作逐像素处理（根据排序结果）

前向渲染的两种Pass的设置和**通常的**光照计算如图所示：

![前向渲染的两种Pass](images/Unity Shader/前向渲染的两种Pass.png)

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

延迟渲染使用的Pass通常就是两个，和场景内光源数目无关，因此延迟渲染的效率不取决于场景的复杂度，而取决于屏幕的大小，因为我们所需要的信息都存储在缓冲区中，而缓冲区的大小和屏幕大小有关。

对于延迟渲染路径来说，她最适合在场景中光源数目很多且在使用前向渲染时会造成性能瓶颈的情况下使用，而且延迟渲染中的每个光源都可以当作逐像素光源处理。

但，延迟渲染路径有一些缺点：

- 不支持真正的抗锯齿功能
- 不能处理半透明物体
- 对显卡有一定要求，显卡必须支持MRT、Shader Mode 3.0及以上、深度渲染纹理以及双面模板缓冲。

当使用延迟渲染时，Unity要求我们提供两个Pass：

第一个Pass用于渲染G缓冲。这个Pass中我们会把漫反射颜色、高光反射颜色、平滑度、法线、自发光和深度等信息渲染到屏幕的G缓冲区。对于每个物体，这个Pass只会执行一次。

第二个Pass用于计算真正的光照，默认情况下仅可以使用Unity内置的Standard光照模型。

默认的G缓冲区包含以下几个渲染纹理（注意，不同Unity版本这些渲染纹理存储的内容会有所不同，以下为Unity 2019f4）：

- RT0，ARGB32格式，漫反射颜色（RGB）和遮罩（A）
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

![帧调试器打开位置](images/Unity Shader/帧调试器窗口打开位置.png)

通过帧调试器可以看出，Base Pass只执行了一次，而Additional Pass执行了两次，且顺序为红点光源到绿点光源，这是由于Unity对光源的重要程度进行排序的结果，但我们并不知道Unity具体是如何排序的。

![帧调试结果](images/Unity Shader/帧调试结果.png)

#### 5.2.3 Unity中的光照衰减

Unity通常使用一张纹理作为查找表来计算光照衰减，好处在于不依赖复杂的数学公式，缺点在于需要预处理得到采样纹理且纹理的大小也会影响衰减的精度，同时也不直观。但在一定程度上对性能友好，所以Unity默认选择了依靠纹理来计算光照衰减。

Unity内部使用了一张名为`_LightTexture0`（对光源使用cookie？后为`_LightTextureB0`）的纹理来计算光照衰减。(0, 0)处的点表示与光源点重合的点的衰减值，(1, 1)处的点表明了在光源空间中所关心的距离最远的点的衰减。参考5.2.2节的CG代码：

```c
float3 lightCoord = mul(unity_WorldToLight, float4(i.worldPos, 1)).xyz;
```

这行代码使用WorldToLight矩阵与顶点坐标进行运算，就能得到该点在光源空间下的坐标，于是可以通过：

```c
fixed atten = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).UNITY_ATTEN_CHANNEL;
```

通过坐标对光照的衰减纹理进行采样，使用`UNITY_ATTEN_CHANNEL`宏获取衰减分量，得出衰减值。

当然也可以通过数学公式来计算衰减，不过书上说Unity 5没有开放记录光源范围、朝向、角度等的变量，所以使用自己指定的数学公式效果往往不太好，不知道2019有没有开放相应变量，之后在看看。

### 5.3 Unity的阴影

#### 5.3.1 ShadowMap技术

这种技术理解起来非常的简单，她首先将摄像机的位置放在与光源重合的位置上，那些摄像机看不到的区域就是该光源的阴影区域。

在前向渲染路径中，如果场景中最重要的平行光开启了阴影，Unity就是为该光源计算她的阴影映射纹理（Shadow Map）。阴影映射纹理本质上也是一张深度图，她记录了从该光源的位置出发、能看到的场景中距离她最近的表面信息（深度信息）。

计算阴影映射纹理时，判断距离光源最近的表面位置可以通过Base Pass和Additional Pass来更新深度信息，得到阴影映射纹理，但是这种方法会对性能造成一些浪费，因为我们只要深度信息而已，这两个Pass中往往有一些复杂的光照模型计算。

所以Unity选择使用一个额外的Pass来专门更新光源的阴影映射纹理，这个Pass的标签为`ShadowCaster`。

Unity首先将摄像机放到光源的位置上，然后调用该（渲染物体上的）Pass，通过顶点变换得到光源空间下的位置，并以此来输出深度信息到阴影映射纹理中。

如果找不到该Pass，Unity就会到Fallback指定的Shader中再去寻找。如果仍然没有找到，该物体就无法向其他物体投射阴影。

#### 5.3.2 不透明物体的阴影

投射阴影的Pass其实在Fallback里的Shader（VertexLit.shader），一般我们就不用管了。这里列出具体的代码：

```c
// Pass to render object as a shadow cas七er
Pass { 
    Name "ShadowCaster" 
	Tags { "LightMode" = "ShadowCaster" ) 
    CGPROGRAM 
    #pragma vertex vert 
    #pragma fragment frag 
    #pragma multi_compile_shadowcaster
    #include "UnityCG.cginc" 
    struct v2f { 
    	V2F SHADOW CASTER;  
    ); 
    v2f vert(appdata_base v) 
    {
        v2f o; 
        TRANSFER SHADOW CASTER NORMALOFFSET(o) 
        return o;
    }
    float4 frag(v2f i) : SV_Target
    {
        SHADOW_CASTER_FRAGMENT(i);
    }

    ENDCG
}
```

以下的CG代码基于前向渲染路径Shader修改。为了接受阴影，我们需要在物体的Shader中Base Pass里，包含`AutoLight.cginc`，在顶点着色器输出结构体里添加一个内置宏：

```c
struct v2f
{
    ......
    SHADOW_COORDS(n)
}
```

注意，没有分号。宏里的参数是下一个可用的插值寄存器的索引值，比如：

```c
struct v2f
{
    ...
    float3 worldNormal : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    // 0和1已经被用了，所以用2
    SHADOW_COORDS(2)
}
```

在顶点着色器返回前添加一个宏：

```c
v2f vert(a2v v)
{
    v2f o;
    // ......
    
    TRANSFER_SHADOW(o);
    return o;
}
```

之后在片元着色器中计算阴影值，与漫反射和高光反射相乘：

```c
fixed4 frag(v2f i)
{
    // ......
    
    fixed shadow = SHADOW_ATTENUATION(i);
    
    // ......
    
    return fixed4(ambient + (diffuse + specular) * atten * shadow, 1.0);
}
```

需要注意的是，这些宏使用了上下文变量进行相关计算，也就是说，`a2v`中顶点坐标变量名必须为`vertex`，顶点着色器中的输入结构体必须命名为`v`，`v2f`中的顶点位置必须为`pos`。

（所以其实我觉得限制还是有一点的，但是Unity是为了适应多平台才封装成宏的，也没啥办法，~~除了自己重写（bushi~~）

光照的衰减和阴影其实可以共同计算：

```c
fixed4 frag(v2f i)
{
    // ......
    
    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
    
    return fixed4(ambient + (diffuse + specular) * atten, 1.0);
}
```

`UNITY_LIGHT_ATTENUATION`宏将会帮我们声明第一个参数，也就是上面的`atten`，所以原本的声明就要去掉了。

#### 5.3.3 透明物体的阴影

这里我就要滚回去补上书上的透明效果那一章了。。。

在此之前都没有涉及到渲染顺序。当场景中包含很多模型时，我们并没有考虑先渲染模型A再渲染模型B，最后再渲染模型C，还是按照别的什么顺序进行渲染。

实际上，由于深度缓冲的存在，不需要考虑她们的渲染顺序也能得到正确的渲染效果，深度缓冲的思想是，当渲染一个片元时，与深度缓冲中的值进行对比，如果深度缓冲中的值比片元距离摄像机更近，那么这个片元就不应该被渲染到屏幕上，否则片元的深度值将会写到深度缓冲区中，同时更新颜色缓冲区中的像素。

Unity中通常使用两种方法实现透明效果：透明度测试和透明度混合。

- 透明度测试很暴力，如果一个物体是透明的，那么她就完全不可见，否则完全可见，不需要关闭深度写入。这种方式无法得出半透明效果。

- 透明度混合能得到真正的半透明效果，她会使用当前片元的透明度作为混合因子，与存在颜色缓冲区中的颜色值进行混合从而得到新的颜色，但是需要关闭深度写入。

  需要注意的是，我们只关闭了深度写入，但是深度测试没有被关闭。当使用透明度混合渲染一个片元时，还是会比较她的深度值和深度缓冲区中的深度值，如果她的深度值比缓冲区中的更远，那么就不会进行混合操作。这一点决定了，当一个不透明物体出现在透明物体前，不透明物体仍然可以正常遮挡住透明物体。

  如果不关闭深度写入，那么，如果一个透明物体在不透明物体前面，本来可以透过透明物体看到不透明物体，但由于深度缓冲中的值被透明物体覆盖，导致颜色缓冲也被更新。也就是看不到后面的物体了。

但是，关闭深度写入破坏了深度缓冲的工作机制，这是一个***非常糟糕***的事情，虽然我们不得不这么做。关闭深度写入导致渲染顺序变得非常重要。

我们假设场景中有两个物体，一前一后，前面的是透明物体A，后面的是不透明物体B。考虑一下渲染顺序的不同会发生什么样的结果：

- B先渲染，B写入深度缓冲和颜色缓冲。A后渲染，我们会对A进行深度测试，发现在B的前面，于是我们把A的颜色与B的颜色进行混合，得到正确的透明效果。
- A先渲染，A只写入颜色缓冲。B后渲染，由于深度缓冲区内没有值，B将会写入深度缓冲区，并覆盖A在颜色缓冲区的写入，于是乎B就愉快地出现在了A的前面。

两个物体都是半透明物体，都没有对深度缓冲进行写入，渲染顺序还重要吗？

我对于书上的描述有点懵，按理来说哈，颜色混合的顺序跟结果没有关系吧，可能是透视的关系？

基于渲染顺序的问题，渲染引擎通常会先对物体进行排序后进行渲染，顺序通常为：

1. 正常渲染所有不透明物体
2. 把半透明物体按距离摄像机的远近进行排序，按照从后往前的顺序渲染，并开启深度测试，关闭深度写入。

可是这样仍然无法解决所有问题，最大的问题就是如何排序。因为排序无法完全正确，总是有一些奇妙的排列让引擎无法判断排序顺序，像是循环重叠的半透明物体们。

Unity为了解决渲染顺序的问题提供了渲染队列的解决方案，可以使用SubShader的Queue标签来决定模型归于那个渲染队列。Unity使用一系列整数来表示渲染顺序，整数索引越小表示越早被渲染，Unity预定义的渲染队列索引见[附表](#预制渲染队列)。

> 由千透明度混合需要关闭深度写入，由此带来的问题也影响了阴影的生成。总体来说 要想为这些半透明物体产生正确的阴影，需要在每个光源空间下仍然严格按照从后往前的顺序进渲染，这会让阴影处理变得非常复杂，而且也会影响性能。因此，在Unity中，所有内置的半透明Shader是不会产生任何阴影效果的。

so，这里就不继续写了，因为这种阴影的效果和不透明物体的阴影效果是一模一样的，和实际想要的效果不一致。

### 5.A 附表

<span id="LightMode标签支持的渲染路径设置选项">LightMode标签支持的渲染路径设置选项：</span>

| 标签名                               | 描述                                                         |
| ------------------------------------ | ------------------------------------------------------------ |
| `Always`                             | 不管使用哪种渲染路径，该Pass总是会被渲染，但不会计算任何光照 |
| `ForwardBase`                        | 用于**前向渲染**。该Pass会计算环境光，最重要的平行光，逐顶点/SH光源和Lightmaps |
| `ForwardAdd`                         | 用于**前向渲染**。计算额外的逐像素光源，每个Pass对应一个光源 |
| `Deferred`                           | 用于**延迟渲染**。该Pass会渲染G缓冲                          |
| `ShadowCaster`                       | 把物体的深度信息渲染到阴影映射纹理（shadowmap）或一张深度纹理中 |
| `PrepassBase`                        | 用于**遗留的延迟渲染**。渲染法线和高光反射的指数部分？       |
| `PrepassFinal`                       | 用于**遗留的延迟渲染**。该Pass通过合并纹理了、光照和自发光来渲染得到最后的颜色 |
| `Vertex`、`VertexLMRGBM`和`VertexLM` | 用于**遗留的顶点照明渲染**                                   |

<span id="前向渲染可以使用的内置光照变量">前向渲染可以使用的内置光照变量：</span>

| 名称                                                      | 类型     | 描述                                                         |
| --------------------------------------------------------- | -------- | ------------------------------------------------------------ |
| `_LightColor0`                                            | float4   | 该Pass处理的**逐像素光源**颜色                               |
| `_WorldSpaceLightPos0`                                    | float4   | xyz分量为该Pass处理的**逐像素光源**的世界位置，w分量为0时为平行光，为1时为其他光源。 |
| `_LightMatrix0`                                           | float4x4 | 从世界空间到光源空间的变换矩阵。可以用于采样cookie和光强衰减纹理 |
| `unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0` | float4   | **仅用于Base Pass**。前四个非重要的点光源在世界空间中的位置  |
| `unity_4LightAtten0`                                      | float4   | **仅用于Base Pass**。前四个非重要的点光源的衰减因子          |
| `unity_LightColor`                                        | half4[4] | **仅用于Base Pass**。前四个非重要的点光源的颜色              |

<span id="延迟渲染路径可以使用的内置变量">延迟渲染路径可以使用的内置变量：</span>

| 名称            | 类型     | 描述                                                         |
| --------------- | -------- | ------------------------------------------------------------ |
| `_LightColor`   | float4   | 光源颜色                                                     |
| `_LightMatrix0` | float4x4 | 从世界空间到光源空间的变换矩阵。可以用于采样cookie和光强衰减纹理 |

<span id="预制渲染队列">预制渲染队列索引号和描述：</span>

| 名称           | 索引 | 描述                                                         |
| -------------- | ---- | ------------------------------------------------------------ |
| `Background`   | 1000 | 此渲染队列在任何其他渲染队列之前渲染。                       |
| `Geometry`     | 2000 | 不透明几何体使用此队列。                                     |
| `AlphaTest`    | 2450 | 经过 Alpha 测试的几何体将使用此队列。                        |
| `GeometryLast` |      | 视为“不透明”的最后的渲染队列。（这个是多出来的，书上没有，不知道索引多少） |
| `Transparent`  | 3000 | 此渲染队列在 Geometry 和 AlphaTest 之后渲染，按照从后到前的顺序。 |
| `Overlay`      | 4000 | 此渲染队列旨在获得覆盖效果。                                 |

### 5.B 一些疑问

光源的Cookie，搜了一下，引用官方文档：

> A cookie is a mask that you place on a Light to create a shadow with a specific shape or color, which changes the appearance and intensity of the Light. Cookies are an efficient way of simulating complex lighting effects with minimal or no runtime performance impact. Effects you can simulate with cookies include caustics, soft shadows, and light shapes.
>
> 剪影是一个蒙版，您可以将其放置在光源上，以创建具有特定形状或颜色的阴影，从而改变光源的外观和强度。Cookie 是一种模拟复杂光照效果的有效方式，对运行时性能的影响微乎其微。您可以使用剪影模拟的效果包括焦散、柔和阴影和光源形状。

还没学到，之后再看

## 6. 高级纹理

这一节主要学习使用立方体纹理、渲染纹理、程序纹理。

### 6.1 立方体纹理

立方体纹理是环境映射的一种实现方法。环境映射可以模拟物体周围的环境，而使用了环境映射的物体可以看起来像镀了一层金属一样反射出周围的环境。

立方体纹理和之前见到的二维纹理不同，立方体纹理一共包含了6张图像，这些图像对应一个立方体的六个面。

立方体纹理的应用有很多，常用于天空盒和环境映射。

#### 6.1.1 天空盒

创建天空盒的方法很简单，在`Project`窗口下新建一个材质，并将材质的Shader设为Unity内置的`Mobile/Skybox`（书上是`Skybox/6 Sided`，两个效果貌似一样，留个问号以后看看？），然后将纹理正确地赋值，如图所示：

![创建天空盒](images/Unity Shader/如何创建天空盒.png)

在Unity中，天空盒是在所有不透明物体之后渲染的，其使用的网格是一个立方体或一个细分后的球体。

#### 6.1.2 环境映射

创建环境映射所需的立方体纹理的方法有三种：

1. 直接由一些特殊布局的纹理创建。

   我们需要提供类似立方体展开图的交叉布局、全景布局等的纹理，然后将该纹理的类型设为`Cubemap`即可，在基于物理的渲染中，我们通常会使用一张HDR图像来生成高质量的Cubemap。

2. 手动创建Cubemap资源，再把6张图赋给她。这是老旧的方法，Unity建议使用第一种方法，因为第一种方法可以对纹理数据进行压缩，且可以支持边缘修正、光滑反射和HDR等功能。

3. 由脚本生成。前两种方法都需要提前准备好立方体纹理的图像，但理想情况下，我们希望根据物体在场景位置的不同，生成不同的立方体纹理。于是我们可以使用Unity的脚本，利用`Camera.RenderToCubemap`方法来实现。书上的脚本实现如下：

   ```c#
   using UnityEngine;
   using UnityEditor;
   using System.Collections;
   
   public class RenderCubemapWizard : ScriptableWizard {
   	
   	public Transform renderFromPosition;
   	public Cubemap cubemap;
   	
   	void OnWizardUpdate () {
   		helpString = "Select transform to render from and cubemap to render into";
   		isValid = (renderFromPosition != null) && (cubemap != null);
   	}
   	
   	void OnWizardCreate () {
   		// create temporary camera for rendering
   		GameObject go = new GameObject("CubemapCamera");
   		go.AddComponent<Camera>();
   		// place it on the object
   		go.transform.position = renderFromPosition.position;
   		// render into cubemap		
   		go.GetComponent<Camera>().RenderToCubemap(cubemap);
   		
   		// destroy temporary camera
   		DestroyImmediate(go);
   	}
   	
   	[MenuItem("GameObject/Render into Cubemap")]
   	static void RenderCubemap () {
   		ScriptableWizard.DisplayWizard<RenderCubemapWizard>("Render cubemap", "Render!");
   	}
   }
   ```

   该脚本需要被放在`Assets/Editor`目录下，成为一个编辑器脚本，然后我们就能在GameObject选项栏里找到Render into Cubemap选项，将物体的位置和导出的纹理设置好，按下Render就可以得到立方体纹理了。如图所示：

   ![编辑器脚本](images/Unity Shader/渲染到立方体纹理脚本.png)

   ![渲染步骤](images/Unity Shader/渲染到立方体纹理步骤.png)

#### 6.1.3 反射和折射

创建完立方体纹理之后，就可以进行环境映射。环境映射最常见的应用就是反射和折射。

我们通过入射光线方向和法线方向计算反射方向，再利用反射方向对立方体纹理进行采样即可。代码如下：

```c
Shader "Custom/Reflection"
{
    Properties
    {
        _Color ("颜色", Color) = (1, 1, 1, 1)
        _ReflectColor ("反射颜色", Color) = (1, 1, 1, 1)
        _ReflectAmount ("反射程度", Range(0, 1)) = 1
        _Cubemap ("环境映射纹理", Cube) = "_Skybox" {}
    }

    SubShader
    {
		Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Tags {"LightMode"="ForwardBase"}

            CGPROGRAM
            
			#pragma multi_compile_fwdbase

            #pragma vertex vert
            #pragma fragment frag

            #include "Lighting.cginc"
            #include "AutoLight.cginc"

			fixed4 _Color;
			fixed4 _ReflectColor;
			fixed _ReflectAmount;
			samplerCUBE _Cubemap;

			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				fixed3 worldNormal : TEXCOORD1;
				fixed3 worldViewDir : TEXCOORD2;
				fixed3 worldRefl : TEXCOORD3;
				SHADOW_COORDS(4)
			};


            v2f vert(a2v v)
            {
                v2f o;
                // pos是裁剪空间的坐标
                o.pos = UnityObjectToClipPos(v.vertex);
                // 模型空间法线转换到世界空间法线
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                // 模型空间下顶点坐标转换到世界坐标
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // 获取世界坐标下
                o.worldViewDir = UnityWorldSpaceViewDir(o.worldPos);
                // 计算该顶点的反射方向
                // 通过光路可逆原则反向求得物体反射到摄像机中的光线方向
                o.worldRefl = reflect(-o.worldViewDir, o.worldNormal);

                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                fixed3 worldNormal = normalize(i.worldNormal);
                fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                fixed3 worldViewDir = normalize(i.worldViewDir);

                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;
                fixed3 diffuse = _LightColor0.rgb * _Color.rgb * max(0, dot(worldNormal, worldLightDir));

                // 使用反射方向对立方体纹理进行采样
                fixed3 reflection = texCUBE(_Cubemap, i.worldRefl).rgb * _ReflectColor.rgb;
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                // 使用插值函数混合漫反射和反射
                fixed3 color = ambient + lerp(diffuse, reflection, _ReflectAmount) * atten;

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
	FallBack "Reflective/VertexLit"
}

```

折射的原理比反射复杂一些。当光线从一种介质斜射进入另一种介质时，传播方向一般会发生改变，当给定入射角时，可以使用斯涅耳定律来计算反射角（试用一下公式的功能）：
$$
\eta_1sin\theta_1=\eta_2sin\theta_2
$$
其中$\eta_1$和$\eta_2$分别是两个介质的折射率。真空的折射率是1，玻璃的折射率一般是1.5。

对于实际的物理规律来说，折射一般发生两次，一次是进入时发生的折射，一次是离开时发生的折射。但是想要在实时渲染中模拟出第二次折射方向是比较复杂的，而仅模拟一次得到的效果从视觉上说也还可以，所以通常我们只模拟第一次折射。

![斯涅尔定律](images/Unity Shader/斯涅耳定律.png)

代码改动不大：

```c
    Properties
    {
        _Color ("颜色", Color) = (1, 1, 1, 1)
        _RefractColor ("折射颜色", Color) = (1, 1, 1, 1)
        _RefractAmount ("折射程度", Range(0, 1)) = 1
        _RefractRatio ("折射比率", Range(0.1, 1)) = 0.5
        _Cubemap ("环境映射纹理", Cube) = "_Skybox" {}
    }
```

添加一个新属性存放折射率比率。

```c
v2f vert(a2v v)
{
    // ......
    // 参数为 入射光线反向 表面法线 光线所在介质与新介质之间的折射率的比值
    o.worldRefr = refract(-normalize(o.worldViewDir), normalize(o.worldNormal), _RefractRatio);
    // ......
    return o;
}
fixed4 frag(v2f i) : SV_TARGET
{
    // ......
    // 使用折射方向对立方体纹理进行采样
    fixed3 refraction = texCUBE(_Cubemap, i.worldRefr).rgb * _RefractColor.rgb;
    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
    // 使用插值函数混合漫反射和折射
    fixed3 color = ambient + lerp(diffuse, refraction, _RefractAmount) * atten;

    return fixed4(color, 1.0);
}
```

#### 6.1.4 菲涅尔反射

菲涅尔反射描述了一种光学现象，当光线照到物体表面上时，一部分发生折射，一部分进入物体内部，发生折射或散射。被反射的光和入射光之间存在一定的比率关系，这个比率关系可以通过菲涅尔等式进行计算。

一个常见的例子就是，当你站在湖边，你会发现你看你脚底附近的水体时，你可以看清浅水底下水底，而当你看远处的湖面时，你会发现你只能看到水面反射的环境。

现实生活中的菲涅尔等式是十分复杂的，所以在实时渲染中我们通常使用一些近似等式,比如**Schlick菲涅尔近似等式**：
$$
F_{schlick}(\pmb{v},\pmb{n})=F_0+(1-F_0)(1-\pmb{v}\cdot\pmb{n})^5
$$
