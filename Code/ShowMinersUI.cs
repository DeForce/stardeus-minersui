using Game.Constants;
using Game.Data;
using ShowMiners.Systems;
using KL.Utils;
using ShowMiners.Constants;
using Game.Data.Planets;
using Game.Data.Space;
using System.Collections.Generic;
using System.Linq;
using Game.Utils;
using Game.UI;

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
        private UDB header;

        public string GetName() {
            return sys.Id;
        }

        // This only shows the context menu, so it doesn't need a main UI block
        public UDB GetUIBlock() {
            return null;
        }

        // Not Needed
        public void GetUIDetails(List<UDB> res) {
        }

        public void ContextActions(List<UDB> res) {
        }

        internal void AddMinersUI(List<UDB> res, int resourceMaxIdx) {
            header ??= UDB.Create(this, UDBT.DTextRB2Header, IconId.CDrill, TS.Translate(TS.UITitle));
            res.Add(header);

            var resources = new List<(PlanetResource, MatType)>();

            for (int i = resourceMaxIdx - 1; i >= 0; i--) {
                if (S.Sys.Planets.TryGetResource(i, out var resource)) {
                    if (resource.AutoMineSpeed > automineThreshold) {
                        var mat = MatType.FastGet(resource.ResourceId);
                        D.Err("Resource {0}", mat.NameT, resource.AutoMineSpeed);
                        resources.Add((resource, mat));
                    }
                }
            }

            var playerLocation = S.Universe.Player.Parent;
            var sortedResources = resources.OrderBy(r => r.Item2.NameT)
                .OrderByDescending(r => S.Universe.Find(r.Item1.Id)?.Parent == playerLocation);

            foreach (var resMat in sortedResources) {
                var resource = resMat.Item1;
                var material = resMat.Item2;

                var starObject = S.Universe.Find(resource.Id);
                bool isInSystem = starObject?.Parent == playerLocation;

                string textTint = GetTextColor(isInSystem, material.NameT);
                string resourceString = GetResourceString(resource, textTint);

                UDBT buttonType = isInSystem ? UDBT.DProgressBtn : UDBT.DProgress;
                UDB resourceItem = UDB.Create(this, buttonType, material.IconId, resourceString)
                    .WithIconTint(material.IconTint)
                    .WithRange(0, resource.UnretrievedMax)
                    .WithClickFunction(() => { RequestDrillRig(starObject); });


                resourceItem.Value = resource.Unretrieved;

                resourceItem.UpdateText2(TS.Translate(TS.RetrieveMiner));

                res.Add(resourceItem);
            }
        }

        private void RequestDrillRig(SpaceObject resource) {
            var result = S.Sys.Copters.TryOrderMinerRetrievalFrom(resource);
            if (result) {
                UISounds.PlayDigitalConfirm();
            }
            else {
                UISounds.PlayDigitalCancel();
            }
        }

        private string GetResourceString(PlanetResource resource, string text) {
            var amountMinedPerDay = sys.GetMiningRate(resource);
            return $"{text,-35} +{amountMinedPerDay:F1}";
        }

        private string GetTextColor(bool isInSystem, string matName) {
            return isInSystem ? Texts.Green(matName) : matName;
        }
    }
}