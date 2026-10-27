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

## The HUD

`Level01` carries a Screen Space - Overlay canvas called `HUD`, scaled the same
way as the title screen. The maze fills the canvas vertically at both 16:9 and
4:3, so the HUD lives in the side margins: level name, score and lives down the
left, the round clock and the ghost scared countdown down the right, and Exit in
the bottom-right corner. The scared countdown hides itself whenever it is at
zero.

`HUDController.cs` is display only. It holds no game rules; the round logic
calls `AddScore`, `SetLives` and `ShowScared` and tells it when to run the
clock. Exit is the one control that acts on its own.

Each seal also carries a World Space canvas (`Ghost1Canvas` through
`Ghost4Canvas`) showing which ghost it is.

## Movement

All motion is hand-written and frame-rate independent. `Tweener.cs` interpolates
between two points using accumulated `Time.deltaTime`; there is no Rigidbody
driving anything, no CharacterController and no built-in or third-party movement
helper anywhere in the project.

`PacStudentController.cs` uses the lastInput / currentInput pattern.  lastInput
remembers the most recent key pressed; currentInput is the direction actually
being travelled.  At the end of every cell lastInput gets first refusal, and if
that way is blocked the old currentInput carries on.  If neither is possible
PacStudent stops and the wall clip loops instead of the walking clip.  Speed is
5 cells a second.

`DustTrail.cs` drives a `ParticleSystem` that kicks up snow behind PacStudent
while he is moving. Drop a Particle System onto him and assign it to tune the
effect in the Inspector; leave the field empty and one is built and configured
at startup instead. The particle material is copied from the sprite material
already in the scene rather than looked up by shader name, so there is nothing
to go missing. The particles simulate in world space, so a puff stays where it
was dropped while he carries on.

## The level generator

`LevelGenerator.cs` stores the top-left quadrant of the maze and mirrors it
right, then down.  Wall pieces are not stored with a rotation: each one works
its own out from the neighbours it connects to, so editing the quadrant is
enough to change the level.

Level01 already has its tiles in the scene, so `generateOnStart` is off there
and the generator acts as the level's source of truth — PacStudent asks it which
cells it may enter.  Turn `generateOnStart` on, or call `Generate()`, to build
the maze from the prefabs in `Assets/Prefabs/Tiles/` instead.

## Playing a round

`GameManager.cs` runs the round: a three second countdown over the intro music,
then the clock starts and PacStudent is unfrozen.

Pellets, power pellets, the seals and the bonus squid all carry a trigger
collider, and PacStudent carries the project's only `Rigidbody2D`. It is
Kinematic, has no gravity and is never driven by physics; it is there purely so
trigger contacts get reported, with Use Full Kinematic Contacts on so static
triggers register too.

Contacts and the grid both lead to the same place. PacStudent also reports the
cell he arrives on, and taking a pickup removes it from the table, so whichever
route gets there first wins and the other does nothing. That means a pickup can
never be scored twice, and at five cells a second it can never be missed either.

Walls and the tunnel stay on the grid, where they belong: they are rules about
where PacStudent may move rather than things he bumps into.

A pellet is 10, a power pellet 50, the bonus squid 100 and an edible seal 300.
A power pellet makes the seals edible for ten seconds, with the last three spent
flashing white as a warning, and the music follows the state. Running into a
seal that is not edible costs a life and restarts the round; running out of
lives, or clearing every pellet, ends it.

At the end the run is written to `PlayerPrefs` under the same keys the title
screen reads, keeping the better one: a higher score wins, and on a tie the
quicker time does.

Row 14 is the tunnel. `LevelGenerator.TryWrap` sends anything that steps off one
end of a row that is open at both edges round to the other side.

## The seals

`GhostController.cs` decides one cell at a time from what is immediately next
door, so there is no pathfinding anywhere. A seal never stops, never turns back
on itself unless a dead end leaves it no choice, and never uses the tunnel —
anything off the edge of the map is a wall to it.

Each seal has its own way of choosing, set in the Inspector:

| Seal | Behaviour |
| --- | --- |
| 1 Sky | picks at random among the moves that leave PacStudent at least as far away as he is now |
| 2 Cocoa | picks at random among the moves that leave him at least as close |
| 3 Snow | picks at random among every valid move |
| 4 Slate | heads for the outer corridor, then keeps the wall on its left, which carries it clockwise around the map |

Distances are straight line. Seals 1 and 2 choose at random among everything
that qualifies rather than taking the single best move, so the same corner does
not give the same answer twice, and if nothing qualifies they take any valid
move, because the one thing a seal may never do is stop.

Frightened and recovering seals all use seal 1's rule.

Speeds are set against PacStudent: 90% of his 5 cells a second while normal,
half that while frightened, and the same again while recovering or dead.

The house has a gap top and bottom. Seals 1 and 3 use the top one, 2 and 4 the
bottom, both on release and when they come back. The gap is one way: a seal can
leave through it, and re-enter only when it has been eaten, so none of them can
sit in the doorway instead of hunting. Once eaten, a seal heads straight back
into the pen, walls and all, and the moment it arrives the round puts it into
whatever state the others are in and walks it out through its own gap. If it was
the last one on its way home, the music goes back to matching that state.

## The bonus item

`CherryController.cs` spawns the bonus squid every five seconds at a random
point on a circle outside whatever the camera can see, sends it in a straight
line through the centre of the level, and destroys it once it has left the view
on the far side.  The circle is measured from the camera at spawn time, so it
stays off screen at either aspect ratio.

## Controls

The title screen is driven by the Level 1 and Level 2 buttons.  In the level,
WASD or the arrow keys steer PacStudent and Exit returns to the menu.
