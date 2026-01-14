using Game.Common;
using Game.Net;
using C2VM.TrafficLightsEnhancement.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.Serialization
{
    public partial class TLEDataMigrationSystem
    {
#if WITH_BURST
        [BurstCompile]
#endif
        private struct ValidateExtraLaneSignalJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<ExtraLaneSignal> extraLaneSignalTypeHandle;
            [ReadOnly] public BufferLookup<Game.Net.SubLane> subLaneData;
            [ReadOnly] public EntityStorageInfoLookup entityInfoLookup;
            public NativeQueue<Entity>.ParallelWriter invalidEntities;
            public EntityCommandBuffer.ParallelWriter commandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
                NativeArray<ExtraLaneSignal> extraLaneSignals = chunk.GetNativeArray(ref extraLaneSignalTypeHandle);

                ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int index))
                {
                    Entity entity = entities[index];
                    ExtraLaneSignal signal = extraLaneSignals[index];
                    bool isValid = true;

                    
                    if (signal.m_SourceSubLane != Entity.Null)
                    {
                        if (!entityInfoLookup.Exists(signal.m_SourceSubLane))
                        {
                            isValid = false;
                        }
                    }

                    if (!isValid)
                    {
                        
                        signal.m_YieldGroupMask = 0;
                        signal.m_IgnorePriorityGroupMask = 0;
                        signal.m_SourceSubLane = Entity.Null;
                        extraLaneSignals[index] = signal;
                        invalidEntities.Enqueue(entity);
                    }
                }
            }
        }

#if WITH_BURST
        [BurstCompile]
#endif
        private struct ValidateCustomTrafficLightsJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<CustomTrafficLights> customTrafficLightsTypeHandle;
            [ReadOnly] public ComponentLookup<Node> nodeData;
            [ReadOnly] public EntityStorageInfoLookup entityInfoLookup;
            public NativeQueue<Entity>.ParallelWriter invalidEntities;
            public EntityCommandBuffer.ParallelWriter commandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
                NativeArray<CustomTrafficLights> customTrafficLights = chunk.GetNativeArray(ref customTrafficLightsTypeHandle);

                ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int index))
                {
                    Entity entity = entities[index];
                    
                    
                    if (!nodeData.HasComponent(entity))
                    {
                        commandBuffer.RemoveComponent<CustomTrafficLights>(unfilteredChunkIndex, entity);
                        invalidEntities.Enqueue(entity);
                    }
                }
            }
        }

#if WITH_BURST
        [BurstCompile]
#endif
        private struct ValidateTrafficGroupMemberJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<TrafficGroupMember> trafficGroupMemberTypeHandle;
            [ReadOnly] public ComponentLookup<TrafficGroup> trafficGroupData;
            [ReadOnly] public EntityStorageInfoLookup entityInfoLookup;
            public NativeQueue<Entity>.ParallelWriter invalidEntities;
            public EntityCommandBuffer.ParallelWriter commandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
                NativeArray<TrafficGroupMember> trafficGroupMembers = chunk.GetNativeArray(ref trafficGroupMemberTypeHandle);

                ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int index))
                {
                    Entity entity = entities[index];
                    TrafficGroupMember member = trafficGroupMembers[index];
                    bool needsUpdate = false;
                    bool shouldRemove = false;

                    
                    if (member.m_GroupEntity != Entity.Null)
                    {
                        if (!entityInfoLookup.Exists(member.m_GroupEntity) || !trafficGroupData.HasComponent(member.m_GroupEntity))
                        {
                            shouldRemove = true;
                        }
                    }
                    else
                    {
                        shouldRemove = true;
                    }

                    
                    if (!shouldRemove && member.m_LeaderEntity != Entity.Null)
                    {
                        if (!entityInfoLookup.Exists(member.m_LeaderEntity))
                        {
                            member.m_LeaderEntity = Entity.Null;
                            needsUpdate = true;
                        }
                    }

                    
                    if (!shouldRemove)
                    {
                        if (member.m_PhaseOffset < 0 || member.m_PhaseOffset > 16)
                        {
                            member.m_PhaseOffset = 0;
                            needsUpdate = true;
                        }
                        if (member.m_SignalDelay < 0)
                        {
                            member.m_SignalDelay = 0;
                            needsUpdate = true;
                        }
                        if (member.m_GroupIndex < 0)
                        {
                            member.m_GroupIndex = 0;
                            needsUpdate = true;
                        }
                        if (member.m_DistanceToGroupCenter < 0)
                        {
                            member.m_DistanceToGroupCenter = 0;
                            needsUpdate = true;
                        }
                        if (member.m_DistanceToLeader < 0)
                        {
                            member.m_DistanceToLeader = 0;
                            needsUpdate = true;
                        }
                    }

                    if (shouldRemove)
                    {
                        commandBuffer.RemoveComponent<TrafficGroupMember>(unfilteredChunkIndex, entity);
                        invalidEntities.Enqueue(entity);
                    }
                    else if (needsUpdate)
                    {
                        trafficGroupMembers[index] = member;
                        invalidEntities.Enqueue(entity);
                    }
                }
            }
        }

