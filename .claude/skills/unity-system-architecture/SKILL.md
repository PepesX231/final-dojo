---
name: unity-system-architecture
description: Review DREAM RUSH C# code and subsystem boundaries — assemblies, data-driven ScriptableObjects, testability, scene lifecycle, events, memory, error handling and prefab management. Use after writing or changing any runtime system and before closing a milestone.
---

# Unity System Architecture Review — DREAM RUSH

## Relevant files
`docs/TECHNICAL_ARCHITECTURE.md` · `Assets/DreamRush/Scripts/**` · asmdefs · `Assets/DreamRush/Tests/**`

## Checklist
**Boundaries (GDD §25.4)**
- [ ] Input, movement, animation, camera, environmental effects, boss behaviour, win conditions and save data live in separate classes
- [ ] No class > ~400 lines without a reason; no "god" manager
- [ ] Runtime asmdef does not reference UnityEditor (guard with `#if UNITY_EDITOR` only in rare cases)
- [ ] DevBridge does not reference game assemblies

**Data**
- [ ] Authoring data in ScriptableObjects; runtime state (cooldowns, statuses, fatigue) in plain classes/instances — never mutate SO assets at runtime
- [ ] Save is versioned JSON with migration path; written atomically

**Testability**
- [ ] Core logic is pure C# or steppable (`Step(dt, …)`) with injectable inputs/randomness (seeded `System.Random`)
- [ ] New logic has EditMode tests; physics/navigation behaviour has PlayMode tests

**Lifecycle & events**
- [ ] Every `+=` has a matching `-=` (OnEnable/OnDisable); no static events holding scene objects
- [ ] Works after scene reload and after returning from a dream (no stale references, no duplicate singletons)
- [ ] No `FindObjectOfType` in Update; cache references

**Performance & memory**
- [ ] No per-frame allocations in hot paths (use NonAlloc physics, cached lists)
- [ ] Runtime-created materials/textures are destroyed or created once

**Errors**
- [ ] Missing references fail loudly once (log with context) and degrade safely; no silent NullReference spam

## Procedure
1. Read the changed files fully.
2. Run the checklist; for each failure give file:line and the fix.
3. Confirm compile clean (`DevBridge/out/compile.log`) and tests green.

## Output
```
### Architecture Review: <scope>
Pass: ...
Issues (priority): [P1] file:line — problem — fix
Required changes before milestone close: ...
```
