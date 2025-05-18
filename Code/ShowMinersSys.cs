using System.Collections.Generic;
using ShowMiners.UI;
using Game.Data;
using Game.Systems;
using KL.Utils;
using UnityEngine;
using HarmonyLib;
using Game.Systems.Planets;
using System.Reflection;
using System;
using Game.Data.Planets;

namespace ShowMiners.Systems {
    public sealed class ShowMinersSys : GameSystem {
        public const string SysId = "ShowMinersSys";
        public static Harmony Harmony;
        public override string Id => SysId;
        public override bool SkipInSandbox => false;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() {
            GameSystems.Register(SysId, () => new ShowMinersSys());
        }

        public static ShowMinersSys Instance;

        private ShowMinersUI UI;

        // Public so that the UI can use it
        public int SomeVariable;

        [RuntimeInitializeOnLoadMethod]
        static void StaticConstructorOnStartup() {
            D.Err("Loaded Harmony");
            LoadHarmony();
        }

        static void LoadHarmony() {
            Harmony = new Harmony("CzT.ShowMiners");
            Harmony.PatchAll();
        }

        protected override void OnInitialize() {
            Instance = this;
            UI = new ShowMinersUI(this);
        }

        public string GetName() {
            return Id;
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
    }
}