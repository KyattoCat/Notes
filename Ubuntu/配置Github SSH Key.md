# 配置Github SSH Key

环境为Ubuntu 20.04 LTS

一个ssh key可以访问该帐号的所有库，有新电脑了直接在新电脑重复如下操作添加新ssh key即可

同一电脑不同用户也需要创建不同的ssh key

步骤如下：

1. 执行`ssh-keygen -t rsa -b 4096 -C "邮箱"`
2. 按三次回车，让它以默认的方式生成
3. 在`~/.ssh`文件夹中找到生成的`id_rsa.pub`文件，复制其中的所有内容
4. 在github上添加一个新的SSH Key（在头像-设置-SSH and GPG Keys里）
5. 自定一个标题，将刚才复制的东西粘贴到Key那一栏里，之后点击添加
6. 执行`ssh -T git@github.com`，提示是否继续连接时输入yes

接下来配置git：

```bash
git config --global user.name "Your Name"
git config --global user.email "Your Email"
# 推送时的操作有所不同 我对这个还不熟悉 略过
# git config --global push.default matching
# 这个好像是用来处理git中文路径的问题的 略过
# git config --global core.quotepath false
# 修改commit和tags之类的编辑器 略过
# git config --global core.editor "vim" 
```

之后就按照git的使用方法进行使用就可以了