using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Native.Tool.Http;
using Native.Sdk.Cqp;
using System.Net;

class TimeCheck
{
    private static List<TimeCheck> ins = null;
    private static string recordPath = "data/record/kancolle/";
    private static CQApi CQApi = null;
    private static CQLog CQLog = null;
    private const string version = "1.1.6";
    private bool running = false;
    private Thread thread = null;
    private List<Hour> data = null;
    private long group;

    private TimeCheck(long _group) {
        this.group = _group;
    }

    public static TimeCheck GetInstance(long group)
    {
        if (ins == null) ins = new List<TimeCheck>();
        for (int i = 0; i < ins.Count; ++i)
        {
            //CQLog.Debug("List", ins[i].GetGroup().ToString());
            if (ins[i].GetGroup() == group) return ins[i];
        }
        TimeCheck temp = new TimeCheck(group);
        ins.Add(temp);
        return temp;
    }

    public static void AppStart(CQApi _CQApi, CQLog _CQLog)
    {
        CQApi = _CQApi;
        CQLog = _CQLog;
        InitAdminFile();

        if (ins == null) ins = new List<TimeCheck>();
        string path = CQApi.AppDirectory;
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (FileInfo f in root.GetFiles())
        {
            string name = f.Name;
            if (name.Contains("config-"))
            {
                int start = name.IndexOf("-") + 1;
                int end = name.IndexOf(".json");
                long group = long.Parse(name.Substring(start, end - start));
                TimeCheck temp = new TimeCheck(group);
                ins.Add(temp);
                string json = ReadFromFile(path + name);
                temp.Initialize(json);
            }
        }
    }

    public static void AppStop()
    {
        foreach (TimeCheck temp in ins)
        {
            temp.EndCheck(true);
        }
        ins.Clear();
    }

    private static void DeleteInstance(long group)
    {
        for (int i = 0; i < ins.Count; ++i)
        {
            if (ins[i].GetGroup() == group)
            {
                ins.RemoveAt(i);
                return;
            }
        }
    }

