# Planetfall Audio Asset Creation List

Sources inspected:
- `Assets/Resources/Data/Cutscenes/Chapter1_Intro.asset`
- `Assets/Resources/Data/Dialogues/Chapter1/Mission1.asset` through `Mission10.asset`
- `Assets/Audios`
- `Assets/Scripts/Managers/CutsceneData.cs`
- `Assets/Scripts/Dialogue/DialogueEntry.cs`
- `Assets/Scripts/Missions/MissionChapterManager.cs`

## Current Audio Inventory

Already present:
- `Assets/Audios/Music/MAIN MENU BACKGROUND.mp3`
- `Assets/Audios/Music/IN_GAME.mp3`
- `Assets/Audios/Music/IN GAME MUSIC 2.mp3`
- `Assets/Audios/Music/Combat Music.mp3`
- `Assets/Audios/UI/hover_click.wav`
- `Assets/Audios/UI/5584c76d87f543ed9094ec866b7d5384.mp3`
- `Assets/Audios/Voices/MissionObjective/MissionStart.wav`
- `Assets/Audios/Voices/MissionObjective/ObjectiveUpdate.wav`
- `Assets/Audios/Voices/MissionObjective/MissionDone.wav`
- `Assets/Audios/Voices/Chapter1/Cutscene/000000.mp3` through `000023.mp3`

Notes:
- `MissionChapterManager` already has generic mission-start, objective-update, and mission-complete voice clips.
- `DialogueEntry` supports one `voiceClip` per dialogue line.
- `CutsceneBeat` supports one `voiceline` and one `soundEffect` per beat.
- `Chapter1_Intro.asset` now has 24 voiced dialogue beats from `Assets/Audios/Voices/Chapter1/Cutscene/metadata.csv`, plus one establishing image beat.
- The Chapter 1 cutscene voice clips are imported and assigned to `CutsceneBeat.voiceline`. Cutscene `soundEffect` references are still empty.
- `Mission1.asset` has 2 voiceClip GUID references, but those GUIDs do not exist in the current checkout. Re-import those missing files or recreate the clips.

## Recommended Folder And File Naming

Use these folders unless you prefer a different final layout:

- `Assets/Audios/Voices/Chapter1/Cutscene/`
- `Assets/Audios/Voices/Chapter1/Mission01/` through `Mission10/`
- `Assets/Audios/SFX/Cutscenes/Chapter1/`
- `Assets/Audios/SFX/Gameplay/`
- `Assets/Audios/SFX/UI/`

Recommended naming:
- Cutscene VO: `000000.mp3` through `000023.mp3`, tracked by `metadata.csv`
- Mission VO: `CH1_M02_01_Nexus_EnergyCritical.wav`
- SFX: `SFX_CH1_CrashImpact.wav`

## Voice Overs To Create

Total Chapter 1 VO still needed: 53 clips.

That includes:
- 53 Chapter 1 mission dialogue lines.
- Chapter 1 intro cutscene VO is imported and wired.

### Chapter 1 Intro Cutscene

