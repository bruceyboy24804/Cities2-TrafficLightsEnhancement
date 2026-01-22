using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Unity.Entities;
using Unity.Mathematics;
using static C2VM.TrafficLightsEnhancement.Systems.UI.UITypes;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct EdgeGroupMask : IBufferElementData, ISerializable, IJsonWritable
{
    public enum Options : uint
    {
        PerLaneSignal = 1 << 0
    }

    public Entity m_Edge;

    public WorldPosition m_Position;

    public Options m_Options;

    public GroupMask.Turn m_Car;

    public GroupMask.Turn m_PublicCar;

    public GroupMask.Turn m_Track;

    public GroupMask.Signal m_Pedestrian;

    public GroupMask.Signal m_Bicycle;

    public short m_OpenDelay;
    public short m_CloseDelay;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write((ushort)TLEDataVersion.V2);
        writer.Write(m_Edge);
        writer.Write((float3)m_Position);
        writer.Write((uint)m_Options);
        writer.Write(m_Car);
        writer.Write(m_PublicCar);
        writer.Write(m_Track);
        writer.Write(m_Pedestrian);
        writer.Write(m_Bicycle);
        writer.Write(m_OpenDelay);
        writer.Write(m_CloseDelay);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        m_Bicycle = new GroupMask.Signal();
        m_OpenDelay = 0;
        m_CloseDelay = 0;
        
        reader.Read(out ushort version);
        reader.Read(out m_Edge);
        reader.Read(out float3 edgePosition);
        reader.Read(out uint options);
        reader.Read(out m_Car);
        reader.Read(out m_PublicCar);
        reader.Read(out m_Track);
        
        if (version < TLEDataVersion.V2)
        {
            reader.Read(out GroupMask.Signal signal1);
            reader.Read(out GroupMask.Signal signal2);
            m_Pedestrian = signal1.m_GoGroupMask != 0 ? signal1 : signal2;
        }
        else
        {
            reader.Read(out m_Pedestrian);
            reader.Read(out m_Bicycle);
            reader.Read(out m_OpenDelay);
            reader.Read(out m_CloseDelay);
        }
        
        m_Position = edgePosition;
        m_Options = (Options)options;
    }

    public void Write(IJsonWriter writer)
    {
        writer.TypeBegin(typeof(EdgeGroupMask).FullName);
        writer.PropertyName("m_Edge");
        writer.Write(m_Edge);
        writer.PropertyName("m_Position");
        writer.Write<WorldPosition>(m_Position);
        writer.PropertyName("m_Options");
        writer.Write((uint)m_Options);
        writer.PropertyName("m_Car");
        writer.Write(m_Car);
        writer.PropertyName("m_PublicCar");
        writer.Write(m_PublicCar);
        writer.PropertyName("m_Track");
        writer.Write(m_Track);
        writer.PropertyName("m_Pedestrian");
        writer.Write(m_Pedestrian);
        writer.PropertyName("m_Bicycle");
        writer.Write(m_Bicycle);
        writer.PropertyName("m_OpenDelay");
        writer.Write(m_OpenDelay);
        writer.PropertyName("m_CloseDelay");
        writer.Write(m_CloseDelay);
        writer.TypeEnd();
    }

    public EdgeGroupMask()
    {
        m_Edge = Entity.Null;
        m_Position = 0.0f;
        m_Options = 0;
        m_Car = new GroupMask.Turn();
        m_PublicCar = new GroupMask.Turn();
        m_Track = new GroupMask.Turn();
        m_Pedestrian = new GroupMask.Signal();
        m_Bicycle = new GroupMask.Signal();
        m_OpenDelay = 0;
        m_CloseDelay = 0;
    }

    public EdgeGroupMask(Entity edge, float3 position)
    {
        m_Edge = edge;
        m_Position = position;
        m_Options = 0;
        m_Car = new GroupMask.Turn();
        m_PublicCar = new GroupMask.Turn();
        m_Track = new GroupMask.Turn();
        m_Pedestrian = new GroupMask.Signal();
        m_Bicycle = new GroupMask.Signal();
        m_OpenDelay = 0;
        m_CloseDelay = 0;
    }

    public EdgeGroupMask(Entity edge, float3 position, EdgeGroupMask newValue)
    {
        m_Edge = edge;
        m_Position = position;
        m_Options = newValue.m_Options;
        m_Car = newValue.m_Car;
        m_PublicCar = newValue.m_PublicCar;
        m_Track = newValue.m_Track;
        m_Pedestrian = newValue.m_Pedestrian;
        m_Bicycle = newValue.m_Bicycle;
        m_OpenDelay = newValue.m_OpenDelay;
        m_CloseDelay = newValue.m_CloseDelay;
    }

    public EdgeGroupMask(EdgeGroupMask oldValue, EdgeGroupMask newValue)
    {
        m_Edge = oldValue.m_Edge;
        m_Position = oldValue.m_Position;
        m_Options = newValue.m_Options;
        m_Car = newValue.m_Car;
        m_PublicCar = newValue.m_PublicCar;
        m_Track = newValue.m_Track;
        m_Pedestrian = newValue.m_Pedestrian;
        m_Bicycle = newValue.m_Bicycle;
        m_OpenDelay = newValue.m_OpenDelay;
        m_CloseDelay = newValue.m_CloseDelay;
    }
}