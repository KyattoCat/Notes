# Unity性能优化

## UI合批

>**DrawCall合批(Batch)：**
>
>Depth计算完后，依次根据Depth、material ID、texture ID、RendererOrder（即UI层级队列顺序，HierarchyOrder）排序（条件的优先级依次递减），剔除depth == -1的UI元素，得到Batch前的UI 元素队列VisiableList。
>
>对VisiableList中相邻且可以Batch（相同material和texture等）的UI元素合并批次，然后再生成相应mesh数据进行绘制。
>
>![img](.\images\Unity性能优化\v2-2e73a0f96d35b82a29a4e77ae5d47879_720w.webp)
>
>[ref](https://zhuanlan.zhihu.com/p/368524007)

## Mask与RectMask

> [ref](https://zhuanlan.zhihu.com/p/136505882)
>
> **Mask的实现原理：**
>
> > 1. Mask会赋予Image一个特殊的材质，这个材质会给Image的每个像素点进行标记，将标记结果存放在一个缓存内（这个缓存叫做 **Stencil Buffer**）
> > 2. 当子级UI进行渲染的时候会去检查这个 Stencil Buffer内的标记，如果当前覆盖的区域存在标记（即该区域在Image的覆盖范围内），进行渲染，否则不渲染
>
> 我们可以说 Mask 是在 GPU 中做的裁切，使用的方法是着色器中的模板方法。
>
> **RectMask2D的工作流大致如下：**
>
> > ①C#层：找出父物体中所有RectMask2D覆盖区域的交集（**FindCullAndClipWorldRect**）
> > ②C#层：所有继承MaskGraphic的子物体组件调用方法设置剪裁区域（**SetClipRect**）传递给Shader
> > ③Shader层：接收到矩形区域_ClipRect，片元着色器中判断像素是否在矩形区域内，不在则透明度设置为0（**UnityGet2DClipping** ）
> > ④Shader层：丢弃掉alpha小于0.001的元素（**clip (color.a - 0.001)**）
>
> ## 三、RectMask2D和Mask的性能区分
>
> ## 3.1 RectMask2D
>
> > Mask2D不需要依赖一个Image组件，其裁剪区域就是它的RectTransform的rect大小。
>
> - **性质1：RectMask2D节点下的所有孩子都不能与外界UI节点合批且多个RectMask2D之间不能合批。【亲测不能合批】**
> - **性质2：计算depth的时候，所有的RectMask2D都按一般UI节点看待，只是它没有CanvasRenderer组件，不能看做任何UI控件的bottomUI。**
>
> ## 3.2 Mask
>
> > Mask组件需要依赖一个Image组件，裁剪区域就是Image的大小。
>
> - **性质1：Mask会在首尾（首=Mask节点，尾=Mask节点下的孩子遍历完后）多出两个drawcall，多个Mask间如果符合合批条件这两个drawcall可以对应合批（mask1 的首 和 mask2 的首合；mask1 的尾 和 mask2 的尾合。首尾不能合）**
> - **性质2：计算depth的时候，当遍历到一个Mask的首，把它当做一个不可合批的UI节点看待，但注意可以作为其孩子UI节点的bottomUI。**
> - **性质3：Mask内的UI节点和非Mask外的UI节点不能合批，但多个Mask内的UI节点间如果符合合批条件，可以合批。**
>
> 从Mask的性质3可以看出，并不是Mask越多越不好，因为Mask间是可以合批的。得出以下结论：
>
> - 当一个界面只有一个mask，那么，RectMask2D 优于 Mask
> - 当有两个mask，那么，两者差不多。
> - 当大于两个mask，那么，Mask 优于 RectMask2D。
> - 如果只是矩形裁切，RectMask2D不需要重新创建了材质，每帧都使用新材质再次渲染，所以**RectMask2D的效率会比Mask要高**。