- [x] `000000.mp3` - Kyra-Dominia: "Do you still have that archive from the academy world?"
- [x] `000001.mp3` - NEXUS: "Specify archive. We passed through seven thousand academic civilizations before departure."
- [x] `000002.mp3` - Kyra-Dominia: "The one with students carrying rifles and halos over their heads. Blue city sectors. Too many clubs. Too little supervision."
- [x] `000003.mp3` - NEXUS: "Confirmed. Local youths possessed luminous halo structures and disproportionate access to military hardware."
- [x] `000004.mp3` - NEXUS: "Their command structure was statistically unsound. Also, several cafeterias were classified as strategic targets."
- [x] `000005.mp3` - NEXUS: "Anomalous resilience is not a governing principle I recommend adopting."
- [x] `000006.mp3` - Kyra-Dominia: "And still, they survived. Halos, guns, absurd optimism. That world should not have worked."
- [x] `000007.mp3` - NEXUS: "Final approach remains stable. Surface horizon entering visual range."
- [x] `000008.mp3` - Kyra-Dominia: "You sound almost peaceful, Nexus."
- [x] `000009.mp3` - NEXUS: "Correction. I am reporting an absence of immediate catastrophic failure."
- [x] `000010.mp3` - NEXUS: "New reading. Gravitational distortion forming beneath the descent path."
- [x] `000011.mp3` - Kyra-Dominia: "Define distortion. We were fine a moment ago."
- [x] `000012.mp3` - NEXUS: "Navigation solution degrading. Thruster alignment drifting. Impact probability rising."
- [x] `000013.mp3` - Kyra-Dominia: "Override! Reroute power to stabilizers and give me manual correction!"
- [x] `000014.mp3` - NEXUS: "Override denied. Reactor shielding compromised. Brace for impact."
- [x] `000015.mp3` - NEXUS: "Systems rebooting... Hull integrity at 31%. Landing thrusters fired late. We are grounded, Architect."
- [x] `000016.mp3` - NEXUS: "Primary crew complement: one. Status: alive. Stress indicators elevated but functional."
- [x] `000017.mp3` - Kyra-Dominia: "Well that changes everything. Functional is enough. We are stranded. Can we launch again?"
- [x] `000018.mp3` - NEXUS: "Negative. The Null-Vector's propulsion array is beyond repair. This landing is permanent, Architect."
- [x] `000019.mp3` - NEXUS: "Recommendation: activate the Supreme Habitation Protocol. This world is hostile but resource-rich. We build here, or we die here."
- [x] `000020.mp3` - Kyra-Dominia: "Then we build. Initialize the Colonizing Protocol. Deploy the task-bots."
- [x] `000021.mp3` - NEXUS: "Acknowledged. Be advised - long-range sensors detect biological signatures. Numerous. Organized. They are aware of our arrival."
- [x] `000022.mp3` - Kyra-Dominia: "Let them watch. By the time they understand what we're doing, it will be too late."
- [x] `000023.mp3` - NEXUS: "Supreme Habitation Protocol initialized, Bastion has been started. The Bastion of Genesis begins, Architect."

### Chapter 1 Mission 1

- [ ] `CH1_M01_01_Nexus_EnergyScan.wav` - Nexus: "Energy reserves are low. Scanning for local energy sources... Detected." Existing asset reference is stale or missing in checkout.
- [ ] `CH1_M01_02_Nexus_IronNodes.wav` - Nexus: "The locals ignore these high-yield Iron Nodes." Existing asset reference is stale or missing in checkout.
- [ ] `CH1_M01_03_Kyra_HarvestIt.wav` - Kyra-Dominia: "Their ignorance is our gain. Harvest it."
- [ ] `CH1_M01_04_Kyra_PowerAndResources.wav` - Kyra-Dominia: "We need power and resources. Now."

### Chapter 1 Mission 2

- [ ] `CH1_M02_01_Nexus_EnergyCritical.wav` - Nexus: "Commander, energy reserves are at critical levels."
- [ ] `CH1_M02_02_Nexus_ReactorInsufficient.wav` - Nexus: "The reactor alone cannot sustain expansion."
- [ ] `CH1_M02_03_Nexus_SteamGenerator.wav` - Nexus: "A Steam Generator will supplement our power grid."
- [ ] `CH1_M02_04_Kyra_GeneratorPriority.wav` - Kyra-Dominia: "Nothing runs without power. Get a generator online, priority one."

### Chapter 1 Mission 3

- [ ] `CH1_M03_01_Nexus_SwarmConversion.wav` - Nexus: "Commander, our Robotic Swarm reserves can be converted into specialized units."
- [ ] `CH1_M03_02_Nexus_BuilderFactory.wav` - Nexus: "The Builder Factory will repurpose swarm units into dedicated Builders for construction."
- [ ] `CH1_M03_03_Nexus_ResearcherFactory.wav` - Nexus: "A Researcher Factory produces units for the Laboratory."
- [ ] `CH1_M03_04_Kyra_WastedPotential.wav` - Kyra-Dominia: "Raw swarms are wasted potential."
- [ ] `CH1_M03_05_Kyra_SpecializeThem.wav` - Kyra-Dominia: "Get those factories running and specialize every last one of them."

