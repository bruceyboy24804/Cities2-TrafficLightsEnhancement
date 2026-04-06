using C2VM. TrafficLightsEnhancement. Components;
using Game.Net;
using Unity.Collections;
using Unity. Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Systems. TrafficLightSystems. Simulation
{
    public struct CustomStateMachine
    {
        public static bool UpdateTrafficLightState(ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            return UpdateTrafficLightState(ref trafficLights, ref customTrafficLights, customPhaseDataBuffer, customPhaseDataBuffer);
        }

        public static bool UpdateTrafficLightState(ref TrafficLights trafficLights, ref CustomTrafficLights customTrafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer, DynamicBuffer<CustomPhaseData> settingsPhaseDataBuffer)
        {
            if (trafficLights.m_State == TrafficLightState. None || trafficLights.m_State == TrafficLightState. Extending || trafficLights.m_State == TrafficLightState.Extended)
            {
                trafficLights.m_State = TrafficLightState.Beginning;
                trafficLights.m_CurrentSignalGroup = 0;
                trafficLights.m_NextSignalGroup = GetNextSignalGroup(trafficLights.m_CurrentSignalGroup, settingsPhaseDataBuffer, customTrafficLights, out _);
                trafficLights.m_Timer = 0;
                customTrafficLights.m_Timer = 0;
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState. Beginning)
            {
                if (trafficLights.m_NextSignalGroup <= 0)
                {
                    trafficLights.m_State = TrafficLightState. None; 
                    return true;
                }
                trafficLights.m_State = TrafficLightState. Ongoing;
                trafficLights. m_CurrentSignalGroup = trafficLights.m_NextSignalGroup;
                trafficLights.m_NextSignalGroup = 0;
                trafficLights.m_Timer = 0;
                customTrafficLights. m_Timer = 0;
                for (int i = 0; i < customPhaseDataBuffer.Length; i++)
                {
                    CustomPhaseData phase = customPhaseDataBuffer[i];
                    if (trafficLights.m_CurrentSignalGroup == i + 1)
                    {
                        phase.m_TurnsSinceLastRun = 0;
                        phase.m_LowFlowTimer = 0;
                        phase.m_LowPriorityTimer = 0;
                    }
                    else
                    {
                        phase. m_TurnsSinceLastRun++;
                    }
                    phase.m_Options &= ~CustomPhaseData.Options.EndPhasePrematurely;
                    customPhaseDataBuffer[i] = phase;
                }
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Ongoing)
            {
                int currentSignalIndex = trafficLights.m_CurrentSignalGroup - 1;
                if (currentSignalIndex < 0 || currentSignalIndex >= customPhaseDataBuffer.Length)
                {
                    trafficLights.m_State = TrafficLightState.None; 
                    return true;
                }
                customTrafficLights.m_Timer++;
                CustomPhaseData phase = customPhaseDataBuffer[currentSignalIndex];
                
                CustomPhaseData settingsPhase = currentSignalIndex < settingsPhaseDataBuffer.Length 
                    ? settingsPhaseDataBuffer[currentSignalIndex] 
                    : phase;
                
                bool stepDone = ShouldChangeStepWithSettings(
                    ref phase,
                    settingsPhase,
                    customPhaseDataBuffer,
                    currentSignalIndex,
                    customTrafficLights.m_Timer,
                    customTrafficLights,
                    out float flow,
                    out float wait
                );
                
                phase.m_CurrentFlow = flow;
                phase.m_CurrentWait = wait;
                
                if (customTrafficLights.m_ManualSignalGroup > 0 && customTrafficLights.m_ManualSignalGroup != trafficLights.m_CurrentSignalGroup)
                {
                    stepDone = true;
                }
                
                if ((phase.m_Options & CustomPhaseData.Options.EndPhasePrematurely) != 0)
                {
                    stepDone = true;
                }
                
                customPhaseDataBuffer[currentSignalIndex] = phase;
                byte nextGroup = GetNextSignalGroup(trafficLights.m_CurrentSignalGroup, settingsPhaseDataBuffer, customTrafficLights, out var linked);
                if (stepDone && nextGroup != trafficLights.m_CurrentSignalGroup)
                {
                    trafficLights.m_State = TrafficLightState.Ending;
                    trafficLights.m_NextSignalGroup = nextGroup;
                    if (linked)
                    {
                        for (int i = trafficLights.m_CurrentSignalGroup; i < trafficLights.m_NextSignalGroup - 1; i++)
                        {
                            CustomPhaseData nextPhase = customPhaseDataBuffer[i];
                            if (nextPhase.m_Priority <= 0)
                            {
                                nextPhase.m_TurnsSinceLastRun = 0;
                                customPhaseDataBuffer[i] = nextPhase;
                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            else if (trafficLights.m_State == TrafficLightState.Ending)
            {
                trafficLights.m_State = TrafficLightState. Changing;
                return true;
            }
            else if (trafficLights.m_State == TrafficLightState.Changing)
            {
                trafficLights.m_State = TrafficLightState. Beginning;
                return true;
            }
            return false;
        }

        public static void CalculateFlow(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, int unfilteredChunkIndex, DynamicBuffer<SubLane> subLaneBuffer, TrafficLights trafficLights, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            float4 timeFactors = job.m_ExtraData.m_TimeFactors * 0.125f;
            for (int i = 0; i < customPhaseDataBuffer. Length; i++)
            {
                CustomPhaseData customPhaseData = customPhaseDataBuffer[i];
                customPhaseData.m_CarFlow. z = customPhaseData.m_CarFlow.y;
                customPhaseData.m_CarFlow.y = customPhaseData. m_CarFlow.x;
                customPhaseData.m_CarFlow.x = 0f;
                customPhaseDataBuffer[i] = customPhaseData;
            }
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;
                float4 newDistance = 0f;
                float4 newDuration = 0f;
                float4 oldDistance = 0f;
                float4 oldDuration = 0f;
                float4 diffDistance = 0f;
                float4 diffDuration = 0f;
                uint newFrame = job.m_ExtraData.m_Frame;
                uint oldFrame = 0;
                uint diffFrame = 0;

                if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal))
                {
                    continue;
                }
                if (! job.m_ExtraTypeHandle.m_LaneFlow.TryGetComponent(subLaneEntity, out var laneFlow))
                {
                    continue;
                }
                if ((laneSignal.m_GroupMask & (1 << trafficLights.m_CurrentSignalGroup - 1)) == 0)
                {
                    continue;
                }

                newDistance = math.lerp(laneFlow.m_Distance, laneFlow.m_Next.y, timeFactors);
                newDuration = math. lerp(laneFlow.m_Duration, laneFlow.m_Next.x, timeFactors);

                LaneFlowHistory laneFlowHistory = new LaneFlowHistory();
                if (job.m_ExtraTypeHandle. m_LaneFlowHistory.TryGetComponent(subLaneEntity, out laneFlowHistory))
                {
                    oldDistance = laneFlowHistory. m_Distance;
                    oldDuration = laneFlowHistory.m_Duration;
                    oldFrame = laneFlowHistory.m_Frame;
                }
                else
                {
                    job.m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLaneEntity, laneFlowHistory);
                }

                diffDistance = newDistance - oldDistance;
                diffDuration = newDuration - oldDuration;
                diffFrame = newFrame - oldFrame;

                laneFlowHistory.m_Distance = newDistance;
                laneFlowHistory.m_Duration = newDuration;
                laneFlowHistory.m_Frame = newFrame;

                job.m_CommandBuffer.SetComponent(unfilteredChunkIndex, subLaneEntity, laneFlowHistory);

                int group = trafficLights.m_CurrentSignalGroup - 1;
                if (group < customPhaseDataBuffer.Length && diffFrame > 0)
                {
                    CustomPhaseData customPhaseData = customPhaseDataBuffer[group];
                    float totalDiff = math.abs(Max(diffDistance)) + math.abs(Max(diffDuration));
                    customPhaseData.m_CarFlow.x += totalDiff * (64f / (float)diffFrame); 
                    customPhaseDataBuffer[group] = customPhaseData;
                }
            }
        }

        public static void CalculatePriority(PatchedTrafficLightSystem. UpdateTrafficLightsJob job, DynamicBuffer<SubLane> subLaneBuffer, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData customPhaseData = customPhaseDataBuffer[i];
                customPhaseData.m_CarLaneOccupied = 0;
                customPhaseData.m_PublicCarLaneOccupied = 0;
                customPhaseData.m_TrackLaneOccupied = 0;
                customPhaseData.m_PedestrianLaneOccupied = 0;
                customPhaseData.m_BicycleLaneOccupied = 0; 
                customPhaseData. m_Priority = 0;
                customPhaseDataBuffer[i] = customPhaseData;
            }
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;

                if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal))
                {
                    continue;
                }

                Entity lanePetitioner = laneSignal.m_Petitioner;
                int lanePriority = laneSignal.m_Priority;

                laneSignal.m_Petitioner = Entity.Null;
                laneSignal.m_Priority = laneSignal.m_Default;
                job.m_LaneSignalData[subLaneEntity] = laneSignal;

                if (job.m_ExtraTypeHandle. m_MasterLane.HasComponent(subLaneEntity))
                {
                    continue;
                }
                if (lanePetitioner == Entity.Null)
                {
                    continue;
                }

                for (int i = 0; i < customPhaseDataBuffer.Length; i++)
                {
                    if ((laneSignal.m_GroupMask & (1 << i)) == 0)
                    {
                        continue;
                    }

                    CustomPhaseData customPhaseData = customPhaseDataBuffer[i];

                    if (job.m_ExtraTypeHandle. m_CarLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_CarLaneOccupied++;
                        if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.TryGetComponent(subLaneEntity, out var extraLaneSignal))
                        {
                            if (extraLaneSignal.m_SourceSubLane != Entity.Null && job.m_ExtraTypeHandle.m_CarLane.TryGetComponent(extraLaneSignal.m_SourceSubLane, out var sourceCarLane))
                            {
                                if ((sourceCarLane.m_Flags & CarLaneFlags.PublicOnly) != 0)
                                {
                                    customPhaseData.m_PublicCarLaneOccupied++;
                                }
                            }
                        }
                    }
                    if (job.m_ExtraTypeHandle.m_TrackLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_TrackLaneOccupied++;
                    }
                    if (job.m_ExtraTypeHandle.m_PedestrianLane.TryGetComponent(subLaneEntity, out var pedestrianLane))
                    {
                        if ((pedestrianLane.m_Flags & PedestrianLaneFlags. Crosswalk) != 0)
                        {
                            customPhaseData.m_PedestrianLaneOccupied++;
                        }
                    }
                    
                    if (job. m_ExtraTypeHandle.m_SecondaryLane.HasComponent(subLaneEntity))
                    {
                        customPhaseData.m_BicycleLaneOccupied++;
                    }
                    
                    customPhaseData.m_Priority = math.max(customPhaseData. m_Priority, lanePriority);
                    customPhaseDataBuffer[i] = customPhaseData;
                }
            }
            
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                CustomPhaseData phase = customPhaseDataBuffer[i];
                float weightedWaiting = phase.CalculateSmoothedWeightedWaiting(customPhaseDataBuffer.Length);
                phase.m_WeightedWaiting = weightedWaiting;
                float flow = phase.AverageCarFlow();
                float wait = phase.WeightedLaneOccupied();
                phase.UpdateFlowWaitRatios(flow, wait);
                phase.m_CurrentFlow = flow;
                phase.m_CurrentWait = wait * phase.m_WaitFlowBalance;
                customPhaseDataBuffer[i] = phase;
            }
        }

        public static byte GetNextSignalGroup(byte currentGroup, DynamicBuffer<CustomPhaseData> customPhaseDataBuffer, CustomTrafficLights customTrafficLights, out bool linked)
        {
            linked = false;
            if (customTrafficLights.m_ManualSignalGroup > 0 && customTrafficLights.m_ManualSignalGroup - 1 < customPhaseDataBuffer.Length)
            {
                return customTrafficLights.m_ManualSignalGroup;
            }
            if (customPhaseDataBuffer.Length == 0)
            {
                return 0;
            }
            int currentStep = currentGroup - 1;
            if (currentStep < 0) currentStep = 0;

            if (customTrafficLights.GetMode() == CustomTrafficLights.TrafficMode.FixedTimed)
            {
                bool useSmartSelection = (customTrafficLights.GetOptions() & CustomTrafficLights.TrafficOptions.SmartPhaseSelection) != 0;
                
                if (useSmartSelection)
                {
                    int bestStep = CalculateBestNextStep(customPhaseDataBuffer, currentStep, out bool shouldRestart);
                    if (shouldRestart)
                    {
                        return currentGroup;
                    }
                    return (byte)(bestStep + 1);
                }
                else
                {
                    int nextStep = (currentStep + 1) % customPhaseDataBuffer.Length;
                    return (byte)(nextStep + 1);
                }
            }
            // Dynamic mode: cycle sequentially, skip phases with min=0 and no demand
            for (int attempt = 0; attempt < customPhaseDataBuffer.Length; attempt++)
            {
                int candidateStep = (currentStep + 1 + attempt) % customPhaseDataBuffer.Length;
                CustomPhaseData candidatePhase = customPhaseDataBuffer[candidateStep];

                // Always run phases with minimum > 0
                if (candidatePhase.m_MinimumDuration > 0)
                {
                    return (byte)(candidateStep + 1);
                }

                // Min=0: run only if there's demand (vehicles waiting or priority)
                if (candidatePhase.m_Priority > 0 || candidatePhase.WeightedLaneOccupied() > 0)
                {
                    return (byte)(candidateStep + 1);
                }
            }

            // All phases skippable with no demand — default to next sequential
            int fallbackStep = (currentStep + 1) % customPhaseDataBuffer.Length;
            return (byte)(fallbackStep + 1);
        }

        private static int MaxPriority(DynamicBuffer<CustomPhaseData> customPhaseDataBuffer)
        {
            int max = int.MinValue;
            foreach (var phase in customPhaseDataBuffer)
            {
                max = math.max(max, phase.m_Priority);
            }
            return max;
        }

        private static float MaxOtherPhasesWaiting(DynamicBuffer<CustomPhaseData> customPhaseDataBuffer, int currentPhaseIndex)
        {
            float max = 0f;
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                if (i != currentPhaseIndex)
                {
                    var phase = customPhaseDataBuffer[i];
                    float waiting = phase.m_WeightedWaiting;
                    if (waiting <= 0f && phase.m_TurnsSinceLastRun > 0)
                    {
                        waiting = phase.WeightedLaneOccupied();
                    }
                    max = math.max(max, waiting);
                }
            }
            return max;
        }

        private static bool ShouldChangeStep(
            ref CustomPhaseData phase,
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer,
            int currentPhaseIndex,
            uint timer,
            CustomTrafficLights customTrafficLights,
            out float flow,
            out float wait)
        {
            return ShouldChangeStepWithSettings(ref phase, phase, customPhaseDataBuffer, currentPhaseIndex, timer, customTrafficLights, out flow, out wait);
        }

        private static bool ShouldChangeStepWithSettings(
            ref CustomPhaseData phase,
            CustomPhaseData settingsPhase,
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer,
            int currentPhaseIndex,
            uint timer,
            CustomTrafficLights customTrafficLights,
            out float flow,
            out float wait)
        {
            flow = phase.AverageCarFlow();
            
            if (customTrafficLights.GetMode() == CustomTrafficLights.TrafficMode.FixedTimed)
            {
                wait = MaxOtherPhasesWaiting(customPhaseDataBuffer, currentPhaseIndex) * settingsPhase.m_WaitFlowBalance;
                
                if (timer >= settingsPhase.m_MaximumDuration)
                {
                    return true;
                }
                if (timer > settingsPhase.m_MinimumDuration)
                {
                    return ShouldChangeByMetric(settingsPhase.m_ChangeMetric, flow, wait);
                }
                return false;
            }
            else
            {
                wait = MaxOtherPhasesWaiting(customPhaseDataBuffer, currentPhaseIndex) * settingsPhase.m_WaitFlowBalance;
                float targetDuration = 10f * (flow + (float)(phase.m_TrackLaneOccupied * 0.5)) * settingsPhase.m_TargetDurationMultiplier;
                phase.m_TargetDuration = targetDuration;
                
                if (timer <= settingsPhase.m_MinimumDuration)
                {
                    phase.m_LowFlowTimer = 0;
                    phase.m_LowPriorityTimer = 0;
                    return false;
                }
                
                if (timer >= settingsPhase.m_MaximumDuration)
                {
                    return true;
                }
                
                bool metricSaysChange = ShouldChangeByMetric(settingsPhase.m_ChangeMetric, flow, wait);
                
                if (!metricSaysChange)
                {
                    phase.m_LowFlowTimer = 0;
                    phase.m_LowPriorityTimer = 0;
                    return false;
                }
                
                int maxPriority = MaxPriority(customPhaseDataBuffer);
                
                if (phase.m_Priority > 0 && phase.m_Priority >= maxPriority)
                {
                    if (timer <= targetDuration)
                    {
                        phase.m_LowFlowTimer = 0;
                        return false;
                    }
                    else if (phase.m_LowFlowTimer < 3)
                    {
                        phase.m_LowFlowTimer++;
                        return false;
                    }
                    phase.m_LowPriorityTimer = 0;
                    return true;
                }
                else if (phase.m_Priority < maxPriority)
                {
                    if (phase.m_LowPriorityTimer >= 1)
                    {
                        return true;
                    }
                    phase.m_LowPriorityTimer++;
                    return false;
                }
                
                return true;
            }
        }

        public static int CalculateBestNextStep(DynamicBuffer<CustomPhaseData> customPhaseDataBuffer, int currentStep, out bool shouldRestartCurrent)
        {
            shouldRestartCurrent = false;
            
            if (customPhaseDataBuffer.Length == 0)
            {
                return 0;
            }

            int nextStepIndex = (currentStep + 1) % customPhaseDataBuffer.Length;
            CustomPhaseData nextPhase = customPhaseDataBuffer[nextStepIndex];
            CustomPhaseData currentPhase = customPhaseDataBuffer[currentStep];

            if (nextPhase.m_MinimumDuration > 0 || nextPhase.m_ChangeMetric != currentPhase.m_ChangeMetric)
            {
                return nextStepIndex;
            }

            float maxMetric = currentPhase.GetMetric(currentPhase.m_CurrentFlow, currentPhase.m_CurrentWait);
            if (float.IsNaN(maxMetric))
            {
                maxMetric = float.MinValue;
            }

            int bestStepIndex = currentStep;
            int prevStepIndex = currentStep;
            int checkStep = nextStepIndex;

            while (checkStep != prevStepIndex)
            {
                CustomPhaseData checkPhase = customPhaseDataBuffer[checkStep];
                
                
                float flow = checkPhase.AverageCarFlow();
                float wait = checkPhase.m_WeightedWaiting * checkPhase.m_WaitFlowBalance;
                float metric = checkPhase.GetMetric(flow, wait);

                if (metric > maxMetric)
                {
                    maxMetric = metric;
                    bestStepIndex = checkStep;
                }

                if (checkPhase.m_MinimumDuration > 0)
                {
                    int stepAfterPrev = (prevStepIndex + 1) % customPhaseDataBuffer.Length;
                    if (checkStep == stepAfterPrev)
                    {
                        bestStepIndex = stepAfterPrev;
                    }
                    break;
                }

                checkStep = (checkStep + 1) % customPhaseDataBuffer.Length;

                if (checkPhase.m_ChangeMetric != currentPhase.m_ChangeMetric)
                {
                    break;
                }
            }

            if (bestStepIndex == currentStep)
            {
                shouldRestartCurrent = true;
                return currentStep;
            }

            return bestStepIndex;
        }

        private static float Max(float4 f)
        {
            return math.max(f.w, math.max(f.x, math.max(f.y, f.z)));
        }

        public static bool ShouldFollowLeader(
            PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
            Entity currentEntity,
            out Entity groupEntity)
        {
            groupEntity = Entity.Null;
            
            if (!job.m_ExtraTypeHandle.m_TrafficGroupMember.TryGetComponent(currentEntity, out var member))
            {
                return false;
            }

            if (member.m_GroupEntity == Entity.Null || member.m_IsGroupLeader)
            {
                return false;
            }

            if (!job.m_ExtraTypeHandle.m_TrafficGroup.TryGetComponent(member.m_GroupEntity, out var group))
            {
                return false;
            }

            if (!group.m_IsCoordinated || group.m_MasterSignalGroupCount == 0)
            {
                return false;
            }

            groupEntity = member.m_GroupEntity;
            return true;
        }

        public static void AggregateGroupMemberPriority(
            PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
            Entity leaderEntity,
            DynamicBuffer<CustomPhaseData> leaderPhaseDataBuffer,
            NativeArray<Entity> groupMemberEntities)
        {
            if (!job.m_ExtraTypeHandle.m_TrafficGroupMember.TryGetComponent(leaderEntity, out var leaderMember))
            {
                return;
            }

            if (!leaderMember.m_IsGroupLeader || leaderMember.m_GroupEntity == Entity.Null)
            {
                return;
            }

            if (!job.m_ExtraTypeHandle.m_TrafficGroup.TryGetComponent(leaderMember.m_GroupEntity, out var group))
            {
                return;
            }

            if (!group.m_IsCoordinated)
            {
                return;
            }

            foreach (var memberEntity in groupMemberEntities)
            {
                if (memberEntity == leaderEntity || memberEntity == Entity.Null)
                {
                    continue;
                }

                if (!job.m_ExtraTypeHandle.m_TrafficGroupMember.TryGetComponent(memberEntity, out var member))
                {
                    continue;
                }

                if (member.m_GroupEntity != leaderMember.m_GroupEntity)
                {
                    continue;
                }

                if (!job.m_ExtraTypeHandle.m_CustomPhaseDataLookup.TryGetBuffer(memberEntity, out var memberPhaseData))
                {
                    continue;
                }

                int phaseCount = math.min(leaderPhaseDataBuffer.Length, memberPhaseData.Length);
                for (int i = 0; i < phaseCount; i++)
                {
                    CustomPhaseData leaderPhase = leaderPhaseDataBuffer[i];
                    CustomPhaseData memberPhase = memberPhaseData[i];

                    leaderPhase.m_Priority = math.max(leaderPhase.m_Priority, memberPhase.m_Priority);
                    leaderPhase.m_CarLaneOccupied += memberPhase.m_CarLaneOccupied;
                    leaderPhase.m_PublicCarLaneOccupied += memberPhase.m_PublicCarLaneOccupied;
                    leaderPhase.m_TrackLaneOccupied += memberPhase.m_TrackLaneOccupied;
                    leaderPhase.m_PedestrianLaneOccupied += memberPhase.m_PedestrianLaneOccupied;
                    leaderPhase.m_BicycleLaneOccupied += memberPhase.m_BicycleLaneOccupied;
                    
                    leaderPhase.m_CurrentFlow += memberPhase.m_CurrentFlow;
                    leaderPhase.m_CurrentWait += memberPhase.m_CurrentWait;
                    leaderPhase.m_WeightedWaiting += memberPhase.m_WeightedWaiting;

                    leaderPhaseDataBuffer[i] = leaderPhase;
                }
            }
        }

        public static void SyncSignalGroupWithLeader(
            PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
            Entity currentEntity,
            Entity groupEntity,
            ref TrafficLights trafficLights,
            ref CustomTrafficLights customTrafficLights)
        {
            if (!job.m_ExtraTypeHandle.m_TrafficGroup.TryGetComponent(groupEntity, out var group))
            {
                return;
            }

            if (!job.m_ExtraTypeHandle.m_TrafficGroupMember.TryGetComponent(currentEntity, out var member))
            {
                return;
            }

            int followerPhaseCount = trafficLights.m_SignalGroupCount;
            int masterPhaseCount = group.m_MasterSignalGroupCount;

            if (group.m_GreenWaveEnabled && member.m_SignalDelay != 0)
            {
                // Green wave mode: use offset-based staggering
                int signalDelay = GetSignalDelayForJunction(job, currentEntity, trafficLights.m_CurrentSignalGroup);
                int mappedPhase = WrapPhase(group.m_MasterPhase + member.m_PhaseOffset, followerPhaseCount);
                int mappedNext = WrapPhase(group.m_MasterNextPhase + member.m_PhaseOffset, followerPhaseCount);

                trafficLights.m_State = group.m_MasterState;
                trafficLights.m_CurrentSignalGroup = (byte)mappedPhase;
                trafficLights.m_NextSignalGroup = (byte)mappedNext;

                int adjustedTimer = group.m_MasterTimer - signalDelay;
                trafficLights.m_Timer = (byte)math.clamp(adjustedTimer, 0, 255);

                int adjustedCustomTimer = (int)group.m_MasterCustomTimer - signalDelay;
                customTrafficLights.m_Timer = (uint)math.max(0, adjustedCustomTimer);
            }
            else
            {
                // TMPE-style lockstep: mirror master state directly
                int mappedPhase = WrapPhase(group.m_MasterPhase, followerPhaseCount);
                int mappedNext = WrapPhase(group.m_MasterNextPhase, followerPhaseCount);

                trafficLights.m_State = group.m_MasterState;
                trafficLights.m_CurrentSignalGroup = (byte)mappedPhase;
                trafficLights.m_NextSignalGroup = (byte)mappedNext;
                trafficLights.m_Timer = group.m_MasterTimer;
                customTrafficLights.m_Timer = group.m_MasterCustomTimer;
            }
        }

        // TMPE-style: simple wrap of a 1-indexed phase into follower's valid range
        private static int WrapPhase(int phase, int phaseCount)
        {
            if (phaseCount <= 0)
            {
                return 1;
            }
            if (phase <= 0)
            {
                return 1;
            }
            int wrapped = ((phase - 1) % phaseCount) + 1;
            if (wrapped <= 0) wrapped += phaseCount;
            return wrapped;
        }

        private static int GetSignalDelayForJunction(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, Entity junctionEntity, byte currentSignalGroup, bool isClosingDelay = true)
        {
            int signalDelay = 0;
            
            
            if (job.m_ExtraTypeHandle.m_EdgeGroupMaskLookup.TryGetBuffer(junctionEntity, out DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer))
            {
                int signalIndex = currentSignalGroup - 1;
                if (signalIndex >= 0 && signalIndex < edgeGroupMaskBuffer.Length)
                {
                    var edgeMask = edgeGroupMaskBuffer[signalIndex];
                    signalDelay += isClosingDelay ? edgeMask.m_CloseDelay : edgeMask.m_OpenDelay;
                }
            }
            
            
            if (job.m_ExtraTypeHandle.m_SignalDelayLookup.TryGetBuffer(junctionEntity, out DynamicBuffer<SignalDelayData> signalDelayBuffer))
            {
                for (int i = 0; i < signalDelayBuffer.Length; i++)
                {
                    var delayData = signalDelayBuffer[i];
                    if (delayData.m_IsEnabled)
                    {
                        signalDelay += isClosingDelay ? delayData.m_CloseDelay : delayData.m_OpenDelay;
                    }
                }
            }
            
            return signalDelay;
        }

        private static bool ShouldChangeByMetric(CustomPhaseData.StepChangeMetric metric, float flow, float wait)
        {
            switch (metric)
            {
                case CustomPhaseData.StepChangeMetric.FirstFlow:
                    return flow > 0;
                case CustomPhaseData.StepChangeMetric.FirstWait:
                    return wait > 0;
                case CustomPhaseData.StepChangeMetric.NoFlow:
                    return flow <= 0;
                case CustomPhaseData.StepChangeMetric.NoWait:
                    return wait <= 0;
                case CustomPhaseData.StepChangeMetric.Default:
                default:
                    return flow < wait;
            }
        }
    }
}