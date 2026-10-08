using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DungeonEscape
{
    public class EscapeGame : MonoBehaviour
    {
        public static EscapeGame Instance { get; private set; }
        public Explorer player;
        public Sprite spark;
        public Material spriteMaterial;
        public DungeonObject[] objects;
        public Canvas canvas;
        public bool Playing { get; private set; }
        public int Runes { get; private set; }
        public int Health { get; private set; } = 4;
        public string Screen { get; private set; }
        public float Elapsed { get; private set; }
        Font font;
        GameObject page, hud;
        Text healthText, runeText, timeText, messageText, dashText;
        Image dashFill;
        AudioSource audioSource;
        InputAction pause;
        float messageUntil, exitNotice;
        readonly Color ink = new Color(.018f, .018f, .025f, .99f);
        readonly Color muted = new Color(.65f, .67f, .71f);
        readonly Color gold = new Color(.35f, 1f, .65f);
        readonly Color red = new Color(.94f, .12f, .23f);
        readonly Color pale = new Color(.9f, .94f, .91f);
        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = .24f;
            pause = new InputAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape"); pause.AddBinding("<Gamepad>/start"); pause.Enable();
        }
        void Start() { MainMenu(); }
        void OnDestroy() { Time.timeScale = 1; pause.Dispose(); if (Instance == this) Instance = null; }
        void OnApplicationFocus(bool focus) { if (!focus && Playing) Pause(); }
        void Update()
        {
            if (pause.WasPressedThisFrame())
            {
                if (Playing) Pause(); else if (Screen == "Paused") Resume(); else if (Screen == "How to play" || Screen == "Credits") MainMenu();
            }
            if (!Playing) return;
            Elapsed += Time.deltaTime;
            healthText.text = "LIFE   " + new string('◆', Health) + new string('◇', 4 - Health);
            runeText.text = "RUNES   " + Runes + " / 3";
            timeText.text = FormatTime(Elapsed);
            dashFill.fillAmount = player.DashReady;
            dashText.text = player.DashReady >= 1 ? "DASH READY" : "RECHARGING";
            if (Time.unscaledTime > messageUntil) messageText.text = Runes == 3 ? "The seal is broken. Reach the eastern gate." : "Find the three golden runes. Watch the sentry paths.";
        }
        static string FormatTime(float value) { return Mathf.FloorToInt(value / 60).ToString("00") + ":" + Mathf.FloorToInt(value % 60).ToString("00"); }
        public void Begin()
        {
            Time.timeScale = 1; Playing = true; Screen = "Game"; Runes = 0; Health = 4; Elapsed = 0; exitNotice = 0;
            player.ResetExplorer(); foreach (var item in objects) item.ResetObject();
            ClearPage(); CreateHUD(); Notify("Three runes. One way out. Your escape begins."); Sound(440, .16f);
        }
        public void MainMenu()
        {
            Playing = false; Time.timeScale = 1; Screen = "Main menu";
            if (hud != null) Destroy(hud);
            Panel(false);
            Label(page.transform, "MK  /  DUNGEON ESCAPE", 17, gold, 52, -65, 510, 30);
            Label(page.transform, "DUNGEON\nESCAPE", 64, pale, 48, -121, 520, 182, FontStyle.Bold);
            Label(page.transform, "Find the runes. Get out alive.", 25, gold, 52, -305, 500, 38);
            Label(page.transform, "Recover three ancient runes, slip past the\nsentries, and break the seal on the eastern gate.", 20, muted, 52, -357, 490, 76);
            Button first = AddButton("Play", 52, -472, 440, Begin, true);
            AddButton("How to play", 52, -548, 440, HowToPlay);
            AddButton("Credits", 52, -620, 440, Credits);
            var best = PlayerPrefs.GetFloat("DungeonEscape.Best", 0);
            Label(page.transform, best > 0 ? "FASTEST ESCAPE  " + FormatTime(best) : "EXPLORE  /  COLLECT  /  ESCAPE", 15, muted, 52, -735, 480, 28);
            Label(page.transform, "WASD + MOUSE   /   CONTROLLER READY", 13, muted, 52, -792, 490, 24);
            Select(first);
        }
        public void HowToPlay()
        {
            Screen = "How to play"; Panel(true);
            Label(page.transform, "HOW TO PLAY", 16, gold, 54, -44, 700, 30);
            Label(page.transform, "Controls & objective", 43, pale, 50, -95, 730, 70, FontStyle.Bold);
            Label(page.transform, "Collect all 3 golden runes, then step into the eastern gate.\nYou have 4 life points. Red sentries and raised spikes hurt.\nBlue spikes are safe; coral flasks restore one life point.", 22, muted, 54, -193, 725, 130);
            Label(page.transform, "MOVE\nDASH\nPAUSE\nMENUS", 19, gold, 54, -366, 150, 180, FontStyle.Bold);
            Label(page.transform, "WASD / arrow keys \nSpace / Shift   \nEscape   \nMouse or arrows + Enter", 19, pale, 214, -366, 560, 180);
            Label(page.transform, "A dash briefly protects you from hazards. It recharges in a second.\nTall arches can hide you from view. Keep moving to emerge.", 19, muted, 54, -578, 720, 80);
            Select(AddButton("Back to main menu", 54, -696, 700, MainMenu, true));
        }
        public void Credits()
        {
            Screen = "Credits"; Panel(true);
            Label(page.transform, "CREDITS", 16, gold, 54, -44, 700, 30);
            Label(page.transform, "Made by MK", 43, pale, 50, -95, 720, 70, FontStyle.Bold);
            Label(page.transform, "SPECIAL THANKS", 17, gold, 54, -242, 700, 30);
            Label(page.transform, "Thanks to all the artists and developers who contributed to this project.", 19, pale, 54, -282, 720, 64);
            Label(page.transform, "Character art", 17, muted, 54, -362, 720, 80);
            Label(page.transform, "Animated character — Sogomn (CC0)\nopengameart.org/content/animated-character", 18, pale, 54, -402, 720, 80);
            Label(page.transform, "DUNGEON ART", 17, gold, 54, -500, 700, 30);
            Label(page.transform, "Michele \"Buch\" Bucelli · Sponsored by Abram Connelly\nTop down dungeon tileset · CC BY 3.0\nopengameart.org/content/top-down-dungeon-tileset\nopengameart.org/users/buch\ncreativecommons.org/licenses/by/3.0/", 18, pale, 54, -540, 720, 125);
            Label(page.transform, "Sheets sliced for Unity; tiles scaled and floor tinted for readability.\nAdditional game icons and synthesized sound made for this project.\nUnity 6 · Input System · Tilemap · Unity UI", 17, muted, 54, -680, 720, 60);
            Select(AddButton("Back to main menu", 54, -770, 700, MainMenu, true));
        }
        public void Pause()
        {
            if (!Playing) return;
            Playing = false; Time.timeScale = 0; Screen = "Paused"; Panel(true);
            Label(page.transform, "PAUSED", 17, gold, 54, -95, 700, 40);
            Label(page.transform, "Game paused", 45, pale, 50, -168, 725, 80, FontStyle.Bold);
            Label(page.transform, "Your run is paused.\n" + Runes + " of 3 runes collected  •  " + FormatTime(Elapsed), 24, muted, 54, -293, 700, 100);
            Select(AddButton("Resume", 54, -469, 700, Resume, true));
            AddButton("Restart run", 54, -546, 700, Begin);
            AddButton("Main menu", 54, -623, 700, MainMenu);
        }
        public void Resume() { ClearPage(); Playing = true; Screen = "Game"; Time.timeScale = 1; }
        public void CollectRune(DungeonObject item)
        {
            item.gameObject.SetActive(false); Runes++; Burst(item.transform.position, gold); Sound(620 + 140 * Runes, .23f);
            Notify(Runes == 3 ? "All three runes recovered. The eastern gate is open!" : "Rune recovered. " + (3 - Runes) + " still hidden in the vault.");
        }
        public bool Heal() { if (Health >= 4) return false; Health++; Sound(690, .15f); Notify("Life restored. Keep going."); return true; }
        public void Hurt()
        {
            if (!Playing || !player.TakeHit()) return;
            Health--; Sound(115, .23f); Burst(player.transform.position, new Color(1, .3f, .25f));
            Notify("Hit! Dash through danger, or wait for a safe opening.");
            if (Health <= 0) Finish(false);
        }
        public void TryExit()
        {
            if (Runes == 3) Finish(true);
            else if (Time.unscaledTime > exitNotice) { Notify("The gate is sealed. Recover all three golden runes."); exitNotice = Time.unscaledTime + 3; }
        }
        void Finish(bool won)
        {
            Playing = false; Screen = won ? "Victory" : "Defeat"; Time.timeScale = 0;
            if (won && (PlayerPrefs.GetFloat("DungeonEscape.Best", 0) == 0 || Elapsed < PlayerPrefs.GetFloat("DungeonEscape.Best")))
            { PlayerPrefs.SetFloat("DungeonEscape.Best", Elapsed); PlayerPrefs.Save(); }
            Sound(won ? 1040 : 150, .4f); Panel(true);
            Label(page.transform, won ? "THE SEAL IS BROKEN" : "THE VAULT CLAIMED ANOTHER", 17, gold, 54, -95, 700, 40);
            Label(page.transform, won ? "You escaped." : "Lost in the depths.", 52, pale, 50, -172, 730, 85, FontStyle.Bold);
            Label(page.transform, won ? "Three runes recovered. One explorer returned.\nEscape time: " + FormatTime(Elapsed) + "   •   Life remaining: " + Health + " / 4" : "You found " + Runes + " of 3 runes in " + FormatTime(Elapsed) + ".\nWatch the spike rhythm and use your dash for cover.", 24, muted, 54, -302, 720, 120);
            Select(AddButton(won ? "Play again" : "Try again", 54, -521, 700, Begin, true));
            AddButton("Main menu", 54, -608, 700, MainMenu);
        }
        void Notify(string message) { if (messageText != null) messageText.text = message; messageUntil = Time.unscaledTime + 4; }
        void CreateHUD()
        {
            if (hud != null) Destroy(hud);
            hud = new GameObject("In-game HUD", typeof(RectTransform)); hud.transform.SetParent(canvas.transform, false); Stretch(hud.GetComponent<RectTransform>());
            Rect("Top bar", hud.transform, ink, 0, 0, 1440, 82);
            Label(hud.transform, "DUNGEON / ESCAPE", 20, gold, 30, -24, 300, 36, FontStyle.Bold);
            healthText = Label(hud.transform, "LIFE   ◆◆◆◆", 21, pale, 370, -24, 250, 36);
            runeText = Label(hud.transform, "RUNES   0 / 3", 21, gold, 655, -24, 220, 36);
            timeText = Label(hud.transform, "00:00", 21, muted, 956, -24, 115, 36);
            var pauseButton = AddButtonTo(hud.transform, "PAUSE  Ⅱ", 1190, -14, 220, Pause, false, 54);
            Rect("Bottom bar", hud.transform, ink, 0, -815, 1440, 85);
            messageText = Label(hud.transform, "", 20, pale, 30, -838, 1020, 40);
            dashText = Label(hud.transform, "DASH READY", 15, gold, 1190, -829, 220, 28);
            Rect("Dash track", hud.transform, new Color(.12f,.2f,.24f), 1190, -866, 215, 5);
            dashFill = Rect("Dash charge", hud.transform, gold, 1190, -866, 215, 5).GetComponent<Image>();
            dashFill.sprite = spark; dashFill.type = Image.Type.Filled; dashFill.fillMethod = Image.FillMethod.Horizontal;
            EventSystem.current.SetSelectedGameObject(null);
        }
        void ClearPage() { if (page != null) { page.SetActive(false); Destroy(page); } }
        void Panel(bool centered)
        {
            ClearPage();
            page = new GameObject(Screen, typeof(RectTransform)); page.transform.SetParent(canvas.transform, false);
            var rt = page.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = centered ? new Vector2(.5f,.5f) : new Vector2(0,.5f); rt.pivot = centered ? new Vector2(.5f,.5f) : new Vector2(0,.5f); rt.sizeDelta = new Vector2(centered ? 810 : 565, 850); rt.anchoredPosition = centered ? Vector2.zero : new Vector2(26,0);
            var background = page.AddComponent<Image>(); background.color = ink;
            Rect("Red edge", page.transform, red, 0, 0, centered ? 810 : 565, 4);
            Rect("Neon accent", page.transform, gold, 0, -4, 96, 2);
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        GameObject Rect(string name, Transform parent, Color color, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            Position(go.GetComponent<RectTransform>(), x,y,w,h); go.GetComponent<Image>().color = color; return go;
        }
        static void Position(RectTransform rt, float x, float y, float w, float h) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0,1); rt.anchoredPosition = new Vector2(x,y); rt.sizeDelta = new Vector2(w,h); }
        Text Label(Transform parent, string text, int size, Color color, float x, float y, float w, float h, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(text.Split('\n')[0], typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent,false); Position(go.GetComponent<RectTransform>(),x,y,w,h);
            var label = go.GetComponent<Text>(); label.font = font; label.text = text; label.fontSize = size; label.fontStyle = style; label.color = color; label.raycastTarget = false; label.supportRichText = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; label.lineSpacing = 1.2f; return label;
        }
        Button AddButton(string text, float x, float y, float w, UnityEngine.Events.UnityAction action, bool primary = false) { return AddButtonTo(page.transform,text,x,y,w,action,primary,60); }
        Button AddButtonTo(Transform parent, string text, float x, float y, float w, UnityEngine.Events.UnityAction action, bool primary, float h)
        {
            var go = Rect(text,parent,primary ? red : new Color(.065f,.065f,.085f),x,y,w,h);
            var button = go.AddComponent<Button>(); var colors = button.colors; colors.highlightedColor = new Color(1.25f,1.25f,1.25f); colors.selectedColor = new Color(1.25f,1.25f,1.25f); colors.pressedColor = new Color(.7f,.7f,.7f); colors.colorMultiplier = 1; button.colors = colors;
            var outline=go.AddComponent<Outline>(); outline.effectColor=primary ? red : new Color(.22f,.3f,.27f); outline.effectDistance=new Vector2(1,-1);
            Rect("Accent",go.transform,gold,0,0,3,h).GetComponent<Image>().raycastTarget=false;
            var label = Label(go.transform,text,19,pale,22,-3,w-44,h-6,FontStyle.Bold); label.alignment = TextAnchor.MiddleLeft;
            button.onClick.AddListener(() => { Sound(330,.06f); action(); }); return button;
        }
        void Select(Button button) { EventSystem.current.SetSelectedGameObject(button.gameObject); }
        public void Sound(float frequency, float duration)
        {
            int count = Mathf.CeilToInt(22050 * duration); var data = new float[count];
            for (int i = 0; i < count; i++) { float t = i / 22050f; float envelope = Mathf.Min(t * 80, 1) * (1 - (float)i / count); data[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2) + .2f * Mathf.Sin(t * frequency * Mathf.PI * 4)) * envelope * .45f; }
            var clip = AudioClip.Create("Synthesized chime",count,1,22050,false); clip.SetData(data,0); audioSource.PlayOneShot(clip); Destroy(clip,duration + .2f);
        }
        public void Burst(Vector3 position, Color color) { StartCoroutine(Particles(position,color)); }
        IEnumerator Particles(Vector3 position, Color color)
        {
            var bits = new GameObject[8];
            for (int i=0;i<bits.Length;i++) { bits[i] = new GameObject("Rune spark"); var sr=bits[i].AddComponent<SpriteRenderer>(); sr.sprite=spark; sr.sharedMaterial=spriteMaterial; sr.color=color; sr.sortingOrder=40; bits[i].transform.position=position; bits[i].transform.localScale=Vector3.one*.09f; }
            float t=0; while(t<.45f) { t+=Time.unscaledDeltaTime; for(int i=0;i<bits.Length;i++) { float a=i*Mathf.PI/4; bits[i].transform.position=position+new Vector3(Mathf.Cos(a),Mathf.Sin(a))*t*2; bits[i].transform.localScale=Vector3.one*.09f*(1-t/.45f); } yield return null; }
            foreach(var bit in bits) Destroy(bit);
        }
    }
}
