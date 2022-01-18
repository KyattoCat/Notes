## `abstarct` 和 `virtual` 的区别

由 `abstract` 修饰的方法所在的类必须也是由 `abstract` 修饰的类，也就是抽象类

抽象类不能被实例化，只能被继承

抽象类中的抽象方法必须由子类实现

`virtual` 仅对方法起到影响，被她修饰的方法可以被子类 `override` ，也可以不被重写

