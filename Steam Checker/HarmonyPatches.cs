using ExitGames.Client.Photon;
using GorillaNetworking;
using GorillaTag;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace nametag
{
    internal class HarmonyPatches
    {
        public static void PatchAll()
        {
            new Harmony("com.nooob.steamchecker").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}