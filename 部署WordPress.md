# 部署WordPress

环境：腾讯云上刚重装系统空空如也的Ubuntu 20.04 LTS

## 1. 安装和配置MySQL

这两行懂的都懂。


```shell
sudo apt update
sudo apt upgrade
```

安装MySQL服务器（在编写本文时版本为8.0.29）

```bash
sudo apt install mysql-server
```

我看别人的博客说安装的时候会提示创建账号密码的，但是没有。。。

所以安装完MySQL之后直接（因为密码是空的所以可以这么来）：

```shell
mysql -u root
```

进入MySQL，安全起见给root用户设置一个密码：

```mysql
use mysql;
alter user 'root'@'localhost' identified by '新密码';
```

然后创建一个新用户：

```mysql
create user '账号'@'localhost' identified by '密码';
```

记住账号密码，等下配置wordpress要用的。

然后创建一个新数据库并将权限赋给刚才创建的用户：

```mysql
create database 数据库名称;
grant all privileges on 数据库名称 to '账号'@'localhost';
flush privileges;
```

到此结束MySQL的配置，重启一下MySQL服务器：

```shell
sudo systemctl restart mysql.service
```

## 2. 安装Apache服务器

使用Apache2 + PHP7.4搭建环境。

```shell
sudo apt install php libapache2-mod-php

sudo systemctl restart apache2 # 重启apache加载php模块
```

安装wordpress所[需要的PHP模块](https://make.wordpress.org/hosting/handbook/server-environment/#php-extensions)，因为我用MySQL作为数据库所以要装php-mysql，其他的按照官方的文档来安装。

## 3. 下载和配置WordPress

建立一个下载文件夹，下载WordPress并解压：

```shell
mkdir Download
cd Download
wget https://cn.wordpress.org/latest-zh_CN.tar.gz
tar -zxvf latest.tar.gz
```

解压得到wordpress文件夹，将该文件夹移动到/var/www/：

```shell
sudo mv ./wordpress /var/www/
cd /var/www/wordpress
```

复制wordpress配置示例，复制好后打开：

```shell
sudo cp wp-config-sample.php wp-config.php
sudo vim wp-config.php
```

打开后里面有一些配置需要改，改成和之前配置数据库时输入的数据一致：

```php
define( 'DB_NAME', '数据库名称' );
define( 'DB_USER', '账号' );
define( 'DB_PASSWORD', '密码' );
```

其他不需要更改。

然后赋予www-data用户权限，这个用户是Apache安装时自动创建的用户，用于访问服务器内容：

```shell
sudo chown -R www-data /var/www/wordpress
sudo chmod -R 775 /var/www/wordpress
```

## 4. 配置Apache

```shell
cd /etc/apache2/sites-available
sudo cp 000-default.conf wordpress.conf
sudo vim wordpress.conf
```

将wordpress.conf中DocumentRoot改为`/var/www/wordpress/`，注意最后这个斜杠是必须的，然后保存退出。

```shell
cd ../sites-enabled
sudo rm 000-default.conf
sudo ln -s ../sites-available/wordpress.conf ./wordpress.conf
```

链接之前的wordpress.conf到sites-enabled目录，移除apache默认链接。

最后记得重启一下apache

```shell
sudo systemctl restart apache2.service
```

## 5. 登录网站

接下来就是访问自己的服务器，然后就安装页面上的提示输入一些网站信息和网站管理员账号信息就可以愉快的使用了。

到此为止，一个http协议的网站就搭建好了，https要证书，我不会，之后再弄。

## 参考

- [如何安装WordPress](https://wordpress.org/support/article/how-to-install-wordpress/)

- [服务器环境要求](https://make.wordpress.org/hosting/handbook/server-environment/#php-extensions)

- [解决WordPress需要访问您网页服务器的权限](https://cloud.tencent.com/developer/article/1545202)

- [什么是www-data用户](https://qastack.cn/ubuntu/873839/what-is-the-www-data-user)
