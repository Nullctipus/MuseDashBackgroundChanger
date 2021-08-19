using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ModHelper;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace BackgroundChanger
{
    public class Mod : IMod
    {
        public string Name => "Background Changer";

        public string Description => "";

        public string Author => "BustR75";

        public string HomePage => "";
        static bool extract;
        public void DoPatching()
        {
            extract = Environment.GetCommandLineArgs().Contains("--Extract");
            Directory.CreateDirectory("Backgrounds");
            new Harmony(Guid.NewGuid().ToString()).Patch(typeof(GameSceneMainController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance),
                null,new HarmonyMethod(typeof(Mod).GetMethod(nameof(SceneObjectControllerPatch), BindingFlags.NonPublic | BindingFlags.Static)));
        }
        static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static void ChangeImage(SpriteRenderer r)
        {
            try
            {
                string path = "";
                Transform p = r.transform;
                while (p.parent != null && p.parent.name != "SceneObjectController")
                {
                    path = p.parent.name + "\\" + path;
                    p = p.parent;
                }
                Directory.CreateDirectory(Path.Combine("Backgrounds", r.sprite.texture.name.Replace(r.sprite.texture.name.Split('/').Last(), "")));
                if (File.Exists(Path.Combine("Backgrounds", r.sprite.texture.name + ".png")))
                {
                    if (!sprites.ContainsKey(path))
                    {
                        Texture2D tex = new Texture2D(1, 1,TextureFormat.ARGB32,false,true);
                        tex.LoadImage(File.ReadAllBytes(Path.Combine("Backgrounds", r.sprite.texture.name + ".png")));
                        tex.filterMode = r.sprite.texture.filterMode;
                        tex.mipMapBias = r.sprite.texture.mipMapBias;
                        tex.anisoLevel = r.sprite.texture.anisoLevel;
                        tex.name = r.sprite.texture.name;
                        
                        ModLogger.Debug(r.sprite.textureRect);
                        sprites.Add(path, Sprite.Create(tex, r.sprite.textureRect, r.sprite.pivot, r.sprite.pixelsPerUnit));
                    }
                    r.sprite = sprites[path];
                }
                else if (extract)
                {
                    try
                    {
                        File.WriteAllBytes(Path.Combine("Backgrounds", r.sprite.texture.name + ".png"), MakeReadable(r.sprite.texture).EncodeToPNG());
                    }
                    catch (Exception e)
                    {
                        ModLogger.Debug("Failed To Write" + Path.Combine("Backgrounds", r.sprite.texture.name + ".png") + "Because " + e);
                    }
                }
            }
            catch(Exception e)
            {
                ModLogger.Debug(r.name+" Caused Error: "+e);
            }
        }
        static Texture2D MakeReadable(Texture2D img)
        {
            img.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(img.width, img.height);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(img, rt);
            Texture2D img2 = new Texture2D(img.width, img.height);
            img2.ReadPixels(new Rect(0, 0, img.width, img.height), 0, 0);
            img2.Apply();
            RenderTexture.active = null;
            return img2;
        }
        static void SceneObjectControllerPatch()
        {
            foreach(SpriteRenderer r in GameLogic.GameGlobal.gGameMusicScene.scene.GetComponentsInChildren<SpriteRenderer>())
            {
                ModLogger.Debug(r.name);
                ChangeImage(r);
            }

        }
    }
}
