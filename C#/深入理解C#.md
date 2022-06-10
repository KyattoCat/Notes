# C# in Depth 4th

前面几章是回顾了C#2到C#5的发展历程，所以这里就简单写一下，毕竟后续版本会更新之前版本的特性。

## C#2

### 2.1 泛型

#### 2.1.1 泛型

自C#2后，为了解决C#1中使用一个特定类型集合就需要自己创建一个新的类型，出现了泛型。

并非所有类型或类型成员都适用泛型，比如枚举不能声明为泛型，字段、属性、索引器、构造器、终结器、事件不能被声明为泛型。

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
true && null == null;// 该式结果取决于null 意思就是把null当作一个变量 (true && 变量N)的结果自然取决于N
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
>         control.Click += delegate
>         {
>             Console.WriteLine("Control Clicked: {0}", message);
>         }
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
>        yield return 10;
>        for (int i = 0; i < 3; i++) 
>        { 
>            yield return i;
>        }
>        yield return 20;
> }
> // ......
> IEnumerable<int> enumerable = CreateSimpleIterator(); 
> using (IEnumerator<int> enumerator = enumerable.GetEnumerator())
> {
>        while (enumerator.MoveNext())
>        {
>            int value = enumerator.Current;
>            Console.WriteLine(value);
>        }
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

```c#
// foreach的编译结果
// 代码来自迭代器实现机制第一行给的文章
System.Collections.Generic.Stack<int> stack =
    new System.Collections.Generic.Stack<int>();
int number;
using(System.Collections.Generic.Stack<int>.Enumerator enumerator = stack.GetEnumerator())
{
    while (enumerator.MoveNext())
    {
        number = enumerator.Current;
        Console.WriteLine(number);
    }
}
```



由于上述特性，迭代器可以用于需要释放资源的地方，比如文件处理器。

#### 2.4.3 迭代器实现机制

[.NET 本质论 - 了解 C# foreach 的内部工作原理和使用 yield 的自定义迭代器](https://docs.microsoft.com/zh-cn/archive/msdn-magazine/2017/april/essential-net-understanding-csharp-foreach-internals-and-custom-iterators-with-yield)

```c#
// 原代码
public static IEnumerable<int> GenerateIntegers(int count)
{
    try
    {
        for (int i = 0; i < count; i++)
        {
            Console.WriteLine("Yielding {0}", i);
            yield return i;
            int doubled = i * 2; // 注意这个局部变量
            Console.WriteLine("Yielding {0}", doubled);
            yield return doubled;
        }
    }
    finally
    {
        Console.WriteLine("In finally block");
    }
}
```

```c#
// 反编译并将变量命名改为容易阅读的版本
public static IEnumerable<int> GenerateIntegers(int count) // 原方法入口（桩方法）
{
    GeneratedClass ret = new GeneratedClass(-2);
    ret.count = count;
    return ret; // 把状态机返回给调用方
}
private class GeneratedClass : IEnumerable<int>, IEnumerator<int> // 状态机的简化版本
{
    public int count; // 原参数
    /// -3 MoveNext()正在执行
    /// -2 GetEnumerator()尚未被调用
    /// -1 执行完毕（无论成功与否）
    /// 0  GetEnumerator被调用，尚未调用MoveNext()
    /// 1  第一条yield return语句
    /// 2  第二条yield return语句
    /// N  第N条yield return语句
    private int state; // 状态
    private int current; 
    private int initialThreadId; 
    private int i; // 循环体中的局部变量i
    // 可以看出在状态机内并没有保存doubled这个局部变量，这个变量被优化掉了
    // 因为在原代码中doubled在执行完yield return后就没有意义了
    public GeneratedClass(int state) // 
    {
        this.state = state;
        initialThreadId = Environment.CurrentManagedThreadId;
    }
    public bool MoveNext() { ... } // 状态机主体代码
    public IEnumerator<int> GetEnumerator() { ... } // 如果有必要则创建新的状态机 
    public void Reset() // 一般不会实现Reset方法 会抛出异常
    {
        throw new NotSupportedException();
    }
    public void Dispose() { ... } // 执行finally块
    public int Current { get { return current; } } // 状态机当前的值
    private void Finally1() { ... } // finally块代码（书上确实带了个1
    IEnumerator Enumerable().GetEnumerator() // 非通用接口的显式实现 不太明白
    { 
        return GetEnumerator(); 
    } 

    object IEnumerator.Current { get { return current; } } 
}
```

```c#
public bool MoveNext()
{
    try
    {
        switch (state)
        {
                // 跳转表负责跳转到方法中的正确位置
                // 反编译后这里有大量goto语句
        }
        // 方法代码在每个yield return都会返回
        // switch中goto跳转到这个部分
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

### 3.4 匿名类型

#### 3.4.1 基本语法

```c#
var player = new
{
    Name = "Rajesh",
    Score = 3500
};
```

- 匿名类型语法类似对象初始化器，但无需指定类型名称，该类型内部还可以继续嵌套匿名类型。
- 使用var关键字，因为不知道是什么类型的，虽然object也可以，不过一般不用object。
- 内部属性类型通过自动推断确定，所以匿名类型本质还是静态类型。

> 匿名类型有哪些用途呢？这就涉及LINQ了。当执行一个查询时，不管被查询的数据源是SQL数据库还是对象集合，经常需要一种特定的、不同于源数据、只在查询语句中有意义的数据形态。
>
> 假设有一个集合，集合中每个人都有最喜欢的颜色。我们需要把查询结果绘制成一张直方图，该查询结果集按照颜色和喜欢该颜色的人数进行划分，于是这个数据形态所代表的含义只在这个特定的上下文中有意义。使用匿名类型可以更精练地表达这种“一次性”的类型需求，同时还不失静态类型的优势。

匿名类型还有一种简化创建表达式的方式，称为投射初始化器，使用该方式会使匿名类型中的属性或字段名称与初始化时用的值相同：

```c#
// 借用3.3.1的Order类
var flattenedItem = new
{
    order.OrderID,
    customer.Address,
    item.ItemID,
    item.Quantity
};
// 等同于
var flattenedItem = new
{
    OrderID = order.OrderID,
    Address = customer.Address,
    ItemID = item.ItemID,
    Quantity = item.Quantity
};
```

但是我觉得这种方式不太好，一旦初始化值的属性名称改变了，匿名类型里的变量名称也会跟着改变，使用到匿名类型变量的代码会不会出现一些问题？

#### 3.4.2 编译器生成类型

匿名类型最终还是有自己的实际类型的，这是由编译器自动生成的。

> 当采用微软C#编译器时，匿名类型具备以下特点。
>
> - 它是一个类（保证）。
>
> - 其基类是object（保证）。
>
> - 该类是密封的（不保证，虽然非密封的类并没有什么优势）。
> - 属性是只读的（保证）。
> - 构造器的参数名称与属性名称保持一致（不保证，有时对于反射有用）。
> - 对于程序集是internal的（不保证，在处理动态类型时会比较棘 手）。
> - 该类会覆盖GetHashCode()和Equals()方法：两个匿名类型只有在所 有属性都等价的情况下才等价。（可以正常处理null值。）只保证会覆盖这两个方法，但不保证散列值的计算方式。
> - 覆盖并完善ToString()方法，用于呈现各属性名称及其对应值。这一点不保证，但对于问题诊断来说作用重大。
> - 该类型为泛型类，其类型形参会应用于每一个属性。具有相同属性名称但属性类型不同的匿名类型，会使用相同的泛型类型，但拥有不同的类型实参。这一点不保证，不同编译器的实现方式不同。
> - 如果两个匿名对象创建表达式使用相同的属性名称，具有相同的属性类型以及属性顺序，并且在同一个程序集中，那么这两个对象的类型相同。

匿名类型也可以用于创建隐式类型数组，但是必须满足上面引用原文中的最后一个条件：相同属性类型、相同顺序、相同程序集。

#### 3.4.A

只是说说我自己的看法，匿名类型似乎局限性蛮大的，而且感觉可读性反而不是很好，目前在项目中没有见过任何匿名类型。

### 3.5 lambda表达式

自C#3引入lambda表达式，通过内联代码的方式创建委托实例的过程变得更简洁了（之前的创建过程见2.3.2委托的匿名方法创建）。

> C#的设计团队出于各种必要的原因，花费大量精力来简化委托实例的创建过程，其中LINQ是最重要的一个原因。

#### 3.5.1 lambda表达式语法

```C#
// 原
Action<string> action = delegate(string message)
{
    Console.WriteLine("In delegate: {0}", message);
};
action("Message");
// 引入lambda
// 参数列表 => 主体
Action<string> action = (string message) =>
{
    Console.WriteLine("In delegate: {0}", message);
};
action("Message");
```

看起来貌似差不多嘛，也就把`delegate`省略了然后加了一个`=>`这玩意儿代替，不过在特殊场景下，lambda表达式还能更短。

```c#
Action<string> action =
    (string message) => Console.WriteLine("In delegate: {0}", message); // 单句表达式可以省略大括号
Action<string> action =
    (message) => Console.WriteLine("In delegate: {0}", message); // 由于action是Action<string>类型的，所以参数类型可以被推断出来
Action<string> action =
    message => Console.WriteLine("In delegate: {0}", message); // 这种情况甚至可以把括号省略
```

```c#
Func<int, int, int> multiply = (int x, int y) => { return x * y; }; // 原始版本
Func<int, int, int> multiply = (int x, int y) => x * y; // 仅一条return语句，可以省略return和大括号
Func<int, int, int> multiply = (x, y) => x * y; // 由于有两个参数所以小括号不能省了，类型依旧由编译器推断
```

总结一下特殊场景：

- 仅一句表达式（省略大括号）
- 仅一句return语句（省略大括号和return）
- 参数可以推断（省略参数类型）
- 仅一个参数（省略小括号）

#### 3.5.2 捕获变量

在lambda表达式内，可以像普通方法一样使用变量，比如类里的字段、this变量、方法的参数、局部变量。lambda表达式携带的参数不属于捕获变量的范畴，因为她是在lambda表达式局部内的。需要注意的是，在捕获变量时，lambda表达式捕获的是变量本身，而不是创建委托实例时的变量值，也就是在创建委托实例后，修改了捕获的变量的值，那么委托执行时输出的变量也时修改后的。

```c#
class CapturedVariablesDemo 
{ 
    private string instanceField = "instance field"; 
    public Action<string> CreateAction(string methodParameter) 
    { 
        string methodLocal = "method local"; 
        string uncaptured = "uncaptured local"; 
        Action<string> action = lambdaParameter => 
        {
            string lambdaLocal = "lambda local"; 
            Console.WriteLine("Instance field: {0}", instanceField); // 实例字段
            Console.WriteLine("Method parameter: {0}", methodParameter); // 方法参数
            Console.WriteLine("Method local: {0}", methodLocal); // 方法局部变量
            Console.WriteLine("Lambda parameter: {0}", lambdaParameter); // lambda表达式参数
            Console.WriteLine("Lambda local: {0}", lambdaLocal); // lambda表达式局部变量
        };
        methodLocal = "modified method local"; 
        return action; 
    } 
}
// 执行委托
var demo = new CapturedVariablesDemo();
Action<string> action = demo.CreateAction("method argument");
action("lambda argument");
```

> - 如果没有捕获任何变量，那么编译器可以创建一个静态方法，不需要额外的上下文。 
>
> - 如果仅捕获了实例字段，那么编译器可以创建一个实例方法。在这种情况下，捕获1个实例字段和捕获100个没有什么差别，只需一个this便可都可以访问到。 
>
> - 如果有局部变量或参数被捕获，编译器会创建一个私有的嵌套类来保存上下文信息，然后在当前类中创建一个实例方法来容纳原lambda表达式的内容。原先包含lambda表达式的方法会被修改为使用嵌套类来访问捕获变量。
>
> 具体实现细节因编译器而异。

```c#
// lambda表达式转译后代码 因为编译器不会真正生成C#代码 这里只是展示捕获原理
private class LambdaContext // 生成私有嵌套类保存捕获变量 
{
    public CapturedVariablesDemoImpl originalThis; // 捕获的变量 
    public string methodParameter; 
    public string methodLocal; 

    public void Method(string lambdaParameter) // lambda表达式体变成一个实例方法 
    { 
        string lambdaLocal = "lambda local"; 
        Console.WriteLine("Instance field: {0}", 
        originalThis.instanceField); 
        Console.WriteLine("Method parameter: {0}", methodParameter); 
        Console.WriteLine("Method local: {0}", methodLocal); 
        Console.WriteLine("Lambda parameter: {0}", lambdaParameter); 
        Console.WriteLine("Lambda local: {0}", lambdaLocal); 
    } 
}

public Action<string> CreateAction(string methodParameter) 
{ 
    LambdaContext context = new LambdaContext(); // 生成类用于所有捕获的变量 
    context.originalThis = this; 
    context.methodParameter = methodParameter; 
    context.methodLocal = "method local"; 
    string uncaptured = "uncaptured local"; 
    Action<string> action = context.Method; 
    context.methodLocal = "modified method local"; 
    return action; 
}
```

根据上面代码可以看出lambda表达式是如何捕获变量的。下面再看一种情况，被捕获的局部变量是被多次实例化的：

```c#
static List<Action> CreateActions()
{
    List<Action> actions = new List<Action>();
    for (int i = 0; i < 5; i++)
    {
        string text = string.Format("message {0}", i);
        actions.Add(() => Console.WriteLine(text));
    }
    return actions;
}
```

如上所示，text变量在循环内多次实例化，每个text对于lambda表达式都是独立的。其实就相当于创建了多个私有类对象，对象内的text引用指向的是各个循环中属于这个对象的text。

```c#
private class LambdaContext
{
    public string text;
    public void Method()
    {
        Console.WriteLine(text);
    }
}
static List<Action> CreateActions()
{
    List<Action> actions = new List<Action>();
    for (int i = 0; i < 5; i++)
    {
        LambdaContext context = new LambdaContext(); // 每次循环创建一个对象
        context.text = string.Format("message {0}", i); // 所以每个对象的text都是独立的
        actions.Add(context.Method); // 用这个对象来创建Action
    }
    return actions;
}
```

最后看一下捕获不同作用域的变量会发生什么：

```c#
static List<Action> CreateCountingActions()
{
    List<Action> actions = new List<Action>();
    int outerCounter = 0; // 这个变量是方法局部变量 被两个委托捕获
    for (int i = 0; i < 2; i++)
    {
        int innerCounter = 0; // 循环内的局部变量 和之前情况一样会创建多个实例 对于两个委托来说是独立的
        Action action = () =>
        {
            Console.WriteLine("Outer: {0}; Inner: {1}", outerCounter, innerCounter);
            outerCounter++;
            innerCounter++;
        };
        actions.Add(action);
    }
    return actions;
}

List<Action> actions = CreateCountingActions();
actions[0](); // Outer: 0; Inner: 0
actions[0](); // Outer: 1; Inner: 1
actions[1](); // Outer: 2; Inner: 0
actions[1](); // Outer: 3; Inner: 1
```

这种情况编译器会怎么创建私有类？答案是会创建多个私有类：

```c#
private class OuterContext // 外层作用域上下文
{
    public int outerCounter; // 外层的变量
}

private class InnerContex // 内层作用域上下文
{
    public OuterContext outerContext; // 包含外层上下文的引用
    public int innerCounter; // 内层的变量
    
    public void Method()
    {
        Console.WriteLine("Outer: {0}; Inner: {1}", outerContext.outerCounter, innerCounter);
        outerContext.outerCounter++;
        innerCounter++;
    }
}

static List<Action> CreateCountingActions()
{
    List<Action> actions = new List<Action>();
    OuterContext outerContext = new OuterContext(); // 创建外层上下文保存变量
    outerContext.outerCounter = 0;
    for (int i = 0; i < 2; i++)
    {
        InnerContext innerContext = new InnerContext(); // 内层则与之前一样多次创建
        innerContext.outerContext = outerContext;
        innerContext.innerCounter = 0;
        Action action = innerContext.Method;
        actions.Add(action);
    }
    return actions;
}
```

这意味着在使用lambda表达式时需要注意可能会由于捕获变量导致编译器创建过多对象而影响性能。

#### 3.5.3 表达式树

把代码当文本使！

```c#
Expression<Func<int, int, int>> adder = (x, y) => x + y;
Console.WriteLine(adder); // 猜猜打印出什么？ "(x, y) => x + y"！ //不过在我电脑上打印出来的是(x, y) => (x + y)
```

编译器并没有在任何地方创建一个硬编码的字符串，上面的打印结果是表达式树动态构建出来的，这意味着代码是可以在执行时进行检查的。

看着挺短，这是编译器帮我们做了很多事情，如果手动构建表达式树还挺麻烦的：

```c#
ParameterExpression xParameter = Expression.Parameter(typeof(int), "x");
ParameterExpression yParameter = Expression.Parameter(typeof(int), "y");
Expression body = Expression.Add(xParameter, yParameter);
ParameterExpression[] parameters = new[] { xParameter, yParameter };
Expression<Func<int, int, int>> adder = Expression.Lambda<Func<int, int, int>>(body, parameters);
Console.WriteLine(adder);
```

lambda转换为表达式树是存在限制的，只有一个表达式主体的lambda表达式才能转换为表达式树

上面代码中`(x, y) => x + y`可以转换，但是`(x, y) => { return x + y;}`就不行了。

这也引出对象和集合初始化器的重要性，因为她们可以在一个表达式内完成初始化工作，所以才能用在表达式树上：

```c#
Expression<Func<int, int, Test>> adder = (x, y) => new Test{ X = x, Y = y }; // 对象初始化
Expression<Func<int, int, List<int>>> adder = (x, y) => new List<int>{ x, y }; // 集合初始化
Expression<Func<int, int, object>> adder = (x, y) => new { X = x, Y = y }; // 匿名类型
```

构建好表达式树后，可以通过表达式树动态创建委托：

```、c#
Expression<Func<int, int, int>> adder = (x, y) => x + y;
Func<int, int, int> executableAdder = adder.Compile();  // 有一个Compile方法可以编译表达式树
Console.WriteLine(executableAdder(2, 3)); // 可以正常调用委托
```

~~虽然我觉得如果真要用这个表达式树做动态委托创建还是很麻烦的事。~~

> 这项能力可以和反射特性搭配使用，用于访问属性、调用方法来生成并缓存委托，其结果与手动编写委托结果相同。对于单一的方法调用或访问属性，已经存在现有的方法来直接创建委托，不过有时需要额外的转换或操作步骤，而使用表达式树来实现的话十分简便。
>
> 等到介绍完所有相关特性之后，我们再回头讨论为什么表达式树对于LINQ如此重要。

### 3.6 扩展方法

通过编写静态方法扩展原有类的方法，语法：

```c#
public static class DictionaryExtensions
{
    public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    {
        // 处理一些东西
        return default;
    }
}

Dictionary<int, int> dict = new Dictionary<int, int>();
Console.WriteLine(dict.GetValue(1)); // 输出0，因为default(int) == 0
```

关键在于第一个参数前加上一个`this`关键字，参数类型就是你希望扩展的类型。

查找扩展方法的优先级顺序如下：

1. 同命名空间下的静态类
2. 父命名空间下的静态类
3. 全局命名空间下的静态类
4. using指定的命名空间下的静态类
5. （仅在C#6中）using static指定的静态类

还有一个问题就是null调用的问题：

`x.GetValue(1); // x == null`

如果GetValue是实例方法，那么会报空引用的错误。如果GetValue是扩展方法，那么还是会调用扩展方法。所以扩展方法应当要对null值做特殊处理。

下面展示一个扩展方法的链式调用：

```c#
string[] words = { "keys", "coat", "laptop", "bottle" }; 
IEnumerable<string> query = words
    .Where(word => word.Length > 4) 
    .OrderBy(word => word) 
    .Select(word => word.ToUpper()); 
```

是不是很眼熟？yep，很像SQL语句。

如果不用扩展方法，而是普通的静态方法做相同查询，那代码就有点emmm：

```c#
string[] words = { "keys", "coat", "laptop", "bottle" };
IEnumerable<string> query =
    Enumerable.Select(
        Enumerable.OrderBy(
            Enumerable.Where(words, word => word.Length > 4),
            word => word),
        word => word.ToUpper());
```

可读性低到爆炸，Where明明需要第一个调用，但是在上面代码里面却是嵌在最里面那一层的。改进方法显而易见，用临时变量存上个方法执行的结果：

```c#
string[] words = { "keys", "coat", "laptop", "bottle" };
var tmp1 = Enumerable.Where(words, word => word.Length > 4);
var tmp2 = Enumerable.OrderBy(tmp1, word => word);
var query = Enumerable.Select(tmp2, word => word.ToUpper());
```

顺序正常了，但是容易出现大量局部变量混杂在一起，分散注意力，所以还是扩展方法那一版看起来比较好。

### 3.7 查询表达式

该特性是专门为LINQ设计的，目的就是为了再次简化查询代码。下面的查询代码等价于3.6中出现的那个扩展方法链式调用查询。

```c#
IEnumerable<string> query = from word in words
                            where word.Length > 4
                            orderby word
                            select word.ToUpper();
```

~~（已经完全变成SQL的形状了（呜~））~~

3.5.2节捕获变量中代码是通过转译为C#代码后来讲解的，实际上编译器不会生成任何C#代码。但是查询表达式被C#语言规范直接定义为一种语法转译，上面代码转译后的结果就和3.6中链式调用查询的代码形式一致。

然后引入两个概念：范围变量和隐形标识符。

```c#
from word in words // 这里由from子句引入的word就是范围变量
where word.Length > 4 // 后续可以使用word
orderby word
select word.ToUpper();
```

```c#
from word in words
let length = word.Length // 还可以使用let关键字指定新的范围变量
where length > 4
orderby length
select string.Format("{0}: {1}", length, word.ToUpper());
```

由上面代码转译后的代码如下：

```c#
words.Select(word => new { word, length = word.Length }) // 多个范围变量出现时就会被转为匿名类型
    .Where(tmp => tmp.length > 4) // 这里的tmp并没有在查询表达式中出现
    .OrderBy(tmp => tmp.length)   // 实际上tmp不属于转译的一部分，语言规范并没有规定参数名称
    .Select(tmp =>                // 这个参数tmp在写查询时是不可见的，所以称为隐形标识符
         string.Format("{0}: {1}", tmp.length, tmp.word.ToUpper()));
```

虽然但是，在某些情况下，查询表达式是没有使用扩展方法查询简洁的，所以要看情况使用合适的方法。

### 3.8 LINQ

> ```c#
> var products = from product in dbContext.Products
>                where product.StockCount > 0
>                orderby product.Price descending
>                select new { product.Name, product.Price };
> ```
>
> 短短4行代码，应用了所有新特性。 
>
> - 匿名类型，包括投射初始化器（只选择name和price这两个属性）。 
> - 使用var声明的匿名类型，因为无法声明products变量的有效类型。
> - 查询表达式。当然对于本例可以不使用查询表达式，但对于更复杂的情况，使用查询表达式能事半功倍。 
> - lambda表达式。lambda表达式在这里作为查询表达式转译之后的结果。
> - 扩展方法。它使得转译后的查询可以通过Queryable类实现，因为dbContext.Products实现了IQueryable<Product>接口。 
> - 表达式树。它使得查询逻辑可以按照数据的方式传给LINQ提供器，然后转换成SQL语句并交由数据库执行。 
>
> 不管缺少上述哪个特性，LINQ的实用性都将大打折扣。虽然我们可以用内存集合来取代表达式树，虽然不用查询表达式也能写出可读性比较强的简单查询，虽然不用扩展方法也可以使用专用的类配合相关方法，但是这些特性加在一起将别开生面。

## C#4 互操作性提高

### 4.1 动态类型

语法很简单，`dynamic`关键字：

```c#
dynamic text = "hello world"; 
string world = text.Substring(6); 
Console.WriteLine(world);
string broken = text.SUBSTR(6);  // 没有这个方法，但是编译时不会报错，而是运行时报错
Console.WriteLine(broken);
```

以我目前水平完全没有接触到dynamic（不如说我本能的在静态语言里抗拒使用动态类型），这里就补充一些文章：

1. [使用类型 dynamic（C# 编程指南）](https://docs.microsoft.com/zh-cn/dotnet/csharp/programming-guide/types/using-type-dynamic)
2. [Working with the Dynamic Type in C#](https://www.red-gate.com/simple-talk/development/dotnet-development/working-with-the-dynamic-type-in-c/)

### 4.2 可选形参和命名实参

代码一看就懂：

```c#
static void Method(int x, int y = 5, int z = 10) // 这个就是带默认值的可选形参
{
    Console.WriteLine($"x = {x}, y = {y}, z = {z}");
}
// ...
Method(1); // 因为参数可选，所以等同Method(1, 5, 10)
Method(x:10, z:20); // 这个就是命名实参，可以指定形参赋值，可以不按照形参顺序 等同Method(10, 5, 20)
Method(1, z:2, y:3); // 不带名称的参数称为定位实参，定位实参必须在命名实参前
```

限制：

1. 带默认值的可选形参必须在形参列表的最后，除了params形参数组
2. 调用时命名实参必须在定位实参之后（C#7.2后有所放宽）
3. 定位实参和命名实参不能重复（比如`Method(1, x : 2)`就是非法的，因为参数x重复）
4. 对于`ref`和`out`修饰的形参不能有默认值
5. 默认值必须是编译时可以确定的常量值（null，数值，字符串）
6. 默认值可以是default表达式（像是`default(TestClass)`这样的，其实也是一个常量值）
7. 默认值可以是值类型的new表达式（注意只能是值类型）

形参默认值会被嵌入到IL代码中。

### 4.3 COM互操作性

跳过。

### 4.4 泛型型变

```c#
IEnumerable<string> strings = new List<string> { "a", "b", "c" };
IEnumerable<object> objects = strings;
```

上面的代码有啥问题吗？string类型也是object类型对吧，那么string类型的序列自然也是object类型的序列对吧，所以上面的代码没啥问题。

但是，这在C#4之前确实时不能通过编译的，而且如果将序列拓展为列表，代码却不能过编译了：

```c#
IList<string> strings = new List<string> { "a", "b", "c" };
IList<object> objects = strings; // 无法通过编译
```

为什么呢？这是因为读写的问题。

对于序列`IEnumerable<T>`，T类型变量只作输出，对于`IList<T>`是可以通过Add方法添加T类型的数据的。

```c#
IList<string> strings = new List<string> { "a", "b", "c" };
IList<object> objects = strings;
objects.Add(new object());
string element = strings[3];
```

> 除第2行外，其他代码单独看都没有问题。把一个object类型的引用添加到`IList<object>`列表中没有问题，从`IList<string>`类型的列表中读取一个`string`的元素中也没有问题。可是如果允许把`string`类型的列表看作`object`类型的列表，上面两个行为就会发生冲突。因此C#从语言规则上禁止第2行代码，以保证另外两个操作是安全的。 

还有一种情况，T类型只做输入，比如`Action<T>`。

```c#
Action<object> objectAction = obj => Console.WriteLine(obj);
Action<string> stringAction = objectAction;
stringAction("Print me");
```

对于一个可以接收object类型的Action，必然可以接收string类型。

定义：

- 协变。泛型只作输出。
- 逆变。泛型只作输入。
- 不变。泛型既作输出也作输入。

> C#变体的第一要点：变体只能用于接口和委托，例如类或结构体的协变是不存在的。第二：变体的定义与每一个具体的类型形参绑定。可以概括地说“`IEnumerable<T>`是协变的”，而更准确的说法是“`IEnumerable<T>`对于类型T是协变的”。C#为此还推出了新语法：在声明接口和委托的语法中，可以为每个类型形参添加独立修饰符。`IEnumerable<T>`和`IList<T>`接口以及`Action<T>`委托的声明方式如下： 
>
> ```c#
> public interface IEnumerable<out T>  // T协变
> public delegate void Action<in T> // T 逆变
> public interface IList<T> // T 不变
> ```

使用in、out等关键字修饰的委托或接口一旦出现违背修饰符的情况就会报错。

```c#
public delegate void InvalidCovariant<out T>(T input) // 非法，协变用作输出
public interface IInvalidContravariant<in T>
{
    T GetValue(); // 非法，逆变用作输出
}
```

重申一下前文的要点：变体只能用于接口和委托，不能被实现接口的类或结构体继承。假设有如下类定义：

```c#
public class SimpleEnumerable<T> : IEnumerable<T> // 这里不能使用out定义协变
{
    // ...
}

// 类与类之间没有变体关系
// SimpleEnumerable<string>不能转为SimpleEnumerable<object>
// 可以利用协变将
// SimpleEnumerable<string>转为IEnumerable<object>
// 个人理解因为SimpleEnumerable<string>被看作IEnumerable<object>
```

> 假设我们正在处理某些委托或接口，这些委托或接口具有协变或逆变的类型形参，那么哪些类型转换是可行的呢？解释规则前先定义几个术语。
>
> - 包含变体的转换称为变体转换。 
> - 变体转换属于引用转换。引用转换不改变变量的值，它只改变变量在编译时的类型。 
> - 一致性转换指的是从一个类型转换为一个相同的（从CLR的角度看）类型。它可以是在C#中同类型之间的转换（例如string类型到string类型的转换），也可以是C#中不同类型间的转换，例如从object到dynamic的转换。 
>
> 假设有类型实参A和B，我们希望将`IEnumerable<A>`转换成`IEnumerable<B>`。只有存在从A到B的一致性转换或隐式引用转换时，才能完成目标转换。
>
> 以下转换均合法。 
>
> - `IEnumerable<string>`到`IEnumerable<object>`，因为子类到基类（或者基类的基类，以此类推）都属于隐式引用转换。 
> - `IEnumerable<string>`到`IEnumerable<IConvertible>`，因为实现类到其接口的转换属于隐式引用转换。 
> - `IEnumerable<IDisposable>`到`IEnumerable<object>`，任何引用类型到object或者dynamic类型都属于隐式引用转换。 
>
> 以下转换皆非法。 
>
> - `IEnumerable<object>`到`IEnumerable<string>`，因为object到string属于显式引用转换，而非隐式。 
> - `IEnumerable<string>`到`IEnumerable<Stream>`：string类和Stream类属于非相关类。 
> - `IEnumerable<int>`到`IEnumerable<IConvertible>`：int到IConvertible存在隐式类型转换，但是它属于装箱转换，而不是引用转换。 
> - `IEnumerable<int>`到`IEnumerable<long>`：int到long存在隐式类型转换，但属于数值转换而非引用转换。
>
> 如上所示，类型实参的转换必须是引用转换或一致性转换的要求，出人意料地影响了值类型。

对于多个泛型参数的类型比如Func委托，那么就对参数列表中所有参数进行检查。

## C#5 异步

```c#
class Program
{
    static readonly HttpClient httpClient = new HttpClient();
    static void Main(string[] args)
    {
        DisplayWebSiteLength();
        while (true) ;
    }

    static async void DisplayWebSiteLength()
    {
        Task<string> task = httpClient.GetStringAsync("https://www.baidu.com");
        string text = await task;
        Console.WriteLine(text.Length);
    }
}
```

简单说明异步代码语法。

- 在需要等待的方法之前使用await修饰。
- 若某个方法中出现await关键字，则该方法需要用async修饰。
- await修饰的方法返回值必须为`Task`、`Task<TResult>`、`void`或自定义task类型（C#7引入）

当执行到方法中的await表达式时，方法立即返回。对于上面的代码示例就是执行到await task时直接返回到主函数，当task执行完毕时再继续执行后续代码。

这是当程序执行到await时，如果await修饰的代码没有执行完毕，那么编译器就会创建一个续延（continuation），当GetStringAsync这个Web操作执行完毕后，程序就会继续执行该续延。

> 定义 续延本质上是回调函数（类型为Action的委托），当异步操作（任务）执行完成后被调起。在async方法中，续延负责维护方法的状态。类似于闭包维护变量的上下文，续延会记录方法的执行位置，以便之后恢复方法的执行。Task类有一个专门用于附加续延的方法：Task.ContinueWith。上述过程是由编译器创建的一个复杂的状态机完成的。第6章会探讨该状态机的实现细节，现在重点介绍async/await所提供的功能。首先探讨对于异步编程，开发人员想要的功能和语言实际所能提供的功能。

await在C#中就是为了请求编译器为我们创建续延。

再定义一个东西：异步操作，我的理解异步操作就是使用await关键字调用的方法。

基于任务的异步模式中，延续并没有传递给异步操作，而是异步操作发起并返回了一个Token，这个Token可供续延使用。这个Token代表正在执行的操作，该操作可能在返回调用方之前就已经执行完毕了，也可能还在执行。该Token用于表达：在该操作完成前不能开始后续的处理操作。

> C# 5的异步方法典型的执行流程如下： 
>
> (1) 执行某些操作； 
>
> (2) 启动一个异步操作，并记录其返回的令牌； 
>
> (3) 执行某些其他操作（通常在异步操作完成前不能进行后续操作，对应这一步应该为空）； 
>
> (4) （利用令牌）等待异步操作完成； 
>
> (5) 执行其他操作； 
>
> (6) 完成执行。 

-----

> [深入.NET之异步原理](https://www.cnblogs.com/whisperedwords/articles/15432285.html)
>
> ```c#
> internal sealed class Type1 { }
> internal sealed class Type2 { }
> private static async Task<type1> Method1Async() 
> {
>     /* Does some async thing that returns a Type1 object */ 
> }
> private static async Task<type2> Method2Async() 
> { 
>     /* Does some async thing that returns a Type2 object */ 
> }
> 
> private static async Task<string> MyMethodAsync(Int32 argument) 
> {
>     Int32 local = argument;
>     try
>     {
>         Type1 result1 = await Method1Async();
>         for (Int32 x = 0; x < 3; x++) 
>         {
>             Type2 result2 = await Method2Async();
>         }
>     }
>     catch (Exception) 
>     {
>         Console.WriteLine("Catch");
>     }
>     finally 
>     {
>         Console.WriteLine("Finally");
>     }
>     return "Done";
> }
> ```
>
> ```c#
> // AsyncStateMachine attribute indicates an async method (good for tools using reflection); 
> // the type indicates which structure implements the state machine
> [DebuggerStepThrough, AsyncStateMachine(typeof(StateMachine))]
> private static Task<string> MyMethodAsync(Int32 argument) 
> {
>     // Create state machine instance & initialize it
>     StateMachine stateMachine = new StateMachine() 
>     {
>         // Create builder returning Task<string> from this stub method
>         // State machine accesses builder to set Task completion/exception
>         m_builder = AsyncTaskMethodBuilder<string>.Create(), 
>         m_state = -1, // Initialize state machine location
>         m_argument = argument // Copy arguments to state machine fields
>     };
>     // Start executing the state machine
>     stateMachine.m_builder.Start(ref stateMachine);
>     return stateMachine.m_builder.Task; // Return state machine's Task
> }
> 
> // This is the state machine structure
> [CompilerGenerated, StructLayout(LayoutKind.Auto)]
> private struct StateMachine : IAsyncStateMachine
> {
>     // Fields for state machine's builder (Task) & its location
>     public AsyncTaskMethodBuilder<string> m_builder;
>     public Int32 m_state;
>     // Argument and local variables are fields now:
>     public Int32 m_argument, m_local, m_x;
>     public Type1 m_resultType1;
>     public Type2 m_resultType2;
>     // There is 1 field per awaiter type.
>     // Only 1 of these fields is important at any time. That field refers 
>     // to the most recently executed await that is completing asynchronously:
>     private TaskAwaiter<type1> m_awaiterType1;
>     private TaskAwaiter<type2> m_awaiterType2;
>     // This is the state machine method itself
>     void IAsyncStateMachine.MoveNext()
>     {
>         // 用于保存结果值
>         String result = null;
>         // 编译器生成的try-catch语句块,用于确保状态能够执行完成且捕捉到await的task抛出的异常 
>         try
>         {
>             // 假设我们每次进入该方法时都是在finally块中,如果根据状态判断出当前不在finally块中,则会将该值设为false.
>             Boolean executeFinally = true; 
>             if (m_state == -1)
>             {
>                 //m_state== -1表示我们是初次进入该方法,将参数值赋值给m_local
>                 m_local = m_argument; 
>             }
>             // 这个try-catch块是我们代码中的块
>             try
>             {
>                 //声明两个 TaskAwaiter<>变量
>                 TaskAwaiter<type1> awaiterType1;
>                 TaskAwaiter<type2> awaiterType2;
>                 //通过m_state表示的状态值来切换不同的操作
>                 switch (m_state)
>                 {
>                     case -1: 
>                         //调用异步方法,获取异步方法返回的Task<type1>对象,获取该对象的TaskAwaiter.
>                         awaiterType1 = Method1Async().GetAwaiter();
>                         
>                         //通过awaiter判断等待的task是否执行结束,如果没有执行结束,则执行if块内的操作.
> 						//注意,这里存在一个竞争问题,如果我们判断的时候task没有完成,
> 						//但是当我们进入if块内的时候,task完成了怎么办?这个问题涉及到task的内部操作,将会在下节讲解
>                         if (!awaiterType1.IsCompleted)
>                         {
>                             // '修改状态为0,表示等待的task正在执行中
>                             m_state = 0;
>                             // 将awaiterType1的引用赋值给m_awaiterType1,保存该引用,
>                             //因为一旦离开该方法,awaiterType1就销毁了
>                             m_awaiterType1 = awaiterType1; 
> 
>                             //通过m_builder给状态机设置一个延续.其实就是给等待的task设置一个回调,回调就是MoveNext方法                      
>                             m_builder.AwaitUnsafeOnCompleted(ref awaiterType1, ref this);
>                 
>                             //将executeFinall设置为false,表示我们没有进入finally块
>                             executeFinally = false; // We're not logically leaving the 'try' 
>                             //直接返回
>                             return; 
>                         }
>                         //能执行到此处,表示我们判断task是否完成的时候,task已经完成了,那么我们也就不需要设置延续了,直接跳出switch获取结果
>                         break;
>                     //当再次进入该方法的时候,m_state=0,此时是我们等待的task的调用该方法,同时页表示task已经执行结束,可以获取结果了,那我们将task的TaskAwaiter的引用赋值给局部变量
>                     case 0:
>                         awaiterType1 = m_awaiterType1;
>                         break;
>                     //这个状态表示的是我们await的Method2Async的task执行结束,Method2Async的task会调用MoveNext,
>                     //它进入该方法时看到的状态就是1.
>                     case 1:
>                         //将TaskAwaiter<type2>的引用赋值给局部变量
>                         awaiterType2 = m_awaiterType2;
>                         //跳到 ForLoopEpilog,执行for循环的第三部分
>                         goto ForLoopEpilog;
>                 }
>                 // 当我们等待的第一个task执行结束后,跳出switch,通过GetResult获取结果值,注意可
>                 //能会抛出异常,这个时候就是编译器生成的try语句器作用的时候
>                 m_resultType1 = awaiterType1.GetResult(); 
> 
>             //编译器通过标签+goto的方式来模拟for的执行流程
> 
>             //这个是for循环的初始语句,它将m_x初始化为0,然后跳转到ForLoopBody,这个是for语句的执行主体
>             ForLoopPrologue:
>                 m_x = 0;
>                 goto ForLoopBody;
>             
>             //这个是for语句的结尾语句,它将m_x++,并且获取await的task的结果值
>             ForLoopEpilog:
>                 m_resultType2 = awaiterType2.GetResult();
>                 m_x++; 
> 
>             //这是for语句的执行主体
>             ForLoopBody:
>                 //首先我们判断m_x是否小于3,这也是for循环的条件,如果不满足条件,则表示
>                 //循环执行结束
>                 if (m_x < 3)
>                 {
>                     //通过获取Method2Async的TaskAwaiter<type2>
>                     awaiterType2 = Method2Async().GetAwaiter();
>                     //一样,判断是否完成
>                     if (!awaiterType2.IsCompleted)
>                     {
>                         //task没有完成,我们将状态设置为1,表示await的状态处于正执行的状态
>                         m_state = 1; 
>                         //保存引用
>                         m_awaiterType2 = awaiterType2;
>                         //设置延续
>                         m_builder.AwaitUnsafeOnCompleted(ref awaiterType2, ref this);
>                         //此时我们不在finally,将executeFinally设置为false
>                         executeFinally = false; 
>                         //返回
>                         return;
>                     }
>                     //如果已经完成了,则直接跳到ForLoopEpilog获取结果值
>                     goto ForLoopEpilog;
>                 }
>             }
>             catch (Exception)
>             {
>                 //这是我们代码中的catch块,如果执行过程中出现异常,那么将会打印Catch.
>                 //在获取task的结果时抛出的异常也会被该catch捕捉,但是不建议这么做,因为可能会漏掉一写异常信息
>                 Console.WriteLine("Catch");
>             }
>             finally
>             {
>                 //最终进入finally块,如果
>                 if (executeFinally)
>                 {
>                     Console.WriteLine("Finally");
>                 }
>             }
>             //设置result为Done
>             result = "Done"; // What we ultimately want to return from the async function
>         }
>         catch (Exception exception)
>         {
>             //如果我们代码里没有try-catch,那么就由编译器生成的try-catch捕捉异常,则捕捉到异常之后,
>             //他会将异常设置给m_builder的Task,同时也标志该状态机的task执行结束了,可以执行接下来的步骤了
>             m_builder.SetException(exception);
>             return;
>         }
>         //没有发生异常,那么我们设置结果值,并且设置状态机的task为完成状态
>         m_builder.SetResult(result);
>     }
> }
> ```

----

