using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using C2VM.TrafficLightsEnhancement.Components;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.Serialization
{
    public partial class TLEDataMigrationSystem : GameSystemBase, IDefaultSerializable, ISerializable
    {
        private EntityQuery _customTrafficLightsQuery;
        private EntityQuery _trafficGroupQuery;
        private EntityQuery _trafficGroupMemberQuery;
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
        }

        protected override void OnUpdate()
        {
            if (!_loaded)
            {
                return;
            }

            Mod.m_Log.Info($"{nameof(TLEDataMigrationSystem)} validating data version {_version}...");
            _loaded = false;

            int affectedCount = 0;
            int totalEntities = 0;

            // Validate CustomTrafficLights
            if (!_customTrafficLightsQuery.IsEmptyIgnoreFilter)
            {
                var result = ValidateCustomTrafficLights();
                affectedCount += result.affected;
                totalEntities += result.total;
            }

            // Validate TrafficGroups
            if (!_trafficGroupQuery.IsEmptyIgnoreFilter)
            {
                var result = ValidateTrafficGroups();
                affectedCount += result.affected;
                totalEntities += result.total;
            }

            // Validate TrafficGroupMembers
            if (!_trafficGroupMemberQuery.IsEmptyIgnoreFilter)
            {
                var result = ValidateTrafficGroupMembers();
                affectedCount += result.affected;
                totalEntities += result.total;
            }

            // Run version-specific migrations
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

            Mod.m_Log.Info($"{nameof(TLEDataMigrationSystem)} migration complete. Version {_version} -> {TLEDataVersion.Current}");
        }

        private (int affected, int total) ValidateCustomTrafficLights()
        {
            int affected = 0;
            int total = 0;

            using (var entities = _customTrafficLightsQuery.ToEntityArray(Allocator.Temp))
            {
                total = entities.Length;
                foreach (var entity in entities)
                {
                    if (!EntityManager.HasComponent<Node>(entity))
                    {
                        // CustomTrafficLights on non-node entity - invalid
                        EntityManager.RemoveComponent<CustomTrafficLights>(entity);
                        affected++;
                        Mod.m_Log.Warn($"Removed invalid CustomTrafficLights from entity {entity}");
                    }
                }
            }

            return (affected, total);
        }

        private (int affected, int total) ValidateTrafficGroups()
        {
            int affected = 0;
            int total = 0;

            using (var entities = _trafficGroupQuery.ToEntityArray(Allocator.Temp))
            {
                total = entities.Length;
                foreach (var entity in entities)
                {
                    var group = EntityManager.GetComponentData<TrafficGroup>(entity);
                    
                    // Validate group settings
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
                    
                    if (needsUpdate)
                    {
                        EntityManager.SetComponentData(entity, group);
                        affected++;
                    }
                }
            }

            return (affected, total);
        }

        private (int affected, int total) ValidateTrafficGroupMembers()
        {
            int affected = 0;
            int total = 0;

            using (var entities = _trafficGroupMemberQuery.ToEntityArray(Allocator.Temp))
            {
                total = entities.Length;
                foreach (var entity in entities)
                {
                    var member = EntityManager.GetComponentData<TrafficGroupMember>(entity);
                    
                    // Check if group entity still exists
                    if (member.m_GroupEntity != Entity.Null && !EntityManager.Exists(member.m_GroupEntity))
                    {
                        EntityManager.RemoveComponent<TrafficGroupMember>(entity);
                        affected++;
                        Mod.m_Log.Warn($"Removed orphaned TrafficGroupMember from entity {entity}");
                        continue;
                    }
                    
                    // Check if leader entity still exists
                    if (member.m_LeaderEntity != Entity.Null && !EntityManager.Exists(member.m_LeaderEntity))
                    {
                        member.m_LeaderEntity = Entity.Null;
                        EntityManager.SetComponentData(entity, member);
                        affected++;
                    }
                }
            }

            return (affected, total);
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
                    
                    // Reset phase offset if out of valid range
                    if (member.m_PhaseOffset < 0 || member.m_PhaseOffset > 16)
                    {
                        member.m_PhaseOffset = 0;
                        needsUpdate = true;
                    }
                    
                    // Reset signal delay if negative
                    if (member.m_SignalDelay < 0)
                    {
                        member.m_SignalDelay = 0;
                        needsUpdate = true;
                    }
                    
                    // Reset invalid group index
                    if (member.m_GroupIndex < 0)
                    {
                        member.m_GroupIndex = 0;
                        needsUpdate = true;
                    }
                    
                    // Reset negative distances
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
                        
                    var buffer = EntityManager.GetBuffer<SignalDelayData>(entity);
                    bool bufferModified = false;
                    
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        var delayData = buffer[i];
                        
                        // Remove entries with invalid edge references
                        if (delayData.m_Edge != Entity.Null && !EntityManager.Exists(delayData.m_Edge))
                        {
                            buffer.RemoveAt(i);
                            removedCount++;
                            bufferModified = true;
                            continue;
                        }
                        
                        bool needsUpdate = false;
                        
                        // Clamp delay values to valid range (0-300 seconds)
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
