using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct SignalDelayData : IBufferElementData, ISerializable
{
    public Entity m_Edge;
    public int m_OpenDelay;
    public int m_CloseDelay;
    public bool m_IsEnabled;
    
    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(TLEDataVersion.Current);
        writer.Write(m_Edge);
        writer.Write(m_OpenDelay);
        writer.Write(m_CloseDelay);
        writer.Write(m_IsEnabled);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out int version);
        
        if (version < TLEDataVersion.V2)
        {
            
            reader.Read(out int entityVersion);
            m_Edge = new Entity { Index = version, Version = entityVersion };
            reader.Read(out m_OpenDelay);
            reader.Read(out m_CloseDelay);
            reader.Read(out m_IsEnabled);
        }
        else
        {
            reader.Read(out m_Edge);
            reader.Read(out m_OpenDelay);
            reader.Read(out m_CloseDelay);
            reader.Read(out m_IsEnabled);
        }
    }

    public SignalDelayData(Entity edge, int openDelay = 0, int closeDelay = 0, bool isEnabled = false)
    {
        m_Edge = edge;
        m_OpenDelay = openDelay;
        m_CloseDelay = closeDelay;
        m_IsEnabled = isEnabled;
    }
}
