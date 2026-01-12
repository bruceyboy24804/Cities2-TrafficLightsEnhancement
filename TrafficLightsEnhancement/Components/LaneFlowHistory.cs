using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Components
{
    public struct LaneFlowHistory : IComponentData, IQueryTypeParameter, ISerializable
    {
        public float4 m_Duration;

        public float4 m_Distance;

        public uint m_Frame;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write((ushort)TLEDataVersion.V2);
            writer.Write(m_Duration);
            writer.Write(m_Distance);
            writer.Write(m_Frame);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out ushort version);
            
            if (version <= TLEDataVersion.V2)
            {
                reader.Read(out m_Duration);
                reader.Read(out m_Distance);
                reader.Read(out m_Frame);
            }
        }

        public LaneFlowHistory()
        {
            m_Duration = 0;
            m_Distance = 0;
            m_Frame = 0;
        }
    }
}