    public static bool isAdmin(long qq)
    {
        string[] list = ReadFromFile(CQApi.AppDirectory + "admin.ini").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < list.Length; ++i)
        {
            long num = long.Parse(list[i]);
            if (num == 0 || num == qq) return true;
        }
        return false;
    }

    public long GetGroup() { return group; }

    public bool StartCheck(string name)
    {
        if (running) {
            try { thread.Abort(); }
            catch (ThreadAbortException) { }
        }

        string json = GetJson(name);
        WriteToFile(CQApi.AppDirectory + "config-" + group + ".json", json);
        data = ParseJson(json);
        if (!CheckData()) return false;
        DownloadRecords();
        running = true;
        thread = new Thread(CheckStatus);
        thread.Start();
        return true;
    }

    public bool EndCheck(bool closing = false) //是否正在关闭程序
    {
        if (!running) return false;
        try
        {
            running = false;
            if (!closing) File.Delete(CQApi.AppDirectory + "config-" + group + ".json");
            thread.Abort();
            if (!closing) DeleteInstance(group);
            return true;
        }
        catch (ThreadAbortException) { }
        return false;
    }

    private static void InitAdminFile()
    {
        string path = CQApi.AppDirectory + "admin.ini";
        if (File.Exists(path)) return;
        WriteToFile(path, "123456789,987654321");
    }

    //根据本地JSON数据初始化线程
    private void Initialize(string json)
    {
        if (running) return;
        data = ParseJson(json);
        if (data == null) return;
        running = true;
        thread = new Thread(CheckStatus);
        thread.Start();
    }

    //检查数据合法性
    private bool CheckData()
    {
        if (data == null) return false;
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].cn == "" || data[i].jp == "") return false;
        }
        return true;
    }

    private void DownloadRecords()
    {
        CQApi.SendGroupMessage(group, "正在下载语音文件，请稍候……");

        string dir = recordPath;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        for (int i = 0; i < data.Count; ++i)
        {
            if (!File.Exists(dir + data[i].filename))
                DownloadFile(data[i].audio, dir + data[i].filename);
        }
    }

    private void CheckStatus()
    {
        DateTime currentTime = DateTime.Now;
        while (running)
        {
            currentTime = DateTime.Now;
            if (currentTime.Minute == 0 && currentTime.Second == 0)
            {
                int hour = currentTime.Hour;
                string path = recordPath + GetRecordFileNameByHour(hour);
                if (CQApi.IsAllowSendRecord) CQApi.SendGroupMessage(group, "[CQ:record,file=" + path + "]");
                Thread.Sleep(1000);
                CQApi.SendGroupMessage(group, GetSentenceByHour(hour));
            }
            //CQLog.Debug("DEBUG", "Checking(" + GetGroup() + ")");
            Thread.Sleep(1000);
        }
    }

    private string GetSentenceByHour(int hour)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].hour == hour)
            {
                return data[i].jp + "\n" + data[i].cn;
            }
        }
        return "";
    }

    private string GetRecordFileNameByHour(int hour)
    {
        for (int i = 0; i < data.Count; ++i)
        {
            if (data[i].hour == hour)
            {
                if (!File.Exists(recordPath + data[i].filename)) {
                    DownloadFile(data[i].audio, recordPath + data[i].filename);
                }
                return data[i].filename;
            }
        }
        return "";
    }

    public void DownloadFile(string url, string path)
    {
        HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
        //发送请求并获取相应回应数据
        HttpWebResponse response = request.GetResponse() as HttpWebResponse;
        //直到request.GetResponse()程序才开始向目标网页发送Post请求
        Stream responseStream = response.GetResponseStream();
        //创建本地文件写入流
        Stream stream = new FileStream(path, FileMode.Create);
        byte[] bArr = new byte[1024];
        int size = responseStream.Read(bArr, 0, (int)bArr.Length);
        while (size > 0)
        {
            stream.Write(bArr, 0, size);
            size = responseStream.Read(bArr, 0, (int)bArr.Length);
        }
        stream.Close();
        responseStream.Close();
    }

    public static void WriteToFile(string filename, string content)
    {
        string[] contents = { content };
        WriteToFile(filename, contents);
    }

    public static void WriteToFile(string filename, string[] contents)
    {
        using (StreamWriter file = new StreamWriter(@filename, false, Encoding.UTF8))
        {
            foreach (string line in contents)
            {
                file.WriteLine(line);
            }
        }
    }

    public static string ReadFromFile(string filename)
    {
        string output = "";
        using (StreamReader sr = new StreamReader(filename, Encoding.UTF8))
        {
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                output = line;
                break;
            }
            return output;
        }
    }

    private string GetJson(string name)
    {
        try
        {
            string url = "https://api.mikualpha.com/kancolle/KanColleTime.php?v=" + version + "&name=" + name + "&group=" + group.ToString();
            string json = Encoding.Default.GetString(HttpWebClient.Get(url));
            if (json.Contains("Error: "))
            {
                string temp = json.Substring(json.IndexOf(":") + 2);
                CQApi.SendGroupMessage(group, temp);
                return "";
            }
            if (!json.Contains("[")) return "";
            return json;
        } catch (MissingMethodException) {
            return "";
        }
    }

    private List<Hour> ParseJson(string json)
    {
        if (json == null || json == "") return null;
        List<Hour> temp = JsonConvert.DeserializeObject<List<Hour>>(json);
        return temp;
    }

    /// <summary>
    /// 获取文件MD5值
    /// </summary>
    /// <param name="fileName">文件绝对路径</param>
    /// <returns>MD5值</returns>
    public static string GetMD5HashFromFile(string fileName)
    {
        try
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
        }
    }

    public class Hour
    {
        public int hour { get; set; }
        public string jp { get; set; }
        public string cn { get; set; }
        public string audio { get; set; }
        public string filename { get; set; }
    }
}

