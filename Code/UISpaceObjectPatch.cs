using System.Collections.Generic;
using System.Reflection;
using Game.Constants;
using Game.Data.Space;
using Game.UI;
using Game.Utils;
using HarmonyLib;
using KL.Utils;
using ShowMiners.Systems;
using UnityEngine;
using UnityEngine.UI;

[HarmonyPatch(typeof(UISpaceObject), nameof(UISpaceObject.Reload))]
public static class UISpaceObjectPatch {
    static readonly FieldInfo _extraIcon =
        AccessTools.Field(typeof(UISpaceObject), "extraIcon");

    static readonly FieldInfo _image =
        AccessTools.Field(typeof(UISpaceObject), "image");

    static readonly FieldInfo _so =
        AccessTools.Field(typeof(UISpaceObject), "so");

    public const string DrillMarker = "Marker/DrillMarker";

    [HarmonyPrefix]
    public static void Prefix(UISpaceObject __instance) {
        var icon = (Image)_extraIcon.GetValue(__instance);
        
        // Revert any changes if they exist
        if (icon.rectTransform.localPosition.Equals(new Vector3(128f, 128f, 0))) {
            icon.rectTransform.localPosition = new Vector3(128f, 128f, 0);
        }
        icon.gameObject.SetActive(false);
    }

    [HarmonyPostfix]
    public static void Postfix(UISpaceObject __instance) {
        var so = (SpaceObject)_so.GetValue(__instance);

        var parentTypeId = so.Parent.Type.Id;
        switch (parentTypeId) {
            case SpaceObjectTypeId.Universe:
            case SpaceObjectTypeId.Region: {
                if (HasDescendantOfType(so, SpaceObjectTypeId.Resource)) {
                    var image = (Image)_image.GetValue(__instance);
                    var icon = (Image)_extraIcon.GetValue(__instance);
                    icon.sprite = Images.Sprite(DrillMarker);
                    icon.gameObject.SetActive(true);

                    icon.rectTransform.sizeDelta = new Vector2(128f, 128f) / image.rectTransform.localScale.x;
                    image.rectTransform.rotation = Quaternion.identity;
                    if (parentTypeId == SpaceObjectTypeId.Universe) {
                        icon.transform.localPosition = new Vector3(96f, 256f, 0);
                    }
                    else {
                        icon.transform.localPosition = new Vector3(96f, 128f, 0);
                    }
                }

                break;
            }
        }
    }

    public static bool HasDescendantOfType(this SpaceObject root, string targetType) {
        if (root?.Children != null) {
            foreach (var child in root.Children) {
                if (child.Type.Id == targetType) {
                    if (root.S.Sys.Planets.TryGetResourceFor(child, out var res, out var mt)) {
                        if (res.AutoMineSpeed > 0) return true;
                    }

                    continue;
                }

                if (child.HasDescendantOfType(targetType)) return true;
            }
        }

        return false;
    }
}