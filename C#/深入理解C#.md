# C# in Depth 4th

前面几章是回顾了C#2到C#5的发展历程，所以这里就简单写一下，毕竟后续版本会更新之前版本的特性。

## 第二章 C#2

### 2.1 泛型

#### 2.1.1 泛型

自C#2后，为了解决C#1中适用一个特定类型集合就需要自己创建一个新的类型，出现了泛型。

并非所有类型或类型成员都适用泛型，比如枚举不能声明为泛型，字段、属性、索引器（索引器是什么？）、构造器、终结器、事件（那Action<T>是啥？）不能被声明为泛型。

下面有个例子:

```c#
public class ValidatingList<TItem>
{
    private readonly List<TItem> items = new List<TItem>();
}
```

items看似是`List<TItem>`，这不就是泛型类型吗？书中给的说明是：判断一个声明是否是泛型声明的唯一标准，就是看她是否引入了新的类型形参。

items是`List<TItem>`，而TItem并不是由items这个字段引入的，而是由ValidatingList这个类引入的，所以items不是泛型类型。

#### 2.1.2 泛型方法类型实参的类型推断

看下面的代码：

```c#
public static List<T> CopyAtMost<T>(List<T> input, int maxElement)
...
List<int> numbers = new List<int>();
List<int> firstTwo = CopyAtMost<int>(numbers, 2);
// 上面可以简化为下面
List<int> firstTwo = CopyAtMost(numbers, 2);
```

编译器可以通过numbers这个变量推断出CopyAtMost的参数类型，numbers是`List<int>`，所以编译器推断CopyAtMost的参数T为int类型。

但是，泛型类型推断不适用于构造函数，也就是创建对象时必须通过显式指定参数类型的方式创建：

```c#
var tuple = new Tuple<int, string, int>(10, "x", 20);
```

这样写确实是很麻烦的一件事，所以有人就利用类型推断来简化创建对象。

```c#
// 工厂方法
public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
{
    return new Tuple<T1, T2, T3>(item1, item2, item3);
}

// 利用类型推断，就可以使用下面的语句简化对象创建
var tuple = Tuple.Create(10, "x", 20);
```

#### 2.1.3 类型约束

1. 引用类型约束。where T : class（包括类、接口、委托）
2. 值类型约束。where T : struct
3. 构造器约束。where T : new()
4. 转换约束。where T : SomeType（可以是类、接口、或其他泛型形参）

#### 2.1.A 附录

不太明白。

微软官方的[运行时中的泛型](https://docs.microsoft.com/zh-cn/dotnet/csharp/programming-guide/generics/generics-in-the-run-time)指出了，对于值类型作为参数首次构造泛型类型时，CLR会创建专用的泛型类型，意思是我先建立了一个`List<int>`后又建立了一个`List<long>`，CLR都会重新创建一个专用的类型。对于引用类型作为参数的泛型构造时，会创建一个专用化泛型类型，而之后再使用引用类型作参实例化，也使用之前创建的专用化泛型类型，原因可能在于所有的引用大小都相同。

### 2.2 可空值类型

就目前我的开发环境来看，可空值类型很少用到。

可空值类型背后是`Nullable<T>`结构体，T服从值类型约束，可以简化为`类型名?（比如int?, float?）`，自C#2之后，null不止有空引用的含义，也标识不包含值的可空值类型的值。

非空值类型可以隐式转换为可空值类型。可空值类型不能隐式转换为非空值类型，可以强制转换为非空值类型，当可空值类型为null时强转，报非法操作的错误。

当对可空值类型使用非空值类型的运算符，如果其中有一个操作数为null，那么这些运算符会被提升（重载）并生成，得出的运算结果为null，否则取值然后按非空值类型运算符来进行运算。

`bool?`有些特殊，书中说把null看作一个变量，若与或非的结果取决于变量值，那么就是null。举个例子：

```c#
true && null == null;// 该式结果取决于null
true || null == true;// 取决于true
true ^ null == null; // 取决于null

false && null == false;// 取决于false
false || null == null; // 取决于null
false ^ null == null;  // 取决于null
```

> 对于[相等运算符](https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/operators/equality-operators#equality-operator-)，如果两个操作数都为 `null`，则结果为 `true`；如果只有一个操作数为 `null`，则结果为 `false`；否则，将比较操作数的包含值。
>
> 对于[不等运算符](https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/operators/equality-operators#inequality-operator-)，如果两个操作数都为 `null`，则结果为 `false`；如果只有一个操作数为 `null`，则结果为 `true`；否则，将比较操作数的包含值。

然后简单说一下`??`运算符，这个叫空合并运算符，表达式`a ?? b`当a为null时，为b的值，否则为a的值。

```c#
int? a = 5;
int b = 10;
int c = a ?? b;
// 如果b也是int? c是不是就要强转了？
```

上面的代码中`int c = a ?? b;`之所以可以不用转换，是因为该表达式不可能为空（一旦a空，就取b的值，而b时非空值类型）。

### 2.3 委托

C#2支持泛型委托以及委托的简化创建方式，比如`EventHandler<TEventArgs>`和`Action<T>`。

#### 2.3.1 方法组

一个或多个同名方法就是方法组，调用有多个重载的方法时就会根据实参类型从方法组中寻找合适的重载方法进行调用。比如常用的`Console.WriteLine`就是方法组。

C#1中将方法组用于委托创建表达式：

```c#
private void HandleButtonClick(object sender, EventArgs e);
EventHandler handler = new EventHandler(HandleButtonClick);
```

C#2中通过方法组简化了委托实例的创建：

```c#
EventHandler handler = HandleButtonClick;
// 事件订阅和取消也可以使用方法组简化
button.Click += HandleButtonClick;
```

#### 2.3.2 匿名方法

C#2引入匿名方法可以使委托不需要事先编写好实体方法，只需要在代码中内联创建即可。

```c#
EventHandler handler = delegate
{
    Console.WriteLine("Event Raised");
};
```

还可以带参数：

```c#
EventHandler handler = delegate(object sender, EventArgs args)
{
    Console.WriteLine("Event Raised, sender = {0}, args = {1}", sender.GetType(), args.GetType());
};
```

> 然而匿名方法的真正威力，要等它用作闭包（closure）时才能发挥出来。闭包能够访问其声明作用域内的所有变量，即使当委托执行时这些变量已经不可访问。后面介绍lambda表达式时，会详细讲解闭包这个概念（包括编译器如何处理闭包），现在只需参考如下示例。AddClickLogger方法接收两个参数：control和message，该方法给control的Click事件处理器添加委托实例，该实例根据message来向control输出内容。 
>
> ```c#
> void AddClickLogger(Control control, string message)
> {
>     control.Click += delegate
>     {
>         Console.WriteLine("Control Clicked: {0}", message);
>     }
> }
> ```

message可以被匿名函数捕获，用作方法体内变量。实际上这是编译器完成了代码生成的操作，之后写lambda表达式时会写她是怎么捕获变量的。

