# vod-csharp-server-sdk-v4
腾讯云点播4.0 ServerSDK(For C#)

## 功能说明
vod-csharp-server-sdk是为了让C#开发者能够在自己的代码里更快捷方便地使用点播上传功能而开发的SDK工具包，支持服务器端普通上传、客户端UGC上传，同时提供上传封面、REST API调用方法，用法参见"示例代码"。

## 示例代码
vod-csharper-sdk-v4/SdkDemo.cs
```
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VodCallV4;

namespace SdkDemo
{
    class SdkDemo
    {
        static void Main(string[] args)
        {
            //rest api调用
            VodCall vodCallRestApi = new VodCall();
            vodCallRestApi.Init("AKIDR20GpXsc4fihxxxxxxxxxxCeTpw9ljzt", "wGxKo4cu6WFBWxxxxxxxxxxBTTiUn4bV", VodCall.USAGE_VOD_REST_API_CALL);
            JObject jsonobj = vodCallRestApi.CallRestApi(
                new Dictionary<string, object>()
                {
                    { "Action", "DescribeVodPlayUrls" },
                    { "fileId", "9031868222866819849" },
            });
            Console.WriteLine(jsonobj.ToString());

            //ugc上传调用
            VodCall vodCallUgc = new VodCall();
            vodCallUgc.Init("AKIDR20GpXsc4fihxxxxxxxxxxCeTpw9ljzt", "wGxKo4cu6WFBWxxxxxxxxxxBTTiUn4bV", VodCall.USAGE_UGC_UPLOAD);
            vodCallUgc.SetFileInfo("E:\\a a.mp4", "a a.mp4", "mp3", 1);
            vodCallUgc.Upload();
            Console.WriteLine(vodCallUgc.StrFileId);

            //上传调用
            VodCall vodCallUpload = new VodCall();
            vodCallUpload.Init("AKIDR20GpXsc4fihxxxxxxxxxxCeTpw9ljzt", "wGxKo4cu6WFBWxxxxxxxxxxBTTiUn4bV", VodCall.USAGE_UGC_UPLOAD);
            vodCallUpload.SetFileInfo("E:\\a a.mp4", "a a.mp4", "mp3", 1);
            vodCallUpload.Upload();
            Console.WriteLine(vodCallUpload.StrFileId);

            //上传封面
            VodCall vodCallUploadImage = new VodCall();
            vodCallUploadImage.Init("AKIDR20GpXsc4fihxxxxxxxxxxCeTpw9ljzt", "wGxKo4cu6WFBWxxxxxxxxxxBTTiUn4bV", VodCall.USAGE_UPLOAD);
            vodCallUploadImage.SetFileInfo("E:\\a a.jpg", "a a.jpg", "mp3", 1);
            vodCallUploadImage.AddExtraPara("usage", "1");
            vodCallUploadImage.AddExtraPara("fileId", "9031868222863204403");
            vodCallUploadImage.Upload();
            Console.WriteLine(vodCallUploadImage.StrFileId);

            Thread.Sleep(10000);
        }
    }
}
```

## 使用说明
在第一次使用云API之前，用户首先需要在[腾讯云网站](https://www.qcloud.com/document/product/266/1969#1.-.E7.94.B3.E8.AF.B7.E5.AE.89.E5.85.A8.E5.87.AD.E8.AF.81)申请安全凭证，安全凭证包括 SecretId 和 SecretKey, SecretId 是用于标识 API 调用者的身份，SecretKey是用于加密签名字符串和服务器端验证签名字符串的密钥。SecretKey 必须严格保管，避免泄露。申请之后，可到 https://console.qcloud.com/capi 查看已申请的密钥（SecretId及SecretKey）。
