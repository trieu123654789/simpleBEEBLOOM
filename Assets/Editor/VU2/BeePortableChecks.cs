using System;
using System.Collections.Generic;
using System.IO;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class BeePortableChecks
{
    public static string Run()
    {
        var checks = new List<string>();
        string path = "Library/HoneyPilot/connection-check-" + Guid.NewGuid().ToString("N") + ".json";
        void Check(bool value, string name) { if (!value) throw new Exception(name); checks.Add(name); }
        void Refuses(Action action, string name) {
            bool rejected = false; try { action(); } catch { rejected = true; }
            Check(rejected, name);
        }
        void Profile(string endpoint) => File.WriteAllText(path, new JObject {
            ["schema"]="beebloom-connection-v1", ["simpleEndpoint"]=endpoint
        }.ToString());
        Directory.CreateDirectory("Library/HoneyPilot");
        try {
            Check(BeeConnectionProfile.Resolve("ws://192.0.2.1:8080",path)=="ws://192.0.2.1:8080", "missing file preserves scene connection fallback");
            Profile("ws://192.0.2.2:8080");
            Check(BeeConnectionProfile.Resolve("ws://192.0.2.1:8080",path)=="ws://192.0.2.2:8080", "external profile overrides saved IP without changing scene");
            Profile("ws://localhost:8080");
            Check(BeeConnectionProfile.Resolve("ws://192.0.2.1:8080",path)=="ws://localhost:8080", "profile changes are read without app rebuild or cached IP");
            File.WriteAllText(path,"broken JSON");
            Refuses(()=>BeeConnectionProfile.Resolve("ws://192.0.2.1:8080",path),"malformed profile fails explicitly instead of connecting to old server");
            File.WriteAllText(path,"{\"schema\":\"other\",\"simpleEndpoint\":\"ws://localhost:8080\"}");
            Refuses(()=>BeeConnectionProfile.Resolve("ws://localhost:8080",path),"unsupported schema rejected");
            File.WriteAllText(path,"{\"schema\":\"beebloom-connection-v1\",\"simpleEndpoint\":\"ws://localhost:8080\",\"flightSpeed\":500}");
            Refuses(()=>BeeConnectionProfile.Resolve("ws://localhost:8080",path),"profile cannot load gameplay tuning");
            foreach (string invalid in new[]{"http://localhost:8080","ws://localhost:8080/api","ws://user:pass@localhost:8080","ws://localhost:8080?x=1","ws://localhost:0","ws://[::1]:8080"})
                Refuses(()=>BeeConnectionProfile.ValidateEndpoint(invalid),"reject unsupported endpoint " + invalid);
            string session=Guid.NewGuid().ToString("N");
            var source=new JObject { ["session"]=session,["checkpoint"]="Z:/old-machine/checkpoint.json",["contact_radius"]=0.75 };
            string before=source.ToString();
            var portable=BeeConnectionProfile.ForSimpleServer(source);
            Check(portable["checkpoint"]==null && (string)portable["checkpoint_key"]==session,"SIMPLE payload sends a logical key, no PC filesystem path");
            Check(source.ToString()==before && (double)portable["contact_radius"]==0.75,"transport leaves source configuration and interaction tuning intact");
            source["session"]="../escape";
            Refuses(()=>BeeConnectionProfile.ForSimpleServer(source),"invalid checkpoint key refused");
            var result=new {passed=true,count=checks.Count,checks};
            File.WriteAllText("Library/HoneyPilot/portable-validation.json",JsonConvert.SerializeObject(result,Formatting.Indented));
            return JsonConvert.SerializeObject(result);
        } finally { if(File.Exists(path)) File.Delete(path); }
    }
}
