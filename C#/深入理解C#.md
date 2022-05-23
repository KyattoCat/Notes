# C# in Depth 4th

## 第二章

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