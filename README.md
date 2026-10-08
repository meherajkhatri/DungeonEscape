# Dungeon Escape

A complete, small top-down Unity game. Recover three golden runes in a guarded vault, then escape through the eastern gate. Includes patrolling sentries, timed spikes, healing flasks, a protective dash, a four-point life system, and a saved best escape time.

## Play

1. Open this project in **Unity 6000.5.9f1** (the project's existing version).
2. Open **Assets/DungeonEscape/Scenes/DungeonEscape.unity**.
3. Press **Play**, then **Enter the Dungeon**.


| Action | Keyboard | Controller |
| --- | --- | --- |
| Move | WASD or arrow keys | Left stick or D-pad |
| Dash | Space or left Shift | A (Xbox) / Cross (PlayStation), or right shoulder button |
| Pause/resume | Escape | Use the on-screen Pause button; menu navigation also supports the controller D-pad and South button |
| Menu navigation | Mouse, or arrows and Enter | D-pad and South button |

The dash lasts 0.18 seconds and recharges in 1.1 seconds. During the dash, hazards cannot hurt you. After taking damage, you receive 1.3 seconds of protection. Raised silver spikes are dangerous; lowered blue spikes are safe. Coral flasks restore one life point and remain available if life is full. The eastern gate opens after all three runes are collected.

From the pause screen, choose **Resume**, **Restart run**, or **Main menu**. Victory and defeat screens provide **Play again/Try again** and **Main menu** options. The How to Play and Credits screens both provide **Back to main menu**.

## Assignment evidence

| Requirement | Implementation and where to inspect |
| --- | --- |
| Sprite sheet | `Art/Imported/guy.png`: 16 character frames; `Art/Imported/sheet.png`: dungeon tiles and arches. `Art/DungeonAtlas.png` supplies supplemental gameplay icons. |
| Tilemap, Grid, Tile Palette | Scene object **Dungeon Grid** contains Floor, Walls, and Foreground tilemaps. Select `Assets/DungeonEscape/Tiles/DungeonPalette.prefab` in the Tile Palette window. Its Tile assets are the same assets used to construct the map; the environment remains editable with Unity's painting tools. |
| Foreground layering | Four arches in the openings between chambers render at sorting order 30, above the explorer at 10. Walk through the passages to pass behind their lintels. |
| Unity Input System | `Input/DungeonControls.inputactions`, referenced by `Explorer.cs`, supports keyboard and standard gamepads. Canvas navigation uses `InputSystemUIInputModule`. |
| 2D colliders | Explorer CapsuleCollider2D, wall TilemapCollider2D, and circular trigger colliders on interactable objects. |
| Meaningful triggers | Rune collection, gate completion, spike/sentry damage, and healing flasks all use OnTriggerEnter2D/OnTriggerStay2D. |
| Rigidbody2D physics | Dynamic explorer, zero gravity, frozen rotation, continuous collision detection, velocity applied in FixedUpdate. Sentries use kinematic Rigidbody2D.MovePosition patrols. |
| Canvas UI | Runtime Canvas menus: main menu, How to Play, Credits, Pause, Victory, and Defeat. Every secondary screen offers a route to the main menu. HUD shows life, rune count, time, dash recharge, and contextual feedback. |
| Player animations | `Animation/Explorer.controller` transitions between Idle, Walk, and Dash using Speed and Dashing parameters. Directional blend trees animate SpriteRenderer.sprite from guy.png. |
| Complete game loop | Main menu → collect runes while surviving → unlock gate → victory. Losing all life produces defeat. Both outcomes support replay and return to menu. |
| Polish | Consistent palette, credited pixel art, synthesized sound effects, collectible particles, hit blinking, animated sentries, bobbing pickups, torch flicker, best time, pause, and clear feedback. |

## Credits

Made by MK. Helped by OpenAI Codex.

- Character: **Sogomn**, [Animated character](https://opengameart.org/content/animated-character), [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
- Dungeon: **Michele "Buch" Bucelli**, [Top down dungeon tileset](https://opengameart.org/content/top-down-dungeon-tileset), [artist profile](https://opengameart.org/users/buch), [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/). Asset sponsor: **Abram Connelly**.
- Original source PNGs are preserved in `Assets/DungeonEscape/Art/Imported`. Frames and tiles are sliced in Unity; selected dungeon tiles are imported at the map scale, and the floor is tinted darker for readability. The character has four directional walk cycles.
- Sound effects are synthesized in code.
- Unity supplies the engine, Input System, Tilemap, UI, and built-in LegacyRuntime font.

## Editing and building

- Edit the generated scene and assets directly in Unity. No runtime editor dependency is needed.
- **Dungeon Escape → Build macOS Player** builds `Builds/DungeonEscape.app`.
- **Dungeon Escape → Build Game Assets and Scene** regenerates the authored assets and scene. This intentionally replaces changes inside the generated DungeonEscape asset folders and scene; it does not change the original SampleScene. Save a copy before regenerating a customized version.
- The Canvas uses a 1440×900 reference layout; the orthographic camera adjusts to preserve the map at different aspect ratios.

## Validation

**Verified:** 29 integration checks passed, and the standalone macOS player built successfully. See `Validation/results.txt` and the captured game screens in `Validation/`.

`Assets/DungeonEscape/Editor/DungeonValidation.cs` is a batch-mode integration harness. It checks map reachability and uses virtual keyboard/gamepad devices plus live Rigidbody2D and trigger interactions to exercise gameplay and menus. Run only against a temporary copy of the project because the harness exits the editor when finished:

```sh
Unity -batchmode -projectPath /path/to/temporary/project -executeMethod DungeonEscape.Editor.DungeonValidation.Run -logFile /tmp/dungeon-tests.log
```

It writes screen captures and a result file to `Validation/`. Virtual-device checks do not replace a final check with a physically connected controller or your course's specific in-class setup.