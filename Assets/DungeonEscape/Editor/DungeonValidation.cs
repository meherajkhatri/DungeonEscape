using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace DungeonEscape.Editor
{
    // Runs in an isolated editor project: real player input, physics and trigger callbacks.
    public static class DungeonValidation
    {
        static int step, checks;
        static double due, started;
        static Keyboard keyboard;
        static Gamepad gamepad;
        static Vector2 before;
        static int runtimeErrors;
        static EscapeGame game;
        static bool oldBestPresent;
        static float oldBest;
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/DungeonEscape/Scenes/DungeonEscape.unity");
            CheckReachability();
            oldBestPresent=PlayerPrefs.HasKey("DungeonEscape.Best");oldBest=PlayerPrefs.GetFloat("DungeonEscape.Best");
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived += OnLog;
            EditorApplication.playModeStateChanged += OnPlay;
            started=EditorApplication.timeSinceStartup;
            EditorApplication.isPlaying=true;
        }
        static void OnLog(string text,string stack,LogType type) { if((type==LogType.Exception||type==LogType.Error) && !stack.Contains("UnityEditor.Search"))runtimeErrors++; }
        static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            runtimeErrors=0; keyboard=InputSystem.AddDevice<Keyboard>();gamepad=InputSystem.AddDevice<Gamepad>();
            due=EditorApplication.timeSinceStartup+1;EditorApplication.update+=Tick;
        }
        static void Check(bool condition,string message) { if(!condition)throw new Exception("VALIDATION FAILED: "+message);checks++;Debug.Log("PASS: "+message); }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-started>100){Finish(1,"Timed out");return;}
            if(EditorApplication.timeSinceStartup<due)return;
            due=EditorApplication.timeSinceStartup+.35;
            try
            {
                if(game==null)game=UnityEngine.Object.FindAnyObjectByType<EscapeGame>();
                var body=game.player.GetComponent<Rigidbody2D>();
                switch(step++)
                {
                    case 0: Check(game.Screen=="Main menu","Main menu initializes");Capture("01-main-menu");game.HowToPlay();break;
                    case 1: Check(game.Screen=="How to play","Instructions navigation");Capture("02-how-to-play");game.MainMenu();game.Credits();break;
                    case 2: Check(game.Screen=="Credits","Credits navigation");Capture("06-credits");game.MainMenu();Click("Play");break;
                    case 3: Check(game.Playing&&game.Health==4&&game.Runes==0,"Start button resets run");before=body.position;SendKeys(Key.D);due+=.2;break;
                    case 4: Check(body.position.x>before.x+.5f,"Keyboard physics movement");SendKeys();InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=Vector2.up});before=body.position;due+=.2;break;
                    case 5: Check(body.position.y>before.y+.5f,"Controller analog movement");InputSystem.QueueStateEvent(gamepad,new GamepadState());SendKeys(Key.Escape);break;
                    case 6: Check(game.Screen=="Paused"&&Time.timeScale==0,"Escape pauses");before=body.position;SendKeys();break;
                    case 7: Check(Vector2.Distance(before,body.position)<.01f,"Physics stays still while paused");SendKeys(Key.Escape);break;
                    case 8: Check(game.Playing&&Time.timeScale==1,"Escape resumes");SendKeys();game.Begin();break;
                    case 9: before=body.position;InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=Vector2.right}.WithButton(GamepadButton.South));due=EditorApplication.timeSinceStartup+.1;break;
                    case 10: Check(game.player.IsDashing&&body.position.x>before.x+.5f,"Controller dash");InputSystem.QueueStateEvent(gamepad,new GamepadState());due+=.4;break;
                    case 11: game.Begin();body.position=new Vector2(-12.8f,-6.5f);SendKeys(Key.A);due+=.6;break;
                    case 12: Check(body.position.x>-13,"Tilemap walls stop Rigidbody2D");SendKeys();game.Begin();body.position=game.objects.First(o=>o.kind==ObjectKind.Exit).transform.position;break;
                    case 13: Check(game.Playing&&game.Runes==0,"Gate stays locked without runes");game.Begin();Capture("03-gameplay");body.position=game.objects.First(o=>o.kind==ObjectKind.Rune).transform.position;break;
                    case 14: Check(game.Runes==1,"Rune collected by trigger");body.position=game.objects.First(o=>o.kind==ObjectKind.Rune&&o.gameObject.activeSelf).transform.position;break;
                    case 15: Check(game.Runes==2,"Second rune collected");body.position=game.objects.First(o=>o.kind==ObjectKind.Rune&&o.gameObject.activeSelf).transform.position;break;
                    case 16: Check(game.Runes==3,"All runes recovered");body.position=game.objects.First(o=>o.kind==ObjectKind.Exit).transform.position;break;
                    case 17: Check(game.Screen=="Victory","Exit trigger produces victory");game.Pause();Check(game.Screen=="Victory","HUD pause cannot reopen a finished run");Capture("04-victory");game.MainMenu();Click("Play");break;
                    case 18: Check(game.Runes==0&&game.objects.Count(o=>o.kind==ObjectKind.Rune&&o.gameObject.activeSelf)==3,"Restart restores all collectibles");body.position=game.objects.First(o=>o.kind==ObjectKind.Spikes).transform.position;due+=1.8;break;
                    case 19: Check(game.Health<4,"Spike trigger damages explorer");body.position=game.objects.First(o=>o.kind==ObjectKind.Heart).transform.position;break;
                    case 20: Check(game.Health>=3,"Flask restores health");game.Begin();game.Hurt();game.Hurt();Check(game.Health==3,"Damage immunity prevents duplicate hit");due+=1.4;break;
                    case 21: game.Hurt();due+=1.4;break;
                    case 22: game.Hurt();due+=1.4;break;
                    case 23: game.Hurt();Check(game.Screen=="Defeat","Zero life produces defeat");Capture("05-defeat");game.MainMenu();break;
                    case 24: Check(game.Screen=="Main menu"&&Time.timeScale==1,"Return from defeat restores time scale");Check(runtimeErrors==0,"No runtime errors");Finish(0,"DUNGEON_VALIDATION_OK: "+checks+" checks passed");break;
                }
            }
            catch(Exception e) { Finish(1,e.ToString()); }
        }
        static void Click(string prefix) { UnityEngine.Object.FindObjectsByType<Button>().First(b=>b.gameObject.activeInHierarchy&&b.name.StartsWith(prefix)).onClick.Invoke(); }
        static void SendKeys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
        static void Finish(int code,string result)
        {
            EditorApplication.update-=Tick;
            if(oldBestPresent)PlayerPrefs.SetFloat("DungeonEscape.Best",oldBest);else PlayerPrefs.DeleteKey("DungeonEscape.Best");PlayerPrefs.Save();
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/results.txt",result+"\n");Debug.Log(result);EditorApplication.Exit(code);
        }
        static void Capture(string name)
        {
            var camera=Camera.main;var root=game.canvas.rootCanvas;var texture=new RenderTexture(1440,900,24);texture.Create();
            root.renderMode=RenderMode.ScreenSpaceCamera;root.worldCamera=camera;root.planeDistance=1;camera.targetTexture=texture;Canvas.ForceUpdateCanvases();camera.Render();
            var old=RenderTexture.active;RenderTexture.active=texture;var png=new Texture2D(1440,900,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1440,900),0,0);png.Apply();Directory.CreateDirectory("Validation");File.WriteAllBytes("Validation/"+name+".png",png.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;root.renderMode=RenderMode.ScreenSpaceOverlay;UnityEngine.Object.DestroyImmediate(png);texture.Release();UnityEngine.Object.DestroyImmediate(texture);
        }
        static void CheckReachability()
        {
            var walls=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(t=>t.GetComponent<TilemapCollider2D>()!=null);
            var player=UnityEngine.Object.FindAnyObjectByType<Explorer>();var start=walls.WorldToCell(player.transform.position);var visited=new HashSet<Vector3Int>{start};var queue=new Queue<Vector3Int>();queue.Enqueue(start);
            var dirs=new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right};while(queue.Count>0){var cell=queue.Dequeue();foreach(var d in dirs){var p=cell+d;if(p.x < -14||p.x>14||p.y < -9||p.y>9||walls.HasTile(p)||!visited.Add(p))continue;queue.Enqueue(p);}}
            foreach(var item in UnityEngine.Object.FindObjectsByType<DungeonObject>().Where(o=>o.kind==ObjectKind.Rune||o.kind==ObjectKind.Exit))Check(visited.Contains(walls.WorldToCell(item.transform.position)),"Reachable: "+item.name);
            Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonEscape/Tiles/DungeonPalette.prefab")!=null,"Editable Tile Palette exists");
        }
    }
}
