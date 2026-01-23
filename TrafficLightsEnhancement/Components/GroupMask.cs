using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using System.Runtime.InteropServices;

#nullable disable
namespace C2VM.TrafficLightsEnhancement.Components;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct GroupMask
{
  public struct Signal : ISerializable, IJsonWritable
  {
    public ushort m_GoGroupMask;
    public ushort m_YieldGroupMask;
    public float m_OpenDelay;
    public float m_CloseDelay;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
      writer.Write((ushort)TLEDataVersion.V2);
      writer.Write(m_GoGroupMask);
      writer.Write(m_YieldGroupMask);
      writer.Write(m_OpenDelay);
      writer.Write(m_CloseDelay);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
      m_OpenDelay = 0.0f;
      m_CloseDelay = 0.0f;
      
      reader.Read(out ushort version);
      if (version <= TLEDataVersion.V1)
      {
        reader.Read(out m_GoGroupMask);
        reader.Read(out m_YieldGroupMask);
      }
      else if ( version <= TLEDataVersion.V2 )
      {
        reader.Read(out m_OpenDelay);
        reader.Read(out m_CloseDelay);
      }
    }

    public void Write(IJsonWriter writer)
    {
      writer.TypeBegin(typeof (Signal).FullName);
      writer.PropertyName("m_GoGroupMask");
      writer.Write((int) m_GoGroupMask);
      writer.PropertyName("m_YieldGroupMask");
      writer.Write((int) m_YieldGroupMask);
      writer.PropertyName("m_OpenDelay");
      writer.Write(m_OpenDelay);
      writer.PropertyName("m_CloseDelay");
      writer.Write(m_CloseDelay);
      writer.TypeEnd();
    }

    public Signal()
    {
      m_GoGroupMask = 0;
      m_YieldGroupMask = 0;
      m_OpenDelay = 0.0f;
      m_CloseDelay = 0.0f;
    }

    public bool IsAnySet()
    {
      return m_GoGroupMask != (ushort) 0 || m_YieldGroupMask > (ushort) 0;
    }
  }

  public struct Turn : ISerializable, IJsonWritable
  {
    public Signal m_Left;
    public Signal m_Straight;
    public Signal m_Right;
    public Signal m_UTurn;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
      writer.Write(TLEDataVersion.V1);
      writer.Write(m_Left);
      writer.Write(m_Straight);
      writer.Write(m_Right);
      writer.Write(m_UTurn);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
      reader.Read(out int version);
      if (version <= TLEDataVersion.V1)
      {
        reader.Read(out m_Left);
        reader.Read(out m_Straight);
        reader.Read(out m_Right);
        reader.Read(out m_UTurn);
      }
    }

    public void Write(IJsonWriter writer)
    {
      writer.TypeBegin(typeof (Turn).FullName);
      writer.PropertyName("m_Left");
      writer.Write(m_Left);
      writer.PropertyName("m_Straight");
      writer.Write(m_Straight);
      writer.PropertyName("m_Right");
      writer.Write(m_Right);
      writer.PropertyName("m_UTurn");
      writer.Write(m_UTurn);
      writer.TypeEnd();
    }

    public Turn()
    {
      m_Left = new Signal();
      m_Straight = new Signal();
      m_Right = new Signal();
      m_UTurn = new Signal();
    }

    public bool IsAnySet()
    {
      return m_Left.IsAnySet() ||m_Straight.IsAnySet() || m_Right.IsAnySet() || m_UTurn.IsAnySet();
    }
  }
}