using System;
using System.IO;
using System.Reactive.Subjects;
using Newtonsoft.Json;
using JsonC = Newtonsoft.Json.JsonConvert;

namespace BrainCommon
{
    /// <summary>
    /// 配置文件默认存放在BrainInterfaceClientConfig.json
    /// GetConfig自动读取，关闭程序自动保存
    /// 如果客户端运行过程中其他程序修改了配置文件，这个类不会检测修改，
    /// 关闭时保存的依然是客户端内部的配置数据
    /// </summary>
    public class ClientConfig
    {
        public string Ip= "127.0.0.1";
        public int Port= 9211;
        public float ReferenceVoltage=4.5f;
        public uint DeviceId=19831980;
        public bool IsAutoStart;
        public bool EnableCommandTimeout;
        public uint TimeoutMilliseconds=100;
        public WaveletReconstructionConfig WaveletRecCfg;
        public FilterTypeList FilterLst;
        
        //TODO to be replaced by FilterLst
        public int LowRate=5;
        public int HighRate=100;
        public int FilterHalfOrder = 5;//replace by BandPassStopFilter.HalfOrder

        private ClientConfig()
        {
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        private void ProcessExit(object sender, EventArgs e)
        {
            _instance?.WriteToFile("BrainInterfaceClientConfig.json");
        }

        public string ToJson()
        {
            return JsonC.SerializeObject(this,Formatting.Indented);
        }

        public static ClientConfig FromJson(string str)
        {
            return JsonC.DeserializeObject<ClientConfig>(str);
        }

        public void WriteToFile(string filename)
        {
            try
            {
                File.WriteAllText(filename,ToJson());
            }
            catch (Exception e)
            {
                AppLogger.Error(e.Message);
            }
        }

        public static ClientConfig LoadFromFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    var json=File.ReadAllText(filename);
                    return FromJson(json);
                }
                return new ClientConfig();
            }
            catch (Exception e)
            {
                AppLogger.Error(e.Message);
                return new ClientConfig();
            }
        }
        
        public static void ChangeTimeout(bool enalbedTimeout, uint timeoutMilliseconds)
        {
            var tmp = GetConfig();
            var changed = false;
            if (tmp.EnableCommandTimeout != enalbedTimeout)
            {
                changed = true;
                tmp.EnableCommandTimeout = enalbedTimeout;
            }
            if (tmp.TimeoutMilliseconds != timeoutMilliseconds)
            {
                changed = true;
                tmp.TimeoutMilliseconds = timeoutMilliseconds;
            }
            if (changed)
                _changineConfig.OnNext((enalbedTimeout, timeoutMilliseconds));
        }

        private static readonly Subject<(bool, uint)> _changineConfig = new Subject<(bool, uint)>();
        public static IObservable<(bool, uint)> ChangingConfig => _changineConfig;

        private static ClientConfig _instance;

        public static ClientConfig GetConfig()
        {
            if (_instance == null)
                _instance = LoadFromFile("BrainInterfaceClientConfig.json");
            return _instance;
        }

        public static void ChangeTimeout(bool? enalbedTimeout, string timeoutMilliseconds)
        {
            var tmp = GetConfig();
            if (!uint.TryParse(timeoutMilliseconds, out var tm) || tm<=0)
            {
                tm = tmp.TimeoutMilliseconds;
            }
            var en = enalbedTimeout ?? tmp.EnableCommandTimeout;
            ChangeTimeout(en, tm);
        }
    }
}