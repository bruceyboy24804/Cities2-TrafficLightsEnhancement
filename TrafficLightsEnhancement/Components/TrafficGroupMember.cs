using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TrafficGroupMember : IComponentData, ISerializable
{
    public Entity m_GroupEntity;
    public Entity m_LeaderEntity;
    public int m_GroupIndex;
    public float m_DistanceToGroupCenter;
    public float m_DistanceToLeader;
    public int m_PhaseOffset;
    public int m_SignalDelay;
    public bool m_IsGroupLeader;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(TLEDataVersion.V2);
        writer.Write(m_GroupEntity);
        writer.Write(m_LeaderEntity);
        writer.Write(m_GroupIndex);
        writer.Write(m_DistanceToGroupCenter);
        writer.Write(m_DistanceToLeader);
        writer.Write(m_PhaseOffset);
        writer.Write(m_SignalDelay);
        writer.Write(m_IsGroupLeader);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out int version);
        
        if (version <= TLEDataVersion.V2)
        {
            reader.Read(out m_GroupEntity);
            reader.Read(out m_LeaderEntity);
            reader.Read(out m_GroupIndex);
            reader.Read(out m_DistanceToGroupCenter);
            reader.Read(out m_DistanceToLeader);
            reader.Read(out m_PhaseOffset);
            reader.Read(out m_SignalDelay);
            reader.Read(out m_IsGroupLeader);
        }
    }

    public TrafficGroupMember()
    {
        m_GroupEntity = Entity.Null;
        m_LeaderEntity = Entity.Null;
        m_GroupIndex = -1;
        m_DistanceToGroupCenter = 0f;
        m_DistanceToLeader = 0f;
        m_PhaseOffset = 0;
        m_SignalDelay = 0;
        m_IsGroupLeader = false;
    }

    public TrafficGroupMember(Entity groupEntity, Entity leaderEntity, int groupIndex, float distanceToCenter = 0f, float distanceToLeader = 0f, int phaseOffset = 0, int signalDelay = 0, bool isGroupLeader = false)
    {
        m_GroupEntity = groupEntity;
        m_LeaderEntity = leaderEntity;
        m_GroupIndex = groupIndex;
        m_DistanceToGroupCenter = distanceToCenter;
        m_DistanceToLeader = distanceToLeader;
        m_PhaseOffset = phaseOffset;
        m_SignalDelay = signalDelay;
        m_IsGroupLeader = isGroupLeader;
    }
}
