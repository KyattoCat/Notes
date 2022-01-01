Ubuntu 20.04 LTS 相关资料

有时间还是要自己写一遍，省得以后资源失效了就owarida

1. [nVidia官网显卡驱动安装](https://zhuanlan.zhihu.com/p/115758882)(说实话还是直接附加驱动里安装就好了)
2. [nVidia官网显卡驱动卸载](https://www.cxyzjd.com/article/qq_40947610/114759620)
3. [gnome美化](https://juejin.cn/post/6875280250939375624)
4. [开机执行脚本](https://www.jianshu.com/p/3be1a8cbfa6f)(写rc.local记得加上`#!/bin/bash`)
5. [开机模式设置 字符界面和图形界面的切换](https://blog.csdn.net/Jailman/article/details/116301693)
6. [tty3~6命令行模式中文乱码](https://www.jb51.net/os/Ubuntu/367166.html)，zhcon需要将用户添加到video用户组
7. 图形模式切换可以使用init指令,具体可以参考man init或info init,涉及到run level,但是执行这个命令需要管理员权限
8. 文件权限[-][rwx][rwx][rwx],四个中括号分别代表[文件类型][文件所有者的权限][同一用户组的权限][其他用户的权限]