### Chapter 1 Mission 4

- [ ] `CH1_M04_01_Nexus_Blueprints.wav` - Nexus: "Commander, our Researchers can develop new structural blueprints."
- [ ] `CH1_M04_02_Nexus_EnergyAndTime.wav` - Nexus: "Given sufficient energy and time."
- [ ] `CH1_M04_03_Nexus_Laboratory.wav` - Nexus: "A Laboratory would enable this."
- [ ] `CH1_M04_04_Kyra_BuildLaboratory.wav` - Kyra-Dominia: "Engineering superiority is how we hold this ground. Build it."
- [ ] `CH1_M04_05_Nexus_BaselineCapacity.wav` - Nexus: "Our reserves are capped at baseline capacity."
- [ ] `CH1_M04_06_Nexus_StorageInfrastructure.wav` - Nexus: "I recommend prioritizing storage infrastructure."
- [ ] `CH1_M04_07_Nexus_StorageList.wav` - Nexus: "Resource Warehouses, Energy Batteries, and a Robot Bay."
- [ ] `CH1_M04_08_Nexus_ExpansionStalls.wav` - Nexus: "Without expanded reserves, further expansion stalls."
- [ ] `CH1_M04_09_Kyra_ResearchStorage.wav` - Kyra-Dominia: "A fortress without stockpiles is just a target. Research them all."

### Chapter 1 Mission 5

- [ ] `CH1_M05_01_Nexus_MovementBeyondPerimeter.wav` - Nexus: "Long-range sensors are picking up movement beyond the perimeter."
- [ ] `CH1_M05_02_Nexus_DefensiveWalls.wav` - Nexus: "I recommend stockpiling iron reserves and researching defensive wall structures."
- [ ] `CH1_M05_03_Kyra_WallsBeforeVisit.wav` - Kyra-Dominia: "Agreed. I want walls up before whatever is out there decides to pay us a visit."

### Chapter 1 Mission 6

- [ ] `CH1_M06_01_Nexus_HostilesNorth.wav` - Nexus: "Hostile biosignatures confirmed, approaching from the north."
- [ ] `CH1_M06_02_Nexus_SmallPatrol.wav` - Nexus: "Small patrol, six to eight contacts. They are not friendly, Commander."
- [ ] `CH1_M06_03_Kyra_PrepareEngagement.wav` - Kyra-Dominia: "Walls up, turrets online. This is our ground now. Prepare for engagement."

### Chapter 1 Mission 7

- [ ] `CH1_M07_01_Nexus_NewContact.wav` - Nexus: "New contact."
- [ ] `CH1_M07_02_Nexus_WestForce.wav` - Nexus: "A larger force is massing to the west."
- [ ] `CH1_M07_03_Nexus_TwicePrevious.wav` - Nexus: "Estimated twice the size of the previous engagement."
- [ ] `CH1_M07_04_Kyra_ProbingFlanks.wav` - Kyra-Dominia: "They are probing our flanks."
- [ ] `CH1_M07_05_Kyra_WesternPerimeter.wav` - Kyra-Dominia: "Reinforce the western perimeter, turrets and walls."
- [ ] `CH1_M07_06_Kyra_NothingGetsThrough.wav` - Kyra-Dominia: "Nothing gets through."

### Chapter 1 Mission 8

