托管内存管理

无指针类型、一般类型、不可回收类型

每个类型下存有一个链表数组

下标为0的链表指向16字节内存空间，下标每加1，对应链表指向的内存多16字节，最高2048字节



参考[【笔记】Unity内存分配和回收的底层原理 - 知乎](https://zhuanlan.zhihu.com/p/381859536)

参考[Unity使用的GC方式——贝姆GC（BOEHM GC）_boehmgc-CSDN博客](https://blog.csdn.net/zhiai315/article/details/136260511)

参考[了解自动内存管理 - Unity 手册](https://docs.unity.cn/cn/2019.4/Manual/UnderstandingAutomaticMemoryManagement.html)
