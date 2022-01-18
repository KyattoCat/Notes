## 对象池`ObjectPool`

有两张哈希表一个列表

1. `_hashTableObjs` 存放具体的对象
2. `_hashTableStatus` 存放对象的状态
3. `_keyList` 键表 记录存在的键

### 构造

`构造函数 -> Initialize(min, max) -> InstanceObjects() -> CreateOneObject() -> constructObject()`

初始化时分配池名称和大小，大小包括最小和最大

最小不得小于0， 最大不得小于5

有一个属性`_shrinkPoint`，暂且理解为收缩系数

`_shrinkPoint = (1 - (min / max)) * min`

之后创建`min`个对象

一旦在创建对象的过程中出现异常，则停止创建对象并且重设最小和最大值为当前对象的数量

创建完成后

1. 将对象的哈希值作为键，对象作为值，添加到 `_hashTableObjs`
2. 将对象的哈希值作为键，真值，添加到 `_hashTableStatus` 
3. 将对象的哈希值作为键，添加到 `_keyList`

### 取出

遍历 `_keyList` 

- 若该键在 `_hashTableStatus` 中的状态为 `true` （个人理解true为可用，闲置）
  - 取出在 `_hashTableObjs` 中键对应的对象，存入 `target` 变量
  - 将 `_hashTableStatus` 中的状态设为 `false`
  - 结束循环

若 `target` 为空且允许创建新对象则

- 若当前 `_keyList` 长度小于对象池最大容量
  - 调用 `CreateOneObject` 创建对象，该方法同时返回新建对象的哈希值作为键
  - 将 `_hashTableStatus` 中的状态设为 `false`
  - 取出在 `_hashTableObjs` 中键对应的对象，存入 `target` 变量

### 回收

~~在回收时强制添加有什么意义吗？~~

回收时传入对象的哈希值，作为键值，将 `_hashTableStatus` 置为 `true` 即可

若不存在则不做任何处理

