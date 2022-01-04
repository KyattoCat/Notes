# Qt5.14无法输入中文的解决方法

1. 安装 `fcitx-frontend-qt5`
    
    `sudo apt install fcitx-frontend-qt5`

2. 查看 `fcitx-frontend-qt5` 的安装目录

    `dpkg -L fcitx-frontend-qt5`

    找到 `libfcitxplatforminputcontextplugin.so` 的路径

3. 将 `libfcitxplatforminputcontextplugin.so` 复制到qt插件目录下

    插件目录为 `qt安装目录/Tools/QtCreator/lib/Qt/plugins/platforminputcontexts/`

4. 重启Qt即可