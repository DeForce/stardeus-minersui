using HarmonyLib;
using System.Collections.Generic;
using Game.Systems;
using Game.Data;
using ShowMiners.Systems;


[HarmonyPatch(typeof(ScanSys), nameof(ScanSys.AddUIButton))]
public static class ScanSysAddUIButtonPatch {
    [HarmonyPostfix]
    public static void Postfix(List<UDB> res, ScanSys __instance) {
        ShowMinersSys.Instance.AddMinersUI(res);
    }
}