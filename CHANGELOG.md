# Changelog

## [1.3.1] - 2026-05-17

### Changed ###

- Fixed a bug with the volume slider not getting set properly with new green demons

## [1.3.0] - 2026-05-06

### Àdded ###

- Added fallback effects when spawining GameObjects in some effects fails.
- Added 3 random effect pool preset to give further control over green demons outcomes:
  - This new lobby setting (named `Type of Green Demon Random Effects`) only takes effect when using the `RANDOM` catch effect and can be any of:
    - `CASUAL`: Removes harsh effects, like `KILL` or `POOR_BOY`
    - `STANDARD` _(default)_: Keeps all caught effects
    - `HARDCORE`: Removes soft effects, like `SLIP` or `BEES`

### Changed ###

- Code cleanup, refactorings.
- Fixed Lucky Blocks support.
- When UnnamedProducts enabled, flairs that were taken away at the shore now respawn at the crash plane using their correct brand.
- Made spawned tornado always target the caught player.
- Moved calera's green demon spawn closer to The Kiln and closer to the lava.
- Added a slight delay when spawning multiple green demons.

## [1.2.0] - 2026-05-03

### Added ###

- Added a small flocking behavior to Green Demons when there are more than 1

## [1.1.2] - 2026-05-03

### Changed ###

- Fixed soft dependencies (again)
- Fixed a physics bug that can make demons sticks to each other.

## [1.1.1] - 2026-05-02

### Changed ###

- Fixed Lucky Blocks compatibility when spawning demons

## [1.1.0] - 2026-05-01

### Added ###

- Added support for [TimeTheme](https://thunderstore.io/c/peak/p/OracleTeam/TimeTheme/)

### Changed ###

- Slight tracker optimisations
- Fixed soft dependencies

## [1.0.3] - 2026-04-29

### Changed ###

- Reducced bundle sizes.
- Added EQs, reberbs to chase loop.

## [1.0.2] - 2026-04-25

### Removed ###

- Removed unwanted code

## [1.0.1] - 2026-04-25

### Added ###

- Made Green Demons despawn when the credits starts

### Changed ###

- Replaced the demon's PositionSyncer script with a PhysicsSyncer for smoother movement.
- Added a simple raycast to spawn the demon at a correct position
- Spawning balancing
  - CALDERA:
    - Crow flight threshold 50% -> 40%
  - MESA, ALPINE:
    - Crow flight threshold 50% -> 40%
  - THE KILN:
    - Pull demon spawn point up a bit.
    - Climb threshold 50% -> 33%
  - PEAK:
    - Put demon spawing position closer to the peak
    - Climb threshold: 50% -> 15%
- Tweaked demons speeds:
  - THE KILN:
    - Reduced demon altitude speed nerfs -50% -> -5%
    - Reduced lateral speeds by 20%
  - CALDERA:
    - Removed altitude speed nerfs

## [1.0.0] - 2026-04-24

- Initial Release