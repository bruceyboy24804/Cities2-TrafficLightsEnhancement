using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct ExtraLaneSignal : IComponentData, IQueryTypeParameter, ISerializable
{
    private enum Flags : uint
    {
        Yield = 1 << 0,
        IgnorePriority = 1 << 1
    }

    public ushort m_YieldGroupMask;
    public ushort m_IgnorePriorityGroupMask;
    public Entity m_SourceSubLane;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(TLEDataVersion.V1);
        writer.Write(m_YieldGroupMask);
        writer.Write(m_IgnorePriorityGroupMask);
        writer.Write(m_SourceSubLane);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        m_YieldGroupMask = 0;
        m_IgnorePriorityGroupMask = 0;
        m_SourceSubLane = Entity.Null;

        reader.Read(out int version);
        reader.Read(out m_YieldGroupMask);
        reader.Read(out m_IgnorePriorityGroupMask);
        reader.Read(out m_SourceSubLane);
    }

    public ExtraLaneSignal()
    {
        m_YieldGroupMask = 0;
        m_IgnorePriorityGroupMask = 0;
        m_SourceSubLane = Entity.Null;
    }
}