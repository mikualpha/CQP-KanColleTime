# CQP-KanColleTime
一个基于CoolQ平台的KanColle报时插件。

因CoolQ已经关闭，可采用[MiraiNative框架](https://github.com/iTXTech/mirai-native)使用本插件。

## 使用组件
[CQP Native SDK](https://github.com/Jie2GG/Native.Cqp.Csharp) ([MIT License](https://github.com/Jie2GG/Native.Cqp.Csharp/blob/Final/LICENSE))

## 使用方法
启用后，请先在```data/cn.mikualpha.kancolle.time/admin.ini```中设置管理员，并重启。

随后，在对应群内使用如下指令即可：

```
/启用报时-(船名)
/禁用报时
```
注：船名以[KCWIKI](https://zh.kcwiki.cn/wiki/)页面地址栏中显示的为准。

## 生成方式
请更改Native.Core项目属性中的生成路径，然后以Release x86直接进行生成即可。（更多信息详见框架README）
