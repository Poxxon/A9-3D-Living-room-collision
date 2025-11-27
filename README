## LivingRoom3D

A small OpenTK (C# / OpenGL 3.3) first-person scene with collision, interactions, and basic audio + UI overlays.

### Controls
- `W/A/S/D`: move
- Mouse: look
- `E`: interact when the crosshair turns orange
- `Esc`: quit

### Features
- First-person movement with collision against walls, furniture, and props.
- Interaction system (E) with console logs, audio cues, and floating text:
  - Door: “Knocking...”
  - Megaphone: “Hello..Can you hear me?” - "GIBERISH"
  - Sofa: “Don’t wanna sit”
  - Television: “*STATIC*”
  - Plant: “Water me”
- Crosshair that swaps between default/interactable textures and change up color when in range.
- Floating on-screen messages above the interacted object.
- Simple textured environment (floor, walls, door) and modeled furniture/props.

### How It Works
- Rendering: a single shader with MVP matrices; cubes for primitives plus imported glTF models. A shared cube VAO draws walls/floor/door; models use per-mesh VAOs/EBOs.
- Collision: axis-aligned bounding boxes for solids; a sphere-vs-AABB XZ check in `PlayerController` blocks movement. Triggers use separate colliders.
- Interaction: per-frame proximity check against trigger colliders; when within radius, the crosshair swaps, and `E` plays a sound, logs a message, and spawns a short-lived floating text billboard over the object.
- UI: crosshair and messages are drawn in screen space with blending disabled for depth and enabled for alpha; crosshair textures load from `Models/Crosshair`.
- Audio: `AudioService` (NAudio) loads MP3s and plays them on interaction; megaphone audio is trimmed to 3 seconds.

### Assets & Credits
- Door texture: [Poly Haven – rough_pine_door](https://polyhaven.com/a/rough_pine_door)
- Sofa model/texture: [Poly Haven – sofa_03](https://polyhaven.com/a/sofa_03)
- Television model/texture: [Poly Haven – Television_01](https://polyhaven.com/a/Television_01)
- Plant model/texture: [Poly Haven – potted_plant_01](https://polyhaven.com/a/potted_plant_01)
- Megaphone model/texture: [Poly Haven – Megaphone_01](https://polyhaven.com/a/Megaphone_01)
- Ottoman model/texture: [Poly Haven – Ottoman_01](https://polyhaven.com/a/Ottoman_01)
- Floor texture: [Poly Haven – wood_floor](https://polyhaven.com/a/wood_floor)
- Wall texture: [Poly Haven – plastered_wall](https://polyhaven.com/a/plastered_wall)
- Crosshair icons: [Flaticon – interactable dot](https://www.flaticon.com/free-icon/period_9664307?term=dot&page=1&position=96&origin=tag&related_id=9664307), [Flaticon – default dot](https://www.flaticon.com/free-icon/period_9455213?term=dot&page=1&position=74&origin=tag&related_id=9455213)
- Audio:
  - Door knock: [myinstants](https://www.myinstants.com/en/instant/door-knock-53634/)
  - Phone static: [myinstants](https://www.myinstants.com/en/instant/phone-static-38070/)
  - Megaphone loop: [Pixabay – loop-megaphone-voice-in-beijing-99892](https://pixabay.com/sound-effects/loop-megaphone-voice-in-beijing-99892/)

### Notes
- Built with .NET and OpenTK; requires GPU with OpenGL 3.3 support.
- Models/textures live under `Models/` and `Models/textures/`; audio under `Models/Audios/`.
- Interaction text rendering uses System.Drawing (Windows).
- Artificial intelligence assistance was used during development.
