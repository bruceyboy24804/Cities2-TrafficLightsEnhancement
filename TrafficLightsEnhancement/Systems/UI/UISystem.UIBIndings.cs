using System.Collections;
using System.Collections.Generic;
using System.Linq;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Extensions;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation;
using C2VM.TrafficLightsEnhancement.Utils;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Net;
using Game.Rendering;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public partial class UISystem
{
    public static GetterValueBinding<string> m_MainPanelBinding { get; private set; }

    private static GetterValueBinding<string> m_LocaleBinding;

    private GetterValueBinding<string> m_CityConfigurationBinding;

    private GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>> m_ScreenPointBinding;

    private GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>> m_EdgeInfoBinding;

    private GetterValueBinding<string> m_SignalDelayDataBinding;

    private ValueBindingHelper<int> m_ActiveEditingCustomPhaseIndexBinding;

    private ValueBindingHelper<int> m_ActiveViewingCustomPhaseIndexBinding;
    internal ValueBindingHelper<UITypes.ToolTooltipMessage[]> m_ToolTooltipMessageBinding;
    
    private GetterValueBinding<string> m_AddMemberStateBinding;

    private void AddUIBindings()
    {
        m_MainPanelBinding = CreateBinding("GetMainPanel", GetMainPanel, autoUpdate: false);
        m_LocaleBinding = CreateBinding("GetLocale", GetLocale, autoUpdate: false);
        m_CityConfigurationBinding = CreateBinding("GetCityConfiguration", GetCityConfiguration, autoUpdate: false);
        
        var screenPointBindingKey = UseKeyPrefixes ? "BINDING:GetScreenPoint" : "GetScreenPoint";
        AddBinding(m_ScreenPointBinding = new GetterValueBinding<Dictionary<string, UITypes.ScreenPoint>>(Mod.modName, screenPointBindingKey, GetScreenPoint, new DictionaryWriter<string, UITypes.ScreenPoint>(null, new ValueWriter<UITypes.ScreenPoint>()), new JsonWriter.FalseEqualityComparer<Dictionary<string, UITypes.ScreenPoint>>()));
        
        var edgeInfoBindingKey = UseKeyPrefixes ? "BINDING:GetEdgeInfo" : "GetEdgeInfo";
        AddBinding(m_EdgeInfoBinding = new GetterValueBinding<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>(Mod.modName, edgeInfoBindingKey, GetEdgeInfo, new JsonWriter.EdgeInfoWriter(), new JsonWriter.FalseEqualityComparer<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>()));
        
        var signalDelayDataBindingKey = UseKeyPrefixes ? "BINDING:GetSignalDelayData" : "GetSignalDelayData";
        m_SignalDelayDataBinding = CreateBinding(signalDelayDataBindingKey, GetSignalDelayData, autoUpdate: false);

        m_ActiveEditingCustomPhaseIndexBinding = CreateBinding("GetActiveEditingCustomPhaseIndex", -1, autoUpdate: false);
        m_ActiveViewingCustomPhaseIndexBinding = CreateBinding("GetActiveViewingCustomPhaseIndex", -1, autoUpdate: false);
        
        var toolTooltipBindingKey = UseKeyPrefixes ? "BINDING:GetToolTooltipMessage" : "GetToolTooltipMessage";
        m_ToolTooltipMessageBinding = new ValueBindingHelper<UITypes.ToolTooltipMessage[]>(new ValueBinding<UITypes.ToolTooltipMessage[]>(Mod.modName, toolTooltipBindingKey, new UITypes.ToolTooltipMessage[] { }, new ListWriter<UITypes.ToolTooltipMessage>(new ValueWriter<UITypes.ToolTooltipMessage>())));
        AddBinding(m_ToolTooltipMessageBinding.Binding);

        CreateTrigger<string>("CallMainPanelUpdatePattern", CallMainPanelUpdatePattern);
        CreateTrigger<string>("CallMainPanelUpdateOption", CallMainPanelUpdateOption);
        CreateTrigger<string>("CallMainPanelUpdateValue", CallMainPanelUpdateValue);
        CreateTrigger<string>("CallMainPanelUpdatePosition", CallMainPanelUpdatePosition);
        CreateTrigger<string>("CallMainPanelSave", CallMainPanelSave);
        CreateTrigger<string>("CallLaneDirectionToolReset", CallLaneDirectionToolReset);

        CreateTrigger<string>("CallSetMainPanelState", CallSetMainPanelState);
        CreateTrigger<string>("CallAddCustomPhase", CallAddCustomPhase);
        CreateTrigger<string>("CallRemoveCustomPhase", CallRemoveCustomPhase);
        CreateTrigger<string>("CallSwapCustomPhase", CallSwapCustomPhase);
        CreateTrigger<string>("CallSetActiveCustomPhaseIndex", CallSetActiveCustomPhaseIndex);
        CreateTrigger<string>("CallUpdateEdgeGroupMask", CallUpdateEdgeGroupMask);
        CreateTrigger<string>("CallUpdateSubLaneGroupMask", CallUpdateSubLaneGroupMask);
        CreateTrigger<string>("CallUpdateCustomPhaseData", CallUpdateCustomPhaseData);
        CreateTrigger<string>("CallSetTrafficGroupSignalDelay", CallSetTrafficGroupSignalDelay);
        CreateTrigger<string>("CallRemoveSignalDelay", CallRemoveSignalDelay);
        CreateTrigger<string>("CallUpdateSignalDelay", CallUpdateSignalDelay);
        CreateTrigger<string>("CallUpdateEdgeDelay", CallUpdateEdgeDelay);

        CreateTrigger<string>("CallKeyPress", CallKeyPress);

        CreateTrigger<string>("CallAddWorldPosition", CallAddWorldPosition);
        CreateTrigger<string>("CallRemoveWorldPosition", CallRemoveWorldPosition);

        CreateTrigger<string>("CallOpenBrowser", CallOpenBrowser);

        CreateTrigger<int>("SetDebugDisplayGroup", (group) => { m_DebugDisplayGroup = group; RedrawGizmo(); });

        CreateTrigger<string>("CallCreateTrafficGroup", CallCreateTrafficGroup);
        CreateTrigger<string>("CallAddJunctionToGroup", CallAddJunctionToGroup);
        CreateTrigger<string>("CallRemoveJunctionFromGroup", CallRemoveJunctionFromGroup);
        CreateTrigger<string>("CallDeleteTrafficGroup", CallDeleteTrafficGroup);
        CreateTrigger<string>("CallSetTrafficGroupName", CallSetTrafficGroupName);
        CreateTrigger<string>("CallSetGreenWaveEnabled", CallSetGreenWaveEnabled);
        CreateTrigger<string>("CallSetGreenWaveSpeed", CallSetGreenWaveSpeed);
        CreateTrigger<string>("CallSetGreenWaveOffset", CallSetGreenWaveOffset);
        CreateTrigger<string>("CallCalculateSignalDelays", CallCalculateSignalDelays);
        CreateTrigger<string>("CallSetCoordinated", CallSetCoordinated);
        CreateTrigger<string>("CallSetCycleLength", CallSetCycleLength);
        CreateTrigger<string>("CallSelectJunction", CallSelectJunction);
        CreateTrigger<string>("CallEnterAddMemberMode", CallEnterAddMemberMode);
        CreateTrigger<string>("CallExitAddMemberMode", CallExitAddMemberMode);
        CreateTrigger<string>("CallFinishAddMemberMode", CallFinishAddMemberMode);
        CreateTrigger<string>("CallGetGroupMembers", CallGetGroupMembers);
        CreateTrigger<string>("CallForceSyncToLeader", CallForceSyncToLeader);
        CreateTrigger<string>("CallRecalculateCycleLength", CallRecalculateCycleLength);
        CreateTrigger<string>("CallJoinGroups", CallJoinGroups);
        CreateTrigger<string>("CallSetGroupLeader", CallSetGroupLeader);
        CreateTrigger<string>("CallSkipStep", CallSkipStep);
        CreateTrigger<string>("CallCopyPhasesToJunction", CallCopyPhasesToJunction);
        CreateTrigger<string>("CallMatchPhaseDurationsToLeader", CallMatchPhaseDurationsToLeader);
        CreateTrigger<string>("CallApplyBestPhase", CallApplyBestPhase);
        CreateTrigger<string>("CallHousekeepingGroup", CallHousekeepingGroup);
        CreateTrigger<string>("CallUpdateMemberPhaseData", CallUpdateMemberPhaseData);
        CreateTrigger<string>("CallUpdateEdgeGroupMaskForJunction", CallUpdateEdgeGroupMaskForJunction);
        CreateTrigger<string>("CallUpdateMemberPattern", CallUpdateMemberPattern);
        CreateTrigger<string>("CallHighlightEdge", CallHighlightEdge);
        AddBinding(new TriggerBinding<Entity>(Mod.modName, "GoTo", NavigateTo));

        m_AddMemberStateBinding = CreateBinding("GetAddMemberState", GetAddMemberState, autoUpdate: false);
    }

    protected string GetMainPanel()
    {
        var menu = new
        {
            title = Mod.IsBeta() ? "TLE Beta" : "Traffic Lights Enhancement",
            image = "Media/Game/Icons/TrafficLights.svg",
            position = m_MainPanelPosition,
            showPanel = m_MainPanelState != MainPanelState.Hidden,
            showFloatingButton = true,
            state = m_MainPanelState,
            selectedEntity = new { index = m_SelectedEntity.Index, version = m_SelectedEntity.Version },
            items = new ArrayList()
        };
        if (m_MainPanelState == MainPanelState.Main && m_SelectedEntity != Entity.Null)
        {
            menu.items.Add(new UITypes.ItemTitle{title = "TrafficSignal"});
            menu.items.Add(UITypes.MainPanelItemPattern("Vanilla", (uint)CustomTrafficLights.Patterns.Vanilla, (uint)m_CustomTrafficLights.GetPattern()));
            if (PredefinedPatternsProcessor.IsValidPattern(m_EdgeInfoDictionary[m_SelectedEntity], CustomTrafficLights.Patterns.SplitPhasing))
            {
                menu.items.Add(UITypes.MainPanelItemPattern("SplitPhasing", (uint)CustomTrafficLights.Patterns.SplitPhasing, (uint)m_CustomTrafficLights.GetPattern()));
            }
            if (PredefinedPatternsProcessor.IsValidPattern(m_EdgeInfoDictionary[m_SelectedEntity], CustomTrafficLights.Patterns.ProtectedCentreTurn))
            {
                if (m_CityConfigurationSystem.leftHandTraffic)
                {
                    menu.items.Add(UITypes.MainPanelItemPattern("ProtectedRightTurns", (uint)CustomTrafficLights.Patterns.ProtectedCentreTurn, (uint)m_CustomTrafficLights.GetPattern()));
                }
                else
                {
                    menu.items.Add(UITypes.MainPanelItemPattern("ProtectedLeftTurns", (uint)CustomTrafficLights.Patterns.ProtectedCentreTurn, (uint)m_CustomTrafficLights.GetPattern()));
                }
            }
            if (PredefinedPatternsProcessor.IsValidPattern(m_EdgeInfoDictionary[m_SelectedEntity], CustomTrafficLights.Patterns.SplitPhasingProtectedLeft))
            {
                menu.items.Add(UITypes.MainPanelItemPattern("SplitPhasingProtectedLeft", (uint)CustomTrafficLights.Patterns.SplitPhasingProtectedLeft, (uint)m_CustomTrafficLights.GetPattern()));
            }
            bool isCustomPhaseMode = m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase || 
                                     m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.FixedTimed;
            menu.items.Add(UITypes.MainPanelItemPattern("CustomPhases", (uint)CustomTrafficLights.Patterns.CustomPhase, 
                isCustomPhaseMode ? (uint)CustomTrafficLights.Patterns.CustomPhase : (uint)m_CustomTrafficLights.GetPattern()));
            if (isCustomPhaseMode)
            {
                menu.items.Add(new UITypes.ItemButton{label = "CustomPhaseEditor", key = "state", value = $"{(int)MainPanelState.CustomPhase}", engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetMainPanelState"});
            }
            if (m_CustomTrafficLights.GetPatternOnly() < CustomTrafficLights.Patterns.ModDefault && !NodeUtils.HasTrainTrack(m_EdgeInfoDictionary[m_SelectedEntity]))
            {
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemTitle{title = "Options"});
                menu.items.Add(UITypes.MainPanelItemOption("AllowTurningOnRed", (uint)CustomTrafficLights.Patterns.AlwaysGreenKerbsideTurn, (uint)m_CustomTrafficLights.GetPattern()));
                if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.Vanilla)
                {
                    menu.items.Add(UITypes.MainPanelItemOption("GiveWayToOncomingVehicles", (uint)CustomTrafficLights.Patterns.CentreTurnGiveWay, (uint)m_CustomTrafficLights.GetPattern()));
                }
                menu.items.Add(UITypes.MainPanelItemOption("ExclusivePedestrianPhase", (uint)CustomTrafficLights.Patterns.ExclusivePedestrian, (uint)m_CustomTrafficLights.GetPattern()));
                if (((uint)m_CustomTrafficLights.GetPattern() & (uint)CustomTrafficLights.Patterns.ExclusivePedestrian) != 0)
                {
                    menu.items.Add(default(UITypes.ItemDivider));
                    menu.items.Add(new UITypes.ItemTitle{title = "Adjustments"});
                    menu.items.Add(new UITypes.ItemRange
                    {
                        key = "CustomPedestrianDurationMultiplier",
                        label = "CustomPedestrianDurationMultiplier",
                        valuePrefix = "",
                        valueSuffix = "CustomPedestrianDurationMultiplierSuffix",
                        min = 0.5f,
                        max = 10,
                        step = 0.5f,
                        defaultValue = 1f,
                        enableTextField = false,
                        value = m_CustomTrafficLights.m_PedestrianPhaseDurationMultiplier,
                        engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallMainPanelUpdateValue"
                    });
                }
            }
            menu.items.Add(default(UITypes.ItemDivider));
            if (EntityManager.HasBuffer<C2VM.CommonLibraries.LaneSystem.CustomLaneDirection>(m_SelectedEntity))
            {
                menu.items.Add(new UITypes.ItemTitle{title = "LaneDirectionTool"});
                menu.items.Add(new UITypes.ItemButton{label = "Reset", key = "status", value = "0", engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallLaneDirectionToolReset"});
                menu.items.Add(default(UITypes.ItemDivider));
            }
            menu.items.Add(new UITypes.ItemButton{label = "TrafficGroups", key = "state", value = $"{(int)MainPanelState.TrafficGroups}", engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetMainPanelState"});
            menu.items.Add(new UITypes.ItemButton{label = "Save", key = "save", value = "1", engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallMainPanelSave"});
            if (m_ShowNotificationUnsaved)
            {
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemNotification{label = "PleaseSave", notificationType = "warning"});
            }
        }
        else if (m_MainPanelState == MainPanelState.CustomPhase)
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, true, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            EntityManager.TryGetComponent(m_SelectedEntity, out TrafficLights trafficLights);
            EntityManager.TryGetComponent(m_SelectedEntity, out CustomTrafficLights customTrafficLights);
            
            // Always add a header item with traffic light mode info (even when no phases exist)
            menu.items.Add(new UITypes.ItemCustomPhaseHeader
            {
                trafficLightMode = customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.FixedTimed ? 1 : 0,
                phaseCount = customPhaseDataBuffer.Length
            });
            
            for (int i = 0; i < customPhaseDataBuffer.Length; i++)
            {
                menu.items.Add(new UITypes.ItemCustomPhase
                {
                    activeIndex = m_ActiveEditingCustomPhaseIndexBinding.Value,
                    activeViewingIndex = m_ActiveViewingCustomPhaseIndexBinding.Value,
                    currentSignalGroup = trafficLights.m_CurrentSignalGroup,
                    manualSignalGroup = customTrafficLights.m_ManualSignalGroup,
                    index = i,
                    length = customPhaseDataBuffer.Length,
                    timer = trafficLights.m_CurrentSignalGroup == i + 1 ? customTrafficLights.m_Timer : 0,
                    turnsSinceLastRun = customPhaseDataBuffer[i].m_TurnsSinceLastRun,
                    lowFlowTimer = customPhaseDataBuffer[i].m_LowFlowTimer,
                    carFlow = customPhaseDataBuffer[i].AverageCarFlow(),
                    carLaneOccupied = customPhaseDataBuffer[i].m_CarLaneOccupied,
                    publicCarLaneOccupied = customPhaseDataBuffer[i].m_PublicCarLaneOccupied,
                    trackLaneOccupied = customPhaseDataBuffer[i].m_TrackLaneOccupied,
                    pedestrianLaneOccupied = customPhaseDataBuffer[i].m_PedestrianLaneOccupied,
                    weightedWaiting = customPhaseDataBuffer[i].m_WeightedWaiting,
                    targetDuration = customPhaseDataBuffer[i].m_TargetDuration,
                    priority = customPhaseDataBuffer[i].m_Priority,
                    minimumDuration = customPhaseDataBuffer[i].m_MinimumDuration,
                    maximumDuration = customPhaseDataBuffer[i].m_MaximumDuration,
                    targetDurationMultiplier = customPhaseDataBuffer[i].m_TargetDurationMultiplier,
                    intervalExponent = customPhaseDataBuffer[i].m_IntervalExponent,
                    linkedWithNextPhase = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.LinkedWithNextPhase) != 0,
                    endPhasePrematurely = (customPhaseDataBuffer[i].m_Options & CustomPhaseData.Options.EndPhasePrematurely) != 0,
                    bicycleLaneOccupied = customPhaseDataBuffer[i].m_BicycleLaneOccupied,
                    changeMetric = (int)customPhaseDataBuffer[i].m_ChangeMetric,
                    waitFlowBalance = customPhaseDataBuffer[i].m_WaitFlowBalance,
                    trafficLightMode = customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.FixedTimed ? 1 : 0,
                    carActive = IsTrafficTypeActive(m_SelectedEntity, i, "car"),
                    publicCarActive = IsTrafficTypeActive(m_SelectedEntity, i, "publicCar"),
                    trackActive = IsTrafficTypeActive(m_SelectedEntity, i, "track"),
                    pedestrianActive = IsTrafficTypeActive(m_SelectedEntity, i, "pedestrian"),
                    bicycleActive = IsTrafficTypeActive(m_SelectedEntity, i, "bicycle"),
                    hasSignalDelays = true,
                    carOpenDelay = customPhaseDataBuffer[i].m_CarOpenDelay,
                    carCloseDelay = customPhaseDataBuffer[i].m_CarCloseDelay,
                    publicCarOpenDelay = customPhaseDataBuffer[i].m_PublicCarOpenDelay,
                    publicCarCloseDelay = customPhaseDataBuffer[i].m_PublicCarCloseDelay,
                    trackOpenDelay = customPhaseDataBuffer[i].m_TrackOpenDelay,
                    trackCloseDelay = customPhaseDataBuffer[i].m_TrackCloseDelay,
                    pedestrianOpenDelay = customPhaseDataBuffer[i].m_PedestrianOpenDelay,
                    pedestrianCloseDelay = customPhaseDataBuffer[i].m_PedestrianCloseDelay,
                    bicycleOpenDelay = customPhaseDataBuffer[i].m_BicycleOpenDelay,
                    bicycleCloseDelay = customPhaseDataBuffer[i].m_BicycleCloseDelay,
                    carWeight = customPhaseDataBuffer[i].m_CarWeight,
                    publicCarWeight = customPhaseDataBuffer[i].m_PublicCarWeight,
                    trackWeight = customPhaseDataBuffer[i].m_TrackWeight,
                    pedestrianWeight = customPhaseDataBuffer[i].m_PedestrianWeight,
                    bicycleWeight = customPhaseDataBuffer[i].m_BicycleWeight,
                    smoothingFactor = customPhaseDataBuffer[i].m_SmoothingFactor,
                    flowRatio = customPhaseDataBuffer[i].m_FlowRatio,
                    waitRatio = customPhaseDataBuffer[i].m_WaitRatio
                });
            }
        }
        else if (m_MainPanelState == MainPanelState.TrafficGroups)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            var allGroups = trafficGroupSystem.GetAllGroups();
            Entity currentJunctionGroup = trafficGroupSystem.GetJunctionGroup(m_SelectedEntity);
            
            foreach (var groupEntity in allGroups)
            {
                if (EntityManager.HasComponent<TrafficGroup>(groupEntity))
                {
                    var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
                    string groupName = trafficGroupSystem.GetGroupName(groupEntity);
                    int memberCount = trafficGroupSystem.GetGroupMemberCount(groupEntity);
                    
                    Entity leaderEntity = Entity.Null;
                    float distanceToLeader = 0f;
                    int phaseOffset = 0;
                    int signalDelay = 0;
                    bool isCurrentJunctionLeader = false;
                    
                    if (groupEntity == currentJunctionGroup && m_SelectedEntity != Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
                    {
                        var currentMember = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
                        leaderEntity = currentMember.m_LeaderEntity;
                        distanceToLeader = currentMember.m_DistanceToLeader;
                        phaseOffset = currentMember.m_PhaseOffset;
                        signalDelay = currentMember.m_SignalDelay;
                        isCurrentJunctionLeader = currentMember.m_IsGroupLeader;
                    }
                    
                    var membersList = new ArrayList();
                    var leaderMembersList = new ArrayList();
                    var groupMembers = trafficGroupSystem.GetGroupMembers(groupEntity);
                    foreach (var memberEntity in groupMembers)
                    {
                        if (EntityManager.HasComponent<TrafficGroupMember>(memberEntity))
                        {
                            var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
                            
                            // Get phase data for this member
                            var phases = new ArrayList();
                            if (EntityManager.HasBuffer<CustomPhaseData>(memberEntity))
                            {
                                var phaseBuffer = EntityManager.GetBuffer<CustomPhaseData>(memberEntity);
                                for (int i = 0; i < phaseBuffer.Length; i++)
                                {
                                    phases.Add(new {
                                        index = i,
                                        minimumDuration = (int)phaseBuffer[i].m_MinimumDuration,
                                        maximumDuration = (int)phaseBuffer[i].m_MaximumDuration
                                    });
                                }
                            }
                            
                            // Get pattern data for this member
                            uint currentPattern = 0;
                            var availablePatterns = new ArrayList();
                            bool hasTrainTrack = false;
                            
                            if (EntityManager.HasComponent<CustomTrafficLights>(memberEntity))
                            {
                                var memberTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(memberEntity);
                                currentPattern = (uint)memberTrafficLights.GetPattern();
                                
                                // Check available patterns based on edge info
                                if (m_EdgeInfoDictionary.ContainsKey(memberEntity))
                                {
                                    var memberEdgeInfo = m_EdgeInfoDictionary[memberEntity];
                                    hasTrainTrack = NodeUtils.HasTrainTrack(memberEdgeInfo);
                                    
                                    availablePatterns.Add(new { name = "Vanilla", value = (uint)CustomTrafficLights.Patterns.Vanilla });
                                    
                                    if (PredefinedPatternsProcessor.IsValidPattern(memberEdgeInfo, CustomTrafficLights.Patterns.SplitPhasing))
                                    {
                                        availablePatterns.Add(new { name = "SplitPhasing", value = (uint)CustomTrafficLights.Patterns.SplitPhasing });
                                    }
                                    if (PredefinedPatternsProcessor.IsValidPattern(memberEdgeInfo, CustomTrafficLights.Patterns.ProtectedCentreTurn))
                                    {
                                        string patternName = m_CityConfigurationSystem.leftHandTraffic ? "ProtectedRightTurns" : "ProtectedLeftTurns";
                                        availablePatterns.Add(new { name = patternName, value = (uint)CustomTrafficLights.Patterns.ProtectedCentreTurn });
                                    }
                                    if (PredefinedPatternsProcessor.IsValidPattern(memberEdgeInfo, CustomTrafficLights.Patterns.SplitPhasingProtectedLeft))
                                    {
                                        availablePatterns.Add(new { name = "SplitPhasingProtectedLeft", value = (uint)CustomTrafficLights.Patterns.SplitPhasingProtectedLeft });
                                    }
                                    availablePatterns.Add(new { name = "CustomPhases", value = (uint)CustomTrafficLights.Patterns.CustomPhase });
                                }
                            }
                            
                            var memberInfo = new {
                                entity = memberEntity,
                                index = memberEntity.Index,
                                version = memberEntity.Version,
                                groupIndex = memberData.m_GroupIndex,
                                isLeader = memberData.m_IsGroupLeader,
                                distanceToLeader = memberData.m_DistanceToLeader,
                                phaseOffset = memberData.m_PhaseOffset,
                                signalDelay = memberData.m_SignalDelay,
                                isCurrentJunction = memberEntity == m_SelectedEntity,
                                phases = phases,
                                phaseCount = phases.Count,
                                currentPattern = currentPattern,
                                availablePatterns = availablePatterns,
                                hasTrainTrack = hasTrainTrack
                            };
                            
                            if (memberData.m_IsGroupLeader)
                            {
                                leaderMembersList.Add(memberInfo);
                            }
                            else
                            {
                                membersList.Add(memberInfo);
                            }
                        }
                    }
                    groupMembers.Dispose();
                    
                    // Sort non-leader members by group index (order added) for stable ordering
                    var sortedMembers = membersList.ToArray()
                        .OrderBy(m => (int)m.GetType().GetProperty("groupIndex").GetValue(m))
                        .ToArray();
                    
                    var sortedMembersList = new ArrayList();
                    sortedMembersList.AddRange(leaderMembersList);
                    sortedMembersList.AddRange(sortedMembers);
                    
                    menu.items.Add(new UITypes.ItemTrafficGroup
                    {
                        groupIndex = groupEntity.Index,
                        groupVersion = groupEntity.Version,
                        name = groupName,
                        memberCount = memberCount,
                        isCoordinated = group.m_IsCoordinated,
                        isCurrentJunctionInGroup = groupEntity == currentJunctionGroup,
                        greenWaveEnabled = group.m_GreenWaveEnabled,
                        greenWaveSpeed = group.m_GreenWaveSpeed,
                        greenWaveOffset = group.m_GreenWaveOffset,
                        leaderIndex = leaderEntity.Index,
                        leaderVersion = leaderEntity.Version,
                        currentJunctionIndex = m_SelectedEntity.Index,
                        currentJunctionVersion = m_SelectedEntity.Version,
                        distanceToLeader = distanceToLeader,
                        phaseOffset = phaseOffset,
                        signalDelay = signalDelay,
                        isCurrentJunctionLeader = isCurrentJunctionLeader,
                        members = sortedMembersList,
                        previousState = (int)m_PreviousMainPanelState,
                        cycleLength = group.m_CycleLength
                    });
                }
            }
            allGroups.Dispose();
        }
        else if (m_MainPanelState == MainPanelState.Empty)
        {
            if (m_IsAddingMember)
            {
                var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
                string groupName = trafficGroupSystem.GetGroupName(m_TargetGroupForMember);
                menu.items.Add(new UITypes.ItemMessage{message = $"AddingMemberTo:{groupName}"});
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemButton{label = "Cancel", key = "cancel", value = "{}", engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallExitAddMemberMode"});
                menu.items.Add(new UITypes.ItemButton{label = "Finish", key = "finish", value = "{}", engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallFinishAddMemberMode"});
            }
            else
            {
                menu.items.Add(new UITypes.ItemMessage{message = "PleaseSelectJunction"});
                menu.items.Add(default(UITypes.ItemDivider));
                menu.items.Add(new UITypes.ItemButton{label = "TrafficGroups", key = "state", value = $"{(int)MainPanelState.TrafficGroups}", engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetMainPanelState"});
            }
        }
        if (Mod.IsBeta() && Mod.m_Settings != null && Mod.m_Settings.m_SuppressCanaryWarningVersion != Mod.m_InformationalVersion)
        {
            menu.items.Add(default(UITypes.ItemDivider));
            menu.items.Add(new UITypes.ItemNotification{label = "BetaBuildWarning", notificationType = "warning"});
        }
        string result = JsonConvert.SerializeObject(menu);
        return result;
    }

    public static string GetLocale()
    {
        var result = new
        {
            locale = GetLocaleCode(),
        };

        return JsonConvert.SerializeObject(result);
    }

    public string GetCityConfiguration()
    {
        var result = new
        {
            leftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
        };

        return JsonConvert.SerializeObject(result);
    }

    protected Dictionary<string, UITypes.ScreenPoint> GetScreenPoint()
    {
        Dictionary<string, UITypes.ScreenPoint> screenPointDictionary = [];
        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;
        foreach (var wp in m_WorldPositionList)
        {
            if (!screenPointDictionary.ContainsKey(wp))
            {
                screenPointDictionary[wp] = new UITypes.ScreenPoint(m_Camera.WorldToScreenPoint(wp), m_ScreenHeight);
            }
        }
        return screenPointDictionary;
    }

    protected Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> GetEdgeInfo()
    {
        return m_EdgeInfoDictionary;
    }

    protected void CallMainPanelUpdatePattern(string input)
    {
        UITypes.ItemRadio pattern = JsonConvert.DeserializeObject<UITypes.ItemRadio>(input);
        m_CustomTrafficLights.SetPattern(((uint)m_CustomTrafficLights.GetPattern() & 0xFFFF0000) | uint.Parse(pattern.value));
        if (m_CustomTrafficLights.GetPatternOnly() != CustomTrafficLights.Patterns.Vanilla)
        {
            m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern() & ~CustomTrafficLights.Patterns.CentreTurnGiveWay);
        }
        if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
        {
            if (!EntityManager.HasBuffer<CustomPhaseData>(m_SelectedEntity))
            {
                EntityManager.AddComponent<CustomPhaseData>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<EdgeGroupMask>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<SubLaneGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<SubLaneGroupMask>(m_SelectedEntity);
            }
            m_CustomTrafficLights.SetPattern(CustomTrafficLights.Patterns.CustomPhase);
        }
        if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.FixedTimed)
        {
            if (!EntityManager.HasBuffer<CustomPhaseData>(m_SelectedEntity))
            {
                EntityManager.AddComponent<CustomPhaseData>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<EdgeGroupMask>(m_SelectedEntity);
            }
            if (!EntityManager.HasBuffer<SubLaneGroupMask>(m_SelectedEntity))
            {
                EntityManager.AddComponent<SubLaneGroupMask>(m_SelectedEntity);
            }
            m_CustomTrafficLights.SetPattern(CustomTrafficLights.Patterns.FixedTimed);
        }
        UpdateEntity();
        
        // Propagate pattern to group members if this is the leader
        if (EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            if (member.m_IsGroupLeader && member.m_GroupEntity != Entity.Null)
            {
                var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
                trafficGroupSystem.PropagatePatternToMembers(member.m_GroupEntity, m_CustomTrafficLights.GetPattern());
            }
        }
        
        m_MainPanelBinding.Update();
    }

    protected void CallMainPanelUpdateOption(string input)
    {
        UITypes.ItemCheckbox option = JsonConvert.DeserializeObject<UITypes.ItemCheckbox>(input);
        foreach (CustomTrafficLights.Patterns pattern in System.Enum.GetValues(typeof(CustomTrafficLights.Patterns)))
        {
            if (((uint) pattern & 0xFFFF0000) != 0)
            {
                if (uint.Parse(option.key) == (uint)pattern)
                {
                    m_CustomTrafficLights.SetPattern(m_CustomTrafficLights.GetPattern() ^ pattern);
                }
            }
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
    }

    protected void CallMainPanelUpdateValue(string jsonString)
    {
        var keyDefinition = new { key = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        if (parsedKey.key == "CustomPedestrianDurationMultiplier")
        {
            var valueDefinition = new { value = 0.0f };
            var parsedValue = JsonConvert.DeserializeAnonymousType(jsonString, valueDefinition);
            m_CustomTrafficLights.SetPedestrianPhaseDurationMultiplier(parsedValue.value);
        }
        UpdateEntity();
        m_MainPanelBinding.Update();
    }

    protected void CallMainPanelUpdatePosition(string jsonString)
    {
        m_MainPanelPosition = JsonConvert.DeserializeObject<UITypes.ScreenPoint>(jsonString);
        m_MainPanelBinding.Update();
    }

    protected void CallMainPanelSave(string value)
    {
        SaveSelectedEntity();
    }

    protected void CallLaneDirectionToolReset(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            EntityManager.RemoveComponent<CommonLibraries.LaneSystem.CustomLaneDirection>(m_SelectedEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetMainPanelState(string input)
    {
        var definition = new { key = "", value = "" };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        MainPanelState state = (MainPanelState)System.Int32.Parse(value.value);
        SetMainPanelState(state);
    }

    protected void CallAddCustomPhase(string input)
    {
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            customPhaseDataBuffer.Add(new CustomPhaseData());
            UpdateActiveEditingCustomPhaseIndex(customPhaseDataBuffer.Length - 1);
            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);
            UpdateEntity();
        }
    }

    protected void CallRemoveCustomPhase(string input)
    {
        var definition = new { index = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            customPhaseDataBuffer.RemoveAt(value.index);

            DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
            DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
            {
                edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out subLaneGroupMaskBuffer))
            {
                subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
            }
            for (int i = value.index; i < 16; i++)
            {
                CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, i, i + 1);
                CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, i, i + 1);
            }

            if (m_ActiveEditingCustomPhaseIndexBinding.Value >= customPhaseDataBuffer.Length)
            {
                UpdateActiveEditingCustomPhaseIndex(customPhaseDataBuffer.Length - 1);
            }

            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);

            UpdateEntity();
        }
    }

    protected void CallSwapCustomPhase(string input)
    {
        var definition = new { index1 = 0, index2 = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }
            (customPhaseDataBuffer[value.index2], customPhaseDataBuffer[value.index1]) = (customPhaseDataBuffer[value.index1], customPhaseDataBuffer[value.index2]);

            DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
            {
                edgeGroupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(edgeGroupMaskBuffer, value.index1, value.index2);

            DynamicBuffer<SubLaneGroupMask> subLaneGroupMaskBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out subLaneGroupMaskBuffer))
            {
                subLaneGroupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
            }
            CustomPhaseUtils.SwapBit(subLaneGroupMaskBuffer, value.index1, value.index2);

            UpdateActiveEditingCustomPhaseIndex(value.index2);
            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);

            UpdateEntity();
        }
    }

    protected void CallSetActiveCustomPhaseIndex(string input)
    {
        var definition = new { key = "", value = 0 };
        var result = JsonConvert.DeserializeAnonymousType(input, definition);
        if (result.key == "ActiveEditingCustomPhaseIndex")
        {
            UpdateActiveEditingCustomPhaseIndex(result.value);
            UpdateEntity();
        }
        else if (result.key == "ActiveViewingCustomPhaseIndex")
        {
            UpdateActiveViewingCustomPhaseIndex(result.value);
            RedrawGizmo();
        }
        else if (result.key == "ManualSignalGroup")
        {
            UpdateManualSignalGroup(result.value);
            RedrawGizmo();
        }
        m_MainPanelBinding.Update();
    }

    protected void CallUpdateEdgeGroupMask(string input)
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return;
        }

        EdgeGroupMask[] groupMaskArray = JsonConvert.DeserializeObject<EdgeGroupMask[]>(input);
        DynamicBuffer<EdgeGroupMask> groupMaskBuffer;
        if (EntityManager.HasBuffer<EdgeGroupMask>(m_SelectedEntity))
        {
            groupMaskBuffer = EntityManager.GetBuffer<EdgeGroupMask>(m_SelectedEntity, false);
        }
        else
        {
            groupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(m_SelectedEntity);
        }

        foreach (var newValue in groupMaskArray)
        {
            int index = CustomPhaseUtils.TryGet(groupMaskBuffer, newValue, out EdgeGroupMask oldValue);
            if (index >= 0)
            {
                groupMaskBuffer[index] = new EdgeGroupMask(oldValue, newValue);
            }
            else
            {
                groupMaskBuffer.Add(new EdgeGroupMask(oldValue, newValue));
            }
        }

        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity();
    }

    protected void CallUpdateSubLaneGroupMask(string input)
    {
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return;
        }

        SubLaneGroupMask[] groupMaskArray = JsonConvert.DeserializeObject<SubLaneGroupMask[]>(input);
        DynamicBuffer<SubLaneGroupMask> groupMaskBuffer;
        if (EntityManager.HasBuffer<SubLaneGroupMask>(m_SelectedEntity))
        {
            groupMaskBuffer = EntityManager.GetBuffer<SubLaneGroupMask>(m_SelectedEntity, false);
        }
        else
        {
            groupMaskBuffer = EntityManager.AddBuffer<SubLaneGroupMask>(m_SelectedEntity);
        }

        foreach (var newValue in groupMaskArray)
        {
            int index = CustomPhaseUtils.TryGet(groupMaskBuffer, newValue, out SubLaneGroupMask oldValue);
            if (index >= 0)
            {
                groupMaskBuffer[index] = new SubLaneGroupMask(oldValue, newValue);
            }
            else
            {
                groupMaskBuffer.Add(new SubLaneGroupMask(oldValue, newValue));
            }
        }

        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity();
    }

    protected void CallUpdateCustomPhaseData(string jsonString)
    {
        var input = JsonConvert.DeserializeObject<UITypes.UpdateCustomPhaseData>(jsonString);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
            if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out customPhaseDataBuffer))
            {
                customPhaseDataBuffer = EntityManager.AddBuffer<CustomPhaseData>(m_SelectedEntity);
            }

            int index = input.index >= 0 ? input.index : m_ActiveEditingCustomPhaseIndexBinding.Value;
            if (index < 0 || index >= customPhaseDataBuffer.Length)
            {
                return;
            }
            var newValue = customPhaseDataBuffer[index];

            if (input.key == "MinimumDuration")
            {
                newValue.m_MinimumDuration = (ushort)input.value;
                if (newValue.m_MinimumDuration > newValue.m_MaximumDuration)
                {
                    newValue.m_MaximumDuration = newValue.m_MinimumDuration;
                }
            }
            else if (input.key == "MaximumDuration")
            {
                newValue.m_MaximumDuration = (ushort)input.value;
                if (newValue.m_MinimumDuration > newValue.m_MaximumDuration)
                {
                    newValue.m_MinimumDuration = newValue.m_MaximumDuration;
                }
            }
            else if (input.key == "TargetDurationMultiplier")
            {
                newValue.m_TargetDurationMultiplier = (float)input.value;
            }
            else if (input.key == "IntervalExponent")
            {
                newValue.m_IntervalExponent = (float)input.value;
            }
            else if (input.key == "LinkedWithNextPhase")
            {
                newValue.m_Options ^= CustomPhaseData.Options.LinkedWithNextPhase;
            }
            else if (input.key == "EndPhasePrematurely")
            {
                newValue.m_Options ^= CustomPhaseData.Options.EndPhasePrematurely;
            }
            // Handle signal delay updates
            else if (input.key == "carOpenDelay")
            {
                newValue.m_CarOpenDelay = (short)input.value;
            }
            else if (input.key == "carCloseDelay")
            {
                newValue.m_CarCloseDelay = (short)input.value;
            }
            else if (input.key == "publicCarOpenDelay")
            {
                newValue.m_PublicCarOpenDelay = (short)input.value;
            }
            else if (input.key == "publicCarCloseDelay")
            {
                newValue.m_PublicCarCloseDelay = (short)input.value;
            }
            else if (input.key == "trackOpenDelay")
            {
                newValue.m_TrackOpenDelay = (short)input.value;
            }
            else if (input.key == "trackCloseDelay")
            {
                newValue.m_TrackCloseDelay = (short)input.value;
            }
            else if (input.key == "pedestrianOpenDelay")
            {
                newValue.m_PedestrianOpenDelay = (short)input.value;
            }
            else if (input.key == "pedestrianCloseDelay")
            {
                newValue.m_PedestrianCloseDelay = (short)input.value;
            }
            else if (input.key == "bicycleOpenDelay")
            {
                newValue.m_BicycleOpenDelay = (short)input.value;
            }
            else if (input.key == "bicycleCloseDelay")
            {
                newValue.m_BicycleCloseDelay = (short)input.value;
            }
            else if (input.key == "ChangeMetric")
            {
                newValue.m_ChangeMetric = (CustomPhaseData.StepChangeMetric)(int)input.value;
            }
            else if (input.key == "WaitFlowBalance")
            {
                newValue.m_WaitFlowBalance = (float)input.value;
            }
            else if (input.key == "TrafficLightMode")
            {
                if ((int)input.value == 1)
                {
                    m_CustomTrafficLights.SetPattern(CustomTrafficLights.Patterns.FixedTimed);
                }
                else
                {
                    m_CustomTrafficLights.SetPattern(CustomTrafficLights.Patterns.CustomPhase);
                }
                UpdateEntity();
            }
            
            customPhaseDataBuffer[index] = newValue;

            if (input.key == "MaximumDuration" || input.key == "MinimumDuration")
            {
                if (EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
                {
                    var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
                    if (member.m_GroupEntity != Entity.Null)
                    {
                        var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
                        trafficGroupSystem.RecalculateGroupCycleLength(member.m_GroupEntity);
                    }
                }
            }

            m_MainPanelBinding.Update();
            UpdateEdgeInfo(m_SelectedEntity);
            UpdateEntity(addUpdated: false);
        }
    }

    protected void CallUpdateSignalDelay(string jsonString)
    {
        var keyDefinition = new { key = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        
        var definition = new { edgeIndex = 0, edgeVersion = 0, phaseIndex = 0, laneType = "", direction = "", openDelay = 0, closeDelay = 0 };
        var input = JsonConvert.DeserializeAnonymousType(parsedKey.key, definition);
        
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return;
        }

        DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
        if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
        {
            return;
        }

        for (int i = 0; i < edgeGroupMaskBuffer.Length; i++)
        {
            var edgeMask = edgeGroupMaskBuffer[i];
            if (edgeMask.m_Edge.Index == input.edgeIndex && edgeMask.m_Edge.Version == input.edgeVersion)
            {
                // Update the appropriate signal delay based on laneType and direction
                if (input.laneType == "carLane")
                {
                    if (input.direction == "left") { edgeMask.m_Car.m_Left.m_OpenDelay = (short)input.openDelay; edgeMask.m_Car.m_Left.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "straight") { edgeMask.m_Car.m_Straight.m_OpenDelay = (short)input.openDelay; edgeMask.m_Car.m_Straight.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "right") { edgeMask.m_Car.m_Right.m_OpenDelay = (short)input.openDelay; edgeMask.m_Car.m_Right.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "uTurn") { edgeMask.m_Car.m_UTurn.m_OpenDelay = (short)input.openDelay; edgeMask.m_Car.m_UTurn.m_CloseDelay = (short)input.closeDelay; }
                }
                else if (input.laneType == "publicCarLane")
                {
                    if (input.direction == "left") { edgeMask.m_PublicCar.m_Left.m_OpenDelay = (short)input.openDelay; edgeMask.m_PublicCar.m_Left.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "straight") { edgeMask.m_PublicCar.m_Straight.m_OpenDelay = (short)input.openDelay; edgeMask.m_PublicCar.m_Straight.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "right") { edgeMask.m_PublicCar.m_Right.m_OpenDelay = (short)input.openDelay; edgeMask.m_PublicCar.m_Right.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "uTurn") { edgeMask.m_PublicCar.m_UTurn.m_OpenDelay = (short)input.openDelay; edgeMask.m_PublicCar.m_UTurn.m_CloseDelay = (short)input.closeDelay; }
                }
                else if (input.laneType == "trackLane")
                {
                    if (input.direction == "left") { edgeMask.m_Track.m_Left.m_OpenDelay = (short)input.openDelay; edgeMask.m_Track.m_Left.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "straight") { edgeMask.m_Track.m_Straight.m_OpenDelay = (short)input.openDelay; edgeMask.m_Track.m_Straight.m_CloseDelay = (short)input.closeDelay; }
                    else if (input.direction == "right") { edgeMask.m_Track.m_Right.m_OpenDelay = (short)input.openDelay; edgeMask.m_Track.m_Right.m_CloseDelay = (short)input.closeDelay; }
                }
                else if (input.laneType == "pedestrianLane" || input.laneType == "pedestrianLaneStopLine" || input.laneType == "pedestrianLaneNonStopLine")
                {
                    edgeMask.m_Pedestrian.m_OpenDelay = (short)input.openDelay;
                    edgeMask.m_Pedestrian.m_CloseDelay = (short)input.closeDelay;
                }
                else if (input.laneType == "bicycleLane")
                {
                    edgeMask.m_Bicycle.m_OpenDelay = (short)input.openDelay;
                    edgeMask.m_Bicycle.m_CloseDelay = (short)input.closeDelay;
                }

                edgeGroupMaskBuffer[i] = edgeMask;
                break;
            }
        }

        m_MainPanelBinding.Update();
        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity(addUpdated: false);
    }

    protected void CallUpdateEdgeDelay(string jsonString)
    {
        var keyDefinition = new { key = "", value = 0 };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        
        var definition = new { edgeIndex = 0, edgeVersion = 0, phaseIndex = 0, field = "" };
        var input = JsonConvert.DeserializeAnonymousType(parsedKey.key, definition);
        
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return;
        }

        DynamicBuffer<EdgeGroupMask> edgeGroupMaskBuffer;
        if (!EntityManager.TryGetBuffer(m_SelectedEntity, false, out edgeGroupMaskBuffer))
        {
            return;
        }

        for (int i = 0; i < edgeGroupMaskBuffer.Length; i++)
        {
            var edgeMask = edgeGroupMaskBuffer[i];
            if (edgeMask.m_Edge.Index == input.edgeIndex && edgeMask.m_Edge.Version == input.edgeVersion)
            {
                if (input.field == "openDelay")
                {
                    edgeMask.m_OpenDelay = (short)parsedKey.value;
                }
                else if (input.field == "closeDelay")
                {
                    edgeMask.m_CloseDelay = (short)parsedKey.value;
                }
                edgeGroupMaskBuffer[i] = edgeMask;
                break;
            }
        }

        m_MainPanelBinding.Update();
        UpdateEdgeInfo(m_SelectedEntity);
        UpdateEntity(addUpdated: false);
    }

    protected void CallKeyPress(string value)
    {
        var definition = new { ctrlKey = false, key = "" };
        var keyPressEvent = JsonConvert.DeserializeAnonymousType(value, definition);
        if (keyPressEvent.ctrlKey && keyPressEvent.key == "S")
        {
            if (!m_SelectedEntity.Equals(Entity.Null))
            {
                SaveSelectedEntity();
            }
        }
    }

    protected void CallAddWorldPosition(string input)
    {
        UITypes.WorldPosition[] posArray = JsonConvert.DeserializeObject<UITypes.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Add(pos);
        }
        m_CameraPosition = float.MaxValue; 
    }

    protected void CallRemoveWorldPosition(string input)
    {
        UITypes.WorldPosition[] posArray = JsonConvert.DeserializeObject<UITypes.WorldPosition[]>(input);
        foreach (var pos in posArray)
        {
            m_WorldPositionList.Remove(pos);
        }
        m_CameraPosition = float.MaxValue; 
    }

    protected void CallOpenBrowser(string jsonString)
    {
        var keyDefinition = new { key = "", value = "" };
        var parsedKey = JsonConvert.DeserializeAnonymousType(jsonString, keyDefinition);
        System.Diagnostics.Process.Start(parsedKey.value);
    }

    protected void UpdateActiveEditingCustomPhaseIndex(int index)
    {
        m_ActiveEditingCustomPhaseIndexBinding.Value = index;
        m_ActiveEditingCustomPhaseIndexBinding.ForceUpdate();
        if (index >= 0)
        {
            m_ActiveViewingCustomPhaseIndexBinding.Value = -1;
            m_ActiveViewingCustomPhaseIndexBinding.ForceUpdate();
        }
    }

    protected void UpdateActiveViewingCustomPhaseIndex(int index)
    {
        m_ActiveViewingCustomPhaseIndexBinding.Value = index;
        m_ActiveViewingCustomPhaseIndexBinding.ForceUpdate();
        if (index >= 0)
        {
            m_ActiveEditingCustomPhaseIndexBinding.Value = -1;
            m_ActiveEditingCustomPhaseIndexBinding.ForceUpdate();
        }
    }

    protected void UpdateManualSignalGroup(int group)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            m_CustomTrafficLights.m_ManualSignalGroup = (byte)group;
            if (group > 0 && EntityManager.TryGetComponent<TrafficLights>(m_SelectedEntity, out var trafficLights))
            {
                trafficLights.m_NextSignalGroup = (byte)group;
                EntityManager.SetComponentData(m_SelectedEntity, trafficLights);
            }
            UpdateEntity(addUpdated: false);
        }
        if (group > 0)
        {
            m_ActiveViewingCustomPhaseIndexBinding.Value = -1;
            m_ActiveViewingCustomPhaseIndexBinding.ForceUpdate();
            m_ActiveEditingCustomPhaseIndexBinding.Value = -1;
            m_ActiveEditingCustomPhaseIndexBinding.ForceUpdate();
        }
    }

    protected void CallCreateTrafficGroup(string input)
    {
        var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
        Entity groupEntity = trafficGroupSystem.CreateGroup(null); 
        
        if (m_SelectedEntity != Entity.Null)
        {
            trafficGroupSystem.AddJunctionToGroup(groupEntity, m_SelectedEntity);
        }
        
        m_MainPanelBinding.Update();
    }

    protected void CallAddJunctionToGroup(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        Entity groupEntity = new Entity { Index = value.groupIndex, Version = value.groupVersion };
        
        if (m_SelectedEntity != Entity.Null && groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.AddJunctionToGroup(groupEntity, m_SelectedEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallRemoveJunctionFromGroup(string input)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.RemoveJunctionFromGroup(m_SelectedEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallDeleteTrafficGroup(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        Entity groupEntity = new Entity { Index = value.groupIndex, Version = value.groupVersion };
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.DeleteGroup(groupEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetTrafficGroupName(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0, name = "" };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        Entity groupEntity = new Entity { Index = value.groupIndex, Version = value.groupVersion };
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SetGroupName(groupEntity, value.name);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetGreenWaveEnabled(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0, enabled = false, key = "", value = "", isChecked = false };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        Entity groupEntity;
        bool enabled;
        
        if (data.key == "GreenWaveEnabled")
        {
            if (m_SelectedEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
            {
                return;
            }
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
            
            if (EntityManager.HasComponent<TrafficGroup>(groupEntity))
            {
                var currentGroup = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
                enabled = !currentGroup.m_GreenWaveEnabled;
            }
            else
            {
                enabled = false;
            }
        }
        else
        {
            groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            enabled = data.enabled;
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SetGreenWaveEnabled(groupEntity, enabled);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetGreenWaveSpeed(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0, speed = 50f, key = "", value = 0f };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        Entity groupEntity;
        float speed;
        
        if (data.key == "GreenWaveSpeed")
        {
            speed = data.value;
            if (m_SelectedEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
            {
                return;
            }
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        else
        {
            groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            speed = data.speed;
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SetGreenWaveSpeed(groupEntity, speed);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetGreenWaveOffset(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0, offset = 0f, key = "", value = 0f };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        Entity groupEntity;
        float offset;
        
        if (data.key == "GreenWaveOffset")
        {
            offset = data.value;
            if (m_SelectedEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
            {
                return;
            }
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        else
        {
            groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            offset = data.offset;
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SetGreenWaveOffset(groupEntity, offset);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetTrafficGroupSignalDelay(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0, memberIndex = 0, memberVersion = 0, signalDelay = 0, key = "", value = 0 };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        Entity groupEntity;
        Entity memberEntity;
        int signalDelay;
        
        if (data.key == "SignalDelay")
        {
            signalDelay = (int)data.value;
            if (m_SelectedEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
            {
                return;
            }
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
            memberEntity = m_SelectedEntity;
        }
        else
        {
            groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            memberEntity = new Entity { Index = data.memberIndex, Version = data.memberVersion };
            signalDelay = data.signalDelay;
        }
        
        if (groupEntity != Entity.Null && memberEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SetSignalDelay(groupEntity, memberEntity, signalDelay);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallCalculateSignalDelays(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0 };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        Entity groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.CalculateSignalDelays(groupEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetCoordinated(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0, coordinated = false, key = "", value = "", isChecked = false };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        Entity groupEntity;
        bool coordinated;
        
        if (data.key == "Coordinated")
        {
            if (m_SelectedEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
            {
                return;
            }
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
            
            if (EntityManager.HasComponent<TrafficGroup>(groupEntity))
            {
                var currentGroup = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
                coordinated = !currentGroup.m_IsCoordinated;
            }
            else
            {
                coordinated = false;
            }
        }
        else
        {
            groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            coordinated = data.coordinated;
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SetCoordinated(groupEntity, coordinated);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetCycleLength(string input)
    {
        var definition = new { key = "", value = 0f };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        if (m_SelectedEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            return;
        }
        
        var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
        Entity groupEntity = member.m_GroupEntity;
        
        if (groupEntity != Entity.Null && EntityManager.HasComponent<TrafficGroup>(groupEntity))
        {
            var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
            group.m_CycleLength = data.value;
            EntityManager.SetComponentData(groupEntity, group);
            m_MainPanelBinding.Update();
        }
    }

    private bool IsTrafficTypeActive(Entity entity, int phaseIndex, string trafficType)
    {
        if (!m_EdgeInfoDictionary.ContainsKey(entity))
        {
            return false;
        }

        var edgeInfos = m_EdgeInfoDictionary[entity];
        int phaseMask = 1 << phaseIndex;

        foreach (var edgeInfo in edgeInfos)
        {
            switch (trafficType)
            {
                case "car":
                    if (edgeInfo.m_CarLaneLeftCount > 0 || edgeInfo.m_CarLaneStraightCount > 0 || edgeInfo.m_CarLaneRightCount > 0 || edgeInfo.m_CarLaneUTurnCount > 0)
                    {
                        if ((edgeInfo.m_EdgeGroupMask.m_Car.m_Left.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Car.m_Straight.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Car.m_Right.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Car.m_UTurn.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Car.m_Left.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Car.m_Straight.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Car.m_Right.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Car.m_UTurn.m_YieldGroupMask & phaseMask) != 0)
                        {
                            return true;
                        }
                    }
                    break;

                case "publicCar":
                    if (edgeInfo.m_PublicCarLaneLeftCount > 0 || edgeInfo.m_PublicCarLaneStraightCount > 0 || edgeInfo.m_PublicCarLaneRightCount > 0 || edgeInfo.m_PublicCarLaneUTurnCount > 0)
                    {
                        if ((edgeInfo.m_EdgeGroupMask.m_PublicCar.m_Left.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_PublicCar.m_Straight.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_PublicCar.m_Right.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_PublicCar.m_UTurn.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_PublicCar.m_Left.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_PublicCar.m_Straight.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_PublicCar.m_Right.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_PublicCar.m_UTurn.m_YieldGroupMask & phaseMask) != 0)
                        {
                            return true;
                        }
                    }
                    break;

                case "track":
                    if (edgeInfo.m_TrainTrackCount > 0)
                    {
                        if ((edgeInfo.m_EdgeGroupMask.m_Track.m_Left.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Track.m_Straight.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Track.m_Right.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Track.m_Left.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Track.m_Straight.m_YieldGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Track.m_Right.m_YieldGroupMask & phaseMask) != 0)
                        {
                            return true;
                        }
                    }
                    break;

                case "pedestrian":
                    if (edgeInfo.m_PedestrianLaneStopLineCount > 0 || edgeInfo.m_PedestrianLaneNonStopLineCount > 0)
                    {
                        if ((edgeInfo.m_EdgeGroupMask.m_Pedestrian.m_GoGroupMask & phaseMask) != 0 ||
                            (edgeInfo.m_EdgeGroupMask.m_Pedestrian.m_YieldGroupMask & phaseMask) != 0)
                        {
                            return true;
                        }
                    }
                    break;
            }
        }

        return false;
    }

    protected void CallGetGroupMembers(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0 };
        var value = JsonConvert.DeserializeAnonymousType(input, definition);
        Entity groupEntity = new Entity { Index = value.groupIndex, Version = value.groupVersion };
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            var members = trafficGroupSystem.GetGroupMembers(groupEntity);
            
            var memberList = new ArrayList();
            foreach (var memberEntity in members)
            {
                if (EntityManager.HasComponent<TrafficGroupMember>(memberEntity))
                {
                    var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
                    var isLeader = memberData.m_IsGroupLeader;
                    var distanceToLeader = memberData.m_DistanceToLeader;
                    var phaseOffset = memberData.m_PhaseOffset;
                    var signalDelay = memberData.m_SignalDelay;
                    
                    memberList.Add(new {
                        index = memberEntity.Index,
                        version = memberEntity.Version,
                        isLeader = isLeader,
                        distanceToLeader = distanceToLeader,
                        phaseOffset = phaseOffset,
                        signalDelay = signalDelay
                    });
                }
            }
            
            members.Dispose();
            
            var result = JsonConvert.SerializeObject(new { members = memberList });
        
        }
    }

    protected void CallSelectJunction(string input)
    {
        
        var selectionData = new { index = 0, version = 0, stayOnTrafficGroups = false };
        var parsedData = JsonConvert.DeserializeAnonymousType(input, selectionData);
        
        if (parsedData != null)
        {
            Entity targetEntity = new Entity
            {
                Index = parsedData.index,
                Version = parsedData.version
            };
            
            
            if (EntityManager.Exists(targetEntity) && EntityManager.HasComponent<Game.Net.TrafficLights>(targetEntity))
            {
                
                if (m_SelectedEntity != Entity.Null && m_SelectedEntity != targetEntity)
                {
                    SaveSelectedEntity();
                }
                
                ChangeSelectedEntity(targetEntity);
                
                if (parsedData.stayOnTrafficGroups)
                {
                    SetMainPanelState(MainPanelState.TrafficGroups);
                }
                
                m_MainPanelBinding.Update();
            }
        }
    }

    protected void CallEnterAddMemberMode(string input)
    {
        
        var groupData = new { groupIndex = 0, groupVersion = 0 };
        var parsedData = JsonConvert.DeserializeAnonymousType(input, groupData);
        
        if (parsedData != null)
        {
            Entity targetGroup = new Entity
            {
                Index = parsedData.groupIndex,
                Version = parsedData.groupVersion
            };
            
            if (EntityManager.Exists(targetGroup))
            {
                m_IsAddingMember = true;
                m_TargetGroupForMember = targetGroup;
                SetMainPanelState(MainPanelState.Empty);
                m_AddMemberStateBinding?.Update();
            }
        }
    }

    protected void CallExitAddMemberMode(string input)
    {
        m_IsAddingMember = false;
        m_TargetGroupForMember = Entity.Null;
        SetMainPanelState(MainPanelState.TrafficGroups);
        m_AddMemberStateBinding?.Update();
    }

    protected void CallFinishAddMemberMode(string input)
    {
        m_IsAddingMember = false;
        m_TargetGroupForMember = Entity.Null;
        SetMainPanelState(MainPanelState.TrafficGroups);
        m_AddMemberStateBinding?.Update();
    }

    protected string GetAddMemberState()
    {
        string groupName = "";
        int memberCount = 0;
        var membersList = new ArrayList();
        
        if (m_IsAddingMember && !m_TargetGroupForMember.Equals(Entity.Null))
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            groupName = trafficGroupSystem.GetGroupName(m_TargetGroupForMember);
            memberCount = trafficGroupSystem.GetGroupMemberCount(m_TargetGroupForMember);
            
            var members = trafficGroupSystem.GetGroupMembers(m_TargetGroupForMember);
            foreach (var memberEntity in members)
            {
                bool isLeader = false;
                if (EntityManager.HasComponent<TrafficGroupMember>(memberEntity))
                {
                    var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
                    isLeader = memberData.m_IsGroupLeader;
                }
                membersList.Add(new {
                    index = memberEntity.Index,
                    version = memberEntity.Version,
                    isLeader = isLeader
                });
            }
            members.Dispose();
        }
        
        var result = new
        {
            isAddingMember = m_IsAddingMember,
            targetGroupIndex = m_TargetGroupForMember.Index,
            targetGroupVersion = m_TargetGroupForMember.Version,
            targetGroupName = groupName,
            memberCount = memberCount,
            members = membersList
        };
        
        return JsonConvert.SerializeObject(result);
    }

    protected void CallSetSignalDelay(string jsonString)
    {
        var input = JsonConvert.DeserializeObject<UITypes.SetSignalDelayData>(jsonString);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            Entity edgeEntity = new Entity { Index = input.edgeIndex, Version = input.edgeVersion };

            if (!EntityManager.Exists(edgeEntity))
            {
                return;
            }
            
            SignalDelaySystem.SetSignalDelay(EntityManager, m_SelectedEntity, edgeEntity, input.openDelay, input.closeDelay, input.isEnabled);
            UpdateEntity();
            
            if (m_SignalDelayDataBinding != null)
            {
                m_SignalDelayDataBinding.Update();
            }
        }
    }

    protected void CallRemoveSignalDelay(string jsonString)
    {
        var input = JsonConvert.DeserializeObject<UITypes.RemoveSignalDelayData>(jsonString);
        if (!m_SelectedEntity.Equals(Entity.Null))
        {
            Entity edgeEntity = new Entity { Index = input.edgeIndex, Version = input.edgeVersion };

            if (!EntityManager.Exists(edgeEntity))
            {
                return;
            }
            SignalDelaySystem.RemoveSignalDelay(EntityManager, m_SelectedEntity, edgeEntity);
            UpdateEntity();
            
            if (m_SignalDelayDataBinding != null)
            {
                m_SignalDelayDataBinding.Update();
            }
        }
    }

    

    protected string GetSignalDelayData()
    {
        var result = new Dictionary<string, object>();
        
        if (m_SelectedEntity.Equals(Entity.Null))
        {
            return JsonConvert.SerializeObject(result);
        }
        
        if (EntityManager.TryGetBuffer(m_SelectedEntity, false, out DynamicBuffer<SignalDelayData> signalDelayBuffer))
        {
            for (int i = 0; i < signalDelayBuffer.Length; i++)
            {
                var delayData = signalDelayBuffer[i];
                var key = $"{delayData.m_Edge.Index}_{delayData.m_Edge.Version}";
                
                result[key] = new
                {
                    edgeIndex = delayData.m_Edge.Index,
                    edgeVersion = delayData.m_Edge.Version,
                    openDelay = delayData.m_OpenDelay,
                    closeDelay = delayData.m_CloseDelay,
                    isEnabled = delayData.m_IsEnabled
                };
            }
        }
        

        var jsonResult = JsonConvert.SerializeObject(result);
        return jsonResult;
    }

    protected void CallForceSyncToLeader(string input)
    {
        Entity groupEntity = Entity.Null;
        
        if (m_SelectedEntity != Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        
        if (groupEntity == Entity.Null && !string.IsNullOrEmpty(input))
        {
            var definition = new { groupIndex = 0, groupVersion = 0 };
            var data = JsonConvert.DeserializeAnonymousType(input, definition);
            if (data != null)
            {
                groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            }
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.ForceSyncToLeader(groupEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallRecalculateCycleLength(string input)
    {
        Entity groupEntity = Entity.Null;
        
        if (m_SelectedEntity != Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        
        if (groupEntity == Entity.Null && !string.IsNullOrEmpty(input))
        {
            var definition = new { groupIndex = 0, groupVersion = 0 };
            var data = JsonConvert.DeserializeAnonymousType(input, definition);
            if (data != null)
            {
                groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            }
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.RecalculateGroupCycleLength(groupEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallJoinGroups(string input)
    {
        var definition = new { targetGroupIndex = 0, targetGroupVersion = 0, sourceGroupIndex = 0, sourceGroupVersion = 0 };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        if (data == null)
        {
            return;
        }
        
        Entity targetGroup = new Entity { Index = data.targetGroupIndex, Version = data.targetGroupVersion };
        Entity sourceGroup = new Entity { Index = data.sourceGroupIndex, Version = data.sourceGroupVersion };
        
        if (targetGroup != Entity.Null && sourceGroup != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.JoinGroups(targetGroup, sourceGroup);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSetGroupLeader(string input)
    {
        var definition = new { groupIndex = 0, groupVersion = 0, leaderIndex = 0, leaderVersion = 0 };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        if (data == null)
        {
            return;
        }
        
        Entity groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
        Entity leaderEntity = new Entity { Index = data.leaderIndex, Version = data.leaderVersion };
        
        if (leaderEntity == Entity.Null && m_SelectedEntity != Entity.Null)
        {
            leaderEntity = m_SelectedEntity;
            
            if (groupEntity == Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
            {
                var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
                groupEntity = member.m_GroupEntity;
            }
        }
        
        if (groupEntity != Entity.Null && leaderEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SetGroupLeader(groupEntity, leaderEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallSkipStep(string input)
    {
        Entity groupEntity = Entity.Null;
        
        if (m_SelectedEntity != Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        
        if (groupEntity == Entity.Null && !string.IsNullOrEmpty(input))
        {
            var definition = new { groupIndex = 0, groupVersion = 0 };
            var data = JsonConvert.DeserializeAnonymousType(input, definition);
            if (data != null)
            {
                groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            }
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.SkipStep(groupEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallCopyPhasesToJunction(string input)
    {
        var definition = new { sourceIndex = 0, sourceVersion = 0, targetIndex = 0, targetVersion = 0 };
        var data = JsonConvert.DeserializeAnonymousType(input, definition);
        
        if (data == null)
        {
            return;
        }
        
        Entity sourceJunction = new Entity { Index = data.sourceIndex, Version = data.sourceVersion };
        Entity targetJunction = new Entity { Index = data.targetIndex, Version = data.targetVersion };
        
        if (sourceJunction == Entity.Null && m_SelectedEntity != Entity.Null)
        {
            sourceJunction = m_SelectedEntity;
        }
        
        if (sourceJunction != Entity.Null && targetJunction != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.CopyPhasesToJunction(sourceJunction, targetJunction);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallMatchPhaseDurationsToLeader(string input)
    {
        Entity groupEntity = Entity.Null;
        
        if (m_SelectedEntity != Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        
        if (groupEntity == Entity.Null && !string.IsNullOrEmpty(input))
        {
            var definition = new { groupIndex = 0, groupVersion = 0 };
            var data = JsonConvert.DeserializeAnonymousType(input, definition);
            if (data != null)
            {
                groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            }
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.MatchPhaseDurationsToLeader(groupEntity);
            m_MainPanelBinding.Update();
        }
    }
    
    

    

    protected void CallApplyBestPhase(string input)
    {
        Entity groupEntity = Entity.Null;
        
        if (m_SelectedEntity != Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        
        if (groupEntity == Entity.Null && !string.IsNullOrEmpty(input))
        {
            var definition = new { groupIndex = 0, groupVersion = 0 };
            var data = JsonConvert.DeserializeAnonymousType(input, definition);
            if (data != null)
            {
                groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            }
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.ApplyBestPhaseToGroup(groupEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallHousekeepingGroup(string input)
    {
        Entity groupEntity = Entity.Null;
        
        if (m_SelectedEntity != Entity.Null && EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
        {
            var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
            groupEntity = member.m_GroupEntity;
        }
        
        if (groupEntity == Entity.Null && !string.IsNullOrEmpty(input))
        {
            var definition = new { groupIndex = 0, groupVersion = 0, all = false };
            var data = JsonConvert.DeserializeAnonymousType(input, definition);
            if (data != null)
            {
                if (data.all)
                {
                    var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
                    trafficGroupSystem.HousekeepingAllGroups();
                    m_MainPanelBinding.Update();
                    return;
                }
                groupEntity = new Entity { Index = data.groupIndex, Version = data.groupVersion };
            }
        }
        
        if (groupEntity != Entity.Null)
        {
            var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
            trafficGroupSystem.HousekeepingGroup(groupEntity);
            m_MainPanelBinding.Update();
        }
    }

    protected void CallUpdateMemberPhaseData(string jsonString)
    {
        var definition = new { 
            junctionIndex = 0, 
            junctionVersion = 0, 
            phaseIndex = -1, 
            key = "", 
            value = 0.0 
        };
        var input = JsonConvert.DeserializeAnonymousType(jsonString, definition);
        
        if (input == null)
        {
            return;
        }
        
        Entity junctionEntity = new Entity { Index = input.junctionIndex, Version = input.junctionVersion };
        
        if (junctionEntity == Entity.Null || !EntityManager.Exists(junctionEntity))
        {
            return;
        }

        DynamicBuffer<CustomPhaseData> customPhaseDataBuffer;
        if (!EntityManager.TryGetBuffer(junctionEntity, false, out customPhaseDataBuffer))
        {
            return;
        }
        
        int index = input.phaseIndex;
        if (index < 0 || index >= customPhaseDataBuffer.Length)
        {
            return;
        }
        
        var newValue = customPhaseDataBuffer[index];
        
        if (input.key == "MinimumDuration")
        {
            newValue.m_MinimumDuration = (ushort)input.value;
            if (newValue.m_MinimumDuration > newValue.m_MaximumDuration)
            {
                newValue.m_MaximumDuration = newValue.m_MinimumDuration;
            }
        }
        else if (input.key == "MaximumDuration")
        {
            newValue.m_MaximumDuration = (ushort)input.value;
            if (newValue.m_MinimumDuration > newValue.m_MaximumDuration)
            {
                newValue.m_MinimumDuration = newValue.m_MaximumDuration;
            }
        }
        else if (input.key == "TargetDurationMultiplier")
        {
            newValue.m_TargetDurationMultiplier = (float)input.value;
        }
        else if (input.key == "IntervalExponent")
        {
            newValue.m_IntervalExponent = (float)input.value;
        }
        else if (input.key == "LinkedWithNextPhase")
        {
            newValue.m_Options ^= CustomPhaseData.Options.LinkedWithNextPhase;
        }
        else if (input.key == "EndPhasePrematurely")
        {
            newValue.m_Options ^= CustomPhaseData.Options.EndPhasePrematurely;
        }
        else if (input.key == "ChangeMetric")
        {
            newValue.m_ChangeMetric = (CustomPhaseData.StepChangeMetric)(int)input.value;
        }
        else if (input.key == "WaitFlowBalance")
        {
            newValue.m_WaitFlowBalance = (float)input.value;
        }
        
        customPhaseDataBuffer[index] = newValue;
        
        if (input.key == "MaximumDuration" || input.key == "MinimumDuration")
        {
            if (EntityManager.HasComponent<TrafficGroupMember>(junctionEntity))
            {
                var member = EntityManager.GetComponentData<TrafficGroupMember>(junctionEntity);
                if (member.m_GroupEntity != Entity.Null)
                {
                    var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
                    trafficGroupSystem.RecalculateGroupCycleLength(member.m_GroupEntity);
                }
            }
        }
        
        m_MainPanelBinding.Update();
    }

    protected void CallUpdateEdgeGroupMaskForJunction(string input)
    {
        var definition = new { 
            junctionIndex = 0, 
            junctionVersion = 0, 
            edgeGroupMasks = new EdgeGroupMask[0]
        };
        var parsed = JsonConvert.DeserializeAnonymousType(input, definition);
        
        if (parsed == null || parsed.edgeGroupMasks == null)
        {
            return;
        }

        Entity junctionEntity = new Entity { Index = parsed.junctionIndex, Version = parsed.junctionVersion };
        
        if (junctionEntity == Entity.Null || !EntityManager.Exists(junctionEntity))
        {
            return;
        }

        DynamicBuffer<EdgeGroupMask> groupMaskBuffer;
        if (EntityManager.HasBuffer<EdgeGroupMask>(junctionEntity))
        {
            groupMaskBuffer = EntityManager.GetBuffer<EdgeGroupMask>(junctionEntity, false);
        }
        else
        {
            groupMaskBuffer = EntityManager.AddBuffer<EdgeGroupMask>(junctionEntity);
        }

        foreach (var newValue in parsed.edgeGroupMasks)
        {
            int index = CustomPhaseUtils.TryGet(groupMaskBuffer, newValue, out EdgeGroupMask oldValue);
            if (index >= 0)
            {
                groupMaskBuffer[index] = new EdgeGroupMask(oldValue, newValue);
            }
            else
            {
                groupMaskBuffer.Add(new EdgeGroupMask(oldValue, newValue));
            }
        }

        UpdateEdgeInfo(junctionEntity);
        EntityManager.AddComponentData(junctionEntity, default(Game.Common.Updated));
        RedrawGizmo();
        m_MainPanelBinding.Update();
    }

    protected void CallUpdateMemberPattern(string jsonString)
    {
        var definition = new { 
            junctionIndex = 0, 
            junctionVersion = 0, 
            patternValue = 0u,
            navigateToCustomPhase = false
        };
        var input = JsonConvert.DeserializeAnonymousType(jsonString, definition);
        
        if (input == null)
        {
            return;
        }
        
        Entity junctionEntity = new Entity { Index = input.junctionIndex, Version = input.junctionVersion };
        
        if (junctionEntity == Entity.Null || !EntityManager.Exists(junctionEntity))
        {
            return;
        }
        
        if (!EntityManager.HasComponent<CustomTrafficLights>(junctionEntity))
        {
            return;
        }
        
        var customTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(junctionEntity);
        customTrafficLights.SetPattern(((uint)customTrafficLights.GetPattern() & 0xFFFF0000) | input.patternValue);
        
        if (customTrafficLights.GetPatternOnly() != CustomTrafficLights.Patterns.Vanilla)
        {
            customTrafficLights.SetPattern(customTrafficLights.GetPattern() & ~CustomTrafficLights.Patterns.CentreTurnGiveWay);
        }
        
        if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
        {
            if (!EntityManager.HasBuffer<CustomPhaseData>(junctionEntity))
            {
                EntityManager.AddComponent<CustomPhaseData>(junctionEntity);
            }
            if (!EntityManager.HasBuffer<EdgeGroupMask>(junctionEntity))
            {
                EntityManager.AddComponent<EdgeGroupMask>(junctionEntity);
            }
            if (!EntityManager.HasBuffer<SubLaneGroupMask>(junctionEntity))
            {
                EntityManager.AddComponent<SubLaneGroupMask>(junctionEntity);
            }
            customTrafficLights.SetPattern(CustomTrafficLights.Patterns.CustomPhase);
        }
        if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.FixedTimed)
        {
            if (!EntityManager.HasBuffer<CustomPhaseData>(junctionEntity))
            {
                EntityManager.AddComponent<CustomPhaseData>(junctionEntity);
            }
            if (!EntityManager.HasBuffer<EdgeGroupMask>(junctionEntity))
            {
                EntityManager.AddComponent<EdgeGroupMask>(junctionEntity);
            }
            if (!EntityManager.HasBuffer<SubLaneGroupMask>(junctionEntity))
            {
                EntityManager.AddComponent<SubLaneGroupMask>(junctionEntity);
            }
            customTrafficLights.SetPattern(CustomTrafficLights.Patterns.FixedTimed);
        }
        
        EntityManager.SetComponentData(junctionEntity, customTrafficLights);
        EntityManager.AddComponentData(junctionEntity, default(Game.Common.Updated));
        
        // Update the cached m_CustomTrafficLights if this is the selected entity
        if (junctionEntity == m_SelectedEntity)
        {
            m_CustomTrafficLights = customTrafficLights;
            UpdateEntity();
        }
        
        // Navigate to CustomPhase editor if requested
        if (input.navigateToCustomPhase && 
            (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase ||
             customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.FixedTimed))
        {
            // Select the junction first if not already selected
            if (junctionEntity != m_SelectedEntity)
            {
                if (m_SelectedEntity != Entity.Null)
                {
                    SaveSelectedEntity();
                }
                m_SelectedEntity = junctionEntity;
                m_CustomTrafficLights = customTrafficLights;
                UpdateEdgeInfo(junctionEntity);
            }
            SetMainPanelState(MainPanelState.CustomPhase);
        }
        else
        {
            m_MainPanelBinding.Update();
        }
    }

    public void NavigateTo(Entity entity)
    {
        var cameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
        if (cameraUpdateSystem.orbitCameraController != null && entity != Entity.Null)
        {
            cameraUpdateSystem.orbitCameraController.followedEntity = entity;
            cameraUpdateSystem.orbitCameraController.TryMatchPosition(cameraUpdateSystem.activeCameraController);
            cameraUpdateSystem.activeCameraController = cameraUpdateSystem.orbitCameraController;
        }
    }

    protected void CallHighlightEdge(string jsonString)
    {
        var definition = new { edgeIndex = 0, edgeVersion = 0 };
        var input = JsonConvert.DeserializeAnonymousType(jsonString, definition);
        
        if (input.edgeIndex < 0)
        {
            m_HighlightedEdge = Entity.Null;
        }
        else
        {
            m_HighlightedEdge = new Entity { Index = input.edgeIndex, Version = input.edgeVersion };
        }
        
        RedrawGizmo();
    }
}