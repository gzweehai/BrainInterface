打开工程后一定要先去还原NuGet Packages
BrainNetwork项目使用DisableDevTimeout编译会禁用网络超时检查（Debug模式的默认值，Release模式默认不包含这个选项）
BrainNetwork项目使用Debug模式时日志会输出所有发送和接收数据，使用Release模式编译只会打印发送数据到日志
解决方案使用Debug模式编辑，日志文件用明文；使用Release模式编译，日志文件使用gzip压缩。
BrainInterfaceClientConfig.json是客户端配置文件，json格式。
BrainSimulator是放大器模拟程序，使用IP端口为127.0.0.1:9211

运行程序需要安装Microsoft .NET Framework 4.7.1,离线安装:
https://www.microsoft.com/en-us/download/details.aspx?id=56116