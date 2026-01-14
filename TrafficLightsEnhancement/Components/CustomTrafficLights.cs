using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomTrafficLights : IComponentData, IQueryTypeParameter, ISerializable
{
  private const int DefaultSelectedPatternLength = 16 ;
  private Patterns m_Pattern;
  public uint m_Timer;
  public byte m_ManualSignalGroup;

  public float m_PedestrianPhaseDurationMultiplier { get; private set; }

  public int m_PedestrianPhaseGroupMask { get; private set; }

  public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
  {
    writer.Write(uint.MaxValue);
    writer.Write(TLEDataVersion.Current);
    writer.Write((uint)m_Pattern);
    writer.Write(m_PedestrianPhaseDurationMultiplier);
    writer.Write(m_PedestrianPhaseGroupMask);
    writer.Write(m_Timer);
    writer.Write(m_ManualSignalGroup);
  }

  public void Deserialize<TReader>(TReader reader) where TReader : IReader
  {
    
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = 0;
    
    reader.Read(out uint marker);
    int version;
    if (marker == uint.MaxValue)
      reader.Read(out version);
    else
      version = 1;
    
    if (version < TLEDataVersion.V2)
    {
      
      for (int i = 1; i < 16; i++)
        reader.Read(out uint _);
      m_Pattern = Patterns.Vanilla;
    }
    else
    {
      reader.Read(out uint pattern);
      m_Pattern = (Patterns)pattern;
      reader.Read(out float pedestrianPhaseDurationMultiplier);
      reader.Read(out int pedestrianPhaseGroupMask);
      m_PedestrianPhaseDurationMultiplier = pedestrianPhaseDurationMultiplier;
      m_PedestrianPhaseGroupMask = pedestrianPhaseGroupMask;
      reader.Read(out m_Timer);
      reader.Read(out m_ManualSignalGroup);
    }
  }

  public CustomTrafficLights()
  {
    m_Pattern = Patterns.Vanilla;
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = (byte) 0;
  }

  public CustomTrafficLights(Patterns pattern)
  {
    m_Pattern = pattern;
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = (byte) 0;
  }

  public Patterns GetPattern() => m_Pattern;

  public Patterns GetPatternOnly()
  {
    return GetPattern() & (Patterns) 65535 ;
  }

  public void SetPattern(uint pattern) => SetPattern((Patterns) pattern);

  public void SetPattern(Patterns pattern) => m_Pattern = pattern;

  public void SetPatternOnly(Patterns pattern)
  {
    m_Pattern = m_Pattern & (Patterns) 4294901760 | pattern & (Patterns) 65535;
  }

  public void SetPedestrianPhaseDurationMultiplier(float durationMultiplier)
  {
    m_PedestrianPhaseDurationMultiplier = durationMultiplier;
  }

  public void SetPedestrianPhaseGroupMask(int groupMask)
  {
    m_PedestrianPhaseGroupMask = groupMask;
  }

  public enum Patterns : uint
  {
    Vanilla = 0,
    SplitPhasing = 1,
    ProtectedCentreTurn = 2,
    SplitPhasingProtectedLeft = 3,
    ModDefault = 4,
    CustomPhase = 5,
    FixedTimed = 6,
    ExclusivePedestrian = 65536, 
    AlwaysGreenKerbsideTurn = 131072, 
    CentreTurnGiveWay = 262144, 
  }
}