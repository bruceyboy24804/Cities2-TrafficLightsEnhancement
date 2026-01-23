using System;
using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomTrafficLights : IComponentData, IQueryTypeParameter, ISerializable
{
  private const int DefaultSelectedPatternLength = 16 ;
  private TrafficPattern m_Pattern;
  private TrafficMode m_Mode;
  private TrafficOptions m_Options;
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
    writer.Write((uint)m_Mode);
    writer.Write((uint)m_Options);
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
      
      m_Pattern = TrafficPattern.Vanilla;
    }
    else if (version <= TLEDataVersion.V2)
    {
      reader.Read(out uint pattern);
      m_Pattern = (TrafficPattern)pattern;
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
    else
    {
      reader.Read(out uint pattern);
      m_Pattern = (TrafficPattern)pattern;
      reader.Read(out float pedestrianPhaseDurationMultiplier);
      reader.Read(out int pedestrianPhaseGroupMask);
      m_PedestrianPhaseDurationMultiplier = pedestrianPhaseDurationMultiplier;
      m_PedestrianPhaseGroupMask = pedestrianPhaseGroupMask;
      reader.Read(out m_Timer);
      reader.Read(out m_ManualSignalGroup);
      reader.Read(out uint mode);
      m_Mode = (TrafficMode)mode;
      reader.Read(out uint options);
      m_Options = (TrafficOptions)options;
    }
  }

  public CustomTrafficLights()
  {
    m_Pattern = TrafficPattern.Vanilla;
    m_Mode = TrafficMode.Dynamic;
    m_Options = TrafficOptions.SmartPhaseSelection;
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = (byte) 0;
  }

  public CustomTrafficLights(TrafficPattern pattern, TrafficMode mode = TrafficMode.Dynamic)
  {
    m_Pattern = pattern;
    m_Mode = mode;
    m_Options = TrafficOptions.SmartPhaseSelection;
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = (byte) 0;
  }

  // Legacy constructor for backward compatibility
  public CustomTrafficLights(Patterns oldPattern)
  {
    MigrateFromOldPattern(oldPattern);
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = (byte) 0;
  }

  public TrafficPattern GetPattern() => m_Pattern;
  public TrafficMode GetMode() => m_Mode;
  public TrafficOptions GetOptions() => m_Options;

  public Patterns GetLegacyPattern() 
  {
    Patterns result = (Patterns)m_Pattern;
    if (m_Mode == TrafficMode.FixedTimed)
    {
      result = Patterns.FixedTimed;
    }
    else if (m_Mode == TrafficMode.Dynamic)
    {
      result = Patterns.CustomPhase;
    }
    result |= (Patterns)m_Options;
    return result;
  }

  public Patterns GetPatternOnly()
  {
    return GetLegacyPattern() & (Patterns) 65535 ;
  }

  public void SetPattern(TrafficPattern pattern) => m_Pattern = pattern;
  public void SetMode(TrafficMode mode) => m_Mode = mode;
  public void SetOptions(TrafficOptions options) => m_Options = options;

  // Legacy methods for backward compatibility
  public void SetLegacyPattern(uint pattern) => SetPattern((Patterns) pattern);
  public void SetPattern(Patterns pattern) => MigrateFromOldPattern(pattern);

  public void SetPatternOnly(Patterns pattern)
  {
    m_Pattern = (TrafficPattern)(pattern & (Patterns)65535);
    // Extract mode from old pattern
    if ((pattern & (Patterns)65535) == Patterns.FixedTimed)
    {
      m_Mode = TrafficMode.FixedTimed;
    }
    else if ((pattern & (Patterns)65535) == Patterns.CustomPhase)
    {
      m_Mode = TrafficMode.Dynamic;
    }
  }

  private void MigrateFromOldPattern(Patterns oldPattern)
  {
    Patterns patternOnly = oldPattern & (Patterns)65535;
    
    m_Options = (TrafficOptions)(oldPattern & (Patterns)0xFFFF0000);
    
    switch (patternOnly)
    {
      case Patterns.Vanilla:
        m_Pattern = TrafficPattern.Vanilla;
        m_Mode = TrafficMode.Dynamic;
        break;
      case Patterns.SplitPhasing:
        m_Pattern = TrafficPattern.SplitPhasing;
        m_Mode = TrafficMode.Dynamic;
        break;
      case Patterns.ProtectedCentreTurn:
        m_Pattern = TrafficPattern.ProtectedCentreTurn;
        m_Mode = TrafficMode.Dynamic;
        break;
      case Patterns.SplitPhasingProtectedLeft:
        m_Pattern = TrafficPattern.SplitPhasingProtectedLeft;
        m_Mode = TrafficMode.Dynamic;
        break;
      case Patterns.ModDefault:
        m_Pattern = TrafficPattern.ModDefault;
        m_Mode = TrafficMode.Dynamic;
        break;
      case Patterns.CustomPhase:
        m_Pattern = TrafficPattern.Vanilla; // Default pattern
        m_Mode = TrafficMode.Dynamic;
        break;
      case Patterns.FixedTimed:
        m_Pattern = TrafficPattern.Vanilla; // Default pattern
        m_Mode = TrafficMode.FixedTimed;
        break;
      default:
        m_Pattern = TrafficPattern.Vanilla;
        m_Mode = TrafficMode.Dynamic;
        break;
    }
  }

  public void SetPedestrianPhaseDurationMultiplier(float durationMultiplier)
  {
    m_PedestrianPhaseDurationMultiplier = durationMultiplier;
  }

  public void SetPedestrianPhaseGroupMask(int groupMask)
  {
    m_PedestrianPhaseGroupMask = groupMask;
  }

  public enum TrafficPattern : uint
  {
    Vanilla = 0,
    SplitPhasing = 1,
    ProtectedCentreTurn = 2,
    SplitPhasingProtectedLeft = 3,
    ModDefault = 4
  }

  public enum TrafficMode : uint
  {
    Dynamic = 0,    // Normal traffic-responsive behavior
    FixedTimed = 1  // Fixed duration timing
  }

  public enum TrafficOptions : uint
  {
    None = 0,
    ExclusivePedestrian = 65536,
    AlwaysGreenKerbsideTurn = 131072,
    CentreTurnGiveWay = 262144,
    SmartPhaseSelection = 524288
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
    SmartPhaseSelection = 524288  // Add this line

  }
}