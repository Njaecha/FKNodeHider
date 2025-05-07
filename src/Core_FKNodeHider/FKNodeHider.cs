using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Illusion.Extensions;
using KKAPI;
using KKAPI.Studio;
using Studio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FKNodeHider
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(KoikatuAPI.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class FKNodeHider : BaseUnityPlugin
    {
        public const string PluginName = "FKNodeHider";
        public const string GUID = "org.njaecha.plugins.fknodehider";
        public const string Version = "1.0.0";
        
        private static bool _initialized;
        
        internal new static ManualLogSource Logger;
        
        public static readonly Dictionary<OCIChar, Dictionary<int, bool>> FkVisibilitySettings = new Dictionary<OCIChar, Dictionary<int, bool>>();
        public static Toggle HairToggle {get; private set;}
        public static Toggle SkirtToggle {get; private set;}
        public static Toggle RightHandToggle {get; private set;}
        public static Toggle LeftHandToggle {get; private set;}
        public static Toggle BreastToggle {get; private set;}
        public static Toggle NeckToggle {get; private set;}
        public static Toggle BodyToggle {get; private set;}
        
        public static ConfigEntry<bool> Lines { get; set; }
        
        void Awake()
        {
            Logger = base.Logger;
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Lines = Config.Bind("", "Disable Lines", true, "Should hide the lines between FK nodes when the nodes are hidden.");
            
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (!StudioAPI.StudioLoaded || _initialized) return;
            HairToggle = CreateFkViewToggle("Toggle Hair", (int)OIBoneInfo.BoneGroup.Hair);
            SkirtToggle = CreateFkViewToggle("Toggle Skirt", (int)OIBoneInfo.BoneGroup.Skirt);
            RightHandToggle = CreateFkViewToggle("Toggle Right Hand", (int)OIBoneInfo.BoneGroup.RightHand);
            LeftHandToggle = CreateFkViewToggle("Toggle Left Hand", (int)OIBoneInfo.BoneGroup.LeftHand);
            BreastToggle = CreateFkViewToggle("Toggle Breast", (int)OIBoneInfo.BoneGroup.Breast);
            NeckToggle = CreateFkViewToggle("Toggle Neck", (int)OIBoneInfo.BoneGroup.Neck);
            BodyToggle = CreateFkViewToggle("Toggle Body", (int)OIBoneInfo.BoneGroup.Body, 3, 5, 9, 17);
            ChangeAdjustText();
            MoveResetButtons();
            _initialized = true;
        }
        
        public Toggle CreateFkViewToggle(string elementName, params int[] boneGroups)
        {
            Transform toggleGameObject = Studio.Studio.instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.transform.Find("02_Kinematic/00_FK").Find(elementName);
            GameObject newToggle = Instantiate(toggleGameObject.gameObject, toggleGameObject.transform.parent, true);
            newToggle.name = $"{elementName} Control View";
            Toggle toggle = newToggle.GetComponent<Toggle>();
            try
            {
                toggle.isOn = true;
            }
            catch (Exception)
            {
                // ignored
            }

            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(value =>
            {
                OCIChar chara = Studio.Studio.instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.ociChar;
                if (chara == null) return;
                SetBoneGroupVisibility(chara, value, boneGroups);
            });
            newToggle.transform.localPosition = toggle.transform.localPosition + new Vector3(30f, 0f, 0f);
            newToggle.transform.localScale = Vector3.one;
            return toggle;
        }

        public static void UpdateInfo(OCIChar chara)
        {
            if (!_initialized) return;
            if (HairToggle) HairToggle.isOn = !FkVisibilitySettings.ContainsKey(chara) || !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Hair) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Hair];
            if (SkirtToggle) SkirtToggle.isOn = !FkVisibilitySettings.ContainsKey(chara) || !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Skirt) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Skirt];
            if (RightHandToggle) RightHandToggle.isOn = !FkVisibilitySettings.ContainsKey(chara) || !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.RightHand) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.RightHand];
            if (LeftHandToggle) LeftHandToggle.isOn = !FkVisibilitySettings.ContainsKey(chara) || !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.LeftHand) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.LeftHand];
            if (BreastToggle) BreastToggle.isOn = !FkVisibilitySettings.ContainsKey(chara) || !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Breast) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Breast];
            if (NeckToggle) NeckToggle.isOn = !FkVisibilitySettings.ContainsKey(chara) || !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Neck) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Neck];
            if (BodyToggle) BodyToggle.isOn = !FkVisibilitySettings.ContainsKey(chara) || !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Body) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Body];
        }

        public static bool ShouldDrawLine(OCIChar chara, int fkCategory)
        {
            if (!Lines.Value || !FkVisibilitySettings.ContainsKey(chara)) { return true;}
            switch (fkCategory)
            {
                case 0:
                    return !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Hair) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Hair];
                case 1:
                    return !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Neck) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Neck];
                case 2:
                    return !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Breast) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Breast];
                case 3:
                    return !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Body) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Body];
                case 4:
                    return !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.RightHand) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.RightHand];
                case 5:
                    return !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.LeftHand) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.LeftHand];
                case 6:
                    return !FkVisibilitySettings[chara].ContainsKey((int)OIBoneInfo.BoneGroup.Skirt) || FkVisibilitySettings[chara][(int)OIBoneInfo.BoneGroup.Skirt];
                default:
                    return true;
            }
            
        }

        public void SetBoneGroupVisibility(OCIChar chara, bool visible, int[] boneGroups)
        {
            if (!FkVisibilitySettings.ContainsKey(chara)) FkVisibilitySettings.Add(chara, new Dictionary<int, bool>());
            foreach (int boneGroup in boneGroups)
            {
                FkVisibilitySettings[chara][boneGroup] = visible;
                chara.listBones.Where(bi => (int)bi.boneGroup == boneGroup).ToList().ForEach(bi =>
                {
                    GuideObject guideObject = bi.guideObject;
                    if (guideObject) guideObject.visible = visible;
                });
            }
        }
        
        public void ChangeAdjustText()
        {
            Text component = Studio.Studio.instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.transform.Find("02_Kinematic/00_FK/Text Draw").gameObject.GetComponent<Text>();
            component.text = "Active | Visible";
        }

        public void MoveResetButtons()
        {
            Transform fkPanel = Studio.Studio.instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.transform.Find("02_Kinematic/00_FK");
            
            var buttonNames = new List<string> { "Button Hair Init", "Button For Anime (1)", "Button For Anime (2)", "Button For Anime (3)", "Button For Anime (4)", "Button Skirt Init" };
            fkPanel.Children().Where(t => buttonNames.Contains(t.name)).ToList().ForEach(t =>
            {
                RectTransform rt = t.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(40f, 20f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + 30,  rt.anchoredPosition.y);
            });
        }
    }
}