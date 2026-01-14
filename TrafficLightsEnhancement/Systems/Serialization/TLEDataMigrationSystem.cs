using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Entities;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace C2VM.TrafficLightsEnhancement.Systems.Serialization
{
    public partial class TLEDataMigrationSystem : GameSystemBase, IDefaultSerializable, ISerializable
    {
        private EntityQuery _customTrafficLightsQuery;
        private EntityQuery _trafficGroupQuery;
        private EntityQuery _trafficGroupMemberQuery;
        private EntityQuery _extraLaneSignalQuery;
        private EntityQuery _edgeGroupMaskQuery;
        private EntityQuery _customPhaseDataQuery;
        private int _version;
        private bool _loaded = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            _customTrafficLightsQuery = SystemAPI.QueryBuilder()
                .WithAll<CustomTrafficLights>()
                .Build();

            _trafficGroupQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficGroup>()
                .Build();

            _trafficGroupMemberQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficGroupMember>()
                .Build();

            _extraLaneSignalQuery = SystemAPI.QueryBuilder()
                .WithAll<ExtraLaneSignal>()
                .Build();

            _edgeGroupMaskQuery = SystemAPI.QueryBuilder()
                .WithAll<EdgeGroupMask>()
                .Build();

            _customPhaseDataQuery = SystemAPI.QueryBuilder()
                .WithAll<CustomPhaseData>()
                .Build();
        }

        protected override void OnUpdate()
        {
            if (!_loaded)
            {
                return;
            }

            Mod.m_Log.Info($"{nameof(TLEDataMigrationSystem)} validating data version {_version}...");
            _loaded = false;

            var invalidEntities = new NativeQueue<Entity>(Allocator.TempJob);
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var entityStorageInfoLookup = GetEntityStorageInfoLookup();

            int totalEntities = 0;
            JobHandle jobHandle = default;

            
            if (!_extraLaneSignalQuery.IsEmptyIgnoreFilter)
            {
                totalEntities += _extraLaneSignalQuery.CalculateEntityCount();
                var extraLaneSignalJob = new ValidateExtraLaneSignalJob
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    extraLaneSignalTypeHandle = GetComponentTypeHandle<ExtraLaneSignal>(),
                    subLaneData = GetBufferLookup<Game.Net.SubLane>(true),
                    entityInfoLookup = entityStorageInfoLookup,
                    invalidEntities = invalidEntities.AsParallelWriter(),
                    commandBuffer = commandBuffer.AsParallelWriter()
                };
                jobHandle = extraLaneSignalJob.ScheduleParallel(_extraLaneSignalQuery, jobHandle);
            }

            
            if (!_customTrafficLightsQuery.IsEmptyIgnoreFilter)
            {
                totalEntities += _customTrafficLightsQuery.CalculateEntityCount();
                var customTrafficLightsJob = new ValidateCustomTrafficLightsJob
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    customTrafficLightsTypeHandle = GetComponentTypeHandle<CustomTrafficLights>(),
                    nodeData = GetComponentLookup<Node>(true),
                    entityInfoLookup = entityStorageInfoLookup,
                    invalidEntities = invalidEntities.AsParallelWriter(),
                    commandBuffer = commandBuffer.AsParallelWriter()
                };
                jobHandle = customTrafficLightsJob.ScheduleParallel(_customTrafficLightsQuery, jobHandle);
            }

            
            if (!_trafficGroupQuery.IsEmptyIgnoreFilter)
            {
                totalEntities += _trafficGroupQuery.CalculateEntityCount();
                var trafficGroupJob = new ValidateTrafficGroupJob
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    trafficGroupTypeHandle = GetComponentTypeHandle<TrafficGroup>(),
                    invalidEntities = invalidEntities.AsParallelWriter()
                };
                jobHandle = trafficGroupJob.ScheduleParallel(_trafficGroupQuery, jobHandle);
            }

            
            if (!_trafficGroupMemberQuery.IsEmptyIgnoreFilter)
            {
                totalEntities += _trafficGroupMemberQuery.CalculateEntityCount();
                var trafficGroupMemberJob = new ValidateTrafficGroupMemberJob
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    trafficGroupMemberTypeHandle = GetComponentTypeHandle<TrafficGroupMember>(),
                    trafficGroupData = GetComponentLookup<TrafficGroup>(true),
                    entityInfoLookup = entityStorageInfoLookup,
                    invalidEntities = invalidEntities.AsParallelWriter(),
                    commandBuffer = commandBuffer.AsParallelWriter()
                };
                jobHandle = trafficGroupMemberJob.ScheduleParallel(_trafficGroupMemberQuery, jobHandle);
            }

            
            if (!_edgeGroupMaskQuery.IsEmptyIgnoreFilter)
            {
                totalEntities += _edgeGroupMaskQuery.CalculateEntityCount();
                var edgeGroupMaskJob = new ValidateEdgeGroupMaskJob
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    edgeGroupMaskTypeHandle = GetBufferTypeHandle<EdgeGroupMask>(),
                    edgeData = GetComponentLookup<Edge>(true),
                    entityInfoLookup = entityStorageInfoLookup,
                    invalidEntities = invalidEntities.AsParallelWriter(),
                    commandBuffer = commandBuffer.AsParallelWriter()
                };
                jobHandle = edgeGroupMaskJob.ScheduleParallel(_edgeGroupMaskQuery, jobHandle);
            }

            
            if (!_customPhaseDataQuery.IsEmptyIgnoreFilter)
            {
                totalEntities += _customPhaseDataQuery.CalculateEntityCount();
                var customPhaseDataJob = new ValidateCustomPhaseDataJob
                {
                    entityTypeHandle = GetEntityTypeHandle(),
                    customPhaseDataTypeHandle = GetBufferTypeHandle<CustomPhaseData>(),
                    invalidEntities = invalidEntities.AsParallelWriter()
                };
                jobHandle = customPhaseDataJob.ScheduleParallel(_customPhaseDataQuery, jobHandle);
            }

            
            jobHandle.Complete();

            
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            
            int affectedCount = invalidEntities.Count;
            invalidEntities.Dispose();

            
            if (_version < TLEDataVersion.V2)
            {
                MigrateTrafficGroupMembers();
            }

            if (_version < TLEDataVersion.V1)
            {
                MigrateSignalDelayData();
            }

            if (affectedCount > 0)
            {
                Mod.m_Log.Warn($"{nameof(TLEDataMigrationSystem)} found {affectedCount} affected entities of {totalEntities} total");
                
                var messageDialog = new MessageDialog(
                    "Traffic Lights Enhancement - Data Migration",
                    $"Traffic Lights Enhancement mod detected data from an older version.\n\n" +
                    $"Migrated {affectedCount} of {totalEntities} entities.\n\n" +
                    "Some traffic light configurations may need to be reconfigured.",
                    LocalizedString.Id("Common.OK"));
                GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
            }

            
            CheckGroupsWithMissingPhases();

            Mod.m_Log.Info($"{nameof(TLEDataMigrationSystem)} migration complete. Version {_version} -> {TLEDataVersion.Current}");
        }

        private void MigrateTrafficGroupMembers()
        {
            Mod.m_Log.Info($"Migrating TrafficGroupMember data to version {TLEDataVersion.V2}");
            
            int migratedCount = 0;
            using (var entities = _trafficGroupMemberQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var member = EntityManager.GetComponentData<TrafficGroupMember>(entity);
                    bool needsUpdate = false;
                    
                    
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
                    
                    if (needsUpdate)
                    {
                        EntityManager.SetComponentData(entity, member);
                        migratedCount++;
                    }
                }
            }
            
            Mod.m_Log.Info($"Migrated {migratedCount} TrafficGroupMember entities");
        }

        private void MigrateSignalDelayData()
        {
            Mod.m_Log.Info($"Migrating SignalDelayData to version {TLEDataVersion.V2}");
            
            int migratedCount = 0;
            int removedCount = 0;
            
            var signalDelayQuery = SystemAPI.QueryBuilder()
                .WithAll<SignalDelayData>()
                .Build();
            
            using (var entities = signalDelayQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    if (!EntityManager.HasBuffer<SignalDelayData>(entity))
                        continue;
                        
                    EntityManager.TryGetBuffer<SignalDelayData>(entity, false, out var buffer);
                    bool bufferModified = false;
                    
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        var delayData = buffer[i];
                        
                        
                        if (delayData.m_Edge != Entity.Null && !EntityManager.Exists(delayData.m_Edge))
                        {
                            buffer.RemoveAt(i);
                            removedCount++;
                            bufferModified = true;
                            continue;
                        }
                        
                        bool needsUpdate = false;
                        
                        
                        if (delayData.m_OpenDelay < 0 || delayData.m_OpenDelay > 300)
                        {
                            delayData.m_OpenDelay = System.Math.Clamp(delayData.m_OpenDelay, 0, 300);
                            needsUpdate = true;
                        }
                        
                        if (delayData.m_CloseDelay < 0 || delayData.m_CloseDelay > 300)
                        {
                            delayData.m_CloseDelay = System.Math.Clamp(delayData.m_CloseDelay, 0, 300);
                            needsUpdate = true;
                        }
                        
                        if (needsUpdate)
                        {
                            buffer[i] = delayData;
                            migratedCount++;
                            bufferModified = true;
                        }
                    }
                }
            }
            
            Mod.m_Log.Info($"Migrated {migratedCount} SignalDelayData entries, removed {removedCount} invalid entries");
        }

        private void CheckGroupsWithMissingPhases()
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            var affectedGroups = new NativeList<Entity>(Allocator.Temp);
            int affectedFollowerCount = 0;

            using (var groupEntities = _trafficGroupQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (var groupEntity in groupEntities)
                {
                    Entity leaderEntity = trafficGroupSystem.GetGroupLeader(groupEntity);
                    if (leaderEntity == Entity.Null)
                        continue;

                    
                    if (!EntityManager.HasComponent<CustomTrafficLights>(leaderEntity))
                        continue;

                    var leaderLights = EntityManager.GetComponentData<CustomTrafficLights>(leaderEntity);
                    var leaderPattern = leaderLights.GetPatternOnly();

                    
                    if (leaderPattern != CustomTrafficLights.Patterns.CustomPhase)
                        continue;

                    
                    if (!EntityManager.HasBuffer<CustomPhaseData>(leaderEntity))
                        continue;

                    EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false, out var leaderPhases);
                    if (leaderPhases.Length == 0)
                        continue;

                    
                    var members = trafficGroupSystem.GetGroupMembers(groupEntity);
                    bool hasAffectedFollower = false;

                    foreach (var memberEntity in members)
                    {
                        if (memberEntity == leaderEntity)
                            continue;

                        
                        if (EntityManager.HasComponent<CustomTrafficLights>(memberEntity))
                        {
                            var memberLights = EntityManager.GetComponentData<CustomTrafficLights>(memberEntity);
                            var memberPattern = memberLights.GetPatternOnly();

                            if (memberPattern == CustomTrafficLights.Patterns.CustomPhase)
                            {
                                bool hasPhases = EntityManager.HasBuffer<CustomPhaseData>(memberEntity) &&
                                    EntityManager.TryGetBuffer<CustomPhaseData>(memberEntity, false, out var memberPhases);

                                if (!hasPhases )
                                {
                                    hasAffectedFollower = true;
                                    affectedFollowerCount++;
                                }
                            }
                        }
                    }

                    members.Dispose();

                    if (hasAffectedFollower)
                    {
                        affectedGroups.Add(groupEntity);
                    }
                }
            }

            if (affectedGroups.Length > 0)
            {
                Mod.m_Log.Warn($"Found {affectedGroups.Length} groups with {affectedFollowerCount} followers missing custom phases");

                
                _affectedGroupsForMigration = affectedGroups.ToArray(Allocator.Persistent);

                var messageDialog = new MessageDialog(
                    "Traffic Lights Enhancement - Phase Configuration",
                    $"Detected {affectedFollowerCount} group member(s) in {affectedGroups.Length} group(s) that have Custom Phases enabled but no phases configured.\n\n" +
                    "Would you like to copy phase configurations from the group leader to these members?\n\n" +
                    "• Yes - Copy phases from leader (recommended)\n" +
                    "• No - Reset signal configuration (you will need to reconfigure manually)",
                    LocalizedString.Id("Common.YES"),
                    LocalizedString.Id("Common.NO"));

                GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, OnMissingPhasesDialogResult);
            }

            affectedGroups.Dispose();
        }

        private NativeArray<Entity> _affectedGroupsForMigration;

        private void OnMissingPhasesDialogResult(int result)
        {
            if (!_affectedGroupsForMigration.IsCreated)
                return;

            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            bool copyFromLeader = result == 0; 

            Mod.m_Log.Info($"User selected {(copyFromLeader ? "copy from leader" : "reset to vanilla")} for affected followers");

            foreach (var groupEntity in _affectedGroupsForMigration)
            {
                if (!EntityManager.Exists(groupEntity))
                    continue;

                Entity leaderEntity = trafficGroupSystem.GetGroupLeader(groupEntity);
                if (leaderEntity == Entity.Null)
                    continue;

                var members = trafficGroupSystem.GetGroupMembers(groupEntity);

                foreach (var memberEntity in members)
                {
                    if (memberEntity == leaderEntity)
                        continue;

                    if (!EntityManager.HasComponent<CustomTrafficLights>(memberEntity))
                        continue;

                    var memberLights = EntityManager.GetComponentData<CustomTrafficLights>(memberEntity);
                    var memberPattern = memberLights.GetPatternOnly();

                    if (memberPattern != CustomTrafficLights.Patterns.CustomPhase)
                        continue;

                    bool hasPhases = EntityManager.HasBuffer<CustomPhaseData>(memberEntity) &&
                        EntityManager.GetBuffer<CustomPhaseData>(memberEntity).Length > 0;

                    if (hasPhases)
                        continue;

                    if (copyFromLeader)
                    {
                        
                        trafficGroupSystem.CopyPhasesToJunction(leaderEntity, memberEntity);
                        Mod.m_Log.Info($"Copied phases from leader to member {memberEntity.Index}");
                    }
                    else
                    {
                        
                        if (EntityManager.HasBuffer<EdgeGroupMask>(memberEntity))
                        {
                            EntityManager.TryGetBuffer<EdgeGroupMask>(memberEntity, false, out var edgeMasks);
                            edgeMasks.Clear();
                        }

                        
                        if (EntityManager.HasBuffer<SubLaneGroupMask>(memberEntity))
                        {
                            EntityManager.TryGetBuffer<SubLaneGroupMask>(memberEntity, false, out var subLaneMasks);
                            subLaneMasks.Clear();
                        }

                        Mod.m_Log.Info($"Reset EdgeGroupMask for member {memberEntity.Index} - user must reconfigure");
                    }

                    EntityManager.AddComponentData(memberEntity, default(Updated));
                }

                members.Dispose();
            }

            _affectedGroupsForMigration.Dispose();
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            _loaded = true;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(TLEDataVersion.Current);
            Mod.m_Log.Info($"Saving {nameof(TLEDataMigrationSystem)} data version: {TLEDataVersion.Current}");
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out _version);
            Mod.m_Log.Info($"Loaded {nameof(TLEDataMigrationSystem)} data version: {_version}");
        }

        public void SetDefaults(Context context)
        {
            _version = 0;
        }
    }

    public static class TLEDataVersion
    {
        public const int V1 = 1;
        public const int V2 = 2;
        
        public const int Current = V2;
    }
}
