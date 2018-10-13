using System;
using System.Collections.Generic;
using Windows.Storage;

namespace PdfReader.Classes {

    static class LocalSettingManager {
        public static string ReadSetting(string address,bool roam = false) {
            var ls = !roam ? ApplicationData.Current.LocalSettings : ApplicationData.Current.RoamingSettings;
            try {
                return ls.Values[address].ToString();
            }
            catch(Exception) { return "[ERROR!]"; }
        }

        public static bool ExistsSetting(string address,bool roam = false) {
            return ReadSetting(address,roam) != "[ERROR!]";
        }
        public static bool SaveSetting(string address,string setting,bool roam = false) {
            var ls = !roam ? ApplicationData.Current.LocalSettings : ApplicationData.Current.RoamingSettings;
            try {
                ls.Values[address] = setting;
                return true;
            }
            catch(Exception) { return false; }
        }
        public static void RemoveSetting(string address,bool roam = false) {
            var ls = !roam ? ApplicationData.Current.LocalSettings : ApplicationData.Current.RoamingSettings;
            try {
                ls.Values.Remove(address);
            }
            catch(Exception) { }
        }
        public static bool SaveSetting(string address,string setting) {
            var ls = ApplicationData.Current.LocalSettings;
            try {
                ls.Values[address] = setting;
                return true;
            }
            catch(Exception) { return false; }
        }
        public static void SetRecentToOpened(string path) {
            var sv = new List<string>();
            if(!ExistsSetting(App.SettingStrings["recent"])) 
                sv.Add(path);
            else {
                sv = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(ReadSetting(App.SettingStrings["recent"]));
                if(!sv.Contains(path))
                    sv.Add(path);
                //Sort reverst + just 5 of them
            }
            SaveSetting(App.SettingStrings["recent"],Newtonsoft.Json.JsonConvert.SerializeObject(sv));
        }
    }
}
