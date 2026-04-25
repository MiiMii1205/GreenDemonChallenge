# Green Demon Challenge #
A relentless Green Demon spawns halfway through each biome and hunts the closest player.

It doesn’t stop. It doesn’t forget.

And it _will_ ruin you.

Inspired by the [Green Demon Challenge](https://vgchallenge.fandom.com/wiki/Green_Demon_Challenge), this mod turns every run into a panic-inducing frantic race where teamwork,
betrayal, and terrible decisions decide who survives.

## 😈 What This Mod Does ##
Halfway through each biome, a [Green Demon](https://vgchallenge.fandom.com/wiki/Green_Demon_Challenge) will spawn behind the group, and begins to chase the closest player.

if it touches you... you're done.

To keep things fair, the demon slows down as it gets closer, and it's a bit slower at going up or down.
It will also play a little tune, so you can easily know where it currently is. 
There is also a visual HUD tracker that tracks the demon on screen.

By default, they also despawn after lighting the next [campfire](https://peak.wiki.gg/wiki/Campfire).

## 🧠 Tips ##
Like the original [Green Demon Challenge](https://vgchallenge.fandom.com/wiki/Green_Demon_Challenge), the key is to track where the demon is.

You'll then need to use the mountain itself as your shield.
So by moving swiftly behind rocks/obstacles, you can effectively make the demon stuck and climb without any issue.

Another good strategy is to split up. You could potentially confuse the demon, earn some ease of mind and even make it stuck if you're smart about it.
You could also decide to sacrifice one of you when it becomes dire.

## 🔌 Mod Compatibility ##
This mod is compatible with a couple of other mods. When a compatible mod is detected, additional mechanics will be added to spice up the challenge.
These mod includes:

- [Lucky Blocks](https://thunderstore.io/c/peak/p/legocool/Lucky_Blocks/)
- [Unnamed Products](https://thunderstore.io/c/peak/p/OracleTeam/UnnamedProducts/)

## ⚙️ Settings ##
You can tune the mod to fit your group's needs. This is particularly useful if you want a more casual climb, or add a bit more chaos in +20 lobbies.

### Lobby Settings (Host Controlled) ###
When in a lobby, the host’s settings applies to everyone.

> [!NOTE]
> ⚠️ These **cannot be changed after leaving the airport** (host included)
> Any changes mid-run only apply to the next run
 
Here's the list of lobby settings available:

#### General Settings ####

##### Demon Caught Effect  #####
What happens to players caught by Green Demons. Can be any of:
- `RANDOM`: Random effect each times. (smart selection)
- `KILL` _(default)_: Instant death.
- `ZOMBIFY` : Turn you into a [Zombie](https://peak.wiki.gg/wiki/Zombie) instantly.
- `FULL_INJURY`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Injury.png/28px-Status_Injury.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +100% Injury](https://peak.wiki.gg/wiki/Stamina_bar#Injury), giving teammates a chance to save you. (Insta-death for [skeletons](https://peak.wiki.gg/wiki/The_Book_of_Bones#Skeleton_behavior))
- `HALF_INJUSRT`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Injury.png/28px-Status_Injury.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +50% Injury](https://peak.wiki.gg/wiki/Stamina_bar#Injury). (Insta-death for [skeletons](https://peak.wiki.gg/wiki/The_Book_of_Bones#Skeleton_behavior))
- `FULL_POISON`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Poison.png/28px-Status_Poison.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +100% Poison](https://peak.wiki.gg/wiki/Stamina_bar#Poison), giving teammates a chance to save you and/or wait it out. ([skeletons](https://peak.wiki.gg/wiki/The_Book_of_Bones#Skeleton_behavior) are immune)
- `HALF_POISON`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Poison.png/28px-Status_Poison.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +50% Poison](https://peak.wiki.gg/wiki/Stamina_bar#Poison). ([skeletons](https://peak.wiki.gg/wiki/The_Book_of_Bones#Skeleton_behavior) are immune)
- `FULL_SPORES`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Spores.png/28px-Status_Spores.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +100% Spores](https://peak.wiki.gg/wiki/Stamina_bar#Spores), eventually turning you into a [Zombie](https://peak.wiki.gg/wiki/Zombie) if nothing is done. ([skeletons](https://peak.wiki.gg/wiki/The_Book_of_Bones#Skeleton_behavior) are immune)
- `HALF_SPORES`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Spores.png/28px-Status_Spores.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +50% Spores](https://peak.wiki.gg/wiki/Stamina_bar#Spores), potentially turning you into [Zombie](https://peak.wiki.gg/wiki/Zombie). ([skeletons](https://peak.wiki.gg/wiki/The_Book_of_Bones#Skeleton_behavior) are immune)
- `CURSE`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Curse.png/28px-Status_Curse.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +25% Curse](https://peak.wiki.gg/wiki/Stamina_bar#Curse).
- `EPPY`: Gives [<img src="https://peak.wiki.gg/images/thumb/Status_Drowsy.png/28px-Status_Drowsy.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> +100% Drowsy](https://peak.wiki.gg/wiki/Stamina_bar#Drowsy) ([skeletons](https://peak.wiki.gg/wiki/The_Book_of_Bones#Skeleton_behavior) are immune)
- `NO_STAM`: Keeps your base [stamina bar](https://peak.wiki.gg/wiki/Stamina_bar) empty for 60 seconds
- `ASTORNAUT`: Makes you reach for the stars!
- `FLING`: Flings you off the mountain. 
- `FALL`: Makes you fall/play dead for 30 seconds. 
- `SCOUTMASTER`: Calls the [Scoutmaster](https://peak.wiki.gg/wiki/Scoutmaster_Myres) on you for 60 seconds. 
- `POOR_BOY`: Lose your WHOLE [inventory](https://peak.wiki.gg/wiki/How_to_play#Inventory_&_Backpack), [<img src="https://peak.wiki.gg/images/thumb/Backpack.png/28px-Backpack.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Backpack](https://peak.wiki.gg/wiki/Backpack) included.  
- `DYNA_BRUH`: Gives you one [<img src="https://peak.wiki.gg/images/thumb/Dynamite.png/28px-Dynamite.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> lit dynamite](https://peak.wiki.gg/wiki/Dynamite).
- `SCORPION`: Gives you a [<img src="https://peak.wiki.gg/images/thumb/Scorpion.png/28px-Scorpion.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Scorpion](https://peak.wiki.gg/wiki/Scorpion).
- `MANDRAKE`: Gives you a [<img src="https://peak.wiki.gg/images/thumb/Mandrake.png/28px-Mandrake.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Mandrake](https://peak.wiki.gg/wiki/Mandrake).
- `BEES`: Spawns [<img src="https://peak.wiki.gg/images/thumb/Bee.png/28px-Bee.png?6058f9" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Bees](https://peak.wiki.gg/wiki/Bees) on you.
- `EXPLODE`: Makes you explode.
- `SPORE_CLOUD`: Spawns a [<img src="https://peak.wiki.gg/images/thumb/Status_Spores.png/28px-Status_Spores.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Spore Cloud](https://peak.wiki.gg/wiki/Roots#Mushroom_Spore_Clouds) on you.
- `POISON_CLOUD`: Spawns a [<img src="https://peak.wiki.gg/images/thumb/Status_Poison.png/28px-Status_Poison.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Poison Cloud](https://peak.wiki.gg/wiki/Tropics#Poison_Spore_Bomb) on you.
- `FIRE_CLOUD`: Spawns a [<img src="https://peak.wiki.gg/images/thumb/Status_Heat.png/28px-Status_Heat.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> heat-inducing](https://peak.wiki.gg/wiki/Stamina_bar#Heat) Fire Cloud on you.
- `ICE_CLOUD`: Spawns a [<img src="https://peak.wiki.gg/images/thumb/Status_Cold.png/28px-Status_Cold.png?6c4d38" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> cold-inducing](https://peak.wiki.gg/wiki/Stamina_bar#Cold) Ice Cloud on you.
- `BIOME_CLOUD`: Spawns biome-specifics clouds on you.
  - For [SHORE](https://peak.wiki.gg/wiki/Shore), spawns an `ICE_CLOUD` if the night are cold and is at night. Otherwise, spawns a [`POISON_CLOUD`](https://peak.wiki.gg/wiki/Tropics#Poison_Spore_Bomb)
  - For [TROPICS](https://peak.wiki.gg/wiki/Tropics), spawns a [`POISON_CLOUD`](https://peak.wiki.gg/wiki/Tropics#Poison_Spore_Bomb)
  - For [ROOTS](https://peak.wiki.gg/wiki/Roots), spawns a [`SPORE_CLOUD`](https://peak.wiki.gg/wiki/Roots#Mushroom_Spore_Clouds),
  - For [ALPINE](https://peak.wiki.gg/wiki/Alpine), spawns an `ICE_CLOUD`,
  - For [MESA](https://peak.wiki.gg/wiki/Mesa), [CALDERA](https://peak.wiki.gg/wiki/Caldera), [THE KILN](https://peak.wiki.gg/wiki/The_Kiln), [PEAK](https://peak.wiki.gg/wiki/Peak_(biome)), spawns a `FIRE_CLOUD`.
  - For anything else, spawns a [`POISON_CLOUD`](https://peak.wiki.gg/wiki/Tropics#Poison_Spore_Bomb).
- `BLINDS`: [Blinds](https://peak.wiki.gg/wiki/Flash_Bulbs) you for 30 seconds.
- `NUMBS`: [Numbs](https://peak.wiki.gg/wiki/Stamina_bar#Numbness) you for 30 seconds.
- `BAD_SHROOMBERRY`: Gives you a random bad [<img src="https://peak.wiki.gg/images/thumb/Green_Shroomberry.png/28px-Green_Shroomberry.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Shroomberry effect](https://peak.wiki.gg/wiki/Shroomberry#Effects).
- `TORNADO`: Spawns a strong [tornado](https://peak.wiki.gg/wiki/Tornado) on you for 12 seconds.
- `NO_FLARE`: Lose ALL your [<img src="https://peak.wiki.gg/images/thumb/Flare.png/28px-Flare.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> flare](https://peak.wiki.gg/wiki/Flare)
- `COOKED`: [Cooks](https://peak.wiki.gg/wiki/Cooking) your WHOLE [inventory](https://peak.wiki.gg/wiki/How_to_play#Inventory_&_Backpack).
- `SLIP`: Makes you slip on a [<img src="https://peak.wiki.gg/images/thumb/Pink_Berrynana_Peel.png/28px-Pink_Berrynana_Peel.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Berrynana Peel](https://peak.wiki.gg/wiki/Pink_Berrynana_Peel) .
- `W_KEY_STUCK`: Forces you to move forward for 60 seconds.
- `GO_BACK`: Uses your last [<img src="https://peak.wiki.gg/images/thumb/Checkpoint_Flag.png/28px-Checkpoint_Flag.png?c20300" decoding="async" loading="lazy" width="28" height="28" data-file-width="512" data-file-height="512"/> Checkpoint Flag](https://peak.wiki.gg/wiki/Checkpoint_Flag), or teleports you to the previous [campfire](https://peak.wiki.gg/wiki/Campfire) (or to the [crash site](https://peak.wiki.gg/wiki/Shore#Crash_Site) in the [SHORE](https://peak.wiki.gg/wiki/Shore)) if none were placed.
- `SET_FIRE`: Sets you on fire (If [Unnamed Products](https://thunderstore.io/c/peak/p/OracleTeam/UnnamedProducts/) is not active, defaults to `FIRE_CLOUD`)
- `FIREBALL`: Explodes a fireball on you (If [Unnamed Products](https://thunderstore.io/c/peak/p/OracleTeam/UnnamedProducts/) is not active, defaults to `FIRE_CLOUD`)
- `SPAWN_LUCKY_BLOCK`: Bonks a [Lucky Block](https://thunderstore.io/c/peak/p/legocool/Lucky_Blocks/) on your head. (If [Lucky Blocks](https://thunderstore.io/c/peak/p/legocool/Lucky_Blocks/) is not active, defaults to `RANDOM`)
- `UNNAMEIFY`: Turns ALL your [inventory](https://peak.wiki.gg/wiki/How_to_play#Inventory_&_Backpack) into unnamed variants. ( If [Unnamed Products](https://thunderstore.io/c/peak/p/OracleTeam/UnnamedProducts/) is not active, defaults to `RANDOM`)

##### Green Demon Amount #####
The amount of Green Demons that spawns in at once. (Max: 10)

##### Green Demon Delay #####
How long the demon will wait after spawning before starting to chase players.

##### Green Demon Mode #####
Sets how Green Demons spawns and despawns. Can be any of:
  - `NORMAL` _(default)_: Despawns when [campfire](https://peak.wiki.gg/wiki/Campfire) is lit.
  - `HARD`: Pauses at [campfire](https://peak.wiki.gg/wiki/Campfire), resumes when it goes out. If there are more green demons than `Green Demon Amount`, the mod will not spawn demons.
  - `VERY_HARD` : Same as `HARD` but demons always spawns at each biome.

##### Green Demon Speed #####
How fast the demon goes. There's 3 speed:
  - `SLOW` 
  - `MEDIUM`_(default)_
  - `FAST`

#### Biome Lobby Settings ####
- `Enable Shore`: Enables/Disables Green Demon spawns for [SHORE](https://peak.wiki.gg/wiki/Shore).
- `Enable Tropics`: Enables/Disables Green Demon spawns for [TROPICS](https://peak.wiki.gg/wiki/Tropics).
- `Enable Roots`: Enables/Disables Green Demon spawns for [ROOTS](https://peak.wiki.gg/wiki/Roots).
- `Enable Alpine`: Enables/Disables Green Demon spawns for [ALPINE](https://peak.wiki.gg/wiki/Alpine).
- `Enable Mesa`: Enables/Disables Green Demon spawns for [MESA](https://peak.wiki.gg/wiki/Mesa).
- `Enable Caldera`: Enables/Disables Green Demon spawns for [CALDERA](https://peak.wiki.gg/wiki/Caldera).
- `Enable The Kiln`: Enables/Disables Green Demon spawns for [THE KILN](https://peak.wiki.gg/wiki/The_Kiln).

### Client Settings ###
Aside from lobby settings, these settings only affect your personal experience:

#### Green Demon Volume ####
Adjust the chase music (set to `0` to mute)

#### Green Demon Tracker Mode ####
Changes how the HUD Green Demon tracker works:
- `ALWAYS`: Always visible.
- `OFFSCREEN`: Only when off-screen.
- `NEVER`: Disabled.

## 🐛 Issues ##

If you find any bugs or have any suggestion,
please [open an issue on the GitHub repository here](https://github.com/MiiMii1205/GreenDemonChallenge/issues/new).

Include as much info as possible, and we'll respond as soon as we can.