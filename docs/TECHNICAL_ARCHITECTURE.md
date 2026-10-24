# Technical Architecture — P0
ตาม GDD §25 · ตรวจด้วย skill `unity-system-architecture`

## Assemblies
| asmdef | Path | อ้างอิง | หมายเหตุ |
|---|---|---|---|
| `DreamRush.Runtime` | `Assets/DreamRush/Scripts/Runtime` | Unity.InputSystem, Unity.AI.Navigation | gameplay ทั้งหมด |
| `DreamRush.Editor` | `Assets/DreamRush/Scripts/Editor` | DreamRush.Runtime | scene/prefab/material builders (graybox สร้างจากโค้ด → reproducible) |
| `DreamRush.Tests.EditMode` | `Assets/DreamRush/Tests/EditMode` | Runtime + test framework | logic ล้วน |
| `DreamRush.Tests.PlayMode` | `Assets/DreamRush/Tests/PlayMode` | Runtime + test framework | simulation กับ collider จริง |
| `DreamRush.DevBridge` | `Assets/DreamRush/Editor/DevBridge` | UnityEditor.TestRunner เท่านั้น | **ไม่อ้าง game code** เพื่อให้ยังทำงานได้แม้ game code compile พัง |

## Namespaces / Systems (P0)
```
DreamRush.Core        GameFlowManager, SaveManager(versioned JSON), Services, Palette
DreamRush.Player      PlayerMotor3D, JumpAssist, MovementMath, JumpMath, PlayerMotorConfig(SO),
                      PlayerInputReader, PlayerCommand, PlayerSquashStretch, AppleProceduralAnimator
DreamRush.CameraSystem ThirdPersonCameraController, StageCameraController, CameraModeDirector, MenuFocusCamera
DreamRush.Interaction IInteractable, InteractionSystem, InteractionPrompt
DreamRush.Dream       DreamTransitionController, DreamLevelManager, SleepStability
DreamRush.DreamLogic  DreamProperty flags, IEffectReceiver, StatusEffectSystem
DreamRush.Boss        BossChairController(FSM), ChairFatigue, EscapeDestinationSelector, ChairSeatInteraction,
                      IBossWinCondition, BossWinConditionEvaluator
DreamRush.Progression BedUpgradeManager, DreamDiary
DreamRush.UI          UIManager, prompt, HUD, settings, pause
DreamRush.Presentation BlobShadow, VFX, AudioManager
```

## หลักการ
1. **แยกความรับผิดชอบ** (GDD §25.4): input → `PlayerCommand` struct → motor; motor ไม่รู้จัก Input System; animation อ่านสถานะ motor ผ่าน event/property; กล้องไม่ขยับผู้เล่น
2. **Deterministic stepping**: `PlayerMotor3D.Step(dt, command)` เรียกจาก Update ปกติ แต่ test เรียกตรงด้วย dt คงที่ → test ซ้ำได้
3. **Data-driven**: ค่า tuning อยู่ใน ScriptableObject (`PlayerMotorConfig`, `ChairAIConfig`) — runtime state แยกจาก definition
4. **Effects ผ่าน properties** (GDD §18): `EffectContext` → `IEffectReceiver.CanReceive` (เช็ค `DreamProperty`) → status → boss reaction → win evaluator. ห้าม hard-code ชนะ. หมายเหตุ 2026-10-23: ระบบการ์ดถูกนำออกแล้ว receiver (`ChairEffectReceiver`, `ToyEffectReceiver`, `StoryMechanism`, `StoryPulseHazard`) ยังอยู่ในโค้ด แต่ตอนนี้ไม่มีระบบใดใน runtime ส่ง effect เข้ามา
5. **Physics queries ไม่ใช้ layer พิเศษ**: กรอง self ด้วย `transform.IsChildOf` เพื่อไม่ต้องแก้ TagManager (ลดความเปราะ)
6. **Graybox จากโค้ด**: Editor builders สร้าง scene/prefab/material → ลบแล้วสร้างใหม่ได้เสมอ, review ใน diff ได้

## Scenes (P0)
`Test_MovementGym` (M1) · `BedroomHub` (M2) · `Dream_Chair` (M4) · Bootstrap จะเพิ่มเมื่อมี GameFlow (M3)

## Save (M2+)
`Application.persistentDataPath/dreamrush_save.json` · `{ "version": 1, ... }` · migration ต่อ version · เขียนแบบ temp file + replace
