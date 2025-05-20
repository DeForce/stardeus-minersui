using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Data;
using Game.Data.Planets;
using Game.Systems;
using Game.Systems.Copters;
using Game.Systems.Planets;
using Game.UI;
using HarmonyLib;
using ShowMiners.UI;
using UnityEngine;

namespace ShowMiners.Systems {
    public sealed class ShowMinersSys : GameSystem {
        public const string SysId = "ShowMinersSys";
        public static Harmony Harmony;
        public override string Id => SysId;
        public string GetName => Id;
        public override bool SkipInSandbox => false;

        private Dictionary<int, CopterMissionType> CopterMissions = new();

        // Reflects
        static readonly FieldInfo PlanetResourcesIdx = AccessTools.Field(typeof(PlanetsSys), "planetResourcesIdx");

        static readonly Func<PlanetResource, bool, float> GetAutoMiningRateFor =
            AccessTools.MethodDelegate<Func<PlanetResource, bool, float>>(
                AccessTools.Method(
                    typeof(PlanetsSys),
                    "GetAutoMiningRateFor",
                    new[] { typeof(PlanetResource), typeof(bool) }
                )
            );

        static readonly Action<DetailBlockStarmapWidget> RebuildStarmapMenu =
            AccessTools.MethodDelegate<Action<DetailBlockStarmapWidget>>(
                AccessTools.Method(
                    typeof(DetailBlockStarmapWidget),
                    "RebuildMenu"
                )
            );

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() {
            GameSystems.Register(SysId, () => new ShowMinersSys());
        }

        public static ShowMinersSys Instance;
        private ShowMinersUI UI;

        [RuntimeInitializeOnLoadMethod]
        static void StaticConstructorOnStartup() {
            // D.Err("Loaded Harmony");
            LoadHarmony();
        }

        static void LoadHarmony() {
            Harmony = new Harmony("CzT.ShowMiners");
            Harmony.PatchAll();
        }

        protected override void OnInitialize() {
            Instance = this;
            UI = new ShowMinersUI(this);
            S.Sig.CopterMissionEnded.AddListener(CopterMissionEnded);
            S.Sig.CopterMissionStarted.AddListener(CopterMissionStarted);
        }

        private void CopterMissionStarted(CopterMissionData mission) {
            // D.Err("Mission Started");
            CopterMissions.Add(mission.TargetId, mission.MissionType);
            RebuildMenu();
        }

        private void CopterMissionEnded(CopterMissionData mission) {
            // D.Err("Mission Ended");
            CopterMissions.Remove(mission.TargetId);
            RebuildMenu();
        }

        public CopterMissionType? GetMissionType(int resourceId) {
            if (CopterMissions.TryGetValue(resourceId, out var missionType)) {
                return missionType;
            }

            return null;
        }

        public override void Unload() {
        }

        public void AddMinersUI(List<UDB> res) {
            UI.AddMinersUI(res, GetMaxResourceID());
        }

        // PlanetsSys.planetResourcesIdx is private, so use reflection to get it
        public int GetMaxResourceID() {
            return (int)PlanetResourcesIdx.GetValue(S.Sys.Planets);
        }


        // PlanetsSys.planetResourcesIdx is private, so use reflection to get it
        public float GetMiningRate(PlanetResource resource) {
            return GetAutoMiningRateFor(resource, true);
        }

        public void RebuildMenu() {
            RebuildStarmapMenu(DetailBlockStarmapWidget.Current);
        }
    }
}