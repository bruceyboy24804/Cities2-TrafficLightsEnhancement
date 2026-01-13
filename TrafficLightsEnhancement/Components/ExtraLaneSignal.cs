using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct ExtraLaneSignal : IComponentData, IQueryTypeParameter, ISerializable
{
    public ushort m_YieldGroupMask;

    public ushort m_IgnorePriorityGroupMask;

    public Entity m_SourceSubLane;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(3); // Schema version 3: ushort masks + source sublane
        writer.Write(m_YieldGroupMask);
        writer.Write(m_IgnorePriorityGroupMask);
        writer.Write(m_SourceSubLane);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        // Initialize to defaults first
        m_YieldGroupMask = 0;
        m_IgnorePriorityGroupMask = 0;
        m_SourceSubLane = Entity.Null;
        
        reader.Read(out int version);
        
        // Version 1: old flags format
        if (version == 1)
        {
            reader.Read(out uint flags);
            if ((flags & 1U) > 0U)
                m_YieldGroupMask = ushort.MaxValue;
            if ((flags & 2U) > 0U)
                m_IgnorePriorityGroupMask = ushort.MaxValue;
        }
        
        // Version 2+: ushort masks
        if (version >= TLEDataVersion.V2)
        {
            reader.Read(out m_YieldGroupMask);
            reader.Read(out m_IgnorePriorityGroupMask);
        }
        
        // Version 3+: source sublane
        if (version >= 3)
        {
            reader.Read(out m_SourceSubLane);
        }
    }

    public ExtraLaneSignal()
    {
        m_YieldGroupMask = 0;
        m_IgnorePriorityGroupMask = 0;
        m_SourceSubLane = Entity.Null;
    }
}
