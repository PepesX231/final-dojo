# THE CHAIR THAT REFUSED — Boss Design (boss-encounter-design review)
GDD §11–16, §18, §20.4 · Source of truth = GDD; ข้อเพิ่มจาก pitch ผู้ใช้ระบุไว้ชัด

## Design Review (game-design-review)
- **Player problem**: "จะนั่งบนสิ่งที่วิ่งหนีได้อย่างไร" — ปัญหาเชิงพื้นที่ ไม่ใช่ HP
- **Meaningful choice**: ไล่ตรง ๆ จนเหนื่อย (ปลอดภัยแต่นาน) vs ขึ้น balcony ดักหน้า (เสี่ยงพลาด) vs ดันบล็อกปิดทาง (ต้องวางแผน) — ระบบการ์ดถูกนำออก 2026-10-23
- **Dominant strategy check**: sprint เร็วกว่าเก้าอี้ปกติ (8 > 6.1) แต่เก้าอี้เลือกทางหนีที่อ้อมสิ่งกีดขวาง + panic 7.2 → ไล่ตรงอย่างเดียวได้ผลช้า; ทุก opening ยังต้อง "เดินไปกด E แล้วนั่งค้าง 2 วิ"
- **Readability**: ทุก state มีภาษากายเฉพาะ (ตาโต/สั่น/ล้ม/งอตัวหอบ) + "!" ตอนเห็นผู้เล่น + prompt SIT เฉพาะตอนนั่งได้
- **Acceptance**: ชนะ baseline ได้ด้วยการไล่/ใช้ฉาก/นั่งเอง, bot 50 seeds ไม่มี softlock/ออกนอก arena, victory ครั้งเดียว

## State Machine
```text
            awareness 7m                  escape 4m / 0.4s
   IDLE ───────────────► ALERT ─────────────────────────► FLEE ◄──────────┐
    ▲   ◄─── player gone 2s ┘                              │  │  arrive / 1.2s   │
    │                                                       │  └──► REPOSITION ─┤ (hesitate/look back = opening)
    │                                      player ≤2.5m >1s │                   │
    │                                      or cornered      ▼                   │
    │                                                     PANIC ────────────────┘ (7.2 m/s, ×2 fatigue)
    │   FLEE/PANIC: sharp turn / shortcut landing / cornered ──► STUMBLE (1.5s)   ┐
    │   fatigue ≥ 18 ──────────────────────────────────────────► EXHAUSTED (3s)  ├─ sit-compatible
    │   effects (M6) ──────────────────────────────────────────► ROOTED / FROZEN  │  (SIT_AVAILABLE when
    │   social effect (M6) ─────────────────────────────────────► OFFER_SEAT      ┘   player in range)
    │                                                      [E] + validation
    └──── recovers → FLEE ◄──── opening ends ────────── SITTING (2.0s continuous) ──► DEFEATED → SWEET DREAMS
```

## Escape destination scoring (§15.5) — `EscapeDestinationSelector`
1. Candidates = NavMesh grid points in the arena (every 2.5 m)
2. Reject: unreachable path, < 5 m or > 11 m from chair, < 4 m from player, path passes within 2 m of player
3. Score = distance from player + "away" alignment − detour penalty − recency penalty − corner penalty
4. Pick weighted-random among the top 3 (not always optimal — §15.6), seeded per run

## Openings (fairness §15.6)
| Opening | Trigger | Victory id |
|---|---|---|
| Exhausted 3 s | fatigue ≥ 18 (running +1/s, panic +2/s, resting −1.5/s) | Exhaustion |
| Stumble 1.5 s | sharp turn > 110° in PANIC (35%) | Exhaustion (chase) |
| Stumble (startled) | player lands within 3 m in front after falling ≥ 1 m | Shortcut |
| Cornered → stumble | no valid escape destination while player ≤ 4 m (blocked by walls/pushed block) | Obstacle |
| Hesitation 0.5–0.9 s | after reaching a destination when player is not close (look back) | — (not sit-compatible; lets player close distance) |

## Anti-softlock (§15.8)
off NavMesh / below y −2 / outside arena bounds / stuck (no progress 2 s with a path) → visible "poof" + reappear far from the player with a "!" (never silently in front of the player)

## Pitch additions (ผู้ใช้)
- **Reveal**: เก้าอี้พุ่งมา "เตะนิ้วก้อยเท้า" (knockback เล็ก + "OUCH!") แล้ววิ่งหนี → title THE CHAIR / SIT ON THE CHAIR
- **ชนสิ่งกีดขวาง/ตกฉาก = เสียระยะ**: Sleep Stability −1 และเก้าอี้ได้พักหายเหนื่อย (fatigue −30%)
