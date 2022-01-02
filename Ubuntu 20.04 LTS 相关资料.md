Ubuntu 20.04 LTS 相关资料

有时间还是要自己写一遍，省得以后资源失效了就owarida

1. [nVidia官网显卡驱动安装](https://zhuanlan.zhihu.com/p/115758882)(说实话还是直接附加驱动里安装就好了)
2. [nVidia官网显卡驱动卸载](https://www.cxyzjd.com/article/qq_40947610/114759620)
	执行 `sudo /usr/bin/nvidia-uninstall`
3. [gnome美化](https://juejin.cn/post/6875280250939375624)
4. [开机执行脚本](https://www.jianshu.com/p/3be1a8cbfa6f)  
	添加以下语句到 `/lib/systemd/system/rc-local.service`：

	```
	[Install]
	WantedBy=multi-user.target
	Alias=rc-local.service
	```

	编辑 `/etc/rc.local` 文件，添加你想要执行的命令：

	```bash
	#!/bin/bash
	# 上面这句话是必须的
	# 下面写要执行的语句
	sudo ./home/root/abc.sh
	# 命令必须在exit 0之前写
	exit 0
	```

	给rc.local添加可执行权限： `chmod +x /etc/rc.local`

	在 `/lib/systemd/system` 路径下创建一个软链接： `ln -s /lib/systemd/system/rc.local.service /etc/systemd/system/`

	该软链接的目的是为了让系统在启动时通过 `/etc/systemd/system/` 读取到 `rc.local.service`

5. 可以在登录页面按下 `ctrl+alt+f2~f6` 进入命令行模式
6. [tty2~6命令行模式中文乱码](https://www.jb51.net/os/Ubuntu/367166.html)
	使用 `sudo apt install zhcon` 安装zhcon
	然后将用户添加到video用户组,注销后重新登临,执行 `zhcon --utf8`
7. ls -al 执行后的内容意义(例如`[-rwxr-x-r] [1] [rick] [rick] [4096] [Jan 1 16:44] [README.md]`)
	- 文件权限`[-][rwx][rwx][rwx]`,四个中括号分别代表`[文件类型][文件所有者的权限][同一用户组的权限][其他用户的权限]`
	- 连接 理解为有多少个文件和这个文件有关,比如文件夹dev里面有三个文件,那么连接数量就为3
	- 所属帐号
	- 所属用户组
	- 文件大小,单位为B
	- 修改日期
	- 文件名
