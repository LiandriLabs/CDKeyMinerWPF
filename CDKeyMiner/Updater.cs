using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CDKeyMiner
{
    public class Updater
    {
        private static Updater upd = null;
        private string appDir = AppDomain.CurrentDomain.BaseDirectory;
        private Dictionary<string, string> manifest;
        private Dictionary<string, string> serverManifest;
        private string baseURL = "http://localhost:81/static/downloads/cdkm-latest/";
        public string NewVersion;

        private Updater() {
            var manifestJson = File.ReadAllText(Path.Combine(appDir, "manifest.json"));
            manifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(manifestJson);
            DeleteBackups(appDir);
        }

        public static Updater Instance
        {
            get
            {
                if (upd == null)
                {
                    upd = new Updater();
                }
                return upd;
            }
        }

        public bool CheckForUpdates()
        {
            using (var http = new WebClient())
            {
                var serverManifestJson = http.DownloadString(baseURL + "manifest.json");
                serverManifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(serverManifestJson);
            }
            foreach (var kvp in serverManifest)
            {
                if (!manifest.ContainsKey(kvp.Key) || kvp.Value != manifest[kvp.Key])
                {
                    NewVersion = serverManifest["CDKeyMiner.exe"];
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> DownloadUpdates()
        {
            if (serverManifest == null)
            {
                throw new Exception("CheckForUpdates not called");
            }
            using (var http = new WebClient())
            {
                foreach (var kvp in serverManifest)
                {
                    if (!manifest.ContainsKey(kvp.Key) || kvp.Value != manifest[kvp.Key])
                    {
                        var url = baseURL + kvp.Key.Replace("\\", "/");
                        var destination = Path.Combine(appDir, kvp.Key);
                        File.Move(destination, destination + ".bak");
                        await http.DownloadFileTaskAsync(url, destination);
                    }
                }
            }
            File.WriteAllText(Path.Combine(appDir, "manifest.json"), JsonConvert.SerializeObject(serverManifest));
            return true;
        }

        private void DeleteBackups(string dir)
        {
            foreach (var f in Directory.GetFiles(dir, "*.bak"))
            {
                File.Delete(f);
            }
            foreach (var d in Directory.GetDirectories(dir))
            {
                DeleteBackups(d);
            }
        }
    }
}
