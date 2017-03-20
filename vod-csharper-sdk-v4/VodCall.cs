using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace VodCallV4
{
    class FilePartInfo
    {
        private long offset;
        private long dataSize;
        private int isSend = 0;

        public long Offset
        {
            get{
                return offset;
            }
            set{
                offset = value;
            }
        }
        public long DataSize
        {
            get{
                return dataSize;
            }

            set{
                dataSize = value;
            }
        }
        public int IsSend
        {
            get{
                return isSend;
            }

            set{
                isSend = value;
            }
        }
    }
    class VodCall
    {
        String m_strSecId;
        String m_strSecKey;
        String m_strFilePath;
        String m_strFileName;
        String m_strFileSha;
        String m_strSigEx="";
        public string FileSha
        {
            get { return m_strFileSha; }
        }
        String m_strRegion = "gz";
        String m_strReqHost = "vod2.qcloud.com";
        String m_strReqPath = "/v3/index.php";
        String m_strFileType;
        String m_strFileId = "";
        public string FileId
        {
            get { return m_strFileId; }
        }
        int m_iErrCode;
        public int ErrCode
        {
            get { return m_iErrCode; }
        }
        private float m_fUploadRate;
        private float m_fUploadSpeed;
        private float m_fCalShaRate;
        private Int64 m_qwStartTransTime;

        public float UploadRate
        {
            get { return (float)m_iFinishBlock / (float)m_arrPartInfo.Count(); }
        }
        public float UploadSpeed
        {
            get {
                Int64 time_now = GetIntTimeStamp();
                if (m_qwEndTime != 0)
                {
                    time_now = m_qwEndTime;
                }
                if (time_now > m_qwStartTransTime)
                {
                    return (float)(m_iSendBolock * m_dataSize)
                        / (float)(time_now - m_qwStartTransTime);
                }
                return 0;
            }
        }
        public float CalShaRate
        {
            get { return m_fCalShaRate; }
        }
        private string m_strErrDesc;
        public string ErrDesc
        {
            get { return m_strErrDesc; }
        }

        string m_strInitUploadCmd = "InitUploadEx";
        string m_strPartUploadCmd = "UploadPartEx";
        string m_strFinishUploadCmd = "FinishUploadEx";

        int m_iUsage = 0;
        int m_iClassId = 0;
        public List<FilePartInfo> m_arrPartInfo;
 //       List<PartUploadThread> m_arrThreadList;
        List<String> m_arrTags;

        int m_isTrans = 0;
        int m_isScreenShot = 0;
        int m_isWaterMark = 0;
        long m_qwFileSize = 0;
        long m_dataSize = 1024 * 1024;
        private int m_iHttpTimeOut = 100 * 1000;

        private int m_iFinishBlock = 0;
        private int m_iSendBolock = 0;

        private const int MIN_DATA_SIZE = 512 * 1024;
        private const int MAX_DATA_SIZE = 1 * 1024 * 1024;
        private const int HTTP_TIME_OUT = -1;

        private const int HTTP_CLIENT_ERROR = -2;
        private const int MAX_RETRY_TIME = 3;

        public const int FILE_WAIT = 0;
        public const int FILE_FINISH = 1;
        public const int FILE_RUNNING = 2;
        public const int FILE_SVR_ERROR = 3;
        public const int FILE_CANCLED = 4;

        public const int USAGE_UPLOAD = 0;
        public const int USAGE_UGC_UPLOAD = 1;
        public const int USAGE_VOD_REST_API_CALL = 2;
        String m_strUploadReqHost = "vod2.qcloud.com";
        String m_strUploadReqPath = "/v3/index.php";

        String m_strRestApiReqHost = "vod.api.qcloud.com";
        String m_strRestApiReqPath = "/v2/index.php";

        private long m_qwStartTime;
        private long m_qwEndTime;
        private Dictionary<String, Object> m_arrExtraPara = new Dictionary<string, object>();

        public int Init(String secId, String secKey, int iUsage, int threadNum=6)
        {
            m_strSecId = secId;
            m_strSecKey = secKey;
            m_arrTags = new List<String>();
            m_arrPartInfo = new List<FilePartInfo>();
            m_iThreadNum = threadNum;
            m_iUsage = iUsage;

            if (iUsage == USAGE_VOD_REST_API_CALL)
            {
                m_strReqHost = this.m_strRestApiReqHost;
                m_strReqPath = this.m_strRestApiReqPath;
            }
            else if (iUsage == USAGE_UPLOAD)
            {
                m_strReqHost = this.m_strUploadReqHost;
                m_strReqPath = this.m_strUploadReqPath;
                m_strInitUploadCmd = "InitUpload";
                m_strPartUploadCmd = "UploadPart";
                m_strFinishUploadCmd = "FinishUpload";
            }
            else if ( iUsage == USAGE_UGC_UPLOAD)
            {
                m_strReqHost = this.m_strUploadReqHost;
                m_strReqPath = this.m_strUploadReqPath;
                m_strInitUploadCmd = "InitUploadEx";
                m_strPartUploadCmd = "UploadPartEx";
                m_strFinishUploadCmd = "FinishUploadEx";
            }
            else
            {
                return -1;
            }

            return 0;
        }

        public JObject CallRestApi(Dictionary<String, Object> dicVals)
        {
            dicVals["Region"] = m_strRegion;
            dicVals["SecretId"] = m_strSecId;
            dicVals["Timestamp"] = GetTimeStamp();
            dicVals["Nonce"] = new Random().Next(0, 1000000).ToString();
            String strSign = GetReqSign(dicVals);
            if (strSign == "")
            {
                return null;
            }
            String strReq = GetReqUrl(dicVals, strSign);
            System.Console.WriteLine(strReq);
            JObject jsonobj = new JObject();
            int ret = SendData(strReq, null, 0, ref jsonobj);
            if (ret != 0)
                return null;
            return jsonobj;
        }
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        public static long GetIntTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
        int InitCommPara(Dictionary<String, Object> dicVals)
        {
            dicVals["Region"] = m_strRegion;
            dicVals["SecretId"] = m_strSecId;
            dicVals["fileSha"] = m_strFileSha;
            dicVals["Timestamp"] = GetTimeStamp();
            dicVals["Nonce"] = new Random().Next(0, 1000000).ToString();
            return 0;
        }
        int SetTransCfg(int isTrans, int isScreenShot, int isWaterMark)
        {
            m_isTrans = isTrans;
            m_isScreenShot = isScreenShot;
            m_isWaterMark = isWaterMark;
            return 0;
        }

        public int AddFileTag(String strTag)
        {
            m_arrTags.Add(strTag);
            return 0;
        }
        public int AddExtraPara(String key, Object val)
        {
            m_arrExtraPara.Add(key, val);
            return 0;
        }
        public int SetFileInfo(String strFilePath, String strFileName, String strFileType, int iClassId)
        {
            m_strFilePath = strFilePath;
            m_strFileName = strFileName;
            m_strFileType = strFileType;
            m_arrTags.Clear();
            m_strFileSha = "";
            m_iClassId = iClassId;
            return 0;
        }

        private string CalFileSha(string strFilePath)
        {
            FileStream fStream = new FileStream(strFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.Security.Cryptography.HashAlgorithm algorithm;
            algorithm = System.Security.Cryptography.SHA1.Create();
            byte[] hashBytes = algorithm.ComputeHash(fStream);
            fStream.Close();
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }

        private string hash_hmac(string signatureString, string secretKey)
        {
            var enc = Encoding.UTF8;
            HMACSHA1 hmac = new HMACSHA1(enc.GetBytes(secretKey));
            hmac.Initialize();

            byte[] buffer = enc.GetBytes(signatureString);
            return Convert.ToBase64String(hmac.ComputeHash(buffer));
        }
        private byte[] hash_hmac_byte(string signatureString, string secretKey)
        {
            var enc = Encoding.UTF8;
            HMACSHA1 hmac = new HMACSHA1(enc.GetBytes(secretKey));
            hmac.Initialize();

            byte[] buffer = enc.GetBytes(signatureString);
            return hmac.ComputeHash(buffer);
        }
        private string GetReqExSign()
        {
            string strContent = "";
            strContent += ("s=" + Uri.EscapeDataString((m_strSecId)));
            strContent += ("&f=" + Uri.EscapeDataString(m_strFileName));
            strContent += ("&t=" + GetIntTimeStamp());
            strContent += ("&ft=" + m_strFileType);
            strContent += ("&e=" + (GetIntTimeStamp() + 3600*24*2));
            strContent += ("&fs=" + m_strFileSha);
            strContent += ("&uid=1");
            strContent += ("&r=12") ;
            byte[] bytesSign = hash_hmac_byte(strContent, m_strSecKey);
            byte[] byteContent = System.Text.Encoding.Default.GetBytes(strContent);
            byte[] nCon = new byte[bytesSign.Length + byteContent.Length];
            bytesSign.CopyTo(nCon, 0);
            byteContent.CopyTo(nCon, bytesSign.Length);
            m_strSigEx = Convert.ToBase64String(nCon);
            return m_strSigEx;
        }
        private string GetReqSign(Dictionary<String, Object> dicVals, int iIsReportReq =0 )
        {
            if (m_iUsage == USAGE_UGC_UPLOAD && iIsReportReq == 0)
            {
                return m_strSigEx;
            }
            List<string> arrKeys = new List<string>(dicVals.Keys);
            arrKeys.Remove("Signature");
            arrKeys.Sort(string.CompareOrdinal);
            if (arrKeys.Count() <= 0)
            {
                return "";
            }
            string strContex = "";
            if (m_iUsage == USAGE_VOD_REST_API_CALL)
                strContex += ("GET" + m_strReqHost + m_strReqPath + "?");
            else
                strContex += ("POST" + m_strReqHost + m_strReqPath + "?");
            for (int i = 0; i < arrKeys.Count() - 1; i++)
            {
                strContex += arrKeys[i];
                strContex += "=";
                strContex += dicVals[arrKeys[i]];
                strContex += "&";
            }
            strContex += arrKeys[arrKeys.Count() - 1];
            strContex += "=";
            strContex += dicVals[arrKeys[arrKeys.Count() - 1]];
            return hash_hmac(strContex, m_strSecKey);
        }

        public static String calStreamShaMd5(Stream stream, String strMethod)
        {
            HashAlgorithm algorithm;
            algorithm = MD5.Create();
            byte[] buffer = algorithm.ComputeHash(stream);
            return BitConverter.ToString(buffer).Replace("-", "");
        }
        public String GetReqUrl(Dictionary<String, Object> mapVals, String strSign)
        {
            List<string> arrKeys = new List<string>(mapVals.Keys);
            arrKeys.Remove("Signature");
            arrKeys.Sort(string.CompareOrdinal);
            String reqStr = "";
            foreach (String key in arrKeys)
            {
                if (reqStr == "")
                {
                    reqStr += '?';
                }
                else
                {
                    reqStr += '&';
                }
                reqStr += key + '=' + Uri.EscapeDataString(mapVals[key].ToString());
            }
            reqStr += ("&" + "Signature=" + Uri.EscapeDataString(strSign));
            Console.WriteLine("https://"+reqStr);
            return "https://" + m_strReqHost + m_strReqPath + reqStr;
        }
        private int GeneratePartInfo()
        {
            long partNum = m_qwFileSize / m_dataSize;
            for (int i = 0; i < partNum; i++)
            {
                FilePartInfo stInfo = new FilePartInfo();
                stInfo.DataSize = m_dataSize;
                stInfo.IsSend = 0;
                stInfo.Offset = m_dataSize * i;
                this.m_arrPartInfo.Add(stInfo);
            }
            if (partNum * m_dataSize < m_qwFileSize)
            {
                FilePartInfo stInfo = new FilePartInfo();
                stInfo.DataSize = m_qwFileSize - partNum * m_dataSize;
                stInfo.IsSend = 0;
                stInfo.Offset = partNum * m_dataSize;
                this.m_arrPartInfo.Add(stInfo);
            }
            return 0;
        }
        private int SendData(string strUrl, byte[] byteBuf, int size, ref JObject jo)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strUrl);
            if (this.m_iUsage == USAGE_VOD_REST_API_CALL)
                request.Method = "GET";
            else
                request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = size;
            request.Timeout = m_iHttpTimeOut;
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1;SV1)";
            request.Accept = "*/*";
            //request.setRequestProperty("accept", "*/*");
            //request.setRequestProperty("connection", "Keep-Alive");
            //request.setRequestProperty("user-agent",
            //        "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1;SV1)");
            //

            if (size != 0)
            {
                Stream reqStream = request.GetRequestStream();
                reqStream.Write(byteBuf, 0, size);
            }

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                Console.WriteLine(retString);
                jo = JObject.Parse(retString);
                myStreamReader.Close();
                myResponseStream.Close();
            }
            catch (WebException webEx)
            {
                if (webEx.Status == WebExceptionStatus.Timeout)
                {
                    return HTTP_TIME_OUT;
                }
                Console.WriteLine(webEx);
                return HTTP_CLIENT_ERROR;
            }

            return 0;
        }
        public int PartUpload(int iIndex)
        {
            if (m_strFileSha == "")
            {
                return -1;
            }

            int retryLimit = MAX_RETRY_TIME;
            JObject jsonobj = new JObject();
            Dictionary<String, Object> mapVals = new Dictionary<String, Object>();
            InitCommPara(mapVals);
            while (true)
            {
                FilePartInfo partInfo = m_arrPartInfo[iIndex];
                if (partInfo.IsSend == 1)
                {
                    Console.WriteLine("sent fileInfo " + m_iSendBolock + " " + m_iFinishBlock);
                    return 0;
                }
                FileStream stFile = new System.IO.FileStream(m_strFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                stFile.Seek(partInfo.Offset, 0);
                byte[] buf = new byte[(int)partInfo.DataSize];
                int ret = stFile.Read(buf, 0, (int)partInfo.DataSize);
                stFile.Close();
                if (ret != partInfo.DataSize)
                {
                    return -2001;
                }
                mapVals["Action"] = m_strPartUploadCmd;
                mapVals["dataSize"] = partInfo.DataSize;
                mapVals["offset"] = partInfo.Offset;
                Stream stInput = new MemoryStream(buf);
                String strMd5 = calStreamShaMd5(stInput, "MD5");
                mapVals["dataMd5"] = strMd5.ToLower();
                String strSign = GetReqSign(mapVals);
                String strReq = GetReqUrl(mapVals, strSign);
                try
                {                    
                    ret = SendData(strReq, buf, (int)(partInfo.DataSize), ref jsonobj);
                    if (ret != 0 && retryLimit>0)
                    {
                        retryLimit--;
                        if (retryLimit <= 0)
                            return -2100 + ret;
                        continue;
                    }
                }
                catch (Exception e)
                {
                    if (retryLimit > 0)
                    {
                        retryLimit--;
                        if (retryLimit <= 0)
                            return -2003;
                        continue;
                    }
                }
                int retCode = (int)jsonobj.SelectToken("code");
                if (retCode == 0)
                {
                    Interlocked.Add(ref m_iSendBolock, 1);
                    Interlocked.Add(ref m_iFinishBlock, 1);
                    Console.WriteLine("fileInfo " + m_iSendBolock+ " "+m_iFinishBlock);
                    partInfo.IsSend = 1;
                }
                else
                {
                    int canRetry = (int)jsonobj.SelectToken("canRetry");
                    if (canRetry == 1 && retryLimit > 0)
                    {
                        retryLimit--;
                        if (retryLimit <= 0)
                            return -2002;
                        continue;
                    }
                }
                return 0;
            }
        }
        public int FinishUpload()
        {
            if (m_strFileSha == "")
            {
                return -3001;
            }
            int retryLimit = MAX_RETRY_TIME;
            JObject jsonobj = new JObject();
            Dictionary<String, Object> mapVals = new Dictionary<String, Object>();
            InitCommPara(mapVals);
            while (true)
            {
                mapVals["Action"] = m_strFinishUploadCmd;
                String strSign = GetReqSign(mapVals);
                if (strSign == "")
                {
                    return -3002;
                }
                String strReq = GetReqUrl(mapVals, strSign);
                try
                {
                    int ret = 0;
                    ret = SendData(strReq, null, 0, ref jsonobj);
                    if (ret != 0 && retryLimit > 0)
                    {
                        retryLimit--;
                        if (retryLimit <= 0)
                            return -3100 + ret;
                        continue;
                    }
                }
                catch (Exception e)
                {
                    retryLimit--;
                    continue;
                }
                int retCode = (int)jsonobj.SelectToken("code");
                if (retCode == 0)
                {
                    m_strFileId = jsonobj.SelectToken("fileId").ToString();
                }
                else
                {
                    int canRetry = (int)jsonobj.SelectToken("canRetry");
                    if (canRetry == 1 && retryLimit > 0)
                    {
                        if (retryLimit > 0)
                        {
                            retryLimit--;
                            if (retryLimit <= 0)
                                return -3002;
                            continue;
                        }
                    }
                    return -3003;
                }
                return 0;
            }
        }
        public int Report(int iRetCode)
        {
            JObject jsonobj = new JObject();
            Dictionary<String, Object> mapVals = new Dictionary<String, Object>();
            InitCommPara(mapVals);
            mapVals["Action"] = "Report";
            mapVals["errCode"] = iRetCode;
            if (iRetCode == 0 && m_qwEndTime > m_qwStartTime)
            {
                mapVals["speed"] = m_qwFileSize / (m_qwEndTime - m_qwStartTime);
            }
            else
            {
                mapVals["speed"] = 0;
            }
            mapVals["fileId"] = m_strFileId;
            mapVals["platform"] = "c_sharper";
            mapVals["version"] = "1.0";

            string strSign = GetReqSign(mapVals, 1);
            string strReq = GetReqUrl(mapVals, strSign);
            Console.WriteLine(strReq);
            SendData(strReq, null, 0, ref jsonobj);
            return 0;
        }
        public int InitUpload() {
            if (m_strFileSha == "")
            {
                return -1001;
            }
            int retryLimit = MAX_RETRY_TIME;
            JObject jsonobj = new JObject();
            Dictionary<String, Object> mapVals = new Dictionary<String, Object>();
            InitCommPara(mapVals);
            if (m_arrExtraPara.Count > 0)
            {
                foreach (string key in m_arrExtraPara.Keys)
                {
                    mapVals.Add(key, m_arrExtraPara[key]);
                }
            }
            while (true) {
			    if (!System.IO.File.Exists(m_strFilePath)) {
				    return -1002;
			    }
                FileStream stFile = new System.IO.FileStream(m_strFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                m_qwFileSize = stFile.Length;
                mapVals["Action"] = m_strInitUploadCmd;
			    mapVals["fileSize"] = m_qwFileSize;
			    mapVals["dataSize"]= m_dataSize;
			    mapVals["fileName"] = m_strFileName;
			    mapVals["fileType"] = m_strFileType;
			    mapVals["isTranscode"] = m_isTrans;
			    mapVals["isScreenshot"] = m_isScreenShot;
			    mapVals["isWatermark"] = m_isWaterMark;
                mapVals["classId"] = m_iClassId;

                for (int i = 0; i<m_arrTags.Count; i++) {
			    	String key = "tag." + (i + 1);
                    mapVals[key] = m_arrTags[i];
			    }

                String strSign = GetReqSign(mapVals);
                if (strSign == "") {
                	return -1003;
                }
                String strReq = GetReqUrl(mapVals, strSign);
                try
                {
                    int ret = 0;
                    ret = SendData(strReq, null, 0, ref jsonobj);
                    if (ret != 0 && retryLimit > 0)
                    {
                        retryLimit--;
                        if (retryLimit <= 0)
                            return -1100 + ret;
                        continue;
                    }
                }
                catch (Exception e)
                {
                    if (retryLimit > 0)
                    {
                        retryLimit--;
                        continue;
                    }
                    return -1004;
                }
                int retCode = (int)jsonobj.SelectToken("code");
                if (retCode == 2)
                {
                    m_strFileId = jsonobj.SelectToken("fileId").ToString();
                }
                else if (retCode == 1)
                {
                    m_dataSize = (long)jsonobj.SelectToken("dataSize");
                    JArray ja = JArray.Parse(jsonobj["listParts"].ToString());
                    GeneratePartInfo();
                    int ja_length = ja.Count;
                    for (int i = 0; i < ja_length; i++ )
                    {
                        JObject jo_temp = JObject.Parse(ja[i].ToString());
                        long offset = (long)jo_temp.SelectToken("offset");
                        int index = (int)((long)offset / (long)m_dataSize);
                        m_arrPartInfo[index].IsSend = 1;
                        Interlocked.Add(ref m_iFinishBlock, 1);
                    }
                }
                else if (retCode == 0)
                {
                    GeneratePartInfo();
                }
                else
                {
                    int canRetry = (int)jsonobj.SelectToken("canRetry");
                    if (canRetry == 1 && retryLimit > 0)
                    {
                        retryLimit--;
                        if (retryLimit <= 0)
                            return -1005;
                        continue;
                    }
                    return -1005;
                }
                return 0;
            }
        }
        private Object m_arrayLock = new Object();
        private int m_iCurrentIndex = 0;
        private int m_iThreadUploadError = 0;
        private int m_iThreadNum = 4;

        
        //抢占式多线程
        private List<Thread> m_arrThread;

        private int m_iStatus = 0;
        public int Status { get { return m_iStatus; } }

        public string StrFileId
        {
            get
            {
                return m_strFileId;
            }

            set
            {
                m_strFileId = value;
            }
        }

        private void ThreadUpload()
        {
            do
            {
                int index = -1;
                //抢占index
                lock(m_arrayLock)
                {
                    index = m_iCurrentIndex;
                    m_iCurrentIndex++;
                }
                //index已经走完，认为上传结束，退出线程
                if (m_iCurrentIndex > m_arrPartInfo.Count)
                {
                    return;
                }
                int ret = PartUpload(index);
                if (ret != 0)
                {
                    m_iThreadUploadError = ret;
                    //通知其他线程退出上传过程
                    Console.WriteLine("part upload failed ret " + ret);
                }
            } while (m_iThreadUploadError == 0);
        }
        public int Upload()
        {
            m_strFileId = "";
            m_iStatus = FILE_RUNNING;
            m_strFileSha = CalFileSha(m_strFilePath);
            if (m_iUsage == USAGE_UGC_UPLOAD)
            {
                GetReqExSign();
            }
            m_qwStartTime = GetIntTimeStamp();
            int ret = InitUpload();
            if (ret < 0)
            {
                Report(ret);
                m_iErrCode = ret;
                m_iStatus = FILE_SVR_ERROR;
                Console.WriteLine("init upload failed ret \n" + ret);
                return ret;
            }
            m_qwStartTransTime = GetIntTimeStamp();
            m_arrThread = new List<Thread>();
            for (int i = 0; i < m_iThreadNum; i++)
            {
                m_arrThread.Add(new Thread(new ThreadStart(this.ThreadUpload)));
            }
            for (int i = 0; i < m_iThreadNum; i++)
            {
                m_arrThread[i].Start();
            }
            for (int i = 0; i < m_iThreadNum; i++)
            {
                m_arrThread[i].Join();
            }
            if (m_iThreadUploadError != 0)
            {
                m_iErrCode = m_iThreadUploadError; 
                m_iStatus = FILE_SVR_ERROR;
                Console.WriteLine("upload part failed " + m_iThreadUploadError);
                Report(m_iThreadUploadError);
                return m_iThreadUploadError;
            }
            ret = FinishUpload();
            if (ret!= 0)
            {
                Console.WriteLine("finish upload failed " + ret);
                m_iErrCode = ret;
                m_iStatus = FILE_SVR_ERROR;
                Report(ret);
                return ret;
            }
            m_iStatus = FILE_FINISH;
            m_qwEndTime = GetIntTimeStamp();
            Report(0);
            Console.WriteLine(m_strFileId, m_iThreadNum, m_iThreadUploadError);
            return 0;
        }
        public void UploadEx()
        {
            Upload();
        }

        public int SetTranscode(int iIsTrans)
        {
            m_isTrans = iIsTrans;
            return 0;
        }

        public int SetScreenShort(int iIsScreenShot)
        {
            m_isScreenShot = iIsScreenShot;
            return 0;
        }
        public int SetWatermark(int iIsWaterMark)
        {
            m_isWaterMark = iIsWaterMark;
            return 0;
        }
        public int SetNotifyUrl(string strUrl)
        {
            return 0;
        }
       
    }
}
