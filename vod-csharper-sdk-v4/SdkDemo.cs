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
