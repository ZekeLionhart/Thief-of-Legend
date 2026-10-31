# Thief of Legend — AI review backlog

Initial review: October 25, 2026.

This backlog records the first inspection of the scripts, enemy prefab, scene, and animation setup. Findings have not yet been verified in Play Mode. No gameplay changes have been made as part of this review.

## How we will work

- Discuss one item at a time, explain the relevant code and proposed changes, and let the user implement them.
- Do not edit gameplay code or Unity assets without the user's permission.
- Keep the existing state-machine foundation and make focused improvements as needed.

## Intended behavior

The guard patrols between points. An initial sighting makes him pause, investigate Robin's last seen position, search, and eventually return to patrol. Seeing Robin again during the alert phase triggers pursuit and attacks when in range. Losing her should eventually end pursuit and return him to patrol; we should clarify the desired intervening investigation/search behavior when discussing that item.

## Behavior and correctness

- [ ] **AI-01 — Restrict pursuit to information the guard actually has.** `Pursue()` moves toward Robin's live position before checking visibility. During the lost-sight grace period, he can follow her behind cover. Discuss when movement should target Robin versus the remembered position.
- [ ] **AI-02 — Capture the actual last seen position.** On the first update without visibility, `CreateInspectionPoint()` reads Robin's position again. This can store a location the guard never witnessed. Update memory while she is visible and preserve it after visibility is lost.
- [ ] **AI-03 — Explicitly stop horizontal movement when appropriate.** Disabling movement prevents some velocity assignments but does not clear existing velocity. Detection, search, attack, and patrol waits need deliberate movement rules. The enemy Rigidbody currently has zero linear drag. Preserve vertical physics when deciding how to stop horizontal movement.
- [ ] **AI-04 — Investigate movement, animation, and facing getting out of sync.** User-observed symptom: the guard moves or slides while stuck in Idle, sometimes facing away from his travel direction. Check state transitions, velocity, movement flags, facing updates, and Animator parameters together. AI-03 and AI-05 may contribute, but the cause is not yet confirmed.
- [ ] **AI-05 — Clarify ownership between the AI state machine and Animator.** The script and Animator each track behavior states. The detection animation event calls `CallCheckState()`, while some Animator transitions wait for exit times after script behavior has already changed. Review how to keep decisions and presentation synchronized, and whether the script should own more of the transitions. Retain animation events where precise animation timing is useful, such as a weapon hit.
- [ ] **AI-06 — Reset the lost-sight timer on reacquisition.** `startPursueTime` is not reset when Robin becomes visible again. A later loss can inherit elapsed time from an earlier loss, shortening the next pursuit grace period.
- [ ] **AI-07 — Define and implement deliberate searching.** `Search()` currently waits for its timeout, while sight aims toward the inspection spot. The scripts do not explicitly perform a left/right look-around routine. Decide how the guard should scan, face, and react during the search.
- [ ] **AI-08 — Account for vertical separation when reaching an investigation target.** `IsWithinDistance()` ignores the target's height. A guard can consider himself at a target on another floor. Discuss arrival criteria and unreachable targets as the test level gains vertical complexity.
- [ ] **AI-09 — Restrict automatic wall jumping to appropriate situations.** `CheckWall()` runs before the state switch, including during detection, searching, and attacks. Decide which states permit jumping and whether additional grounding or obstacle checks are needed.

## Technical cleanup

- [ ] **AI-12 — Diagnose intermittent skipped downward jumps.** Reported October 29, 2026: guard patrols the upper platform, detects Robin to the lower left, ignores the left patrol point during investigation, and reaches the downward jump trigger. Sometimes he jumps; sometimes he runs through and falls. He subsequently searches, returns, and consistently uses the separate upward jump connection. Not reproduced by the assistant. Inspect rejection reasons before changing behavior: `canMove` can remain false after an interrupted patrol wait even though `Check()` moves unconditionally; the ground check has radius `0.01` and may miss support near the edge; jump pads are attempted only on trigger entry, so a rejected attempt is not retried while overlapping. Log grounded status, state, movement permission, jumping status, and height/speed rejection at the failed attempt. Distinguish no trigger callback, rejected launch, and a launch that immediately lands on the starting platform.

- [ ] **AI-10 — Use consistent units for movement speed.** Enemy movement assigns Rigidbody velocity multiplied by `Time.fixedDeltaTime`. This makes configured speeds depend on the physics timestep. Explain velocity versus displacement before changing it, and retune Inspector values if corrected.
- [ ] **AI-11 — Separate remembered positions from debug marker creation.** `CreateInspectionPoint()` both updates memory and instantiates an indicator. Visible pursuit can create one marker per physics update, each lasting three seconds. Keep memory updates independent from how often debug markers are displayed.

## Other prototype leftovers

- [ ] **PROJ-01 — Review pause and game-over scene references.** `GameManager` references pause, options, and game-over scenes absent from the inspected project. Decide what the prototype needs before restoring or removing this scaffolding.
- [ ] **PROJ-02 — Define the enemy damage behavior.** `DealDamage()` is empty. Discuss what happens when a guard lands an attack, including how that fits the intended nonlethal player experience.
- [ ] **PROJ-03 — Review the leftover Score field.** `PlayerController` imports Unity's social-platform namespace and declares a `Score` field that appears to be leftover scaffolding. Confirm its purpose before removing it.

## Suggested starting order

Start with AI-03 and AI-04 to investigate the visible sliding/Idle/facing problem, using AI-05 to trace animation transitions as needed. Then address AI-01, AI-02, and AI-06 together so pursuit and memory follow clear rules. Discuss the remaining items individually afterward.

The existing separation between `Enemy`, `EnemySight`, and `FieldOfView`, head/feet visibility checks, and per-patrol-point waiting times are useful foundations to preserve.
