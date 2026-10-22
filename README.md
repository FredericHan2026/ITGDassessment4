# Penguin Patrol

Hi, my name's Frederic. This carries on from the previous build and turns the
project into a playable game.

The Antarctic twist stays the same: PacStudent is a penguin, the ghosts are four
distinctive seals, the pellets are krill, the power pellets are fish and the
bonus item is a squid.

## Scenes

| Scene | Purpose |
| --- | --- |
| `Assets/Scenes/StartScene.unity` | Title screen, menu, best scores |
| `Assets/Scenes/Level01.unity` | The Level 01 maze |

## Project layout

```
Assets/
  Animations/   sprite-swap animation clips
  Animators/    animator controllers
  Audio Clips/  music and sound effects
  Fonts/        Poppins Bold, used by every piece of UI text
  Scenes/       StartScene and Level01
  Scripts/      Audio, Level, Player and UI scripts
  Sprites/      all artwork, plus UI/ for title and menu pieces
```

## The maze

Level 01 is laid out by hand in the scene, one sprite per cell. The top-left
quadrant from the level map is mirrored right, down, and both ways, so the same
shape sits in all four corners. Each wall piece is rotated in 90 degree steps to
line up with its neighbours.

Nothing in the maze lives on a Canvas. The level, the characters, the pellets
and the bonus item are all plain sprites in world space; Canvases are used only
for the title screen and for the in-game HUD.

## The title screen

`StartScene` is a Screen Space - Overlay Canvas with its CanvasScaler in Expand
mode against a 1920x1080 reference, so the whole layout fits at both 16:9 and
4:3 without cropping. `AnimatedBorder.cs` walks a ring of dots around the edge
of the canvas, recomputing their positions from the RectTransform every frame so
the border follows the aspect ratio rather than being baked to one size.

The penguin and the seals on the title screen are world-space sprites rather
than UI Images, because their animation clips drive `SpriteRenderer.sprite`.

Best scores come from `PlayerPrefs` via `HighScoreDisplay.cs` and read 000000 /
00:00:00 until a run has been saved. Level 2 is greyed out until that scene
exists.

## Movement

All motion is hand-written and frame-rate independent. `Tweener.cs` interpolates
between two points using accumulated `Time.deltaTime`; there is no Rigidbody
driving anything, no CharacterController and no built-in or third-party movement
helper anywhere in the project.

## Controls

The title screen is driven by the Level 1 and Level 2 buttons.
