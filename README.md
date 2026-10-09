# SleddersMenu

A small MelonLoader mod for Sledders. Press **F1** in game to open a window that shows your own
snowmobile's values live and lets you set, freeze, or put them back to stock. No injector: it loads
like UnityExplorer does.

## Values

| Row | Game field | What it does |
|---|---|---|
| Power | `MeshInterpretter.power` | Engine power. Stock = horsepower x 782.73, the hp column shows the conversion |
| Power efficiency | `MeshInterpretter.powerEfficiency` | Straight multiplier on power (stock 1) |
| Throttle curve | `SnowmobileController.throttleExponent` | Throttle input is raised to this power |
| Limiter start | `MeshInterpretter.drivetrainMaxSpeed1` | Track speed (m/s) where power starts fading |
| Top speed | `MeshInterpretter.drivetrainMaxSpeed2` | Track speed (m/s) where power reaches zero |
| Launch cap | `MeshInterpretter.drivetrainMinSpeed` | Force = power / max(speed, this); lower launches harder |
| Brake force | `MeshInterpretter.breakForce` | Brake input x this |
| Track drag | `MeshInterpretter.frictionForce` | Constant track friction, added to braking |
| Snow grip | `MeshInterpretter.coefficientOfFriction` | Track friction coefficient |
| Lug height | `MeshInterpretter.lugHeight` | Track paddle bite in snow |
| Snow flotation | `MeshInterpretter.snowSupportForceFactor` | How hard snow pushes the sled up |
| Steering lock | `SnowmobileControllerBase.skisMaxAngle` | Max ski angle in degrees |
| Weight | `mainBody.mass` | Rigidbody mass in kg (includes the 58 kg rider) |

Plus **Refill fuel**, **Infinite fuel** (turns off `enableFuelConsumption`) and **Show trees**.

**Show trees** off disables the `TreeRenderer` object, the same as switching it off in UnityExplorer. That one object
draws every tree (GPU instanced) and spawns the hittable tree colliders near you as its children, so trees disappear
and you drive through them. It stays off across level loads, and ticking it again turns back on only what the menu
turned off. The game empties the Unity terrain trees, so there is no second tree system to hide.

- **Set** writes the box once. **Freeze** locks the box's value (the live value unless you typed one)
  and rewrites it every frame, so it survives respawns and sled swaps. **Stock** puts back the value
  from before the menu first touched this sled.
- The menu only touches the local player's sled (`SnowmobileController`); other players' sleds are
  `SnowmobileControllerRemote`.
- Fields are found by name through reflection. They are Unity-serialized, so they keep their names
  even though the rest of the game is obfuscated. If an update removes one, only that row greys out
  and a warning goes to the MelonLoader log.
- The menu key is `MenuKey` under `[SleddersMenu]` in `UserData/MelonPreferences.cfg`.

## Build and install

```
dotnet build -c Release
```

Then copy `bin/Release/SleddersMenu.dll` into `<Sledders>/Mods/`. The project references the game's
own DLLs from `E:\SteamLibrary\steamapps\common\Sledders`; pass `-p:GameDir=...` for another path.
Needs MelonLoader 0.7.x already installed in the game.