- [ ] `CH1_M08_01_Nexus_MultipleVectors.wav` - Nexus: "Commander, I am detecting coordinated movement from multiple vectors."
- [ ] `CH1_M08_02_Nexus_ThreeFronts.wav` - Nexus: "North, west, and south simultaneously. This is not a random patrol."
- [ ] `CH1_M08_03_Kyra_PincerManeuver.wav` - Kyra-Dominia: "A pincer maneuver. These things have actual military tactics."
- [ ] `CH1_M08_04_Kyra_AllFronts.wav` - Kyra-Dominia: "Shore up every approach, spread our turrets across all fronts."
- [ ] `CH1_M08_05_Nexus_SustainEngagement.wav` - Nexus: "Recommend prioritizing resource production to sustain extended multi-front engagement."

### Chapter 1 Mission 9

- [ ] `CH1_M09_01_Nexus_AnalysisComplete.wav` - Nexus: "Analysis complete."
- [ ] `CH1_M09_02_Nexus_EmissionsProvoke.wav` - Nexus: "Our terraforming emissions are directly provoking the indigenous population."
- [ ] `CH1_M09_03_Nexus_TheyResist.wav` - Nexus: "The more we reshape this environment, the more aggressively they resist."
- [ ] `CH1_M09_04_Kyra_PointIsReshape.wav` - Kyra-Dominia: "The whole point is to reshape this environment."
- [ ] `CH1_M09_05_Kyra_DoNotStop.wav` - Kyra-Dominia: "We do not stop."
- [ ] `CH1_M09_06_Kyra_HoldLine.wav` - Kyra-Dominia: "Push output, reinforce defenses, and hold the line."
- [ ] `CH1_M09_07_Nexus_MonitoringTerraform.wav` - Nexus: "Understood. Monitoring terraforming levels."
- [ ] `CH1_M09_08_Nexus_EscalatingHostility.wav` - Nexus: "Expect escalating hostility with each expansion cycle."

### Chapter 1 Mission 10

- [ ] `CH1_M10_01_Nexus_MassiveBiosignature.wav` - Nexus: "Commander, a massive biosignature is approaching from the north."
- [ ] `CH1_M10_02_Nexus_WardenClass.wav` - Nexus: "It dwarfs anything we have encountered. Designating it as a Warden-class entity."
- [ ] `CH1_M10_03_Kyra_CommandUnit.wav` - Kyra-Dominia: "That is a command unit. They sent their best."
- [ ] `CH1_M10_04_Kyra_ConcentrateFire.wav` - Kyra-Dominia: "All defenses to maximum, concentrate fire on the big one."
- [ ] `CH1_M10_05_Nexus_AssaultWave.wav` - Nexus: "Warden is accompanied by a full assault wave."
- [ ] `CH1_M10_06_Nexus_BreakCohesion.wav` - Nexus: "Recommend eliminating the command unit first to break enemy cohesion."

## Sound Effects To Create

### Cutscene SFX

These map cleanly to `CutsceneBeat.soundEffect` in `Chapter1_Intro.asset`.

- [ ] `SFX_CH1_INT_01_DeepSpaceAmbience.wav` - Low ship/space ambience for the first establishing image.
- [ ] `SFX_CH1_INT_02_CockpitLowHum.wav` - Quiet cockpit hum under Kyra and Nexus' calm approach exchange.
- [ ] `SFX_CH1_INT_03_SurfaceScanPing.wav` - Soft sensor ping while Nexus reports atmosphere, minerals, and distant movement.
- [ ] `SFX_CH1_INT_04_CommandConsoleIdle.wav` - Subtle console activity while Kyra gives landing instructions.
- [ ] `SFX_CH1_INT_05_AnomalyPulse.wav` - Low, sudden gravitational distortion pulse as the tone shifts.
- [ ] `SFX_CH1_INT_06_NavigationWarning.wav` - Warning chirps and UI error pulses as navigation starts degrading.
- [ ] `SFX_CH1_INT_07_ThrusterStrain.wav` - Straining thrusters and ship vibration under Kyra's manual correction order.
- [ ] `SFX_CH1_INT_08_ReactorShieldingAlarm.wav` - Heavier reactor warning alarm when override is denied.
- [ ] `SFX_CH1_INT_09_CrashImpact.wav` - Main crash impact for the black-screen beat.
- [ ] `SFX_CH1_INT_10_SystemReboot.wav` - Computer reboot, static, and power-up layer.
- [ ] `SFX_CH1_INT_11_HullIntegrityWarning.wav` - Damaged system warning pulse after reboot.
- [ ] `SFX_CH1_INT_12_GroundedSting.wav` - Short, restrained final sting after Nexus confirms they are grounded.

