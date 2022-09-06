# Ray Tracing In One Weekend

---

[系列书籍地址](https://raytracing.github.io/)

## 射线-球体相交判断

设球体中心坐标为C，半径为R，有一条射线Ray与球相交于一点P（即切线），则有
$$
(P - C)^2 = R^2\tag{1}
$$
射线Ray有起点O和方向D，且D为单位向量，则有
$$
P = O + tD\tag{2}
$$
其中t为从O点到P点的距离。

将式(2)代入式(1)，得
$$
(O + tD - C)(O + tD - C) = R^2
$$
设O - C = A，则上式展开后为
$$
D^2t^2 + 2ADt + A^2 - R^2 = 0
$$
利用求根公式求解t
$$
\Delta = b^2-4ac\left\{\matrix{>0，2解\\=0，1解\\<0，无解}\right.\\
t=\frac{-b\pm\sqrt{\Delta}}{2a}
$$
t的解的数量等于射线与球的交点数量。

## 射线-平面三角求交

设三角形三个顶点$A_i(x_i,y_i,z_i),i=1,2,3$，有射线Ray与三角形交于一点P，该三角形位于平面，已知平面方程
$$
Ax+By+Cz+D=0\\
D = -(Ax+By+Cz) \tag{1}
$$
已知平面的一个法向量
$$
\vec N=(A,B,C)
$$
代入任意顶点$A_i$到式(1)得
$$
D = -(\vec N \cdot A_i) \tag{2}
$$
又已知射线Ray起点O和方向$\vec D$，则交点P
$$
P = O + t\vec D
$$
其中t为从O点到P点的距离。注意区分射线方向$\vec D$和平面方程常数$D$的区别。

同时P点也位于平面上，所以
$$
\vec N \cdot P + D = 0
$$
展开得
$$
\begin{aligned}
\vec N \cdot (O + t \vec D) + D &= 0\\
\vec N \cdot O + t \vec N \cdot \vec D + D &= 0\\
\end{aligned}
$$
$$
t = -\frac{D+\vec N \cdot O}{\vec N \cdot \vec D}\tag{3}
$$
式(2)代入式(3)得
$$
t = \frac{\vec N \cdot (A_i - O)}{\vec N \cdot \vec D}
$$
规定三角形顶点逆时针排列，三角形法向量方向为
$$
\vec N = \vec{A_3A_1}\times\vec{A_3A_2}
$$
可解t。

接下来判断P点是否在三角形范围内，取三条边向量分别和向量$\vec{A_iP}$叉乘，判断结果是否都与法向量通向，满足全部同向的条件则P在三角形内。
$$
(A_2 - A_1)\times(P-A_1)\\
(A_3 - A_2)\times(P-A_2)\\
(A_1 - A_3)\times(P-A_3)\\
$$

## 射线-AABB求交

自己xjb推的，不是最优的（而且只能用在二维）
$$
\begin{aligned}
设&射线R与直线x=x_{min}相交于点P_0=O+t_0\vec{D}\\
&\vec{N}为垂直于直线x_{min}的单位法向量(1,0)\\
&\because \vec{D}\cdot\vec{N}=|D||N|cos\theta=cos\theta\\
&\because 点O到x_{min}的距离为\space |O_x-x_{min}|\\
&\therefore \frac{|O_x-x_{min}|}{t_0}=\vec{D}\cdot\vec{N}\\
&\therefore t_0 = \frac{|O_x-x_{min}|}{\vec{D}\cdot\vec{N}}\\
又&\because \vec{N}=(1,0)\\
&\therefore \vec{D}\cdot\vec{N}=D_x\\
&\therefore t_0 = \frac{|O_x-x_{min}|}{D_x}\\
同理&得R与x_{max}交点P_1的参数t_1=\frac{|O_x-x_{max}|}{D_x}
&\end{aligned}
$$

用上面的方法可以求得交点P的y轴分量，判断y轴分量是否在AABB的y范围内，若两个点的y分量都不在区间内则没有命中，否则对$y=y_{min}$和$y=y_{max}$两条直线做类似的操作，得出两个交点P的x轴分量是否在范围内。

- 特殊情况处理：射线平行于某个轴

  若射线方向D平行于y轴，则只进行对交点P的x分量的计算

  反之只对P的y轴分量进行计算

- 特殊情况处理：射线起点位于AABB内

  在计算前先判断点O的位置是否在AABB内部，是则必定相交

---

Slab Method

同样把正方形拆成四条直线来计算
$$
\begin{align}
P_0&=O+t_0D\\
(线性关系)x_0&=O_x+t_0D_x\\
t_0&=\frac{x_0 - O_x}{D_x}\\
同理\space t_1&=\frac{x_1 - O_x}{D_x}\\
\end{align}
$$
命中和没命中的情况如下图所示：

![image-20220905172516390](E:\Notes\游戏开发相关\images\Ray Tracing in One Weekend\image-20220905172516390.png)

深蓝色的部分是射线和x轴两条线之间相交的部分A，蓝色则是射线于y轴两条线之间相交的部分B

可以看出，如果射线命中AABB，A和B是存在重叠部分的，那么只需要计算各个射线命中点P的参数t就可以判断是否命中了。妙蛙妙蛙

具体比较如下：

```c#
t0 = (xmin - O.x) / D.x;
t1 = (xmax - O.x) / D.x;
if (t0 > t1) (t0, t1) = (t1, t0); // 交换t0 t1 让小的在前
t2 = (ymin - O.y) / D.y;
t3 = (ymax - O.y) / D.y;
if (t2 > t3) (t2, t3) = (t3, t2);

if (Math.Max(t0, t2) > Math.Min(t1, t3))
    return false;
return true;
```

推广到三维也成立，把AABB拆成六个面来看，对各个面的两个交点参数t0、t1，如果命中了AABB，这些参数是存在重叠部分的。

- 特殊情况处理：射线平行于某个轴

  以平行于y轴为例，此时射线方向为(1, 0)，t0和t1都是无穷大，正负取决于xmin和xmax的正负，如果存在正负差异，交换后t0也是负无穷，t1是正无穷。t2和t3一定有值。

  若t0t1都是负无穷，最终比较时，就是t2比负无穷大，则返回false

  若t0t1都是正无穷，最终比较时，就是正无穷比正无穷，返回false

  若一负一正，就是t2比t3，返回true。

  最终结果变成只和t0t1的正负有关了，t0和t1的正负则取决于射线起点的x轴分量。
