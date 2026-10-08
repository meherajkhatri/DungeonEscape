using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonEscape
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public class Explorer : MonoBehaviour
    {
        public InputActionAsset controls;
        public SpriteRenderer visual;
        public float speed = 4.5f;
        public Vector2 Spawn { get; private set; }
        public bool IsDashing => dashLeft > 0;
        public float DashReady => Mathf.Clamp01(1 - cooldown / 1.1f);
        Rigidbody2D body;
        Animator animator;
        InputAction move, dash;
        Vector2 facing = Vector2.down;
        float dashLeft, cooldown, invulnerable;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            Spawn = transform.position;
            controls = Instantiate(controls);
            move = controls.FindAction("Explorer/Move", true);
            dash = controls.FindAction("Explorer/Dash", true);
            controls.Enable();
        }
        void OnDestroy() { if (controls != null) { controls.Disable(); Destroy(controls); } }
        void Update()
        {
            if (EscapeGame.Instance == null || !EscapeGame.Instance.Playing) { animator.SetFloat("Speed", 0); return; }
            cooldown -= Time.deltaTime;
            invulnerable -= Time.deltaTime;
            dashLeft -= Time.deltaTime;
            var direction = move.ReadValue<Vector2>();
            if (direction.sqrMagnitude > .02f) facing = direction.normalized;
            if (dash.WasPressedThisFrame() && cooldown <= 0)
            {
                dashLeft = .18f;
                cooldown = 1.1f;
                EscapeGame.Instance.Sound(520, .09f);
                EscapeGame.Instance.Burst(transform.position, new Color(.4f, .9f, 1));
            }
            visual.flipX = false;
            animator.SetFloat("Facing", Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? (facing.x < 0 ? 2 : 1) : (facing.y > 0 ? 3 : 0));
            visual.color = invulnerable > 0 && Mathf.FloorToInt(invulnerable * 14) % 2 == 0 ? new Color(1, .45f, .4f, .35f) : Color.white;
            animator.SetFloat("Speed", direction.magnitude);
            animator.SetBool("Dashing", IsDashing);
        }
        void FixedUpdate()
        {
            body.linearVelocity = EscapeGame.Instance != null && EscapeGame.Instance.Playing
                ? (IsDashing ? facing * 13 : Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) * speed)
                : Vector2.zero;
        }
        public bool TakeHit()
        {
            if (invulnerable > 0 || IsDashing) return false;
            invulnerable = 1.3f;
            return true;
        }
        public void ResetExplorer()
        {
            body.position = Spawn;
            body.linearVelocity = Vector2.zero;
            dashLeft = cooldown = invulnerable = 0;
            facing = Vector2.down;
            visual.color = Color.white;
        }
    }
}
