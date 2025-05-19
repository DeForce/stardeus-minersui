using System;
using Game.Constants;
using Game.Data;
using ShowMiners.Systems;
using KL.Utils;
using ShowMiners.Constants;
using Game.Data.Planets;
using Game.Data.Space;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.CodeGen;
using Game.Research;
using Game.Systems.Copters;
using Game.Systems.Planets;
using Game.Utils;
using Game.UI;
using KL.Collections;
using Unity.Collections;

namespace ShowMiners.UI {
    public sealed class ShowMinersUI : IUIDataProvider, IUIContextMenuProvider {
        private readonly ShowMinersSys sys;
        private readonly GameState S;

        private static float automineThreshold = 0.0001f;

        public ShowMinersUI(ShowMinersSys sys) {
            this.sys = sys;
            S = sys.S;
        }

        // This doesn't belong to an entity, so let's return a null
        public Entity Entity => null;
        public string GetName() => sys.Id;

        private UDB header;
        private UDB spacer;
        private Dictionary<int, UDB> UIItems = new();

        public UDB GetUIBlock() {
            D.Err("Getting ui block");
            return null;
        }

        public void GetUIDetails(List<UDB> res) {
            D.Err("Getting UI Details");
        }

        public void ContextActions(List<UDB> res) {
            D.Err("Context Actions");

            header ??= UDB.Create(this, UDBT.DTextRB2Header, IconId.CDrill, TS.Translate(TS.UITitle))
                .WithRBFunction(S.Sig.HideContextMenu.Send);
            res.Add(header);

            var resourceMaxIdx = sys.GetMaxResourceID();
            var inSystemResources = new List<(PlanetResource, MatType, int)>();

            for (int i = resourceMaxIdx - 1; i >= 0; i--) {
                if (S.Sys.Planets.TryGetResource(i, out var resource)) {
                    if (resource.AutoMineSpeed > automineThreshold) {
                        var mat = MatType.FastGet(resource.ResourceId);
                        inSystemResources.Add((resource, mat, i));
                    }
                }
            }

            var sortedResources = inSystemResources.OrderBy(r => r.Item2.NameT);
            foreach (var (resource, matType, resourceId) in sortedResources) {
                var rSO = S.Universe.Find(resource.Id);
                var textTint = matType.NameT;
                var resourceString = GetResourceString(resource, textTint, true, true);

                UDB uiItem = null;
                uiItem = UDB.Create(this, UDBT.DProgressBtn, matType.IconId, resourceString)
                    .WithIconTint(matType.IconTint)
                    .WithText2(T.Show).WithClickFunction(() => {
                        try {
                            if (S == null)
                                return;
                            if (rSO?.Parent == S.Universe.Player.Parent) {
                                S.Sig.HideOverlay.Send();
                                if (!UIShowing.Starmap)
                                    S.Sys.Universe.OpenStarmap();
                                A.Starmap?.FocusOn(rSO);
                            }
                            else {
                                if (rSO == null)
                                    return;
                                UIPopupWidget.Spawn(matType.IconId, uiItem.Title, $"{T.Location}: {rSO.ParentPath()}");
                            }
                        }
                        catch (Exception ex) {
                            GameState s = S;
                            Universe universe = S?.Universe;
                            SpaceObject player = S?.Universe?.Player;
                            MatType o4 = matType;
                            SpaceObject o5 = rSO;
                            D.LogEx(ex,
                                "Failed when clicking planet resource UDB. State: {0}. Universe: {1}. Player: {2}. Material: {3}. SO: {4}",
                                s, universe, player, o4, o5);
                        }
                    })
                    .WithRange(0.0f, resource.UnretrievedMax);
                uiItem.UpdateValue(resource.Unretrieved);
                string tooltip = PlanetsSys.TooltipForResource(in resource, matType);
                uiItem.UpdateTooltip(tooltip);

                res.Add(uiItem);
            }
        }