#if WITH_BURST
        [BurstCompile]
#endif
        private struct ValidateTrafficGroupJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<TrafficGroup> trafficGroupTypeHandle;
            public NativeQueue<Entity>.ParallelWriter invalidEntities;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
                NativeArray<TrafficGroup> trafficGroups = chunk.GetNativeArray(ref trafficGroupTypeHandle);

                ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int index))
                {
                    Entity entity = entities[index];
                    TrafficGroup group = trafficGroups[index];
                    bool needsUpdate = false;

                    
                    if (group.m_GreenWaveSpeed <= 0)
                    {
                        group.m_GreenWaveSpeed = 50f;
                        needsUpdate = true;
                    }
                    if (group.m_CycleLength <= 0)
                    {
                        group.m_CycleLength = 16f;
                        needsUpdate = true;
                    }
                    if (group.m_MaxCoordinationDistance <= 0)
                    {
                        group.m_MaxCoordinationDistance = 500f;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        trafficGroups[index] = group;
                        invalidEntities.Enqueue(entity);
                    }
                }
            }
        }

#if WITH_BURST
        [BurstCompile]
#endif
        private struct ValidateEdgeGroupMaskJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public BufferTypeHandle<EdgeGroupMask> edgeGroupMaskTypeHandle;
            [ReadOnly] public ComponentLookup<Edge> edgeData;
            [ReadOnly] public EntityStorageInfoLookup entityInfoLookup;
            public NativeQueue<Entity>.ParallelWriter invalidEntities;
            public EntityCommandBuffer.ParallelWriter commandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
                BufferAccessor<EdgeGroupMask> edgeGroupMaskAccessor = chunk.GetBufferAccessor(ref edgeGroupMaskTypeHandle);

                ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int index))
                {
                    Entity entity = entities[index];
                    DynamicBuffer<EdgeGroupMask> buffer = edgeGroupMaskAccessor[index];
                    bool bufferModified = false;

                    
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        EdgeGroupMask mask = buffer[i];
                        if (mask.m_Edge != Entity.Null)
                        {
                            if (!entityInfoLookup.Exists(mask.m_Edge) || !edgeData.HasComponent(mask.m_Edge))
                            {
                                buffer.RemoveAt(i);
                                bufferModified = true;
                            }
                        }
                    }

                    if (bufferModified)
                    {
                        invalidEntities.Enqueue(entity);
                    }
                }
            }
        }

#if WITH_BURST
        [BurstCompile]
#endif
        private struct ValidateCustomPhaseDataJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public BufferTypeHandle<CustomPhaseData> customPhaseDataTypeHandle;
            public NativeQueue<Entity>.ParallelWriter invalidEntities;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
                BufferAccessor<CustomPhaseData> customPhaseDataAccessor = chunk.GetBufferAccessor(ref customPhaseDataTypeHandle);

                ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int index))
                {
                    Entity entity = entities[index];
                    DynamicBuffer<CustomPhaseData> buffer = customPhaseDataAccessor[index];
                    bool bufferModified = false;

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        CustomPhaseData data = buffer[i];
                        bool needsUpdate = false;

                        
                        if (data.m_MinimumDuration > data.m_MaximumDuration)
                        {
                            data.m_MinimumDuration = 2;
                            data.m_MaximumDuration = 300;
                            needsUpdate = true;
                        }
                        if (data.m_TargetDurationMultiplier < 0)
                        {
                            data.m_TargetDurationMultiplier = 1f;
                            needsUpdate = true;
                        }
                        if (data.m_LaneOccupiedMultiplier < 0)
                        {
                            data.m_LaneOccupiedMultiplier = 1f;
                            needsUpdate = true;
                        }
                        if (data.m_WaitFlowBalance < 0)
                        {
                            data.m_WaitFlowBalance = 1f;
                            needsUpdate = true;
                        }

                        if (needsUpdate)
                        {
                            buffer[i] = data;
                            bufferModified = true;
                        }
                    }

                    if (bufferModified)
                    {
                        invalidEntities.Enqueue(entity);
                    }
                }
            }
        }
    }
}
