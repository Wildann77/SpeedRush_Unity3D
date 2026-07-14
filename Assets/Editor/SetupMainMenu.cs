using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;

namespace SpeedRush.Editor
{
    public class SetupMainMenu : EditorWindow
    {
        [MenuItem("SpeedRush/Setup Main Menu UI")]
        static void Setup()
        {
            var canvas = GameObject.Find("MainMenuCanvas");
            var bgPanel = canvas.transform.Find("BackgroundPanel");
            var manager = GameObject.Find("MainMenuManager");
            var mainMenuUI = manager.GetComponent<SR_MainMenuUI>();

            // Cleanup all junk
            var allChildren = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in bgPanel) allChildren.Add(c);
            foreach (var c in allChildren)
            {
                if (c.name != "TitleText" && c.name != "SubtitleText")
                    DestroyImmediate(c.gameObject);
            }

            // Create panels
            var mp = CreatePanel("MainPanel", bgPanel);
            var lp = CreatePanel("LevelSelectPanel", bgPanel); lp.SetActive(false);
            var sp = CreatePanel("SettingsPanel", bgPanel); sp.SetActive(false);

            // Create a template button
            var template = new GameObject("TemplateBtn", typeof(RectTransform));
            template.transform.SetParent(bgPanel, false);
            template.AddComponent<CanvasRenderer>();
            var tImg = template.AddComponent<Image>();
            tImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            var tBtn = template.AddComponent<Button>();
            tBtn.transition = Selectable.Transition.ColorTint;
            var tText = new GameObject("Text", typeof(RectTransform));
            tText.transform.SetParent(template.transform, false);
            var tTxt = tText.AddComponent<Text>();
            tTxt.text = "BTN";
            tTxt.fontSize = 28;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = Color.white;
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var trt = tText.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            template.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 70);

            System.Func<string, Transform, GameObject> MkBtn = (name, parent) =>
            {
                var g = Instantiate(template);
                g.name = name; g.transform.SetParent(parent, false);
                g.transform.localPosition = Vector3.zero;
                return g;
            };

            // === MAIN PANEL ===
            var playB = MkBtn("PlayButton", mp.transform);
            playB.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
            playB.GetComponent<Image>().color = new Color(0f, 0.45f, 0.9f, 1f);
            playB.GetComponentInChildren<Text>().text = "PLAY";

