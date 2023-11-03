using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GuiInteract {
    internal static class Globals {
        internal static GameObject[] GAME_OBJECTS => GameObject.FindObjectsOfType<GameObject>(true);
    }

    internal class BoxWindow {
        internal ScrollWindow scrollWindow;
        internal GameObject scrollView;
        internal GameObject content;

        internal BoxWindow(string id, string title) {
            scrollWindow = GameObject.Instantiate<ScrollWindow>((ScrollWindow)Resources.Load("windows/empty", typeof(ScrollWindow)), CanvasMain.instance.transformWindows);

            GameObject.Destroy(scrollWindow.titleText.GetComponent<LocalizedText>());
            scrollWindow.screen_id = id;
            scrollWindow.name = id;
            scrollWindow.titleText.text = title;
            scrollWindow.create(true);

            scrollView = GameObject.Find("/Canvas Container Main/Canvas - Windows/windows/" + id + "/Background/Scroll View");
            content = GameObject.Find("/Canvas Container Main/Canvas - Windows/windows/" + id + "/Background/Scroll View/Viewport/Content");
        }

        internal void setScrollable(Vector2 sizeDelta) {
            RectTransform rect = content.GetComponent<RectTransform>();

            scrollView.SetActive(true);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = sizeDelta;
        }
    }

    internal class BoxButton {
        internal BoxButton(string name, Sprite icon, Transform parent, Vector3 position, UnityAction call, bool drag = true, bool toggle = false, bool inverse = false) {
            GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>(true);
            GameObject oldButton = toggle ? gameObjects.FirstOrDefault(x => x.name == "wars_marks") : GameObject.Find("WorldLaws");
            oldButton.SetActive(false);
            GameObject newButton = GameObject.Instantiate<GameObject>(oldButton);
            oldButton.SetActive(true);
            newButton.SetActive(true);
            Image image = newButton.transform.Find("Icon").GetComponent<Image>();
            Button button = newButton.GetComponent<Button>();
            PowerButton powerButton = newButton.GetComponent<PowerButton>();

            newButton.name = name;
            newButton.transform.SetParent(parent);
            newButton.transform.localPosition = position;
            newButton.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            image.sprite = icon;
            button.onClick.RemoveAllListeners();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(call);
            button.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            powerButton.open_window_id = string.Empty;
            powerButton.type = PowerButtonType.Library;
            powerButton.drag_power_bar = drag;

            if (toggle) {
                ToggleIcon toggleIcon = powerButton.transform.Find("ToggleIcon").GetComponent<ToggleIcon>();
                button.onClick.AddListener(() => InverseToggle(toggleIcon));
                if (inverse) {
                    InverseToggle(toggleIcon);
                }
            }

            LocalizedTextManager.instance.localizedText.Add(name, name);
        }

        private void InverseToggle(ToggleIcon toggleIcon) {
            if (toggleIcon == null) {
                return;
            }

            if (toggleIcon.image != null) {
                toggleIcon.image = toggleIcon.GetComponent<Image>();
                toggleIcon.image.sprite = toggleIcon.image.sprite == toggleIcon.spriteOFF ? toggleIcon.spriteON : toggleIcon.spriteOFF;
            }
        }
    }

    internal class ToggleButton {
        private Sprite ON = SpriteTextureLoader.getSprite("ui/icons/IconOn");
        private Sprite OFF = SpriteTextureLoader.getSprite("ui/icons/IconOff");
        internal bool value = true;

        internal ToggleButton(string name, string description, Transform parent, Vector3 position) {
            GameObject fullScreen = Globals.GAME_OBJECTS.FirstOrDefault(x => x.name == "fullscreen");
            GameObject ditherOption = GameObject.Instantiate<GameObject>(fullScreen, parent);
            Transform container = ditherOption.transform.Find("OptionArea");
            Transform title = container.Find("Title Option");
            Transform toggle = container.Find("Switch");
            TipButton tipButton = toggle.GetComponent<TipButton>();
            Button button = toggle.GetComponent<Button>();
            CanvasGroup canvasGroup = toggle.GetComponent<CanvasGroup>();
            LocalizedText text = toggle.Find("Text").GetComponent<LocalizedText>();
            Image image = toggle.Find("Icon").GetComponent<Image>();

            title.GetComponent<Text>().text = name;
            tipButton.textOnClick = name;
            tipButton.textOnClickDescription = "thingy_description_" + name;
            ditherOption.name = name;
            ditherOption.transform.localPosition = position;
            button.onClick.RemoveAllListeners();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => {
                value = !value;

                if (value) {
                    canvasGroup.alpha = 1f;
                    text.setKeyAndUpdate("short_on");
                    image.sprite = ON;
                } else {
                    canvasGroup.alpha = 0.8f;
                    text.setKeyAndUpdate("short_off");
                    image.sprite = OFF;
                }
            });

            LocalizedTextManager.instance.localizedText.Add(name, name);
            LocalizedTextManager.instance.localizedText.Add("thingy_description_" + name, description);
            GameObject.Destroy(ditherOption.GetComponent<OptionButton>());
            GameObject.Destroy(title.GetComponent<LocalizedText>());
            GameObject.Destroy(container.Find("Icon").gameObject);
        }
    }

    internal class BigButton {
        internal BigButton(string name, Transform parent, Vector3 position, UnityAction call) {
            GameObject autoSaves = Globals.GAME_OBJECTS.FirstOrDefault(x => x.name == "auto_save Bg");
            GameObject bigButton = GameObject.Instantiate<GameObject>(autoSaves, parent);
            GameObject container = bigButton.transform.Find("AutoSaves").gameObject;
            GameObject text = container.transform.Find("Text").gameObject;
            Button button = container.GetComponent<Button>();
            Text textComponent = text.GetComponent<Text>();
            bigButton.name = name;
            bigButton.transform.localPosition = position;
            button.onClick.RemoveAllListeners();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(call);
            textComponent.text = name;
            GameObject.Destroy(text.GetComponent<LocalizedText>());
            GameObject.Destroy(container.GetComponent<TipButton>());
        }
    }
}
