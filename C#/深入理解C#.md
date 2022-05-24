# C# in Depth 4th

前面几章是回顾了C#2到C#5的发展历程，所以这里就简单写一下，毕竟后续版本会更新之前版本的特性。

## C#2

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
var tuple = new Tuple<int, string, int>(10, "x", 20); // var这个关键字是在C#3中推出的新特性，在下一章会写
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

### 2.4 迭代器

迭代器是包含迭代器块的方法或者属性。迭代器块就是包含yield return或yield break语句的代码，只能用于以下返回类型的方法或属性：

- `IEnumerable`
- `IEnumerable<T>`
- `IEnumerator`
- `IEnumerator<T>`

根据返回类型是否是泛型类型（带有泛型参数），迭代器的生成类型也会相应的变化，如果带有泛型参数T，那么生成类型就为T，否则为object。

yield return用于生成返回序列的各个值，yield break用于终止返回序列。

#### 2.4.1 延迟执行

延迟执行基础思想为：只在需要获取计算结果时执行代码。

> ```c#
> static IEnumerable<int> CreateSimpleIterator()
> {
>     yield return 10;
>     for (int i = 0; i < 3; i++) 
>     { 
>         yield return i;
>     }
>     yield return 20;
> }
> // ......
> IEnumerable<int> enumerable = CreateSimpleIterator(); 
> using (IEnumerator<int> enumerator = enumerable.GetEnumerator())
> {
>     while (enumerator.MoveNext())
>     {
>         int value = enumerator.Current;
>         Console.WriteLine(value);
>     }
> }
> ```
>
> 如果读者此前不了解IEnumerable/IEnumerator（及其泛型版本）这对接口，不妨借此机会学习二者的差异。IEnumerable是可用于迭代的序列，IEnumerator则像是序列的一个游标。多个IEnumerator可以遍历同一个IEnumerable，并且不会改变IEnumerable的状态，而IEnumerator本身就是多状态的：每次调用MoveNext()，当前游标都会向前移动一个元素。
>
> 如果还不太清楚，可以把IEnumerable想象成一本书，把IEnumerator想象成书签。一本书可以同时有多个书签，一个书签的移动不会改变书和其他书签的状态，但是书签自身的状态（它在书中的位置）会改变。IEnumerable.GetEnumerator()方法如同一个启动过程，它请求序列来创建一个IEnumerator用于迭代，就像把一个书签插入到一本书的起始页。 

上面代码第11行，在调用CreateSimpleIterator()时，其方法体的代码没有被执行，在12行调用GetEnumerator()时也没有执行，直到14行MoveNext()，执行到了yield return 10;

在遇到以下情况时，迭代器代码会终止运行：

- 抛出异常
- 方法执行完毕
- yield break语句
- yield return语句，返回一个值

> 在本例中，MoveNext()开始迭代之后，它遇到一条yield return 10语句，于是Current赋值为10，然后返回true。
>
> 第一次MoveNext()调用还比较好理解，之后呢？之后不可能从头开始迭代，否则这个函数就陷入无限返回10的死循环了。实际上，当MoveNext()返回时，当前方法就仿佛被暂停了。生成的代码会追踪当前的语句执行进度，还会记录一些相关状态信息，比如循环中局部变量i的值。当MoveNext()再次被调用，就会从之前的位置继续执行，这就是延迟执行名称的由来。这部分内容如果由开发人员自行实现，会比较容易出错。

延迟执行的重要性在于，可以把她当作一个无限长的序列，而且不需要事先运算，可以自由选择迭代的次数。

#### 2.4.2 处理finally块

```c#
static IEnumerable<string> Iterator()
{
    try
    {
        Console.WriteLine("第一个yield之前");
        yield return "first";
        Console.WriteLine("在两次yield之间");
        yield return "second";
        Console.WriteLine("第二个yield之后");
    }
    finally
    {
        Console.WriteLine("执行finally代码块");
    }
}
```

Q:在执行完`yield return "first";`之后会不会执行finally代码块？

