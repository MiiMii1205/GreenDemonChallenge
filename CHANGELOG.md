# Changelog

## [1.1.0] - 2026-05-01

### Added ###

- Added support for [TimeTheme](https://thunderstore.io/c/peak/p/OracleTeam/TimeTheme/)

### Changed ###

- Slight tracker optimisations
- Fixed soft dependencies
- Fixed some null refs issues when updating ui colors

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