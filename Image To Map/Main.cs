using BepInEx;
using NativeFileDialogSharp;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using GuiInteract;

namespace Image_To_Map {
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin {
        private const string pluginGuid = "apexlite.worldbox.imagetomap";
        private const string pluginName = "Image To Map";
        private const string pluginVersion = "1.1.0";
        private bool initiated = false;
        private static List<string> palette = new List<string>();
        public static GameObject[] gameObjects => GameObject.FindObjectsOfType<GameObject>(true);

        void Update() {
            if (global::Config.gameLoaded && !initiated) {
                initiated = true;

                PreloadWindow("saves_list");
                PreloadWindow("settings");

                int tileCount = AssetManager.tiles.list.Count + AssetManager.topTiles.list.Count;
                Vector2[] positions = getPositions(tileCount);
                float pos = -positions[tileCount - 1].y;

                BoxWindow window = new BoxWindow("imagetomap", "Image To Map");
                ToggleButton dithering = new ToggleButton("Dithering", 
                    "Dithering can remove errors that appear when generating an image. If the image only contains the same colors as the tiles then turn this off to avoid a \"grainy\" output.",
                    window.content.transform, new Vector3(120, -pos - 15 - 5 + 442));
                BigButton generateButton = new BigButton("GENERATE", window.content.transform, new Vector3(130, -pos - 15 - 5 - 35), () => {
                    DialogResult dialog = Dialog.FileOpen("png,jpg,jpeg");

                    if (dialog.Path != null && palette.Count > 0) {
                        ImageToMap.Convert(dialog.Path, 31, palette.ToArray(), dithering.value);
                    }
                });

                new BoxButton("Image To Map", Resources.Load<Sprite>("ui/icons/iconMyWorlds"), GameObject.Find("Tab_Main").GetComponent<PowersTab>().transform, new Vector2(403.2f, -18), window.scrollWindow.clickShow);

                window.setScrollable(new Vector2(0, pos + 15 + 10 + 24 + 20));
                InitTiles(window, positions);
            }
        }

        private static void InitTiles(BoxWindow window, Vector2[] positions) {
            int index = 0;
            foreach (TileType tile in AssetManager.tiles.list) {
                string name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(tile.id.ToLower().Replace("_", " ")) + " Tile";
                new BoxButton(name, Resources.Load<Sprite>("tiles/" + tile.id + "/tile_0"), window.content.transform, positions[index], () => InverseTile(tile.id), false, true, true);
                index++;
            }

            foreach (TopTileType tile in AssetManager.topTiles.list) {
                string name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(tile.id.ToLower().Replace("_", " ")) + " Tile";
                string id = tile.id.Contains("high") ? "soil_high:" + tile.id : "soil_low:" + tile.id;
                new BoxButton(name, Resources.Load<Sprite>("tiles/" + tile.id + "/tile_0"), window.content.transform, positions[index], () => InverseTile(id), false, true, true);
                index++;
            }
        }

        private static void InverseTile(string id) {
            if (palette.Contains(id)) {
                palette.Remove(id);
                return;
            }

            palette.Add(id);
        }

        private static void PreloadWindow(string window) {
            ScrollWindow.showWindow(window);
            ScrollWindow.get(window).clickHide();
        }

        private static Vector2[] getPositions(int count) {
            Vector2[] positions = new Vector2[count];
            int rows = greatestDivisor(count, 4);
            int columns = count / rows;
            int padding = 5;

            for (int y = 0; y < rows; y++) {
                for (int x = 0; x < columns; x++) {
                    positions[x + y * columns] = new Vector2(45 + padding + (170 - padding * 2) / (columns - 1) * x, -(15 + padding + (padding + 30) * y));
                }
            }

            return positions;
        }

        private static int greatestDivisor(int number, int min = 2) {
            int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97 };

            for (int i = primes.Length - 1; i >= 0; i--) {
                if (number % primes[i] == 0 && number / primes[i] >= min) {
                    return primes[i];
                }
            }

            return -1;
        }
    }
}