A:不会。在执行`yield return`语句后，执行就暂停了。也就是说，如果我手动取这个迭代器的Enumerator，然后只执行一次MoveNext()，那么也是不会执行finally块的。而如果用foreach语句执行迭代器，那么在foreach循环结束后（包括break退出）就会执行finally代码块，这是因为foreach隐含一条using语句，在跳出循环后会自动执行Dispose方法，最终调用finally块。

由于上述特性，迭代器可以用于需要释放资源的地方，比如文件处理器。

#### 2.4.3 迭代器实现机制

[.NET 本质论 - 了解 C# foreach 的内部工作原理和使用 yield 的自定义迭代器](https://docs.microsoft.com/zh-cn/archive/msdn-magazine/2017/april/essential-net-understanding-csharp-foreach-internals-and-custom-iterators-with-yield)

给我整不会了，只知道是利用状态机模式做的。

```c#
public bool MoveNext()
{
    try
    {
        switch (state)
        {
                // 跳转表负责跳转到方法中的正确位置
        }
        // 方法代码在每个yield return都会返回
    }
    fault // IL代码，仅在发生异常时执行
    {
        Dispose(); // 清理资源
    }
}
```

### 2.5 一些小特性

#### 2.5.1 局部类型

`partial`修饰符修饰，可以修饰类、结构体、接口，使其可以分成多个部分声明，一般分布于多个源文件。

#### 2.5.2 静态类

`static`修饰符修饰的类，需要编写全部都是静态方法的工具类，静态类可以胜任。静态类不能实例化。

#### 2.5.3 get/set访问分离

C#1中仅支持单一的访问修饰符，set和get用同一个。C#2可以将其分离：

```c#
private string text;
public string Text
{
    get { return text; }
    private set { text = value; }
}
```

#### 2.5.4 其他

命名空间别名。平时很少用，都是要么直接using要么全称。C#2在C#1的基础上拓展了对命名空间别名的支持，这里不写了。

编译指令。#pragma后编写的东西，没用过，支持警告指令和校验和指令。

固定大小缓冲区。没用过，只能用在非安全代码的结构体内部，可以为结构体分配一个固定大小的内存，使用`fixed`修饰符。

InternalsVisibleTo。一个Attribute（不是类里的属性，是用[]框起来的那个属性），有一个参数指向另一个程序集。这个属性可以让指定的程序集访问该程序集中带有该属性的成员。

## C#3 LINQ

C#3引入大量新特性，总体上都是为了LINQ服务。

### 3.1 自动实现的属性

```c#
private string name;
public string Name
{
    get { return name; }
    set { name = value; }
}
```

```c#
public string Name { get; set; }
```

这个简化显而易见。C#3自动实现属性连字段都不需要手动声明了，她会由编译器自动创建并赋予一个名称。不过C#3中不能设置只读的自动属性，只能通过将set用private修饰来实现，也不能赋初始值，这两个瑕疵在C#6修复。

### 3.2 隐式类型

`var`关键字，只能用于声明局部变量，其结果依然是一个类型确定的局部变量，只是由编译器通过变量赋值的信息推断这个变量是什么类型的。

```c#
var name = "Rick";
// 编译器通过赋值的"Rick"推断name的类型是string，所以最终生成的还是下面这句代码
string name = "Rick";
```

要使用`var`必须满足两个条件：

1. 变量声明时必须被初始化。
2. 用于初始化变量的表达式必须具备某个类型（null是不能用来初始化的）。

`var`适用于以下场景：

- 变量为匿名类型，不能为其指定类型，这于LINQ相关。
- 变量名过长，且通过初始化表达式可以轻松推断变量类型。
- 变量的精确类型不重要。

隐式类型数组可以简化代码编写：

```c#
var array = new[] { 1, 2, 3, 4, 5};
// 多维也行
var array = new[,] { {1, 2, 3}, {4, 5, 6} };
```

编译器通过统计数组所有元素的类型并整合成一个类型候选集，最终取出候选集中所有类型都能隐式转换的类型（包含范围最大的那个类型）作为数组类型，如果没有符合条件的那么就报错。

### 3.3 对象和集合的初始化

看代码最直观：

```c#
public class Order
{
    private readonly List<OrderItem> items = new List<OrderItem>();
    public string OrderID { get; set; }
    public Customer Customer { get; set; } // 欸嘿，类型名和变量名不会冲突吗
    public List<OrderItem> Items { get { return items; } }
}

public class Customer
{
    public string Name { get; set; }
    public string Address { get; set; }
}

public class OrderItem
{
    public string ItemID { get; set; }
    public int Quantity { get; set; } // 这一串get set下来可谓是极大加深了C#3自动属性的印象
}

// 不使用对象初始化和集合初始化
var customer = new Customer(); 
customer.Name = "Jon"; 
customer.Address = "UK"; 

var item1 = new OrderItem(); 
item1.ItemID = "abcd123"; 
item1.Quantity = 1; 

var item2 = new OrderItem(); 
item2.ItemID = "fghi456"; 
item2.Quantity = 2; 

var order = new Order(); 
order.OrderID = "xyz"; 
order.Customer = customer; 
order.Items.Add(item1); 
order.Items.Add(item2);

// 使用初始化
var order = new Order
{
    OrderID = "xyz",
    Customer = new Customer { Name = "Jon", Address = "UK" },
    Items =
    {
        new OrderItem { ItemID = "abcd123", Quantity = 1 },
        new OrderItem { ItemID = "fghi456", Quantity = 2 }
    }
};
```

很明显，不仅降低了冗余程度，可读性也大大增加了。

#### 3.3.1 对象初始化器

根据上面的代码，可以看出对象初始化器语法`{ property = initializer-value, ...}`，其中property可以是字段或属性，initializer-value是用于初始化的值，可以是表达式、集合初始化器或者其他对象初始化器。

如果对象的构造函数没有参数，使用对象初始化器就可以省略()，就像上面代码一样。对象初始化器实际访问了属性的set访问器。

如果初始化值是另一个对象初始化器、，则不会调用set访问器，而是会调用get访问器，并将对象初始化器的结果用在get访问器返回的属性上。（不理解的看下面代码）

```c#
HttpClient client = new HttpClient
{
    DefaultRequestHeaders = // 调用get 
    {
        From = "user@example.com", // 调用set
        Date = DateTimeOffset.UtcNow // 调用set
    }
};
// 上面的代码等同于
HttpClient client = new HttpClient();
var headers = client.DefaultRequestHeaders; // 调用get
headers.From = "user@example.com"; // 调用set
headers.Date = DateTimeOffset.UtcNow; // 调用set
```

#### 3.3.2 集合初始化器

语法为`{ initializer-value, initializer-value, ... }`，initializer-value是用于初始化的值，可以是表达式或者另一个集合初始化器。集合初始化器只能用于构造器调用或者对象初始化器中。

构造器调用就是下面这样：

```c#
var beatles = new List<string> { "John", "Paul", "Ringo", "George" };
// 等同于
var beatles = new List<string>();
beatles.Add("John");
beatles.Add("Paul");
beatles.Add("Ringo");
beatles.Add("George");
```

编译器会转换成如上形式，会调用集合类型的Add方法，如果是字典这种一个元素多个变量的类型（键值对），则用大括号。

```c#
var releaseYears = new Dictionary<string, int>
{
     { "Please please me", 1963 },
     { "Revolver", 1966 },
     { "Sgt. Pepper’s Lonely Hearts Club Band", 1967 },
     { "Abbey Road", 1970 }
};
// 等同于
var releaseYears = new Dictionary<string, int>();
releaseYears.Add("Please please me", 1963);
releaseYears.Add("Revolver", 1966);
releaseYears.Add("Sgt. Pepper’s Lonely Hearts Club Band", 1967);
releaseYears.Add("Abbey Road", 1970);
```

这也要求使用集合初始化器的类型必须是实现IEnumerable接口的类型。

#### 3.3.3 与LINQ的关系

> 读者可能会好奇：这些特性对于LINQ有什么用呢？前面曾提过，几乎C# 3的所有特性都是为LINQ服务的，那么对象初始化器和集合初始化器的作用何在呢？答案就是：与LINQ相关的其他特性都要求代码具备单一表达式的表达能力。（例如在一个查询表达式中，对于一个给定的输入，select子句不支持通过多条语句生成结果。） 
