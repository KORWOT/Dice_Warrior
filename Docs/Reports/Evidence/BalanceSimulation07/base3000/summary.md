# B0/C7 balance simulation summary

Status: **PASS**. Requested 3000; executed 3000; valid completed 3000; failed 0; NOT_RUN 0.

Scenario `v4-baseline-neutral-v1`; policy `neutral-v1`; rules `shared-world-preview-4`; Git `2420decbaea896973b8fd62110cc0cd54aa9a530`.
Content SHA256 `06DF1643383740F8D60551952CC36FAA1D5A0E130FDA7FB3A85D5632E11E26BA`; seed-set SHA256 `243B664E1AAE8528FF1869CC61242DC1E8DACAEB46952D9B3EF229F06EB50A0D`; source SHA256 `DD7E292F71ED7575B56E73A966526839370CD6C7C58855DFBBCF2923DE3B0EFD`.
Growth 0; die `balanced`; weapon `blade`; master seed 2965831681.

Completion per attempted match: 100%; Wilson 95% [99.872%, 100%]; n=3000, failed=0, NOT_RUN=0, missing=0.

## Interpretation contract

- Results describe the fixed neutral-v1 automated human policy, not human player skill or universal adventurer strength.
- AI results are fixed slot/adventurer/policy combinations, stratified by the human adventurer. Adventurer and AI policy effects are confounded.
- Participant outcome denominators include only valid completed matches. Failures and unattempted matches are disclosed separately, never converted to losses or zero measurements.
- Wilson intervals apply to binomial trial/elimination/rank1 proportions. Fractional first-place scores are not Bernoulli outcomes and do not use Wilson intervals.
- The four participants in a match and repeated human scenarios sharing a seed are dependent. Paired comparisons resample complete seed blocks, not participant rows.
- Paired intervals are unadjusted exploratory 95% intervals across multiple metrics and three hero pairs; post-hoc selection and multiplicity prohibit automatic balance pass/fail conclusions.
- Current absolute HP participates in ranking. HP/maxHP is reported separately. XP is remaining XP, not cumulative XP.
- Descriptive percentiles use nearest rank. Missing measurements are null/NOT_MEASURED; game failures and NOT_RUN matches are separate categories.
- Technical PASS means the requested experiment completed. It does not establish acceptable game balance; no tuning or sensitivity experiment was performed.

## Human final outcomes

