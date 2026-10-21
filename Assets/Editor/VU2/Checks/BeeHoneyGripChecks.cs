using System;
using LetItBee.HoneyPilot;
public static class BeeHoneyGripChecks
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Let It Bee/Check Honey Grip")]
    public static void RunMenu() { UnityEngine.Debug.Log(Run()); }
#endif
    static int count;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); count++; Console.WriteLine("PASS " + label); }
    public static string Run()
    {
        count = 0;
        var g = new BeeHoneyGrip();
        g.Sample(.8f);
        Check(g.Choose(true,true,"ready",true,false)==null,"pickup waits for busy server");
        Check(g.Choose(true,false,"ready",true,false)=="pick","held grip is processed after busy clears");
        Check(g.Choose(true,false,"ready",true,false)==null,"one pickup attempt per squeeze");
        g.Sample(0f);g.Complete("pick",true);
        Check(g.ReleasePending,"release before pickup ACK is retained");
        Check(g.Choose(true,true,"held",false,true)==null && g.ReleasePending,"busy cannot erase queued release");
        Check(g.Choose(true,false,"held",false,true)=="deposit","delayed pickup then release can deposit");
        Check(g.Choose(true,false,"stored",false,true)==null,"successful deposit is not repeated");
        g.Reset();g.Sample(.8f);
        Check(g.Choose(true,false,"ready",false,false)==null,"remote grip cannot pick");
        Check(g.Choose(true,false,"ready",true,false)=="pick","held grip works after reaching honey");
        g.Complete("pick",false);
        Check(g.Choose(true,false,"ready",true,false)==null,"rejected pickup does not flood GAMA");
        g.Sample(0f);g.Sample(.8f);
        Check(g.Choose(true,false,"ready",true,false)=="pick","a new squeeze can retry");
        g.Complete("pick",true);g.Sample(0f);
        Check(g.Choose(true,false,"held",false,false)=="drop","release away from hive drops honey");
        g.Reset();
        Check(g.Choose(true,false,"held",false,false)==null,"restored held checkpoint is not dropped on startup");
        g.Sample(.8f);g.Choose(true,false,"held",false,false);g.Sample(0f);
        Check(g.Choose(true,false,"held",false,true)=="deposit","restored honey can be adopted and released");
        g.Reset();g.Sample(.5f);Check(!g.Pressed,"noise below press threshold ignored");
        g.Sample(.7f);g.Sample(.5f);Check(g.Pressed,"grip hysteresis retains a held squeeze");
        Check(g.Sample(.4f) && !g.Pressed,"release threshold creates one release edge");
        Check(g.Choose(true,false,"held",false,true)==null,"empty-handed release cannot drop restored inventory");
        g.Reset();g.Sample(.8f);Check(g.Choose(false,false,"loose",true,false)==null,"disconnected client sends nothing");
        Check(g.Choose(true,false,"loose",true,false)=="pick","reconnected client honors deliberate held grip");
        g.Reset();Check(!g.ReleasePending && !g.Pressed,"lifecycle reset clears input intent");
        g.Sample(.8f);g.Choose(true,false,"held",false,false);g.Sample(0f);
        Check(g.ReleasePending,"release remains pending while waiting for server");
        g.Sample(.8f);
        Check(!g.ReleasePending && g.Choose(true,false,"held",false,true)==null,"regripping cancels an unsent release");
        return "Grip checks passed: " + count;
    }
}
