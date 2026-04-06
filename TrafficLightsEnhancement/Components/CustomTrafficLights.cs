using System;
using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomTrafficLights : IComponentData, IQueryTypeParameter, ISerializable
{
  private Patterns m_Pattern;
  private TrafficMode m_Mode;
  public uint m_Timer;
  public byte m_ManualSignalGroup;
  public TrafficOptions m_Options;

  public float m_PedestrianPhaseDurationMultiplier { get; private set; }

  public int m_PedestrianPhaseGroupMask { get; private set; }

  public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
  {
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
    m_Pattern = Patterns.Vanilla;
    m_Mode = TrafficMode.Dynamic;
    m_Options = TrafficOptions.SmartPhaseSelection;

    reader.Read(out int version);
    if (version <= TLEDataVersion.V1)
    {
      for (int i = 1; i < 16; i++)
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
    else
    {
      reader.Read(out uint pattern);
      m_Pattern = (Patterns)pattern;

      reader.Read(out float pedestrianPhaseDurationMultiplier);
      reader.Read(out int pedestrianPhaseGroupMask);
      m_PedestrianPhaseDurationMultiplier = pedestrianPhaseDurationMultiplier;
      m_PedestrianPhaseGroupMask = pedestrianPhaseGroupMask;

      if (version >= TLEDataVersion.V4)
      {
        reader.Read(out m_Timer);
        reader.Read(out m_ManualSignalGroup);
      }

      if (version >= TLEDataVersion.V5)
      {
        reader.Read(out uint mode);
        reader.Read(out uint options);
        m_Mode = (TrafficMode)mode;
        m_Options = (TrafficOptions)options;
      }
    }
  }

  public CustomTrafficLights()
  {
    m_Pattern = Patterns.Vanilla;
    m_Mode = TrafficMode.Dynamic;
    m_Options = TrafficOptions.SmartPhaseSelection;
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = (byte) 0;
  }

  public CustomTrafficLights(Patterns pattern, TrafficMode mode = TrafficMode.Dynamic)
  {
    m_Pattern = pattern;
    m_Mode = mode;
    m_Options = TrafficOptions.SmartPhaseSelection;
    m_PedestrianPhaseDurationMultiplier = 1f;
    m_PedestrianPhaseGroupMask = 0;
    m_Timer = 0U;
    m_ManualSignalGroup = (byte) 0;
  }

  

  public Patterns GetPattern()
    {
        return m_Pattern;
    }

    public Patterns GetPatternOnly()
    {
        return (Patterns)((uint)GetPattern() & 0xFFFF);
    }

    public void SetPattern(uint pattern)
    {
        SetPattern((Patterns)pattern);
    }

    public void SetPattern(Patterns pattern)
    {
        m_Pattern = pattern;
    }

    public void SetPatternOnly(Patterns pattern)
    {
        m_Pattern = (Patterns)(((uint)m_Pattern & 0xFFFF0000) | ((uint)pattern & 0xFFFF));
    }
    public void SetModeOnly(TrafficMode mode)
    {
      m_Mode = (TrafficMode)(((uint)m_Mode & 0xFFFF0000) | ((uint)mode & 0xFFFF));
    }
    public void SetOptionsOnly(TrafficOptions options)
    {
      m_Options = (TrafficOptions)(((uint)m_Options & 0xFFFF0000) | ((uint)options & 0xFFFF));
    }
    public void SetMode(TrafficMode mode)
    {
        m_Mode = mode;
    }
    public void SetOptions(TrafficOptions options)
    {
        m_Options = options;
    }
    public TrafficMode GetMode()
    {
      return m_Mode;
    }
    public TrafficOptions GetOptions()
    {
      return m_Options;
    }
    public TrafficMode GetModeOnly()
    {
      return (TrafficMode)((uint)m_Mode & 0xFFFF);
    }
    public TrafficOptions GetOptionsOnly()
    {
      return (TrafficOptions)((uint)m_Options & 0xFFFF);
    }

  

  public void SetPedestrianPhaseDurationMultiplier(float durationMultiplier)
  {
    m_PedestrianPhaseDurationMultiplier = durationMultiplier;
  }

  public void SetPedestrianPhaseGroupMask(int groupMask)
  {
    m_PedestrianPhaseGroupMask = groupMask;
  }

  

  public enum TrafficMode : uint
  {
    Dynamic = 0,
    FixedTimed = 1
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
    SmartPhaseSelection = 524288
  }
  public enum TrafficOptions : uint
  {
    None = 0,
    SmartPhaseSelection = 1
  }
}