### wanderer / human / wanderer

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 967 | 1000 | 96.7% | [95.402%, 97.641%] | 0 | 0 | 0 |
| eliminated | 33 | 1000 | 3.3% | [2.359%, 4.598%] | 0 | 0 | 0 |
| rank1 | 193 | 1000 | 19.3% | [16.973%, 21.862%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.189 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.629 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 2.903 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 65.787 | 0 | 40 | 59 | 65 | 78 | 78 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 77.202 | 60 | 72 | 78 | 78 | 78 | 78 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.846934 | 0 | 0.555556 | 0.75641 | 0.833333 | 1 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.867 | 1 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.505 | 0 | 1 | 7 | 13 | 20 | 27 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.291 | 0 | 10 | 13 | 15 | 16 | 19 | 23 | 0 | 0 |
| gold | 1000 | 0 | 134.633 | 0 | 76 | 116 | 137 | 158 | 187 | 247 | 0 | 0 |
| maxEncounterTurns | 1000 | 0 | 6.549 | 3 | 4 | 5 | 6 | 8 | 12 | 17 | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 193; 2: 265; 3: 262; 4: 280 | 1000 | 0 | 0 | 0 |
| laps | 0: 31; 1: 2; 2: 0; 3: 967 | 1000 | 0 | 0 | 0 |
| eliminationReason | combat-hp-depleted: 33; not-eliminated: 967 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 33; succeeded: 967; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### scholar / human / scholar

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 973 | 1000 | 97.3% | [96.1%, 98.138%] | 0 | 0 | 0 |
| eliminated | 27 | 1000 | 2.7% | [1.862%, 3.9%] | 0 | 0 | 0 |
| rank1 | 199 | 1000 | 19.9% | [17.543%, 22.488%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.195333 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.605 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 2.92 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 66.139 | 0 | 40 | 59 | 69 | 78 | 78 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 77.274 | 60 | 72 | 78 | 78 | 78 | 78 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.851675 | 0 | 0.555556 | 0.75641 | 0.902778 | 1 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.879 | 1 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.564 | 0 | 1 | 7 | 14 | 20 | 27 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.366 | 0 | 10 | 13 | 15 | 16 | 19 | 23 | 0 | 0 |
| gold | 1000 | 0 | 135.39 | 0 | 81 | 117 | 137 | 158 | 188 | 247 | 0 | 0 |
| maxEncounterTurns | 1000 | 0 | 6.494 | 3 | 4 | 5 | 6 | 8 | 12 | 19 | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 199; 2: 271; 3: 256; 4: 274 | 1000 | 0 | 0 | 0 |
| laps | 0: 26; 1: 1; 2: 0; 3: 973 | 1000 | 0 | 0 | 0 |
| eliminationReason | combat-hp-depleted: 27; not-eliminated: 973 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 27; succeeded: 973; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### vanguard / human / vanguard

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 980 | 1000 | 98% | [96.931%, 98.702%] | 0 | 0 | 0 |
| eliminated | 20 | 1000 | 2% | [1.298%, 3.069%] | 0 | 0 | 0 |
| rank1 | 213 | 1000 | 21.3% | [18.875%, 23.945%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.2125 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.547 | 1 | 1 | 2 | 3 | 3 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 2.94 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 77.319 | 0 | 52 | 71 | 77 | 90 | 90 | 96 | 0 | 0 |
| maxHp | 1000 | 0 | 89.382 | 72 | 84 | 90 | 90 | 90 | 90 | 96 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.86253 | 0 | 0.577778 | 0.788889 | 0.855556 | 1 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.897 | 1 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.685 | 0 | 1 | 7 | 14 | 20 | 27 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.474 | 1 | 10 | 13 | 15 | 16 | 19 | 23 | 0 | 0 |
| gold | 1000 | 0 | 136.426 | 12 | 81 | 117 | 137 | 158 | 188 | 247 | 0 | 0 |
| maxEncounterTurns | 1000 | 0 | 7.462 | 3 | 4 | 5 | 7 | 9 | 13 | 21 | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 213; 2: 276; 3: 262; 4: 249 | 1000 | 0 | 0 | 0 |
| laps | 0: 20; 1: 0; 2: 0; 3: 980 | 1000 | 0 | 0 | 0 |
| eliminationReason | combat-hp-depleted: 20; not-eliminated: 980 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 20; succeeded: 980; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

## AI fixed combinations, stratified by human scenario

AI adventurer and policy cannot be separated by this experiment. No pooled independent-participant win rate is estimated.

### wanderer / ai-1 / wanderer / war

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 963 | 1000 | 96.3% | [94.942%, 97.304%] | 0 | 0 | 0 |
| eliminated | 37 | 1000 | 3.7% | [2.696%, 5.058%] | 0 | 0 | 0 |
| rank1 | 598 | 1000 | 59.8% | [56.729%, 62.796%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.595 | 0 | 0 | 0 | 1 | 1 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 1.709 | 1 | 1 | 1 | 1 | 2 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 2.89 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 60.801 | 0 | 28 | 53 | 64 | 76 | 80 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 79.002 | 60 | 78 | 78 | 78 | 84 | 84 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.764772 | 0 | 0.358974 | 0.666667 | 0.807692 | 0.952381 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 4.167 | 1 | 4 | 4 | 4 | 5 | 5 | 5 | 0 | 0 |
| xp | 1000 | 0 | 15.541 | 0 | 1 | 7 | 16 | 23 | 30 | 31 | 0 | 0 |
| honor | 1000 | 0 | 16.695 | 0 | 12 | 15 | 17 | 19 | 22 | 26 | 0 | 0 |
| gold | 1000 | 0 | 159.937 | 0 | 89 | 138 | 164 | 188 | 219 | 271 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 598; 2: 189; 3: 119; 4: 94 | 1000 | 0 | 0 | 0 |
| laps | 0: 36; 1: 1; 2: 0; 3: 963 | 1000 | 0 | 0 | 0 |
| eliminationReason | combat-hp-depleted: 37; not-eliminated: 963 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 37; succeeded: 963; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### wanderer / ai-2 / scholar / wisdom

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 1000 | 1000 | 100% | [99.617%, 100%] | 0 | 0 | 0 |
| eliminated | 0 | 1000 | 0% | [0%, 0.383%] | 0 | 0 | 0 |
| rank1 | 129 | 1000 | 12.9% | [10.964%, 15.12%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.123 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.714 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 69.527 | 40 | 57 | 59 | 74 | 78 | 78 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 77.934 | 72 | 72 | 78 | 78 | 78 | 78 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.891876 | 0.512821 | 0.730769 | 0.77381 | 0.961538 | 1 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.989 | 3 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.595 | 0 | 1 | 8 | 13 | 19 | 26 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.388 | 9 | 11 | 13 | 14 | 16 | 18 | 21 | 0 | 0 |
| gold | 1000 | 0 | 93.086 | 44 | 63 | 81 | 93 | 106 | 123 | 144 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 129; 2: 303; 3: 293; 4: 275 | 1000 | 0 | 0 | 0 |
| laps | 0: 0; 1: 0; 2: 0; 3: 1000 | 1000 | 0 | 0 | 0 |
| eliminationReason | not-eliminated: 1000 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 0; succeeded: 1000; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### wanderer / ai-3 / vanguard / luck

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 1000 | 1000 | 100% | [99.617%, 100%] | 0 | 0 | 0 |
| eliminated | 0 | 1000 | 0% | [0%, 0.383%] | 0 | 0 | 0 |
| rank1 | 93 | 1000 | 9.3% | [7.652%, 11.259%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.093 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.903 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 64.698 | 13 | 35 | 54 | 66 | 77 | 90 | 96 | 0 | 0 |
| maxHp | 1000 | 0 | 88.92 | 78 | 84 | 90 | 90 | 90 | 90 | 96 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.728093 | 0.144444 | 0.392857 | 0.604167 | 0.75 | 0.855556 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.82 | 2 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.118 | 0 | 1 | 7 | 13 | 19 | 26 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.1 | 9 | 11 | 13 | 14 | 15 | 18 | 22 | 0 | 0 |
| gold | 1000 | 0 | 169.115 | 45 | 105 | 142 | 169 | 194 | 236 | 333 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 93; 2: 251; 3: 316; 4: 340 | 1000 | 0 | 0 | 0 |
| laps | 0: 0; 1: 0; 2: 0; 3: 1000 | 1000 | 0 | 0 | 0 |
| eliminationReason | not-eliminated: 1000 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 0; succeeded: 1000; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### scholar / ai-1 / wanderer / war

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 963 | 1000 | 96.3% | [94.942%, 97.304%] | 0 | 0 | 0 |
| eliminated | 37 | 1000 | 3.7% | [2.696%, 5.058%] | 0 | 0 | 0 |
| rank1 | 596 | 1000 | 59.6% | [56.528%, 62.599%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.591833 | 0 | 0 | 0 | 1 | 1 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 1.714 | 1 | 1 | 1 | 1 | 2 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 2.89 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 60.801 | 0 | 28 | 53 | 64 | 76 | 80 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 79.002 | 60 | 78 | 78 | 78 | 84 | 84 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.764772 | 0 | 0.358974 | 0.666667 | 0.807692 | 0.952381 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 4.167 | 1 | 4 | 4 | 4 | 5 | 5 | 5 | 0 | 0 |
| xp | 1000 | 0 | 15.541 | 0 | 1 | 7 | 16 | 23 | 30 | 31 | 0 | 0 |
| honor | 1000 | 0 | 16.695 | 0 | 12 | 15 | 17 | 19 | 22 | 26 | 0 | 0 |
| gold | 1000 | 0 | 159.937 | 0 | 89 | 138 | 164 | 188 | 219 | 271 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 596; 2: 190; 3: 118; 4: 96 | 1000 | 0 | 0 | 0 |
| laps | 0: 36; 1: 1; 2: 0; 3: 963 | 1000 | 0 | 0 | 0 |
| eliminationReason | combat-hp-depleted: 37; not-eliminated: 963 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 37; succeeded: 963; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### scholar / ai-2 / scholar / wisdom

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 1000 | 1000 | 100% | [99.617%, 100%] | 0 | 0 | 0 |
| eliminated | 0 | 1000 | 0% | [0%, 0.383%] | 0 | 0 | 0 |
| rank1 | 124 | 1000 | 12.4% | [10.5%, 14.588%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.119833 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.727 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 69.527 | 40 | 57 | 59 | 74 | 78 | 78 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 77.934 | 72 | 72 | 78 | 78 | 78 | 78 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.891876 | 0.512821 | 0.730769 | 0.77381 | 0.961538 | 1 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.989 | 3 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.595 | 0 | 1 | 8 | 13 | 19 | 26 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.388 | 9 | 11 | 13 | 14 | 16 | 18 | 21 | 0 | 0 |
| gold | 1000 | 0 | 93.086 | 44 | 63 | 81 | 93 | 106 | 123 | 144 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 124; 2: 298; 3: 305; 4: 273 | 1000 | 0 | 0 | 0 |
| laps | 0: 0; 1: 0; 2: 0; 3: 1000 | 1000 | 0 | 0 | 0 |
| eliminationReason | not-eliminated: 1000 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 0; succeeded: 1000; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### scholar / ai-3 / vanguard / luck

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 1000 | 1000 | 100% | [99.617%, 100%] | 0 | 0 | 0 |
| eliminated | 0 | 1000 | 0% | [0%, 0.383%] | 0 | 0 | 0 |
| rank1 | 93 | 1000 | 9.3% | [7.652%, 11.259%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.093 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.908 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 64.698 | 13 | 35 | 54 | 66 | 77 | 90 | 96 | 0 | 0 |
| maxHp | 1000 | 0 | 88.92 | 78 | 84 | 90 | 90 | 90 | 90 | 96 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.728093 | 0.144444 | 0.392857 | 0.604167 | 0.75 | 0.855556 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.82 | 2 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.118 | 0 | 1 | 7 | 13 | 19 | 26 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.1 | 9 | 11 | 13 | 14 | 15 | 18 | 22 | 0 | 0 |
| gold | 1000 | 0 | 169.115 | 45 | 105 | 142 | 169 | 194 | 236 | 333 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 93; 2: 250; 3: 313; 4: 344 | 1000 | 0 | 0 | 0 |
| laps | 0: 0; 1: 0; 2: 0; 3: 1000 | 1000 | 0 | 0 | 0 |
| eliminationReason | not-eliminated: 1000 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 0; succeeded: 1000; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### vanguard / ai-1 / wanderer / war

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 963 | 1000 | 96.3% | [94.942%, 97.304%] | 0 | 0 | 0 |
| eliminated | 37 | 1000 | 3.7% | [2.696%, 5.058%] | 0 | 0 | 0 |
| rank1 | 584 | 1000 | 58.4% | [55.319%, 61.417%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.581 | 0 | 0 | 0 | 1 | 1 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 1.733 | 1 | 1 | 1 | 1 | 2 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 2.89 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 60.801 | 0 | 28 | 53 | 64 | 76 | 80 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 79.002 | 60 | 78 | 78 | 78 | 84 | 84 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.764772 | 0 | 0.358974 | 0.666667 | 0.807692 | 0.952381 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 4.167 | 1 | 4 | 4 | 4 | 5 | 5 | 5 | 0 | 0 |
| xp | 1000 | 0 | 15.541 | 0 | 1 | 7 | 16 | 23 | 30 | 31 | 0 | 0 |
| honor | 1000 | 0 | 16.695 | 0 | 12 | 15 | 17 | 19 | 22 | 26 | 0 | 0 |
| gold | 1000 | 0 | 159.937 | 0 | 89 | 138 | 164 | 188 | 219 | 271 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 584; 2: 197; 3: 121; 4: 98 | 1000 | 0 | 0 | 0 |
| laps | 0: 36; 1: 1; 2: 0; 3: 963 | 1000 | 0 | 0 | 0 |
| eliminationReason | combat-hp-depleted: 37; not-eliminated: 963 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 37; succeeded: 963; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### vanguard / ai-2 / scholar / wisdom

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 1000 | 1000 | 100% | [99.617%, 100%] | 0 | 0 | 0 |
| eliminated | 0 | 1000 | 0% | [0%, 0.383%] | 0 | 0 | 0 |
| rank1 | 119 | 1000 | 11.9% | [10.038%, 14.054%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.116 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.765 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 69.527 | 40 | 57 | 59 | 74 | 78 | 78 | 84 | 0 | 0 |
| maxHp | 1000 | 0 | 77.934 | 72 | 72 | 78 | 78 | 78 | 78 | 84 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.891876 | 0.512821 | 0.730769 | 0.77381 | 0.961538 | 1 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.989 | 3 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.595 | 0 | 1 | 8 | 13 | 19 | 26 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.388 | 9 | 11 | 13 | 14 | 16 | 18 | 21 | 0 | 0 |
| gold | 1000 | 0 | 93.086 | 44 | 63 | 81 | 93 | 106 | 123 | 144 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 119; 2: 292; 3: 294; 4: 295 | 1000 | 0 | 0 | 0 |
| laps | 0: 0; 1: 0; 2: 0; 3: 1000 | 1000 | 0 | 0 | 0 |
| eliminationReason | not-eliminated: 1000 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 0; succeeded: 1000; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

### vanguard / ai-3 / vanguard / luck

| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |
|---|---:|---:|---:|---|---:|---:|---:|
| trialSucceeded | 1000 | 1000 | 100% | [99.617%, 100%] | 0 | 0 | 0 |
| eliminated | 0 | 1000 | 0% | [0%, 0.383%] | 0 | 0 | 0 |
| rank1 | 91 | 1000 | 9.1% | [7.47%, 11.043%] | 0 | 0 | 0 |

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| fractional1st | 1000 | 0 | 0.0905 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| rank | 1000 | 0 | 2.94 | 1 | 1 | 2 | 3 | 4 | 4 | 4 | 0 | 0 |
| laps | 1000 | 0 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 0 | 0 |
| hp | 1000 | 0 | 64.698 | 13 | 35 | 54 | 66 | 77 | 90 | 96 | 0 | 0 |
| maxHp | 1000 | 0 | 88.92 | 78 | 84 | 90 | 90 | 90 | 90 | 96 | 0 | 0 |
| hpRatio | 1000 | 0 | 0.728093 | 0.144444 | 0.392857 | 0.604167 | 0.75 | 0.855556 | 1 | 1 | 0 | 0 |
| level | 1000 | 0 | 3.82 | 2 | 3 | 4 | 4 | 4 | 4 | 5 | 0 | 0 |
| xp | 1000 | 0 | 13.118 | 0 | 1 | 7 | 13 | 19 | 26 | 31 | 0 | 0 |
| honor | 1000 | 0 | 14.1 | 9 | 11 | 13 | 14 | 15 | 18 | 22 | 0 | 0 |
| gold | 1000 | 0 | 169.115 | 45 | 105 | 142 | 169 | 194 | 236 | 333 | 0 | 0 |
| maxEncounterTurns | 0 | 1000 | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| rank | 1: 91; 2: 232; 3: 323; 4: 354 | 1000 | 0 | 0 | 0 |
| laps | 0: 0; 1: 0; 2: 0; 3: 1000 | 1000 | 0 | 0 | 0 |
| eliminationReason | not-eliminated: 1000 | 1000 | 0 | 0 | 0 |
| finalTrialStatus | eliminated-at-final-trial: 0; not-reached: 0; succeeded: 1000; unfinished-at-final-trial: 0 | 1000 | 0 | 0 | 0 |

## Match distributions

### Human wanderer

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| worldRound | 1000 | 0 | 13.423 | 10 | 11 | 12 | 13 | 14 | 16 | 18 | 0 | 0 |
| humanCommands | 1000 | 0 | 43.848 | 25 | 33 | 39 | 43 | 49 | 56 | 70 | 0 | 0 |
| elapsedMs | 1000 | 0 | 24.224189 | 11.5863 | 17.8827 | 20.9191 | 23.2426 | 25.9682 | 32.4713 | 154.3486 | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| endReason | all-complete: 1000; round-limit: 0 | 1000 | 0 | 0 | 0 |
| trialWinnerCount | 0: 0; 1: 0; 2: 2; 3: 66; 4: 932 | 1000 | 0 | 0 | 0 |
| rank1Count | 1: 987; 2: 13; 3: 0; 4: 0 | 1000 | 0 | 0 | 0 |
| rankPattern | 1/1/3/4: 13; 1/2/2/4: 21; 1/2/3/3: 11; 1/2/3/4: 955 | 1000 | 0 | 0 | 0 |
| hasRankTie | no: 955; yes: 45 | 1000 | 0 | 0 | 0 |
| humanHpTiebreakExposure | no: 751; yes: 249 | 1000 | 0 | 0 | 0 |

### Human scholar

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| worldRound | 1000 | 0 | 13.427 | 10 | 11 | 12 | 13 | 14 | 16 | 18 | 0 | 0 |
| humanCommands | 1000 | 0 | 43.839 | 25 | 33 | 39 | 43 | 48 | 56 | 70 | 0 | 0 |
| elapsedMs | 1000 | 0 | 24.253099 | 15.03 | 18.147 | 20.9712 | 23.334 | 26.1998 | 32.4407 | 124.053 | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| endReason | all-complete: 1000; round-limit: 0 | 1000 | 0 | 0 | 0 |
| trialWinnerCount | 0: 0; 1: 0; 2: 0; 3: 64; 4: 936 | 1000 | 0 | 0 | 0 |
| rank1Count | 1: 989; 2: 10; 3: 1; 4: 0 | 1000 | 0 | 0 | 0 |
| rankPattern | 1/1/1/4: 1; 1/1/3/4: 10; 1/2/2/4: 20; 1/2/3/3: 13; 1/2/3/4: 956 | 1000 | 0 | 0 | 0 |
| hasRankTie | no: 956; yes: 44 | 1000 | 0 | 0 | 0 |
| humanHpTiebreakExposure | no: 749; yes: 251 | 1000 | 0 | 0 | 0 |

### Human vanguard

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| worldRound | 1000 | 0 | 13.431 | 10 | 11 | 12 | 13 | 14 | 16 | 18 | 0 | 0 |
| humanCommands | 1000 | 0 | 46.098 | 26 | 34 | 41 | 46 | 51 | 59 | 73 | 0 | 0 |
| elapsedMs | 1000 | 0 | 24.700503 | 14.0781 | 18.4138 | 21.3291 | 23.8799 | 26.8083 | 32.8678 | 116.5987 | 0 | 0 |

| Metric | Value: count | n | Missing | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| endReason | all-complete: 1000; round-limit: 0 | 1000 | 0 | 0 | 0 |
| trialWinnerCount | 0: 0; 1: 0; 2: 0; 3: 57; 4: 943 | 1000 | 0 | 0 | 0 |
| rank1Count | 1: 993; 2: 7; 3: 0; 4: 0 | 1000 | 0 | 0 | 0 |
| rankPattern | 1/1/3/4: 7; 1/2/2/4: 4; 1/2/3/3: 4; 1/2/3/4: 985 | 1000 | 0 | 0 | 0 |
| hasRankTie | no: 985; yes: 15 | 1000 | 0 | 0 | 0 |
| humanHpTiebreakExposure | no: 723; yes: 277 | 1000 | 0 | 0 | 0 |

## Paired seed-block comparisons

complete seed blocks; mean(left-right); xorshift32-13-17-5 with rejection sampling; stable FNV-derived metric seeds; percentile nearest-rank 2.5%/97.5%; replicates=2000; master bootstrap seed=2965831681. Positive values mean left minus right. All three hero matches must be valid for a block to enter any comparison. Proportion differences below are fractions, so 0.01 means one percentage point.

| Left | Right | Metric | Complete seed blocks | Excluded blocks | Mean difference | Bootstrap 95% | Batch failures | Batch NOT_RUN |
|---|---|---|---:|---:|---:|---|---:|---:|
| wanderer | scholar | fractional1st | 1000 | 0 | -0.006333 | [-0.017, 0.003833] | 0 | 0 |
| wanderer | scholar | rank | 1000 | 0 | 0.024 | [-0.008, 0.059] | 0 | 0 |
| wanderer | scholar | laps | 1000 | 0 | -0.017 | [-0.057, 0.022] | 0 | 0 |
| wanderer | scholar | hp | 1000 | 0 | -0.352 | [-1.475, 0.785] | 0 | 0 |
| wanderer | scholar | maxHp | 1000 | 0 | -0.072 | [-0.27, 0.126] | 0 | 0 |
| wanderer | scholar | hpRatio | 1000 | 0 | -0.004741 | [-0.018952, 0.009732] | 0 | 0 |
| wanderer | scholar | level | 1000 | 0 | -0.012 | [-0.044, 0.021] | 0 | 0 |
| wanderer | scholar | xp | 1000 | 0 | -0.059 | [-0.249, 0.133] | 0 | 0 |
| wanderer | scholar | honor | 1000 | 0 | -0.075 | [-0.26, 0.099] | 0 | 0 |
| wanderer | scholar | gold | 1000 | 0 | -0.757 | [-2.57, 1.023] | 0 | 0 |
| wanderer | scholar | trialSucceeded | 1000 | 0 | -0.006 | [-0.019, 0.006] | 0 | 0 |
| wanderer | scholar | eliminated | 1000 | 0 | 0.006 | [-0.006, 0.019] | 0 | 0 |
| wanderer | scholar | rank1 | 1000 | 0 | -0.006 | [-0.017, 0.005] | 0 | 0 |
| wanderer | scholar | worldRound | 1000 | 0 | -0.004 | [-0.018, 0.008] | 0 | 0 |
| wanderer | scholar | humanCommands | 1000 | 0 | 0.009 | [-0.309, 0.351] | 0 | 0 |
| wanderer | vanguard | fractional1st | 1000 | 0 | -0.0235 | [-0.034, -0.014] | 0 | 0 |
| wanderer | vanguard | rank | 1000 | 0 | 0.082 | [0.056, 0.109] | 0 | 0 |
| wanderer | vanguard | laps | 1000 | 0 | -0.037 | [-0.065, -0.011] | 0 | 0 |
| wanderer | vanguard | hp | 1000 | 0 | -11.532 | [-12.494, -10.627] | 0 | 0 |
| wanderer | vanguard | maxHp | 1000 | 0 | -12.18 | [-12.324, -12.054] | 0 | 0 |
| wanderer | vanguard | hpRatio | 1000 | 0 | -0.015596 | [-0.027342, -0.004283] | 0 | 0 |
| wanderer | vanguard | level | 1000 | 0 | -0.03 | [-0.052, -0.009] | 0 | 0 |
| wanderer | vanguard | xp | 1000 | 0 | -0.18 | [-0.311, -0.072] | 0 | 0 |
| wanderer | vanguard | honor | 1000 | 0 | -0.183 | [-0.309, -0.065] | 0 | 0 |
| wanderer | vanguard | gold | 1000 | 0 | -1.793 | [-3.075, -0.598] | 0 | 0 |
| wanderer | vanguard | trialSucceeded | 1000 | 0 | -0.013 | [-0.023, -0.004] | 0 | 0 |
| wanderer | vanguard | eliminated | 1000 | 0 | 0.013 | [0.004, 0.023] | 0 | 0 |
| wanderer | vanguard | rank1 | 1000 | 0 | -0.02 | [-0.031, -0.01] | 0 | 0 |
| wanderer | vanguard | worldRound | 1000 | 0 | -0.008 | [-0.019, 0] | 0 | 0 |
| wanderer | vanguard | humanCommands | 1000 | 0 | -2.25 | [-2.513, -2.009] | 0 | 0 |
| scholar | vanguard | fractional1st | 1000 | 0 | -0.017167 | [-0.027167, -0.008] | 0 | 0 |
| scholar | vanguard | rank | 1000 | 0 | 0.058 | [0.027, 0.088] | 0 | 0 |
| scholar | vanguard | laps | 1000 | 0 | -0.02 | [-0.051, 0.013] | 0 | 0 |
| scholar | vanguard | hp | 1000 | 0 | -11.18 | [-12.238, -10.145] | 0 | 0 |
| scholar | vanguard | maxHp | 1000 | 0 | -12.108 | [-12.27, -11.952] | 0 | 0 |
| scholar | vanguard | hpRatio | 1000 | 0 | -0.010854 | [-0.023653, 0.001943] | 0 | 0 |
| scholar | vanguard | level | 1000 | 0 | -0.018 | [-0.045, 0.01] | 0 | 0 |
| scholar | vanguard | xp | 1000 | 0 | -0.121 | [-0.291, 0.036] | 0 | 0 |
| scholar | vanguard | honor | 1000 | 0 | -0.108 | [-0.259, 0.054] | 0 | 0 |
| scholar | vanguard | gold | 1000 | 0 | -1.036 | [-2.692, 0.572] | 0 | 0 |
| scholar | vanguard | trialSucceeded | 1000 | 0 | -0.007 | [-0.017, 0.003] | 0 | 0 |
| scholar | vanguard | eliminated | 1000 | 0 | 0.007 | [-0.004, 0.018] | 0 | 0 |
| scholar | vanguard | rank1 | 1000 | 0 | -0.014 | [-0.024, -0.005] | 0 | 0 |
| scholar | vanguard | worldRound | 1000 | 0 | -0.004 | [-0.012, 0.004] | 0 | 0 |
| scholar | vanguard | humanCommands | 1000 | 0 | -2.259 | [-2.556, -1.965] | 0 | 0 |

## Human observations

Actual enemy HP damage, enemy shield absorption, generated player block, and enemy-defeated skips are separate. EventHealOffered is the selected option's nominal healing amount, not inferred effective healing. Counts represent observed actions/effects and may exceed the match sample size.

### Human wanderer: MEASURED

Measured matches=1000; missing=0; failed=0; NOT_RUN=0.

| Scope / ID | Counters | Measured matches | Missing matches | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| totals | combatEntries: 5002; combatTurns: 18998; eventGoldCost: 6096; eventHpCost: 0; eventGoldReward: 85580; eventXpReward: 39779; eventHonorReward: 9456; eventHealOffered: 56664; combatHpDamageTaken: 44060; combatDamageBlocked: 60995 | 1000 | 0 | 0 | 0 |
| commandsByKind | advance: 1996; choose: 8524; combat: 18998; lap: 2903; travel: 11427 | 1000 | 0 | 0 | 0 |
| offeredByKind | Combat: 6555; Die: 605; Gear: 2259; Heal: 3483; Risk: 4020; Study: 7855 | 1000 | 0 | 0 | 0 |
| legalOfferedByKind | Combat: 6555; Die: 431; Gear: 1710; Heal: 3333; Risk: 4013; Study: 7855 | 1000 | 0 | 0 | 0 |
| selectedByKind | Combat: 4035; Die: 36; Gear: 84; Heal: 3333; Study: 1036 | 1000 | 0 | 0 | 0 |
| selectedById | altar-pray: 854; citadel-elite: 1080; forest-herb: 559; gate-duel: 303; gate-elite: 608; harbor-heal: 308; harbor-work: 150; hill-rest: 864; library-read: 243; library-trial: 517; market-charm: 45; market-die: 36; market-mail: 39; market-news: 643; pass-bandit: 694; rift-fight: 833; well-rest: 748 | 1000 | 0 | 0 | 0 |
| lapRewardsById | aegis: 967; balanced: 1936 | 1000 | 0 | 0 | 0 |
| equipmentGainedById | aegis: 967; charm: 44; mail: 39 | 1000 | 0 | 0 | 0 |
| diceGainedById | arcane: 36; balanced: 1936 | 1000 | 0 | 0 | 0 |
| skill/arc-slash | applied: 1874; skippedEnemyDefeated: 101; hpDamage: 34591; shieldAbsorbed: 3376; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/bolt | applied: 3195; skippedEnemyDefeated: 49; hpDamage: 14674; shieldAbsorbed: 4689; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/citadel | applied: 806; skippedEnemyDefeated: 0; hpDamage: 10541; shieldAbsorbed: 2167; blockGenerated: 19822 | 1000 | 0 | 0 | 0 |
| skill/fire-bolt | applied: 884; skippedEnemyDefeated: 21; hpDamage: 11596; shieldAbsorbed: 1801; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/flame | applied: 4904; skippedEnemyDefeated: 279; hpDamage: 27252; shieldAbsorbed: 6976; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/flame-arc | applied: 402; skippedEnemyDefeated: 0; hpDamage: 7922; shieldAbsorbed: 1076; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/guard | applied: 8495; skippedEnemyDefeated: 173; hpDamage: 0; shieldAbsorbed: 0; blockGenerated: 94446 | 1000 | 0 | 0 | 0 |
| skill/guard-slash | applied: 2427; skippedEnemyDefeated: 214; hpDamage: 27107; shieldAbsorbed: 2729; blockGenerated: 33378 | 1000 | 0 | 0 | 0 |
| skill/slash | applied: 6350; skippedEnemyDefeated: 453; hpDamage: 61168; shieldAbsorbed: 11293; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| damageType/Fire | applied: 6190; hpDamage: 46770; shieldAbsorbed: 9853 | 1000 | 0 | 0 | 0 |
| damageType/Ice | applied: 1874; hpDamage: 34591; shieldAbsorbed: 3376 | 1000 | 0 | 0 | 0 |
| damageType/Lightning | applied: 3195; hpDamage: 14674; shieldAbsorbed: 4689 | 1000 | 0 | 0 | 0 |
| damageType/Physical | applied: 18078; hpDamage: 98816; shieldAbsorbed: 16189 | 1000 | 0 | 0 | 0 |
| enemy/citadel-elite | entries: 1080; turns: 4879; defeated: 1074; hpDamage: 36741; shieldAbsorbed: 7902; retaliationHpDamage: 14572; retaliationBlocked: 12295 | 1000 | 0 | 0 | 0 |
| enemy/final | entries: 967; turns: 3801; defeated: 967; hpDamage: 65756; shieldAbsorbed: 11266; retaliationHpDamage: 8292; retaliationBlocked: 22202 | 1000 | 0 | 0 | 0 |
| enemy/gate-duel | entries: 303; turns: 1296; defeated: 303; hpDamage: 4848; shieldAbsorbed: 1188; retaliationHpDamage: 2919; retaliationBlocked: 2217 | 1000 | 0 | 0 | 0 |
| enemy/gate-elite | entries: 608; turns: 2031; defeated: 606; hpDamage: 24167; shieldAbsorbed: 3988; retaliationHpDamage: 3825; retaliationBlocked: 6826 | 1000 | 0 | 0 | 0 |
| enemy/library-trial | entries: 517; turns: 1287; defeated: 517; hpDamage: 17137; shieldAbsorbed: 1744; retaliationHpDamage: 1071; retaliationBlocked: 3557 | 1000 | 0 | 0 | 0 |
| enemy/pass-bandit | entries: 694; turns: 2059; defeated: 694; hpDamage: 16544; shieldAbsorbed: 2463; retaliationHpDamage: 2997; retaliationBlocked: 4236 | 1000 | 0 | 0 | 0 |
| enemy/rift-fight | entries: 833; turns: 3645; defeated: 808; hpDamage: 29658; shieldAbsorbed: 5556; retaliationHpDamage: 10384; retaliationBlocked: 9662 | 1000 | 0 | 0 | 0 |

Skill damage distribution sample size is applied effect count, with skips excluded:

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| arc-slash | 1874 | 0 | 18.458378 | 1 | 3 | 13 | 21 | 26 | 27 | 28 | 0 | 0 |
| bolt | 3195 | 0 | 4.592801 | 0 | 0 | 2 | 5 | 7 | 8 | 11 | 0 | 0 |
| citadel | 806 | 0 | 13.078164 | 1 | 4 | 7 | 17 | 18 | 19 | 21 | 0 | 0 |
| fire-bolt | 884 | 0 | 13.117647 | 1 | 3 | 8 | 16 | 17 | 21 | 24 | 0 | 0 |
| flame | 4904 | 0 | 5.557096 | 0 | 0 | 4 | 7 | 8 | 10 | 11 | 0 | 0 |
| flame-arc | 402 | 0 | 19.706468 | 1 | 4 | 13 | 24 | 26 | 36 | 37 | 0 | 0 |
| guard | 8495 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| guard-slash | 2427 | 0 | 11.168933 | 1 | 2 | 8 | 13 | 14 | 15 | 18 | 0 | 0 |
| slash | 6350 | 0 | 9.632756 | 1 | 2 | 5 | 11 | 14 | 15 | 18 | 0 | 0 |

### Human scholar: MEASURED

Measured matches=1000; missing=0; failed=0; NOT_RUN=0.

| Scope / ID | Counters | Measured matches | Missing matches | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| totals | combatEntries: 5022; combatTurns: 18939; eventGoldCost: 6148; eventHpCost: 0; eventGoldReward: 86062; eventXpReward: 39971; eventHonorReward: 9501; eventHealOffered: 56843; combatHpDamageTaken: 44798; combatDamageBlocked: 59689 | 1000 | 0 | 0 | 0 |
| commandsByKind | advance: 1954; choose: 8553; combat: 18939; lap: 2920; travel: 11473 | 1000 | 0 | 0 | 0 |
| offeredByKind | Combat: 6581; Die: 607; Gear: 2263; Heal: 3494; Risk: 4043; Study: 7874 | 1000 | 0 | 0 | 0 |
| legalOfferedByKind | Combat: 6581; Die: 433; Gear: 1714; Heal: 3344; Risk: 4033; Study: 7874 | 1000 | 0 | 0 | 0 |
| selectedByKind | Combat: 4049; Die: 37; Gear: 85; Heal: 3344; Study: 1038 | 1000 | 0 | 0 | 0 |
| selectedById | altar-pray: 862; citadel-elite: 1084; forest-herb: 558; gate-duel: 303; gate-elite: 609; harbor-heal: 309; harbor-work: 150; hill-rest: 863; library-read: 245; library-trial: 514; market-charm: 46; market-die: 37; market-mail: 39; market-news: 643; pass-bandit: 698; rift-fight: 841; well-rest: 752 | 1000 | 0 | 0 | 0 |
| lapRewardsById | aegis: 973; balanced: 1947 | 1000 | 0 | 0 | 0 |
| equipmentGainedById | aegis: 973; charm: 45; mail: 39 | 1000 | 0 | 0 | 0 |
| diceGainedById | arcane: 37; balanced: 1947 | 1000 | 0 | 0 | 0 |
| skill/arc-slash | applied: 1888; skippedEnemyDefeated: 84; hpDamage: 32144; shieldAbsorbed: 3513; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/bolt | applied: 3171; skippedEnemyDefeated: 71; hpDamage: 23093; shieldAbsorbed: 6252; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/citadel | applied: 821; skippedEnemyDefeated: 0; hpDamage: 10187; shieldAbsorbed: 1916; blockGenerated: 20396 | 1000 | 0 | 0 | 0 |
| skill/fire-bolt | applied: 837; skippedEnemyDefeated: 32; hpDamage: 13331; shieldAbsorbed: 1548; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/flame | applied: 5020; skippedEnemyDefeated: 236; hpDamage: 44698; shieldAbsorbed: 9013; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/flame-arc | applied: 359; skippedEnemyDefeated: 0; hpDamage: 7614; shieldAbsorbed: 911; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/guard | applied: 8112; skippedEnemyDefeated: 331; hpDamage: 0; shieldAbsorbed: 0; blockGenerated: 89888 | 1000 | 0 | 0 | 0 |
| skill/guard-slash | applied: 2449; skippedEnemyDefeated: 209; hpDamage: 25473; shieldAbsorbed: 2574; blockGenerated: 34070 | 1000 | 0 | 0 | 0 |
| skill/slash | applied: 6379; skippedEnemyDefeated: 454; hpDamage: 39278; shieldAbsorbed: 9953; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| damageType/Fire | applied: 6216; hpDamage: 65643; shieldAbsorbed: 11472 | 1000 | 0 | 0 | 0 |
| damageType/Ice | applied: 1888; hpDamage: 32144; shieldAbsorbed: 3513 | 1000 | 0 | 0 | 0 |
| damageType/Lightning | applied: 3171; hpDamage: 23093; shieldAbsorbed: 6252 | 1000 | 0 | 0 | 0 |
| damageType/Physical | applied: 17761; hpDamage: 74938; shieldAbsorbed: 14443 | 1000 | 0 | 0 | 0 |
| enemy/citadel-elite | entries: 1084; turns: 4597; defeated: 1081; hpDamage: 36891; shieldAbsorbed: 7381; retaliationHpDamage: 13299; retaliationBlocked: 11679 | 1000 | 0 | 0 | 0 |
| enemy/final | entries: 973; turns: 3854; defeated: 973; hpDamage: 66164; shieldAbsorbed: 12439; retaliationHpDamage: 8207; retaliationBlocked: 22181 | 1000 | 0 | 0 | 0 |
| enemy/gate-duel | entries: 303; turns: 1291; defeated: 303; hpDamage: 4848; shieldAbsorbed: 1224; retaliationHpDamage: 2840; retaliationBlocked: 2158 | 1000 | 0 | 0 | 0 |
| enemy/gate-elite | entries: 609; turns: 1939; defeated: 608; hpDamage: 24223; shieldAbsorbed: 3907; retaliationHpDamage: 3853; retaliationBlocked: 6236 | 1000 | 0 | 0 | 0 |
| enemy/library-trial | entries: 514; turns: 1266; defeated: 514; hpDamage: 17050; shieldAbsorbed: 1717; retaliationHpDamage: 1412; retaliationBlocked: 3150 | 1000 | 0 | 0 | 0 |
| enemy/pass-bandit | entries: 698; turns: 2196; defeated: 698; hpDamage: 16632; shieldAbsorbed: 2577; retaliationHpDamage: 3848; retaliationBlocked: 4322 | 1000 | 0 | 0 | 0 |
| enemy/rift-fight | entries: 841; turns: 3796; defeated: 818; hpDamage: 30010; shieldAbsorbed: 6435; retaliationHpDamage: 11339; retaliationBlocked: 9963 | 1000 | 0 | 0 | 0 |

Skill damage distribution sample size is applied effect count, with skips excluded:

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| arc-slash | 1888 | 0 | 17.025424 | 1 | 3 | 12 | 20 | 23 | 25 | 26 | 0 | 0 |
| bolt | 3171 | 0 | 7.282561 | 0 | 1 | 3 | 9 | 11 | 12 | 15 | 0 | 0 |
| citadel | 821 | 0 | 12.408039 | 1 | 3 | 6 | 16 | 17 | 18 | 20 | 0 | 0 |
| fire-bolt | 837 | 0 | 15.927121 | 1 | 3 | 10 | 19 | 20 | 26 | 28 | 0 | 0 |
| flame | 5020 | 0 | 8.903984 | 0 | 1 | 5 | 11 | 12 | 14 | 17 | 0 | 0 |
| flame-arc | 359 | 0 | 21.208914 | 1 | 4 | 14 | 27 | 28 | 38 | 39 | 0 | 0 |
| guard | 8112 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| guard-slash | 2449 | 0 | 10.401388 | 0 | 2 | 9 | 12 | 13 | 14 | 16 | 0 | 0 |
| slash | 6379 | 0 | 6.157391 | 0 | 0 | 3 | 7 | 9 | 10 | 12 | 0 | 0 |

### Human vanguard: MEASURED

Measured matches=1000; missing=0; failed=0; NOT_RUN=0.

| Scope / ID | Counters | Measured matches | Missing matches | Failed | NOT_RUN |
|---|---|---:|---:|---:|---:|
| totals | combatEntries: 5051; combatTurns: 21122; eventGoldCost: 6172; eventHpCost: 0; eventGoldReward: 86738; eventXpReward: 40269; eventHonorReward: 9574; eventHealOffered: 57239; combatHpDamageTaken: 49207; combatDamageBlocked: 70122 | 1000 | 0 | 0 | 0 |
| commandsByKind | advance: 1886; choose: 8605; combat: 21122; lap: 2940; travel: 11545 | 1000 | 0 | 0 | 0 |
| offeredByKind | Combat: 6620; Die: 612; Gear: 2278; Heal: 3518; Risk: 4061; Study: 7929 | 1000 | 0 | 0 | 0 |
| legalOfferedByKind | Combat: 6620; Die: 438; Gear: 1729; Heal: 3368; Risk: 4059; Study: 7929 | 1000 | 0 | 0 | 0 |
| selectedByKind | Combat: 4071; Die: 37; Gear: 85; Heal: 3368; Study: 1044 | 1000 | 0 | 0 | 0 |
| selectedById | altar-pray: 864; citadel-elite: 1088; forest-herb: 565; gate-duel: 303; gate-elite: 616; harbor-heal: 311; harbor-work: 150; hill-rest: 875; library-read: 245; library-trial: 520; market-charm: 46; market-die: 37; market-mail: 39; market-news: 649; pass-bandit: 698; rift-fight: 846; well-rest: 753 | 1000 | 0 | 0 | 0 |
| lapRewardsById | aegis: 980; balanced: 1960 | 1000 | 0 | 0 | 0 |
| equipmentGainedById | aegis: 980; charm: 45; mail: 39 | 1000 | 0 | 0 | 0 |
| diceGainedById | arcane: 37; balanced: 1960 | 1000 | 0 | 0 | 0 |
| skill/arc-slash | applied: 1973; skippedEnemyDefeated: 92; hpDamage: 31976; shieldAbsorbed: 3501; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/bolt | applied: 3530; skippedEnemyDefeated: 64; hpDamage: 17930; shieldAbsorbed: 5927; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/citadel | applied: 858; skippedEnemyDefeated: 0; hpDamage: 11888; shieldAbsorbed: 2310; blockGenerated: 23693 | 1000 | 0 | 0 | 0 |
| skill/fire-bolt | applied: 909; skippedEnemyDefeated: 14; hpDamage: 12175; shieldAbsorbed: 1803; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/flame | applied: 5492; skippedEnemyDefeated: 247; hpDamage: 29533; shieldAbsorbed: 8340; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/flame-arc | applied: 397; skippedEnemyDefeated: 0; hpDamage: 8197; shieldAbsorbed: 1007; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| skill/guard | applied: 9150; skippedEnemyDefeated: 196; hpDamage: 0; shieldAbsorbed: 0; blockGenerated: 128068 | 1000 | 0 | 0 | 0 |
| skill/guard-slash | applied: 2725; skippedEnemyDefeated: 198; hpDamage: 31755; shieldAbsorbed: 3394; blockGenerated: 45922 | 1000 | 0 | 0 | 0 |
| skill/slash | applied: 7177; skippedEnemyDefeated: 364; hpDamage: 53723; shieldAbsorbed: 12491; blockGenerated: 0 | 1000 | 0 | 0 | 0 |
| damageType/Fire | applied: 6798; hpDamage: 49905; shieldAbsorbed: 11150 | 1000 | 0 | 0 | 0 |
| damageType/Ice | applied: 1973; hpDamage: 31976; shieldAbsorbed: 3501 | 1000 | 0 | 0 | 0 |
| damageType/Lightning | applied: 3530; hpDamage: 17930; shieldAbsorbed: 5927 | 1000 | 0 | 0 | 0 |
| damageType/Physical | applied: 19910; hpDamage: 97366; shieldAbsorbed: 18195 | 1000 | 0 | 0 | 0 |
| enemy/citadel-elite | entries: 1088; turns: 5405; defeated: 1088; hpDamage: 37064; shieldAbsorbed: 8781; retaliationHpDamage: 15384; retaliationBlocked: 15224 | 1000 | 0 | 0 | 0 |
| enemy/final | entries: 980; turns: 4065; defeated: 980; hpDamage: 66640; shieldAbsorbed: 12491; retaliationHpDamage: 8890; retaliationBlocked: 24095 | 1000 | 0 | 0 | 0 |
| enemy/gate-duel | entries: 303; turns: 1533; defeated: 303; hpDamage: 4848; shieldAbsorbed: 1470; retaliationHpDamage: 3549; retaliationBlocked: 2817 | 1000 | 0 | 0 | 0 |
| enemy/gate-elite | entries: 616; turns: 2129; defeated: 616; hpDamage: 24496; shieldAbsorbed: 4387; retaliationHpDamage: 3889; retaliationBlocked: 7404 | 1000 | 0 | 0 | 0 |
| enemy/library-trial | entries: 520; turns: 1337; defeated: 520; hpDamage: 17232; shieldAbsorbed: 1926; retaliationHpDamage: 1311; retaliationBlocked: 3567 | 1000 | 0 | 0 | 0 |
| enemy/pass-bandit | entries: 698; turns: 2413; defeated: 698; hpDamage: 16640; shieldAbsorbed: 2858; retaliationHpDamage: 4337; retaliationBlocked: 5010 | 1000 | 0 | 0 | 0 |
| enemy/rift-fight | entries: 846; turns: 4240; defeated: 826; hpDamage: 30257; shieldAbsorbed: 6860; retaliationHpDamage: 11847; retaliationBlocked: 12005 | 1000 | 0 | 0 | 0 |

Skill damage distribution sample size is applied effect count, with skips excluded:

| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| arc-slash | 1973 | 0 | 16.206792 | 1 | 3 | 11 | 18 | 23 | 24 | 25 | 0 | 0 |
| bolt | 3530 | 0 | 5.07932 | 0 | 0 | 2 | 6 | 8 | 9 | 12 | 0 | 0 |
| citadel | 858 | 0 | 13.855478 | 1 | 3 | 8 | 18 | 19 | 20 | 22 | 0 | 0 |
| fire-bolt | 909 | 0 | 13.393839 | 1 | 3 | 9 | 16 | 17 | 23 | 24 | 0 | 0 |
| flame | 5492 | 0 | 5.377458 | 0 | 0 | 3 | 7 | 8 | 10 | 11 | 0 | 0 |
| flame-arc | 397 | 0 | 20.647355 | 1 | 5 | 13 | 25 | 26 | 35 | 36 | 0 | 0 |
| guard | 9150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| guard-slash | 2725 | 0 | 11.653211 | 1 | 3 | 9 | 13 | 15 | 16 | 19 | 0 | 0 |
| slash | 7177 | 0 | 7.48544 | 0 | 0 | 4 | 9 | 11 | 12 | 15 | 0 | 0 |

## Unmeasured metrics

| Metric | Status | Missing participant/match observations | Value | Reason |
|---|---|---:|---|---|
| ai-full-skill-history | NOT_MEASURED | 9000 | null | Each valid match has three AI participants; public last-round/last-combat state does not reconstruct lifetime events. |
| ai-full-event-choice-history | NOT_MEASURED | 9000 | null | Each valid match has three AI participants; public last-round/last-combat state does not reconstruct lifetime events. |
| ai-lifetime-max-combat-turns | NOT_MEASURED | 9000 | null | Each valid match has three AI participants; public last-round/last-combat state does not reconstruct lifetime events. |

Missing counts above refer only to valid completed matches (n=3000); failed=0, NOT_RUN=0 are separate.

## Performance

Measured batch attempts n=3000; failed=0; NOT_RUN=0. sequential batch; process peak RSS; managed heap sampled after each match; GC/alloc are batch deltas

Elapsed 73.448673 s; throughput 40.84485 matches/s; per-match p50 23.4335 ms, p95 32.6427 ms.
Peak RSS 127275008 bytes; maximum observed managed heap 76156096 bytes; allocated 164692811400 bytes; GC collections [3284,16,4].
Raw bytes before verification/reports: 12796583 bytes. The runner records the bytes measured before performance.json separately; final total directory bytes are recorded by the external run audit.

Sensitivity experiments, independent holdout, live gameplay/UI, Unity execution, and authority/server settlement: NOT_RUN.
