using System;
using System.IO;
using JsonC = Newtonsoft.Json.JsonConvert;

namespace BrainCommon
{
    public class ClientConfig
    {
        public string Ip="192.168.0.101";
        public int Port=8088;
        public float ReferenceVoltage=4.5f;
        public uint DeviceId=19831980;

        private ClientConfig()
        {
            
        }
        
        public string ToJson()
        {
            return JsonC.SerializeObject(this);
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

        private static ClientConfig _instance;
        public static ClientConfig GetConfig()
        {
            if (_instance == null)
                _instance = LoadFromFile("BrainInterfaceClientConfig.json");
            return _instance;
        }

        public static void OnAppExit()
        {
            _instance?.WriteToFile("BrainInterfaceClientConfig.json");
        }
    }
}