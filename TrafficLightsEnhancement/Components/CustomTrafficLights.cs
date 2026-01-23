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
  private TrafficMode m_Mode;
  private TrafficOptions m_Options;
  public float m_PedestrianPhaseDurationMultiplier { get; private set; }

  public int m_PedestrianPhaseGroupMask { get; private set; }

  public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
  {
    writer.Write(TLEDataVersion.V2);
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

   reader.Read(out int version);
    if (version <= TLEDataVersion.V1)
    {
      for (int i = 1; i < DefaultSelectedPatternLength; i++)
      {
        reader.Read(out uint pattern);
      }
      
      m_Pattern = Patterns.Vanilla;
    }
    else if (version <= TLEDataVersion.V2)
    {
      reader.Read(out uint pattern);
      m_Pattern = (Patterns)pattern;
    }
    else if ( version <= TLEDataVersion.V3 )
    {
      reader.Read(out float pedestrianPhaseDurationMultiplier);
      reader.Read(out int pedestrianPhaseGroupMask);
      m_PedestrianPhaseDurationMultiplier = pedestrianPhaseDurationMultiplier;
      m_PedestrianPhaseGroupMask = pedestrianPhaseGroupMask;
    }
    else if (version <= TLEDataVersion.V4)
    {
      reader.Read(out m_Timer);
      reader.Read(out m_ManualSignalGroup);
    }
    else if ( version <= TLEDataVersion.V5 )
    {
      reader.Read(out  uint mode);
      reader.Read(out uint options);
      m_Mode = (TrafficMode)mode;
      m_Options = (TrafficOptions)options;
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
    return GetPattern() & (Patterns) 65535 /*0xFFFF*/;
  }

  public void SetPattern(uint pattern) => SetPattern((Patterns) pattern);

  public void SetPattern(Patterns pattern) => m_Pattern = pattern;

  public void SetPatternOnly(Patterns pattern)
  {
    m_Pattern = m_Pattern & (Patterns) 4294901760 | pattern & (Patterns) 65535;
  }

  public void SetMode(TrafficMode mode) => m_Mode = mode;

  public void SetOptions(TrafficOptions options) => m_Options = options;

  public TrafficMode GetMode() => m_Mode;

  public TrafficOptions GetOptions() => m_Options;

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
    ExclusivePedestrian = 65536, 
    AlwaysGreenKerbsideTurn = 131072, 
    CentreTurnGiveWay = 262144, 
  }

  public enum TrafficMode : uint
  {
    Dynamic = 0,    
    FixedTimed = 1  
  }

  public enum TrafficOptions : uint
  {
    None = 0,
    SmartPhaseSelection = 1
  }
}