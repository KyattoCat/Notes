需要在ubuntu上下载一个脚本

```shell
mkdir .vs-debugger
curl -sSL https://aka.ms/getvsdbgsh -o .vs-debugger/GetVsDbg.sh
```

vs附加到进程，链接类型改为ssh，目标填写远端服务器用户@地址

然后就会自动执行GetVsDbg脚本，服务器在国外，下载速度巨慢无比