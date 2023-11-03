using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace Image_To_Map {
    internal static class ImageToMap {
        private static Map<string, Rgba32> colorMap;

        public static Image<Rgba32> GetPreview(string path, string[] palette, bool dithering = true, bool weighted = false) {
            if (colorMap == null) {
                InstantiateColorMap();
            }

            Image<Rgba32> image = Image.Load<Rgba32>(path);
            int width = image.Width / 64 * 64, height = image.Height / 64 * 64;

            Rgba32[] colors = colorMap.Forward.ToArray();

            for (int i = 0; i < colors.Length; i++) {
                Debug.Log(colors[i] + " : " + colorMap.Reverse[colors[i]]);
            }

            image.Mutate(i => i.Resize(width, height, KnownResamplers.Lanczos3));

            if (dithering) {
                return FloydSteinbergAlgorithm(image, colorMap.Forward.ToArray().Where(c => palette.Contains(colorMap.Reverse[c])).ToArray(), weighted);
            } else {
                return QuantizeToPalette(image, colorMap.Forward.ToArray().Where(c => palette.Contains(colorMap.Reverse[c])).ToArray(), weighted);
            }
        }

        public static void Convert(string path, int saveSlot, string[] palette, bool dithering = true, bool weighted = false) {
            Image<Rgba32> image = GetPreview(path, palette, dithering, weighted);
            (List<string> tileMap, List<int>[] tileArray, List<int>[] tileAmount) = ReadImage(image);
            int width = image.Width / 64 * 64, height = image.Height / 64 * 64;

            SavedMap world = new SavedMap();
            world.saveVersion = 13;
            world.width = width / 64;
            world.height = height / 64;
            world.mapStats = new MapStats();
            world.mapStats.name = "BigBot's Inauspicious Kingdom";
            world.worldLaws = new WorldLaws();
            world.worldLaws.init();
            world.tileMap = tileMap;
            world.tileArray = ConvertTo2DArray(tileArray);
            world.tileAmounts = ConvertTo2DArray(tileAmount);

            SaveManager.setCurrentSlot(saveSlot);
            SaveManager.deleteCurrentSave();
            Directory.CreateDirectory(SaveManager.getSlotSavePath(saveSlot));
            world.toJson(SaveManager.getSlotSavePath(saveSlot) + "/map.wbax", false);
            image.SaveAsPng(SaveManager.generatePngPreviewPath(SaveManager.getSlotSavePath(saveSlot)));
            MapBox.instance.transitionScreen.startTransition(new LoadingScreen.TransitionAction(MapBox.instance.saveManager.loadWorld));
        }

        private static (List<string>, List<int>[], List<int>[]) ReadImage(Image<Rgba32> image) {
            List<string> tileMap = new List<string>();
            List<int>[] tileArray = new List<int>[image.Height];
            List<int>[] tileAmounts = new List<int>[image.Height];
            Dictionary<string, int> tiles = new Dictionary<string, int>();
            int tileCount = 0;

            for (int y = 0; y < image.Height; y++) {
                int previousNum = -1;
                int oppositeIndex = image.Height - 1 - y;
                int tilesAmount = 0;

                tileArray[oppositeIndex] = new List<int>();
                tileAmounts[oppositeIndex] = new List<int>();

                for (int x = 0; x < image.Width; x++) {
                    string tile = colorMap.Reverse[image[x, y]];

                    if (!tileMap.Contains(tile)) {
                        tileMap.Add(tile);
                        tiles[tile] = tileCount;
                        tileCount++;
                    }

                    if (x == 0 || previousNum == tiles[tile]) {
                        tilesAmount++;
                    } else {
                        tileArray[oppositeIndex].Add(previousNum);
                        tileAmounts[oppositeIndex].Add(tilesAmount);
                        tilesAmount = 1;
                    }

                    if (x == image.Width - 1) {
                        tileArray[oppositeIndex].Add(tiles[tile]);
                        tileAmounts[oppositeIndex].Add(tilesAmount);
                    }

                    previousNum = tiles[tile];
                }
            }

            return (tileMap, tileArray, tileAmounts);
        }

        private static Image<Rgba32> QuantizeToPalette(Image<Rgba32> image, Rgba32[] palette, bool weighted = false) {
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    image[x, y] = GetClosestColor(image[x, y], palette, weighted);
                }
            }

            return image;
        }

        private static Image<Rgba32> FloydSteinbergAlgorithm(Image<Rgba32> image, Rgba32[] palette, bool weighted = false) {
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    Rgba32 oldColor = image[x, y];
                    Rgba32 newColor = GetClosestColor(oldColor, palette, weighted);

                    int[] error = { 
                        oldColor.R - newColor.R,
                        oldColor.G - newColor.G,
                        oldColor.B - newColor.B
                    };

                    image[x, y] = newColor;

                    if (x != image.Width - 1) {
                        image[x + 1, y] = GetDiffusedColor(image[x + 1, y], error, 7 / 16.0);
                        if (y != image.Height - 1) {
                            image[x + 1, y + 1] = GetDiffusedColor(image[x + 1, y + 1], error, 1 / 16.0);
                        }
                    }

                    if (y != image.Height - 1) {
                        image[x, y + 1] = GetDiffusedColor(image[x, y + 1], error, 5 / 16.0);
                        if (x != 0) {
                            image[x - 1, y + 1] = GetDiffusedColor(image[x - 1, y + 1], error, 3 / 16.0);
                        }
                    }
                }
            }

            return image;
        }

        private static Rgba32 GetClosestColor(Rgba32 color, Rgba32[] palette, bool weighted = false) {
            Rgba32 closestColor = palette[0];
            double closestDistance = double.PositiveInfinity;

            foreach (Rgba32 testColor in palette) {
                double redWeight = weighted ? 0.3 : 1, greenWeight = weighted ? 0.59 : 1, blueWeight = weighted ? 0.11 : 1;
                double distance = Math.Pow((testColor.R - color.R) * redWeight, 2) + Math.Pow((testColor.G - color.G) * greenWeight, 2) + Math.Pow((testColor.B - color.B) * blueWeight, 2);

                if (distance < closestDistance) {
                    closestColor = testColor;
                    closestDistance = distance;
                }
            }

            return closestColor;
        }

        private static void InstantiateColorMap() {
            colorMap = new Map<string, Rgba32>();

            foreach (TileType tile in AssetManager.tiles.list) {
                Rgba32 color = Color32ToRgba32(tile.color);

                if (!colorMap.Forward.Contains(tile.id) && !colorMap.Reverse.Contains(color)) {
                    colorMap.Add(tile.id, color);
                }
            }

            foreach (TopTileType tile in AssetManager.topTiles.list) {
                string id = tile.id.Contains("high") ? "soil_high:" + tile.id : "soil_low:" + tile.id;
                Rgba32 color = Color32ToRgba32(tile.color);

                if (!colorMap.Forward.Contains(id) && !colorMap.Reverse.Contains(color)) {
                    colorMap.Add(id, color);
                }
            }
        }

        private static Rgba32 GetDiffusedColor(Rgba32 color, int[] error, double errorBias) {
            return new Rgba32(
                ClampToByte(color.R + (error[0] * errorBias)),
                ClampToByte(color.G + (error[1] * errorBias)),
                ClampToByte(color.B + (error[2] * errorBias)));
        }

        // https://stackoverflow.com/a/3040551
        private static byte ClampToByte(double value) {
            int min = 0;
            int max = 255;

            return (byte) ((value < min) ? min : (value > max) ? max : value);
        }

        private static Rgba32 Color32ToRgba32(Color32 color) {
            return new Rgba32(color.r, color.g, color.b, color.a);
        }

        private static int[][] ConvertTo2DArray(List<int>[] array) {
            int[][] newArray = new int[array.Length][];

            for (int i = 0; i < array.Length; i++) {
                newArray[i] = array[i].ToArray();
            }

            return newArray;
        }
    }
}
