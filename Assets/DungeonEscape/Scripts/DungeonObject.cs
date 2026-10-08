using UnityEngine;

namespace DungeonEscape
{
    public enum ObjectKind { Rune, Sentry, Spikes, Exit, Heart, Torch }
    public class DungeonObject : MonoBehaviour
    {
        public ObjectKind kind;
        public Vector2 patrolEnd;
        public float phase;
        public Sprite alternate;
        Vector3 origin;
        SpriteRenderer visual;
        Rigidbody2D body;
        Sprite initial;
        float clock;
        public bool ActiveHazard => kind != ObjectKind.Spikes || Mathf.Repeat(clock + phase, 3.4f) < 1.65f;
        void Awake()
        {
            origin = transform.position;
            visual = GetComponent<SpriteRenderer>();
            initial = visual.sprite;
            body = GetComponent<Rigidbody2D>();
        }
        public void ResetObject() { gameObject.SetActive(true); transform.position = origin; clock = 0; visual.color = Color.white; }
        void Update()
        {
            if (EscapeGame.Instance == null || !EscapeGame.Instance.Playing) return;
            clock += Time.deltaTime;
            if (kind == ObjectKind.Rune || kind == ObjectKind.Heart)
                transform.position = origin + Vector3.up * (Mathf.Sin(clock * 3 + phase) * .09f);
            if (kind == ObjectKind.Spikes)
            {
                visual.sprite = ActiveHazard ? initial : alternate;
                visual.color = ActiveHazard ? Color.white : new Color(.6f, .7f, .8f);
            }
            if (kind == ObjectKind.Exit)
                visual.color = EscapeGame.Instance.Runes == 3 ? new Color(.55f, 1, .8f) : new Color(.55f, .6f, .7f);
            if (kind == ObjectKind.Torch)
                visual.color = new Color(1, .8f + Mathf.Sin(clock * 13 + phase) * .17f, .65f);
            if (kind == ObjectKind.Sentry)
            {
                visual.flipX = Mathf.Cos(clock * .85f + phase) < 0;
                visual.sprite = Mathf.FloorToInt(clock * 5) % 2 == 0 ? initial : alternate;
            }
        }
        void FixedUpdate()
        {
            if (kind == ObjectKind.Sentry && EscapeGame.Instance != null && EscapeGame.Instance.Playing)
                body.MovePosition(Vector2.Lerp(origin, patrolEnd, (Mathf.Sin(clock * .85f + phase) + 1) * .5f));
        }
        void OnTriggerEnter2D(Collider2D other) { Touch(other); }
        void OnTriggerStay2D(Collider2D other) { if (kind == ObjectKind.Spikes || kind == ObjectKind.Sentry || kind == ObjectKind.Exit) Touch(other); }
        void Touch(Collider2D other)
        {
            var explorer = other.GetComponent<Explorer>();
            var game = EscapeGame.Instance;
            if (explorer == null || game == null || !game.Playing) return;
            switch (kind)
            {
                case ObjectKind.Rune: game.CollectRune(this); break;
                case ObjectKind.Heart: if (game.Heal()) { game.Burst(transform.position, Color.red); gameObject.SetActive(false); } break;
                case ObjectKind.Sentry: case ObjectKind.Spikes: if (ActiveHazard) game.Hurt(); break;
                case ObjectKind.Exit: game.TryExit(); break;
            }
        }
    }
}
