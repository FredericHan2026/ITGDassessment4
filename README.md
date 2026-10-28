# PacStudent

Hi, my name's Frederic and this is my finalised game recreation for Assessment 4.

I've decided to take the game on with an Antarctic twist:
PacStudent is a penguin, the ghosts are four distinctive seals, the pellets are krill, the power pellets are fish and the bonus item is a squid.

This is a new repository, continued from my Assessment 3 project at
<https://github.com/FredericHan2026/ITGDassessment3>. The first commit imports
that project whole and everything after it is new work.

## Play

- **Move:** W / A / S / D or the arrow keys. PacStudent remembers your last key and turns as soon as that way opens up.
- **Score:** krill 10, fish 50, bonus squid 100, scared seal 300.
- **Fish** scare the seals for 10 seconds, the last 3 spent flashing as a warning. Eat them to send them home to the pen.
- You have 3 lives. The round ends when you run out or eat every pellet, and your best score and time are saved.
- **Exit** returns to the title screen.

## Running it

1. Install **Unity 2023.2.10f1** and open this folder as a project.
2. Open `Assets/Scenes/StartScene.unity` and press Play.

Everything the project needs is in the repository; there is nothing to download.

## How it works

- **Movement** is grid based. PacStudent and the seals lerp from tile to tile at a fixed speed, worked out from accumulated `Time.deltaTime`, so it is frame-rate independent. No Rigidbody drives anything, no CharacterController, and no third-party tweening. The one Rigidbody2D in the project is kinematic and exists only so trigger contacts get reported.
- **The maze** is stored as a single quadrant and mirrored right then down. Wall pieces are not stored with a rotation; each works its own out from the neighbours it connects to, so editing the quadrant is enough to change the level.
- **The seals** each decide one cell at a time from what is immediately next door, so there is no pathfinding. Seal 1 picks at random among the moves that leave PacStudent at least as far away, seal 2 among those that leave him at least as close, seal 3 among anything valid, and seal 4 heads for the outer corridor and then keeps the wall on its left, which carries it clockwise around the map. Scared seals all use seal 1's rule, and a dead seal flies straight home through the walls.
- **Speeds** are set against PacStudent's 5 tiles a second: the seals run at 90% of that normally, and half of that again while scared, recovering or dead.

## Project layout

```
Assets/Scenes/      StartScene, Level01
Assets/Scripts/     Audio, Ghosts, Level, Player and UI code
Assets/Sprites/     all artwork, plus FX and UI pieces
Assets/Animations/  sprite-swap animation clips
Assets/Animators/   animator controllers
Assets/Audio Clips/ music and sound effects
Assets/Prefabs/     the bonus squid and the generator's tile prefabs
Assets/Fonts/       Poppins Bold, used by every piece of UI text
```
