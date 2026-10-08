using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEditor.Tilemaps;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonEscape.Editor
{
    public static class DungeonBuilder
    {
        const string Root = "Assets/DungeonEscape";
        static Sprite[] sprites;
        static Material material;
        static Sprite[] characterFrames;
        static readonly string[] names = { "Floor", "FloorCracked", "Wall", "WallMoss", "ExplorerIdle", "ExplorerStepA", "ExplorerStepB", "ExplorerDash", "Rune", "SentryA", "SentryB", "SpikesUp", "SpikesDown", "Gate", "Flask", "Torch", "Arch", "Spark" };
        static Color32 C(string hex) { ColorUtility.TryParseHtmlString("#"+hex, out Color c); return c; }
        [MenuItem("Dungeon Escape/Build Game Assets and Scene")]
        public static void Build()
        {
            foreach (string folder in new[]{"Art","Tiles","Animation","Input","Scenes"}) Directory.CreateDirectory(Root+"/"+folder);
            MakeSheet();
            ImportUserArt();
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            ReplaceAsset(material,Root+"/Art/DungeonUnlit.mat");
            var actions = MakeInput();
            var animator = MakeAnimator();
            var tiles = new Tile[4];
            for (int i=0;i<4;i++) { tiles[i]=ScriptableObject.CreateInstance<Tile>(); tiles[i].sprite=sprites[i]; tiles[i].colliderType=i<2?Tile.ColliderType.None:Tile.ColliderType.Grid; ReplaceAsset(tiles[i],Root+"/Tiles/"+names[i]+".asset"); }
            MakePalette(tiles);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            for(int i=0;i<4;i++) tiles[i]=AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/"+names[i]+".asset");
            var camera = new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag="MainCamera"; camera.orthographic=true; camera.orthographicSize=11.7f; camera.transform.position=new Vector3(0,0,-10); camera.backgroundColor=C("080F1B"); camera.clearFlags=CameraClearFlags.SolidColor;
            var grid = new GameObject("Dungeon Grid",typeof(Grid));
            var floor = Map("Floor • walkable",grid.transform,-10); floor.color=new Color(.64f,.64f,.72f);
            var walls = Map("Walls • solid",grid.transform,5); walls.gameObject.AddComponent<TilemapCollider2D>();
            var foreground = Map("Foreground arches • walk behind",grid.transform,30);
            var arch = ScriptableObject.CreateInstance<Tile>(); arch.sprite=sprites[16]; arch.colliderType=Tile.ColliderType.None; ReplaceAsset(arch,Root+"/Tiles/ForegroundArch.asset");
            // The same Tile assets live in the editable palette and paint the dungeon.
            for (int y=-9;y<=9;y++) for(int x=-14;x<=14;x++)
            {
                floor.SetTile(new Vector3Int(x,y,0),tiles[(Math.Abs(x*31+y*17)%11)==0?1:0]);
                if(x==-14||x==14||y==-9||y==9) walls.SetTile(new Vector3Int(x,y,0),tiles[(x+y)%5==0?3:2]);
            }
            // Three interconnected chambers. Wide breaks remain navigable on both axes.
            for(int y=-8;y<=8;y++)
            {
                if(y<-5||y>-3 && y<3||y>5) walls.SetTile(new Vector3Int(-5,y,0),tiles[y%3==0?3:2]);
                if(y<-5||y>-3 && y<3||y>5) walls.SetTile(new Vector3Int(5,y,0),tiles[y%3==0?3:2]);
            }
            for(int x=-12;x<=-7;x++) if(x!=-10&&x!=-9) walls.SetTile(new Vector3Int(x,0,0),tiles[2]);
            for(int x=7;x<=12;x++) if(x!=9&&x!=10) walls.SetTile(new Vector3Int(x,0,0),tiles[2]);
            for(int x=-2;x<=2;x++) if(x!=0) walls.SetTile(new Vector3Int(x,1,0),tiles[2]);
            foreach(int x in new[]{-5,5}) foreach(int y in new[]{-4,4}) foreground.SetTile(new Vector3Int(x,y,0),arch);
            var playerGO=new GameObject("Explorer • Rigidbody movement",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(CapsuleCollider2D),typeof(Animator),typeof(Explorer));
            playerGO.transform.position=new Vector3(-11.5f,-6.5f,0);
            var player=playerGO.GetComponent<Explorer>(); player.controls=actions; player.visual=playerGO.GetComponent<SpriteRenderer>(); player.visual.sprite=sprites[4]; player.visual.sortingOrder=10; player.visual.sharedMaterial=material;
            var body=playerGO.GetComponent<Rigidbody2D>(); body.gravityScale=0; body.freezeRotation=true; body.interpolation=RigidbodyInterpolation2D.Interpolate; body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var capsule=playerGO.GetComponent<CapsuleCollider2D>(); capsule.size=new Vector2(.47f,.58f); capsule.offset=new Vector2(0,-.13f);
            playerGO.GetComponent<Animator>().runtimeAnimatorController=animator;
            var objects=new List<DungeonObject>();
            objects.Add(Prop("Rune I • west reliquary",ObjectKind.Rune,8,-10.5f,6.5f));
            objects.Add(Prop("Rune II • lower vault",ObjectKind.Rune,8,.5f,-6.5f));
            objects.Add(Prop("Rune III • east reliquary",ObjectKind.Rune,8,10.5f,6.5f));
            objects.Add(Prop("Eastern gate • requires three runes",ObjectKind.Exit,13,12.5f,-6.5f));
            objects.Add(Prop("Restoration flask",ObjectKind.Heart,14,1.5f,6.5f));
            objects.Add(Prop("Restoration flask",ObjectKind.Heart,14,8.5f,-6.5f));
            AddSentry(objects,-12,3,-7,3,0); AddSentry(objects,-2,-5,3,-5,1.5f); AddSentry(objects,7,5,12,5,.5f);
            foreach(Vector2 p in new[]{new Vector2(-8.5f,4.5f),new Vector2(-7.5f,4.5f),new Vector2(-1.5f,-3.5f),new Vector2(-.5f,-3.5f),new Vector2(.5f,-3.5f),new Vector2(7.5f,3.5f),new Vector2(8.5f,3.5f)})
            { var spike=Prop("Timed spike trap",ObjectKind.Spikes,11,p.x,p.y); spike.alternate=sprites[12]; spike.phase=Mathf.Abs(p.x)*.13f; objects.Add(spike); }
            foreach(Vector2 p in new[]{new Vector2(-12.5f,8.2f),new Vector2(-6.5f,8.2f),new Vector2(-3.5f,8.2f),new Vector2(3.5f,8.2f),new Vector2(6.5f,8.2f),new Vector2(12.5f,8.2f)}) objects.Add(Prop("Wall flame",ObjectKind.Torch,15,p.x,p.y));
            var eventSystem=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var canvas=new GameObject("Canvas • menus and HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1440,900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            // Keep the reference layout centered at wider aspect ratios.
            var content=new GameObject("Interface",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>(); content.transform.SetParent(canvas.transform,false);
            var contentRect=content.GetComponent<RectTransform>(); contentRect.anchorMin=contentRect.anchorMax=new Vector2(.5f,.5f); contentRect.sizeDelta=new Vector2(1440,900);
            content.gameObject.AddComponent<GraphicRaycaster>();
            var game=new GameObject("Dungeon Escape • game state",typeof(EscapeGame)).GetComponent<EscapeGame>(); game.player=player; game.objects=objects.ToArray(); game.canvas=content; game.spark=sprites[17]; game.spriteMaterial=material;
            camera.gameObject.AddComponent<FitDungeonCamera>();
            if(floor.GetUsedTilesCount()<2 || walls.GetUsedTilesCount()<1) throw new Exception("Dungeon tilemaps are empty");
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/DungeonEscape.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/DungeonEscape.unity",true)};
            PlayerSettings.runInBackground=true; PlayerSettings.productName="Dungeon Escape"; PlayerSettings.defaultScreenWidth=1440; PlayerSettings.defaultScreenHeight=900; PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
            Debug.Log("DUNGEON_BUILD_OK: Scene, sprite sheet, tile palette, animations, input and menus generated.");
        }
        static Tilemap Map(string name,Transform parent,int order)
        { var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer)); go.transform.SetParent(parent); var renderer=go.GetComponent<TilemapRenderer>(); renderer.sortingOrder=order; renderer.sharedMaterial=material; return go.GetComponent<Tilemap>(); }
        static DungeonObject Prop(string name,ObjectKind kind,int sprite,float x,float y)
        {
            var go=new GameObject(name,typeof(SpriteRenderer),typeof(DungeonObject)); go.transform.position=new Vector3(x,y,0);
            var sr=go.GetComponent<SpriteRenderer>(); sr.sprite=sprites[sprite]; sr.sortingOrder=kind==ObjectKind.Spikes?-2:8; sr.sharedMaterial=material;
            var item=go.GetComponent<DungeonObject>(); item.kind=kind;
            if(kind!=ObjectKind.Torch) { var collider=go.AddComponent<CircleCollider2D>(); collider.isTrigger=true; collider.radius=kind==ObjectKind.Exit?.53f:.32f; }
            return item;
        }
        static void AddSentry(List<DungeonObject> objects,float x,float y,float endX,float endY,float phase)
        { var item=Prop("Patrolling ember sentry",ObjectKind.Sentry,9,x,y); item.patrolEnd=new Vector2(endX,endY); item.phase=phase; item.alternate=sprites[10]; var body=item.gameObject.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic; body.interpolation=RigidbodyInterpolation2D.Interpolate; objects.Add(item); }
        static void ReplaceAsset(Object asset,string path) { if(AssetDatabase.LoadMainAssetAtPath(path)!=null) AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(asset,path); }
        static InputActionAsset MakeInput()
        {
            var asset=ScriptableObject.CreateInstance<InputActionAsset>(); var map=asset.AddActionMap("Explorer");
            var move=map.AddAction("Move",InputActionType.Value,expectedControlLayout:"Vector2");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick"); move.AddBinding("<Gamepad>/dpad");
            var dash=map.AddAction("Dash",InputActionType.Button); dash.AddBinding("<Keyboard>/space"); dash.AddBinding("<Keyboard>/leftShift"); dash.AddBinding("<Gamepad>/buttonSouth"); dash.AddBinding("<Gamepad>/rightShoulder");
            string path=Root+"/Input/DungeonControls.inputactions"; File.WriteAllText(path,asset.ToJson()); Object.DestroyImmediate(asset); AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        static AnimatorController MakeAnimator()
        {
            string path=Root+"/Animation/Explorer.controller"; AssetDatabase.DeleteAsset(path); var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Facing",AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float); controller.AddParameter("Dashing",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine; var idle=machine.AddState("Idle"); idle.motion=DirectionalClip("Idle",false,2); var walk=machine.AddState("Walk"); walk.motion=DirectionalClip("Walk",true,9); var dash=machine.AddState("Dash"); dash.motion=DirectionalClip("Dash",true,18); machine.defaultState=idle;
            var t=idle.AddTransition(walk); t.hasExitTime=false;t.duration=0;t.AddCondition(AnimatorConditionMode.Greater,.1f,"Speed");
            t=walk.AddTransition(idle);t.hasExitTime=false;t.duration=0;t.AddCondition(AnimatorConditionMode.Less,.1f,"Speed");
            t=machine.AddAnyStateTransition(dash);t.hasExitTime=false;t.duration=0;t.canTransitionToSelf=false;t.AddCondition(AnimatorConditionMode.If,0,"Dashing");
            t=dash.AddTransition(idle);t.hasExitTime=false;t.duration=0;t.AddCondition(AnimatorConditionMode.IfNot,0,"Dashing"); return controller;
        }
        static AnimationClip Clip(string name,int[] frames,float rate)
        {
            var clip=new AnimationClip { name=name,frameRate=rate }; var keys=new ObjectReferenceKeyframe[frames.Length+1];
            for(int i=0;i<keys.Length;i++) keys[i]=new ObjectReferenceKeyframe { time=i/rate,value=sprites[frames[i%frames.Length]] };
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding { path="",type=typeof(SpriteRenderer),propertyName="m_Sprite" },keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);ReplaceAsset(clip,Root+"/Animation/"+name+".anim");return clip;
        }
        static void MakePalette(Tile[] tiles)
        {
            string path=Root+"/Tiles/DungeonPalette.prefab"; AssetDatabase.DeleteAsset(path);
            var prefab=GridPaletteUtility.CreateNewPalette(Root+"/Tiles","DungeonPalette",GridLayout.CellLayout.Rectangle,GridPalette.CellSizing.Manual,Vector3.one,GridLayout.CellSwizzle.XYZ);
            var instance=PrefabUtility.LoadPrefabContents(path); var map=instance.GetComponentInChildren<Tilemap>(); for(int i=0;i<tiles.Length;i++) map.SetTile(new Vector3Int(i,0,0),tiles[i]); PrefabUtility.SaveAsPrefabAsset(instance,path);PrefabUtility.UnloadPrefabContents(instance);
        }
        static void MakeSheet()
        {
            const int size=32; var tex=new Texture2D(size*names.Length,size,TextureFormat.RGBA32,false); tex.SetPixels32(new Color32[tex.width*tex.height]);
            for(int tile=0;tile<names.Length;tile++) Draw(tex,tile);
            tex.Apply();string path=Root+"/Art/DungeonAtlas.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var rects=new SpriteRect[names.Length];for(int i=0;i<names.Length;i++) rects[i]=new SpriteRect { name=names[i],rect=new Rect(i*32,0,32,32),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate() };
            provider.SetSpriteRects(rects); var ids=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();ids.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();importer.SaveAndReimport();
            var loaded=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();sprites=names.Select(n=>loaded.Single(s=>s.name==n)).ToArray();
        }
        static Sprite[] SliceSheet(string path, Rect[] regions, float pixelsPerUnit, string prefix)
        {
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=pixelsPerUnit; importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true; importer.SaveAndReimport();
            var factory=new SpriteDataProviderFactories(); factory.Init();
            var provider=factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var rects=regions.Select((r,i)=>new SpriteRect { name=prefix+i, rect=r, pivot=new Vector2(.5f,.5f), alignment=SpriteAlignment.Center, spriteID=GUID.Generate() }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var loaded=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return rects.Select(r=>loaded.Single(x=>x.name==r.name)).ToArray();
        }
        static void ImportUserArt()
        {
            // Source sheets are preserved unchanged; rectangles use Unity's bottom-left origin.
            var frames=new List<Rect>();
            for(int row=0;row<4;row++) for(int col=0;col<4;col++) frames.Add(new Rect(col*16,96-(row+1)*24,16,24));
            characterFrames=SliceSheet(Root+"/Art/Imported/guy.png",frames.ToArray(),24,"Character");
            sprites[4]=characterFrames[0]; sprites[5]=characterFrames[1]; sprites[6]=characterFrames[3]; sprites[7]=characterFrames[2];
            var tiles=SliceSheet(Root+"/Art/Imported/sheet.png",new[]{
                new Rect(112,208-16,16,16), new Rect(144,208-16,16,16),
                new Rect(256,208-96-32,32,32), new Rect(256,208-48-32,32,32),
                new Rect(32,208-80-32,32,32)},16,"Dungeon");
            sprites[0]=tiles[0]; sprites[1]=tiles[1];
            // A separate import scale keeps wall art AND grid colliders exactly one cell wide.
            string wallPath=Root+"/Art/Imported/sheet-walls.png";
            File.Copy(Root+"/Art/Imported/sheet.png",wallPath,true);
            var walls=SliceSheet(wallPath,new[]{new Rect(256,208-96-32,32,32),new Rect(256,208-48-32,32,32)},32,"Wall");
            sprites[2]=walls[0]; sprites[3]=walls[1]; sprites[16]=tiles[4];
        }
        static Motion DirectionalClip(string name, bool walking, float rate)
        {
            var tree=new BlendTree { name=name+" directions", blendType=BlendTreeType.Simple1D, blendParameter="Facing", useAutomaticThresholds=false };
            AssetDatabase.AddObjectToAsset(tree,Root+"/Animation/Explorer.controller");
            for(int direction=0;direction<4;direction++)
            {
                var clip=new AnimationClip { name=name+direction, frameRate=rate };
                int count=walking?4:1; var keys=new ObjectReferenceKeyframe[count+1];
                for(int i=0;i<=count;i++) keys[i]=new ObjectReferenceKeyframe { time=i/rate, value=characterFrames[direction*4+i%count] };
                AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding { path="",type=typeof(SpriteRenderer),propertyName="m_Sprite" },keys);
                var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; AnimationUtility.SetAnimationClipSettings(clip,settings);
                ReplaceAsset(clip,Root+"/Animation/"+name+direction+".anim"); tree.AddChild(clip,direction);
            }
            return tree;
        }
        static void Draw(Texture2D tex,int tile)
        {
            void Box(int x,int y,int w,int h,string hex) { for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)if(xx>=0&&xx<32&&yy>=0&&yy<32)tex.SetPixel(tile*32+xx,yy,C(hex)); }
            if(tile<2)
            {
                Box(0,0,32,32,"182B37");Box(1,1,30,30,"233C48");Box(2,3,28,27,tile==0?"263F4A":"28414B");Box(2,30,28,1,"36505A");Box(2,1,28,2,"1D333F");Box(7,7,2,1,"304C55");Box(24,21,2,1,"304C55");
                if(tile==1){Box(19,15,1,12,"142C37");Box(16,14,4,1,"142C37");Box(15,9,1,6,"142C37");}return;
            }
            if(tile<4)
            {
                Box(0,0,32,32,"101F2C");Box(1,6,30,24,"476575");Box(1,29,30,3,"77929A");Box(2,19,28,8,"3D596A");Box(2,8,28,8,"344D60");Box(0,17,32,2,"233747");Box(10,19,2,9,"233747");Box(22,7,2,10,"233747");Box(2,5,28,2,"203342");Box(2,0,28,4,"122330");
                if(tile==3){Box(3,24,8,5,"416958");Box(4,18,3,7,"416958");Box(23,7,5,3,"416958");}return;
            }
            if(tile>=4&&tile<=7)
            {
                int bob=tile==5?1:0;Box(7,2,18,4,"132631");Box(10,4,5,5+(tile==6?2:0),"172232");Box(18,4,5,5+(tile==5?2:0),"172232");Box(8,9,17,13,"173847");Box(9,10,15,12,"419999");Box(11,12,10,9,"66C9BD");Box(9,12,3,4,"EBC18D");Box(23,12,3,4,"EBC18D");Box(10,19+bob,14,10,"142B36");Box(12,20+bob,12,8,"EBC18D");Box(10,25+bob,15,5,"E2E5D0");Box(9,24+bob,17,2,"97AFA9");Box(19,21+bob,2,3,"142B36");Box(12,9,12,2,"AD794D");Box(16,9,3,2,"F5D486");if(tile==7){Box(1,13,7,2,"75DDD4");Box(3,19,5,2,"75DDD4");}return;
            }
            if(tile==8)
            {
                Box(8,3,16,3,"182D36");for(int y=6;y<28;y++){int w=10-Math.Abs(17-y);if(w>0){Box(16-w,y,w*2,1,"946832");Box(17-w,y,Math.Max(1,w*2-3),1,"EEC773");}}Box(14,13,4,9,"FFF1B4");Box(11,17,10,2,"FFF1B4");return;
            }
            if(tile==9||tile==10)
            {
                Box(5,3,23,3,"142832");Box(6,8,20,16,"702F49");Box(8,10,16,14,"CC5361");Box(10,22,12,5,"F17D77");Box(8,tile==9?6:7,5,5,"953D53");Box(20,tile==9?7:6,5,5,"953D53");Box(10,16,4,4,"FFDC9D");Box(19,16,4,4,"FFDC9D");Box(11,17,2,2,"512C45");Box(20,17,2,2,"512C45");Box(14,11,5,2,"642E43");return;
            }
            if(tile==11||tile==12)
            {
                Box(2,3,28,24,"172B37");Box(3,4,26,22,"34505B");foreach(int x in new[]{6,14,22}){Box(x,6,5,4,"101F2C");Box(x,17,5,4,"101F2C");if(tile==11){for(int y=0;y<10;y++){int w=Math.Max(1,5-y/2);Box(x+(5-w)/2,8+y,w,1,"C3CBD0");Box(x+(5-w)/2,19+y,w,1,"C3CBD0");}}else{Box(x+1,7,3,2,"648B94");Box(x+1,18,3,2,"648B94");}}return;
            }
            if(tile==13)
            {
                Box(1,1,30,30,"67858C");Box(4,2,24,26,"142535");Box(6,3,20,22,"254B55");Box(6,24,20,3,"A9BDA8");foreach(int x in new[]{9,15,21})Box(x,4,2,19,"7BAC9B");Box(2,1,28,3,"B3B997");Box(13,25,6,5,"F1CF7F");return;
            }
            if(tile==14){Box(12,23,8,4,"D8D5B1");Box(13,19,6,4,"AFC5BE");Box(9,7,14,13,"AD4960");Box(11,9,10,10,"EC7B83");Box(14,10,3,8,"F9D2B7");Box(12,13,7,3,"F9D2B7");return;}
            if(tile==15){Box(12,3,8,17,"182B37");Box(14,5,4,14,"9A6C49");Box(10,15,12,4,"637681");Box(11,20,10,7,"D47843");Box(13,22,6,8,"F6CA77");Box(15,20,3,6,"FFF1B4");return;}
            if(tile==16){Box(0,21,32,11,"1B2D40");Box(0,27,32,5,"6C8893");Box(1,23,30,3,"466676");Box(0,0,5,25,"4B6676");Box(27,0,5,25,"4B6676");Box(5,18,4,5,"283E50");Box(23,18,4,5,"283E50");Box(13,23,6,7,"9BA699");return;}
            Box(0,0,32,32,"FFFFFF");
        }
        [MenuItem("Dungeon Escape/Build macOS Player")]
        public static void BuildMac()
        {
            Directory.CreateDirectory("Builds");var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{Root+"/Scenes/DungeonEscape.unity"},locationPathName="Builds/DungeonEscape.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None });
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Player build failed: "+result.summary.result);Debug.Log("DUNGEON_PLAYER_BUILD_OK");
        }
    }
}
