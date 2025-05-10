# Entelect Challenge 2025 - Zooscape 🦏 - Release 2025.0.0

- Entelect Challenge 2025 - Zooscape 🦏 - Release 2025.0.0
  - [Table of Contents](#table-of-contents)
  - [Changelog](#change-log)
  - [The Game](#the-game)
  - [General](#general)
  - [Rules](#rules)
    - [Animals](#animals)
    - [Zookeeper](#zookeeper)
    - [The World](#the-world)
    - [Catch and release](#catch-and-release)
    - [Scoring](#scoring)
  - [Game Ticks](#game-ticks)
  - [Animal Spawning](#animal-spawning)
  - [Commands](#commands)
    - [Command processing order](#command-processing-order)
  - [Win Conditions](#win-conditions)


---
>## Change log 
> Initial Release

## The Game

## General

📒 Note: All configuration is subject to change while we balance the game. For the latest configurations please navigate to ./Zooscape for appsettings.json, appsettings.Development.json and appsettings.Production.json

---
## Rules
### Animals
- Only 1 action is allowed per tick
- Actions are processed in the order they are received 
- Actions can be queued, however the queue size is limited
  - The queue size can be viewed in appsettings.json
- Actions **cannot** be cancelled once sent 
- When an animal hits a wall or an untraversable space they will remain stationary until a command to change direction is received
- Animals will not collide with each other and can occupy the same space
### Zookeeper
- At the beginning of the game a single zookeeper will spawn
- The Zookeeper will target the nearest animal and is recalculated every 20 game ticks
- If two animals are equal distance from the zookeeper, the original viable animal will remain the target
- When the game starts, the zookeeper will only start moving once there is exactly one viable animal to target. If two or more animals are the same distance from the zookeeper and the closest, none of them are viable targets 
- The zookeeper’s actions will be processed after all animal actions during each tick 
### The World
- Every traversable tile will contain a food pellet except
  - Animal spawns
  - Zookeeper spawn
- The World will be symmetrical along the x and y axis
- The Zookeeper spawn will always be the centre tile of the map
### Catch and release 
- When the zookeeper occupies the same position in the world as an animal at the end of the tick, the animal has been caught and will be sent back to their cage 
- The animal’s action queue will be cleared when caught 
- There is no limit on the number of times an animal can be caught 
- The animal will not be a viable target for the zookeeper while in the cage but will become one when leaving the cage 
- The animal will start in the cage in an idle state and will only start moving when an action to do so is queued 
- Animals cannot re-enter their cages once they have left, except upon being caught 
  
### Scoring
- If more than one animal land on the same pellet in the same tick, the animal that sent their command first will get the pellet 
--- 
## Game Ticks

Zooscape is a real time game that utilises `Ticks`, as a unit of time to keep track of the game.

---
## Animal Spawning
- Every animal will be assigned a spawn point (aka “Cage”) upon joining the game 
- Every animal’s spawn point will be the same distance from the zookeeper’s spawn point 
- When an animal spawns, it starts in an idle state and will be not be targeted by the Zookeeper(s) until it has left its spawn point. 
- Once an animal has left its spawn point, it cannot get back in. 

---
## Commands

When a animal is spawned, they spawn in an IDLE state on their designated spawn point, and will begin moving as soon as the first command of movement is sent. Animals may traverse the map via basic movement.

Note: Animals have continuous movement and will move in their last direction until a new command is issued or it is at the edge of the map where the animal will stop

The following commands are available:

* `UP` - 1
* `DOWN` - 2
* `LEFT` - 3
* `RIGHT` - 4

### Command Processing Order

The Game Engine maintains a command queue for each animal. A animal can issue a command at any point which will then be added to that animal's command queue. With each game tick the Game Engine pops the first command off of each animal's queue and orders them by received timestamp. The four commands are then processed in that order.

## Win Conditions 

- When the timer runs out or all the food pellets have been collected, the animal with the highest score wins, if there are any ties the engine will calculate the winner using the following order of tie breakers
  1. Highest Score
  2. Fewest times captured
  3. Least time spent on spawn
  4. Farthest distance travelled
  5. First command issued

- When an animal is captured they will lose a percentage of their score, the percentage is set in appsettings.json