        internal void AddMinersUI(List<UDB> res, int resourceMaxIdx) {
            if (!S.Research.IsUnlocked(DefIdsH.ResearchSpaceTravelMiningAutomation)) {
                // Not researched, not showing
                D.Err("NOT RESEARCHED, SKIPPING");
                return;
            }

            UIItems.Clear();

            // header ??= UDB.Create(this, UDBT.DTextRB2Header, IconId.CDrill, TS.Translate(TS.UITitle));
            // res.Add(header);

            var playerLocation = S.Universe.Player.Parent;

            var inSystemResources = new List<(PlanetResource, MatType)>();
            var allDrillResources = 0;

            for (int i = resourceMaxIdx - 1; i >= 0; i--) {
                if (S.Sys.Planets.TryGetResource(i, out var resource)) {
                    var starObject = S.Universe.Find(resource.Id);

                    if (starObject?.Parent == playerLocation) {
                        var mat = MatType.FastGet(resource.ResourceId);
                        inSystemResources.Add((resource, mat));
                    }

                    if (resource.AutoMineSpeed > automineThreshold) {
                        allDrillResources++;
                    }
                }
            }

            if (allDrillResources <= 0) {
                // No Drills, not showing the UI
                return;
            }

            var allDrills = UDB.Create(this, UDBT.DTextBtn, IconId.WPickaxe,
                    $"{TS.Translate(TS.KnownDrills)}: {allDrillResources}")
                .WithClickFunction(() => { S.Sig.ShowContextMenu.Send(this); });

            allDrills.UpdateText2(TS.Translate(TS.ShowDrills));
            res.Add(allDrills);

            spacer ??= UDBCache.Label(TS.Translate(TS.InSector));
            res.Add(spacer);

            var sortedResources = inSystemResources.OrderBy(r => r.Item2.NameT);

            foreach (var (resource, material) in sortedResources) {
                var starObject = S.Universe.Find(resource.Id);
                var isMined = resource.AutoMineSpeed > automineThreshold;

                var textTint = material.NameT;
                var resourceString = GetResourceString(resource, textTint, isMined);
                var buttonType = isMined ? UDBT.DProgressBtn : UDBT.DTextBtn;

                var resourceItem = UDB.Create(this, buttonType, material.IconId, resourceString)
                    .WithIconTint(material.IconTint)
                    .WithClickFunction(() => {
                        if (isMined) {
                            RequestDrillRig(starObject, resource);
                        }
                        else {
                            SendDrillRig(starObject, resource);
                        }
                    });

                if (isMined) {
                    resourceItem = resourceItem.WithRange(0, resource.UnretrievedMax);
                    resourceItem.Value = resource.Unretrieved;
                }

                UIItems.Add(resource.Id, resourceItem);

                var buttonText = GetButtonText(isMined, resource.Id);
                resourceItem.UpdateText2(TS.Translate(buttonText));

                string tooltip = PlanetsSys.TooltipForResource(in resource, material);
                resourceItem.UpdateTooltip(tooltip);

                res.Add(resourceItem);
            }
        }

        private string GetButtonText(bool isMined, int resourceId) {
            D.Err("here");
            var missionType = sys.GetMissionType(resourceId);
            D.Err("mission type", missionType);
            if (missionType is null) {
                return isMined ? TS.RetrieveMiner : TS.SendMiner;
            }

            switch (missionType) {
                case CopterMissionType.DeployMiner:
                    return TS.SentMiner;
                case CopterMissionType.RetrieveMiner:
                    return TS.ProgressMiner;
                default:
                    return "UNKNOWN";
            }
        }

        private void SendDrillRig(SpaceObject starObject, PlanetResource resource) {
            D.Err("Sending Drill Rig");
            var uiElement = UIItems.Get(resource.Id, null);
            if (uiElement is null) return;

            var result = S.Sys.Copters.TryOrderMinerDeploymentTo(starObject);
            if (result) {
                UISounds.PlayDigitalConfirm();
            }
            else {
                UISounds.PlayDigitalCancel();
            }
        }

        private void RequestDrillRig(SpaceObject starObject, PlanetResource resource) {
            var uiElement = UIItems.Get(resource.Id, null);
            if (uiElement is null) return;

            var result = S.Sys.Copters.TryOrderMinerRetrievalFrom(starObject);
            if (result) {
                UISounds.PlayDigitalConfirm();

                // Updates not working?
                // D.Err("Sending stuff");
                // uiElement.UpdateText2(TS.Translate(TS.ProgressMiner));
            }
            else {
                // uiElement.UpdateText2(TS.Translate(TS.ProgressMinerAlready));
                UISounds.PlayDigitalCancel();
            }
        }

        private string GetResourceString(PlanetResource resource, string text, bool isMined, bool biggerUI = false) {
            // Padding is not a thing that I enjoy, but otherwise amount mined will be
            //  next to resource name and it's not cool
            if (isMined) {
                var amountMinedPerDay = sys.GetMiningRate(resource);
                var offset = biggerUI ? 25 : 12;
                return $"{TS.Truncate(text, offset - 3).PadRight(offset)} +{amountMinedPerDay:F1}";
            }

            return text.PadRight(10);
        }
    }
}