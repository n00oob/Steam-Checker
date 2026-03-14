using BepInEx;
using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace nametag
{
    [BepInPlugin("com.nooob.steamchecker", "Steam Checker", "1.0.0")]
    [HarmonyPatch(typeof(GTPlayer), "LateUpdate")]
    public class Main : BaseUnityPlugin
    {
        private static TextMeshPro cocTitle;
        private static TextMeshPro cocHeader;
        private static Dictionary<VRRig, GameObject> nametags = new Dictionary<VRRig, GameObject> { };

        void Awake() => HarmonyPatches.PatchAll();
        public static void Postfix()
        {
            if (!cocTitle || !cocHeader)
            {
                cocTitle = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText").GetComponent<TextMeshPro>();
                cocHeader = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData").GetComponent<TextMeshPro>();
            }

            NameTags();
            COCText();
        }

        public static void COCText()
        {
            cocTitle.richText = true;
            cocTitle.text = "Not in lobby!";
            string text = $"{PhotonNetwork.LocalPlayer.NickName} - {GetLocalPlatform()} - Fps {GetLocalFps()}\n";

            if (PhotonNetwork.InRoom)
            {
                cocTitle.text = $"Current lobby: {PhotonNetwork.CurrentRoom.Name}";
                foreach (VRRig vrrig in VRRigCache.ActiveRigs)
                {
                    if (vrrig == GorillaTagger.Instance.offlineVRRig) continue;
                    text += $"{vrrig.Creator.NickName} - {GetPlatform(vrrig, false)} - Fps {vrrig.fps.ToString()}\n";
                }
            }
            cocHeader.text = text;
        }

        public static void NameTags()
        {
            List<VRRig> removeThings = new List<VRRig>();

            foreach (KeyValuePair<VRRig, GameObject> nametag in nametags)
            {
                bool found = false;
                foreach (VRRig rig in VRRigCache.ActiveRigs)
                {
                    if (rig == nametag.Key) { found = true; break; }
                }
                if (!found)
                {
                    Destroy(nametag.Value);
                    removeThings.Add(nametag.Key);
                }
            }

            foreach (VRRig rig in removeThings) nametags.Remove(rig);

            foreach (VRRig vrrig in VRRigCache.ActiveRigs)
            {
                if (vrrig == GorillaTagger.Instance.offlineVRRig) continue;
                if (vrrig.Creator == null) continue;

                Color playerColor = GetPlayerColor(vrrig);
                NetPlayer rig = vrrig.Creator;
                if (!rig.IsValid) continue;

                if (!nametags.ContainsKey(vrrig))
                {
                    GameObject gameObject = new GameObject("steamchecker_Nametag");
                    gameObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

                    TextMeshPro textMesh = gameObject.AddComponent<TextMeshPro>();
                    textMesh.fontSize = 6;
                    textMesh.alignment = TextAlignmentOptions.Center;
                    textMesh.color = playerColor;
                    textMesh.fontMaterial.renderQueue = 3000;

                    nametags.Add(vrrig, gameObject);
                }

                string text = $"<color=#808080>[{GetPlatform(vrrig, true)}] [{GetFps(vrrig, true)}]</color> <color=#{ColorUtility.ToHtmlStringRGB(playerColor)}>{rig.NickName}</color>\n";
                GameObject nameTag = nametags[vrrig];
                TextMeshPro textMeshPro = nameTag.GetComponent<TextMeshPro>();
                textMeshPro.text = text;
                textMeshPro.fontSize = 6;

                nameTag.transform.position = vrrig.bodyTransform.position + vrrig.bodyTransform.up * (0.8f * vrrig.scaleFactor);
                nameTag.transform.LookAt(Camera.main.transform.position);
                nameTag.transform.Rotate(0f, 180f, 0f);
            }
        }

        public static string GetPlatform(VRRig vrrig, bool color)
        {
            if (vrrig.initializedCosmetics.ToString() == "False") return "Loading";
            HashSet<string> cosmetics = vrrig._playerOwnedCosmetics;
            if (color)
            {
                if (cosmetics.Contains("S. FIRST LOGIN")) return "<color=#FF7F00>Steam</color>";
                if (cosmetics.Contains("FIRST LOGIN")) return "<color=#FF7F00>Quest Pc</color>";
                if (vrrig.Creator.GetPlayerRef().CustomProperties.Count > 1) return "<color=#FF7F00>Pc</color>";
                return "<color=#00FF00>Quest</color>";
            }
            else
            {
                if (cosmetics.Contains("S. FIRST LOGIN")) return "Steam";
                if (cosmetics.Contains("FIRST LOGIN")) return "Quest Pc";
                if (vrrig.Creator.GetPlayerRef().CustomProperties.Count > 1) return "Pc";
                return "Quest";
            }
        }

        public static string GetFps(VRRig vrrig, bool color)
        {
            int fps = (int)Traverse.Create(vrrig).Field("fps").GetValue();
            if (!color) return fps.ToString();

            string thing = fps.ToString();
            if (fps >= 80) thing = $"<color=#00FF00>{fps}</color>";
            if (fps >= 60 && fps < 80) thing = $"<color=#FF7F00>{fps}</color>";
            if (fps < 60) thing = $"<color=#FF0000>{fps}</color>";
            return thing;
        }

        public static string GetLocalPlatform() => PlayFabAuthenticator.instance.platform.ToString().ToLower() == "steam" ? "Steam" : "Quest";
        public static string GetLocalFps() => GorillaTagger.Instance.offlineVRRig.fps.ToString();

        public static Color GetPlayerColor(VRRig Player)
        {
            switch (Player.setMatIndex)
            {
                case 1:
                    return Color.red;
                case 2:
                case 11:
                    return new Color32(255, 128, 0, 255);
                case 3:
                case 7:
                    return Color.blue;
                case 12:
                    return Color.green;
                default:
                    return Player.playerColor;
            }
        }
    }
}