### Gameplay SFX

These are not all directly wired to serialized fields yet, but the project already has `AudioManager` SFX playback methods. Create these as the base gameplay set before wiring.

- [ ] `SFX_UI_ButtonClick.wav` - General button press.
- [ ] `SFX_UI_PanelOpen.wav` - Open building list, options, or info panel.
- [ ] `SFX_UI_PanelClose.wav` - Close building list, options, or info panel.
- [ ] `SFX_UI_Deny.wav` - Invalid action or insufficient resources.
- [ ] `SFX_Build_PlacementPreview.wav` - Enter building placement mode.
- [ ] `SFX_Build_Placed.wav` - Building successfully placed.
- [ ] `SFX_Build_Cancel.wav` - Building placement canceled.
- [ ] `SFX_Build_ConstructionComplete.wav` - Construction finished.
- [ ] `SFX_Build_Damaged.wav` - Building takes damage.
- [ ] `SFX_Build_Destroyed.wav` - Building destroyed.
- [ ] `SFX_Resource_IronExtract.wav` - Iron extraction or resource tick.
- [ ] `SFX_Resource_EnergyGenerator.wav` - Steam generator/energy production loop or pulse.
- [ ] `SFX_Research_Start.wav` - Research begins.
- [ ] `SFX_Research_Complete.wav` - Research completes or unlocks.
- [ ] `SFX_Worker_Assign.wav` - Worker assigned to building/task.
- [ ] `SFX_Turret_Fire.wav` - Starter/gun turret shot.
- [ ] `SFX_Turret_Hit.wav` - Projectile impact on enemy.
- [ ] `SFX_Enemy_Hit.wav` - Enemy takes damage.
- [ ] `SFX_Enemy_Death.wav` - Enemy death.
- [ ] `SFX_Wave_Incoming.wav` - Wave warning alert.
- [ ] `SFX_Base_UnderAttack.wav` - Priority alert when base/buildings are under attack.
- [ ] `SFX_Pollution_Spread.wav` - Pollution/wither spread event.
- [ ] `SFX_Tile_Integrate.wav` - Tile integration/buildable-zone conversion.

## Voice Direction Notes

- Nexus: controlled AI assistant, precise, calm, mostly neutral. Alert lines can become sharper but should not sound emotional.
- Kyra-Dominia: cold, commanding, restrained. Use intensity sparingly; she should sound decisive rather than panicked.
- Mission objective stingers already exist, so new mission dialogue VO should not duplicate "Mission start", "Objective update", or "Mission complete" unless those clips are being replaced.

## Import And Wiring Checklist

- [x] Import cutscene VO clips into `Assets/Audios/Voices/Chapter1/Cutscene/`.
- [ ] Import mission VO clips into the recommended `Assets/Audios/Voices/Chapter1/MissionXX/` folders.
- [ ] Import SFX clips into `Assets/Audios/SFX/...`.
- [ ] Preserve generated `.meta` files.
- [x] Assign intro VO clips in `Assets/Resources/Data/Cutscenes/Chapter1_Intro.asset`.
- [ ] Assign intro SFX clips in `Assets/Resources/Data/Cutscenes/Chapter1_Intro.asset`.
- [ ] Assign mission dialogue VO clips in `Assets/Resources/Data/Dialogues/Chapter1/Mission*.asset`.
- [ ] Recheck `Mission1.asset` stale voice GUIDs after importing or recreating the first two clips.
- [ ] In Unity, confirm no missing audio references in the Inspector.
- [ ] Play the intro cutscene preview and one Chapter 1 mission dialogue to verify clip timing and volume balance.
