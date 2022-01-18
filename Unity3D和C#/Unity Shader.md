# Unity Shader

## 1. 空间

- 模型空间

  以该模型自身中心为原点的坐标系

- 世界空间

  绝对坐标系

- 观察空间

  其实就是摄像机空间，以摄像机作为原点，但是是右手坐标系，z轴正方向指向屏幕外

- 裁剪空间

  

- 屏幕空间

  

## 2. Unity Shader基础使用

### 2.1 基础

```CG
#pragma vertex vert
#pragma fragment frag
```

这两行编译指令指定了哪个函数包含顶点着色器的代码，哪个函数包含了片元着色器的代码。更加通用的编译指令如下：

```CG
#pragma vertex name
#pragma fragment name
```

其中 `name` 为自定义的函数名。

```CG
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

```CG
float4 frag() : SV_TARGET
{
	return fixed4(1.0, 1.0, 1.0, 1.0);
}
```

片元着色器代码，无输入，返回值指定为 `SV_TARGET`，她告诉渲染器把用户输出的颜色存储到一个渲染目标中？，这里输出到默认的帧缓存中。

本例中返回的是一个表示白色的 `fixed4` 类型的变量。

### 2.2 顶点着色器多属性输入

```
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

```
struct StructName
{
	Type Name : Semantic;
	Type Name : Semantic;
	......
};
```

### 2.3 顶点着色器与片元着色器之间传递信息

```
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

```
Properties
{
    // 声明一个Color类型的属性
    _Color ("Color Hint", Color) = (1.0, 1.0, 1.0, 1.0)
}
```

同时，我们需要在CG代码块中添加与其相匹配的变量（同名且同类型），匹配关系见该节附表

````
// 在Properties中声明的属性也需要在CG代码块中定义一个名称和类型都相同的变量
fixed4 _Color;
````

为了显示效果，修改片元着色器代码：

```
float4 frag(a2f i) : SV_TARGET
{
    fixed3 c = i.color;
    c *= _Color.rgb;

    return fixed4(c, 1.0);
}
```

### 附表

常见矩阵及其用法如下表（可能会被`Unity Shader`自动升级为其他的函数实现）：

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

ShaderLab中属性的类型和CG中变量之间的匹配关系：

| ShaderLab属性类型 | CG变量类型            |
| ----------------- | --------------------- |
| Color, Vector     | float4, half4, fixed4 |
| Range, Float      | float, half, fixed    |
| 2D                | sampler2D             |
| Cube              | samplerCube           |
| 3D                | sampler3D             |

