# Penguin Patrol

Hi, my name's Frederic and this is my attempt for assessment 3.

I've decided to take the game on with an Antarctic twist:
PacStudent is a penguin, the ghosts are four distinctive seals, the pellets are krill, the power pellets are fish and the bonus item is a squid.

## Scenes

| Scene | Purpose |
| --- | --- |
| `Assets/Scenes/StartScene.unity` | Title screen and menu music |
| `Assets/Scenes/Level01.unity` | Recreation of the Level 01 maze |

## Project layout

```
Assets/
  Animations/   sprite-swap animation clips
  Animators/    animator controllers
  Audio Clips/  music and sound effects
  Scenes/       StartScene and Level01
  Scripts/      Audio, Level, Player and UI scripts
  Sprites/      all artwork
```

## The maze

Level 01 is laid out by hand in the scene, one sprite per cell. The top-left
quadrant from the level map is mirrored right, down, and both ways, so the same
shape sits in all four corners. Each wall piece is rotated in 90 degree steps to
line up with its neighbours.

A panel beside the maze lists every animation state and every sprite in the
project.

## Movement

All motion is hand-written and frame-rate independent. `Tweener.cs` interpolates
between two points using accumulated `Time.deltaTime`; there is no Rigidbody, no
CharacterController and no built-in movement helper anywhere in the project.

## Controls

`Space`, `Return` or a mouse click starts the game from the title screen.