            var setB = MkBtn("SettingsButton", mp.transform);
            setB.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            setB.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f, 1f);
            setB.GetComponentInChildren<Text>().text = "SETTINGS";

            var quitB = MkBtn("QuitButton", mp.transform);
            quitB.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            quitB.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);
            quitB.GetComponentInChildren<Text>().text = "QUIT";

            WireBtn(playB, mainMenuUI.OnPlayClicked);
            WireBtn(setB, mainMenuUI.OnSettingsClicked);
            WireBtn(quitB, mainMenuUI.QuitGame);

            // === LEVEL SELECT PANEL ===
            var l1b = MkBtn("Level1Button", lp.transform);
            l1b.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
            l1b.GetComponent<Image>().color = new Color(0f, 0.45f, 0.9f, 1f);
            l1b.GetComponentInChildren<Text>().text = "LEVEL 1";

            var l2b = MkBtn("Level2Button", lp.transform);
            l2b.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            l2b.GetComponent<Image>().color = new Color(0.9f, 0.2f, 0.2f, 1f);
            l2b.GetComponentInChildren<Text>().text = "LEVEL 2";

            var backL = MkBtn("BackButton", lp.transform);
            backL.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            backL.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);
            backL.GetComponentInChildren<Text>().text = "BACK";

            // Lock icon on Level2
            var lockIcon = new GameObject("LockIcon", typeof(RectTransform));
            lockIcon.transform.SetParent(l2b.transform, false);
            var lRT = lockIcon.GetComponent<RectTransform>();
            lRT.anchoredPosition = new Vector2(-140, 0); lRT.sizeDelta = new Vector2(60, 30);
            lRT.anchorMin = lRT.anchorMax = lRT.pivot = Vector2.one * 0.5f;
            lockIcon.AddComponent<CanvasRenderer>();
            var lockT = lockIcon.AddComponent<Text>();
            lockT.text = "LOCKED"; lockT.fontSize = 12; lockT.fontStyle = FontStyle.Bold;
            lockT.alignment = TextAnchor.MiddleCenter; lockT.color = Color.gray;
            lockT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            WireBtn(l1b, mainMenuUI.PlayLevel1);
            WireBtn(l2b, mainMenuUI.PlayLevel2);
            WireBtn(backL, mainMenuUI.OnBackFromLevels);

            // === SETTINGS PANEL ===
            var backS = MkBtn("BackButton", sp.transform);
            backS.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            backS.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);
            backS.GetComponentInChildren<Text>().text = "BACK";
            WireBtn(backS, mainMenuUI.OnBackFromSettings);

            // Volume label
            var volL = new GameObject("VolumeLabel", typeof(RectTransform));
            volL.transform.SetParent(sp.transform, false); volL.AddComponent<CanvasRenderer>();
            var volT = volL.AddComponent<Text>();
            volT.text = "VOLUME BGM"; volT.fontSize = 36; volT.fontStyle = FontStyle.Bold;
            volT.alignment = TextAnchor.MiddleCenter; volT.color = Color.white;
            volT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var vrt = volL.GetComponent<RectTransform>();
            vrt.anchoredPosition = new Vector2(0, 100); vrt.sizeDelta = new Vector2(300, 60);
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = Vector2.one * 0.5f;

            // Slider
            var sld = new GameObject("MusicSlider", typeof(RectTransform));
            sld.transform.SetParent(sp.transform, false); sld.AddComponent<CanvasRenderer>();
            sld.AddComponent<Image>();
            var sl = sld.AddComponent<Slider>();
            sl.minValue = 0f; sl.maxValue = 1f; sl.value = 0.5f;
            var srt = sld.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, 10); srt.sizeDelta = new Vector2(500, 40);
            srt.anchorMin = srt.anchorMax = srt.pivot = Vector2.one * 0.5f;

            var fa = new GameObject("Fill Area", typeof(RectTransform));
            fa.transform.SetParent(sld.transform, false);
            fa.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.25f);
            fa.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.75f);
            fa.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
            fa.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fa.transform, false); fill.AddComponent<CanvasRenderer>();
            fill.AddComponent<Image>().color = new Color(0f, 0.45f, 0.9f, 1f);
            fill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fill.GetComponent<RectTransform>().anchorMax = Vector2.one;
            fill.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            fill.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            sl.fillRect = fill.GetComponent<RectTransform>();

            var ha = new GameObject("Handle Slide Area", typeof(RectTransform));
            ha.transform.SetParent(sld.transform, false);
            ha.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            ha.GetComponent<RectTransform>().anchorMax = Vector2.one;
            ha.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            ha.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(ha.transform, false); handle.AddComponent<CanvasRenderer>();
            handle.AddComponent<Image>().color = Color.white;
            var hrt = handle.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(0, 1);
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
            hrt.sizeDelta = new Vector2(30, 0);
            sl.handleRect = hrt;
            sl.targetGraphic = handle.GetComponent<Image>();

            sl.onValueChanged.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(sl.onValueChanged, mainMenuUI.OnMusicVolumeChanged);

            // Delete template
            DestroyImmediate(template);

            // Assign references
            var so = new SerializedObject(mainMenuUI);
            so.FindProperty("mainPanel").objectReferenceValue = mp;
            so.FindProperty("levelSelectPanel").objectReferenceValue = lp;
            so.FindProperty("settingsPanel").objectReferenceValue = sp;
            so.FindProperty("level2Button").objectReferenceValue = l2b.GetComponent<Button>();
            so.FindProperty("level2LockIcon").objectReferenceValue = lockIcon;
            so.FindProperty("musicSlider").objectReferenceValue = sl;
            so.ApplyModifiedProperties();

            Debug.Log("[SetupMainMenu] Scene restructured successfully!");
        }

        static GameObject CreatePanel(string name, Transform parent)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            g.AddComponent<CanvasRenderer>();
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            return g;
        }

        static void WireBtn(GameObject btnObj, UnityAction action)
        {
            var btn = btnObj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(btn.onClick, action);
        }
    }
}
