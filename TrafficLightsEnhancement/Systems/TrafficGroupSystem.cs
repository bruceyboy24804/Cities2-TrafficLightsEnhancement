using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Net;
using Game.Simulation;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using C2VM.TrafficLightsEnhancement.Domain;
using Colossal.Entities;
using Game.SceneFlow;

namespace C2VM.TrafficLightsEnhancement.Systems;

public partial class TrafficGroupSystem : GameSystemBase
{
	private static ILog m_Log = Mod.m_Log;

	private EntityQuery m_GroupQuery;
	private EntityQuery m_MemberQuery;
	private SimulationSystem m_SimulationSystem;

	protected override void OnCreate()
	{
		base.OnCreate();

		m_GroupQuery = GetEntityQuery(
			ComponentType.ReadOnly<TrafficGroup>()
		);

		m_MemberQuery = GetEntityQuery(
			ComponentType.ReadOnly<TrafficGroupMember>()
		);
		
		m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
	}

	protected override void OnUpdate()
	{
		float currentTick = m_SimulationSystem.frameIndex;
		
		var groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
		var groupComponents = m_GroupQuery.ToComponentDataArray<TrafficGroup>(Allocator.Temp);
		
		for (int i = 0; i < groups.Length; i++)
		{
			var groupEntity = groups[i];
			var group = groupComponents[i];
			
			if (!group.m_IsCoordinated)
			{
				continue;
			}
			
			group.m_CycleTimer += 1f;
			if (group.m_CycleTimer >= group.m_CycleLength)
			{
				group.m_CycleTimer = 0f;
			}
			ApplyCoordination(groupEntity, group);
			EntityManager.SetComponentData(groupEntity, group);
		}
		
		groups.Dispose();
		groupComponents.Dispose();
	}

	public Entity CreateGroup(string name = null)
	{
		if (string.IsNullOrEmpty(name))
		{
			var allGroups = GetAllGroups();
			int groupCount = 0;
			foreach (var group in allGroups)
			{
				groupCount++;
			}
			allGroups.Dispose();
			name = $"Group #{groupCount + 1}";
		}
		
		Entity groupEntity = EntityManager.CreateEntity();
		EntityManager.AddComponentData(groupEntity, new TrafficGroup(isCoordinated: true));
		EntityManager.AddComponentData(groupEntity, new TrafficGroupName(name));

		return groupEntity;
	}

	public bool AddJunctionToGroup(Entity groupEntity, Entity junctionEntity)
	{
		if (groupEntity == Entity.Null || junctionEntity == Entity.Null)
		{
			return false;
		}

		if (!EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return false;
		}

		if (EntityManager.HasComponent<TrafficGroupMember>(junctionEntity))
		{
			var existingMember = EntityManager.GetComponentData<TrafficGroupMember>(junctionEntity);
			if (existingMember.m_GroupEntity != Entity.Null)
			{
				return false;
			}
		}

		int memberCount = GetGroupMemberCount(groupEntity);
		bool isLeader = memberCount == 0;
		Entity leaderEntity = isLeader ? junctionEntity : GetGroupLeader(groupEntity);

		var member = new TrafficGroupMember(groupEntity, leaderEntity, memberCount, 0f, 0f, 0, 0, isLeader);
		EntityManager.AddComponentData(junctionEntity, member);
		if (EntityManager.HasComponent<CustomTrafficLights>(junctionEntity))
		{
			var customTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(junctionEntity);
			customTrafficLights.SetPatternOnly(CustomTrafficLights.Patterns.CustomPhase);
			customTrafficLights.m_Timer = 0;
			EntityManager.SetComponentData(junctionEntity, customTrafficLights);
		}
		else
		{
			EntityManager.AddComponentData(junctionEntity, new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase));
		}
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
		EntityManager.AddComponentData(junctionEntity, default(Updated));
		if (isLeader)
		{
			UpdateAllMembersLeader(groupEntity, junctionEntity);
		}
		SyncCycleLengthFromJunction(groupEntity, junctionEntity);
		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		if (group.m_GreenWaveEnabled)
		{
			CalculateGreenWaveTiming(groupEntity);
		}
		
		if (group.m_IsCoordinated && !isLeader && leaderEntity != Entity.Null && EntityManager.HasComponent<TrafficLights>(leaderEntity))
		{
			var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
			PropagateLeaderPhaseChange(groupEntity, leaderLights.m_CurrentSignalGroup, leaderLights.m_State);
		}
		return true;
	}

	public bool RemoveJunctionFromGroup(Entity junctionEntity)
	{
		if (junctionEntity == Entity.Null)
		{
			return false;
		}

		if (!EntityManager.HasComponent<TrafficGroupMember>(junctionEntity))
		{
			return false;
		}

		var member = EntityManager.GetComponentData<TrafficGroupMember>(junctionEntity);
		Entity groupEntity = member.m_GroupEntity;

		EntityManager.RemoveComponent<TrafficGroupMember>(junctionEntity);

		if (member.m_IsGroupLeader && groupEntity != Entity.Null)
		{
			AssignNewLeader(groupEntity);
		}

		ReindexGroupMembers(groupEntity);

		return true;
	}

	public void DeleteGroup(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var members = GetGroupMembers(groupEntity);
		foreach (var memberEntity in members)
		{
			EntityManager.RemoveComponent<TrafficGroupMember>(memberEntity);
		}
		members.Dispose();

		EntityManager.DestroyEntity(groupEntity);
	}

	public NativeList<Entity> GetGroupMembers(Entity groupEntity)
	{
		var members = new NativeList<Entity>(8, Allocator.Temp);

		if (groupEntity == Entity.Null)
		{
			return members;
		}

		var entities = m_MemberQuery.ToEntityArray(Allocator.Temp);
		var memberComponents = m_MemberQuery.ToComponentDataArray<TrafficGroupMember>(Allocator.Temp);

		for (int i = 0; i < entities.Length; i++)
		{
			if (memberComponents[i].m_GroupEntity == groupEntity)
			{
				members.Add(entities[i]);
			}
		}

		entities.Dispose();
		memberComponents.Dispose();

		return members;
	}

	public int GetGroupMemberCount(Entity groupEntity)
	{
		if (groupEntity == Entity.Null)
		{
			return 0;
		}

		int count = 0;
		var memberComponents = m_MemberQuery.ToComponentDataArray<TrafficGroupMember>(Allocator.Temp);

		for (int i = 0; i < memberComponents.Length; i++)
		{
			if (memberComponents[i].m_GroupEntity == groupEntity)
			{
				count++;
			}
		}

		memberComponents.Dispose();
		return count;
	}

	public NativeArray<Entity> GetAllGroups()
	{
		return m_GroupQuery.ToEntityArray(Allocator.Temp);
	}

	public Entity GetJunctionGroup(Entity junctionEntity)
	{
		if (junctionEntity == Entity.Null)
		{
			return Entity.Null;
		}

		if (!EntityManager.HasComponent<TrafficGroupMember>(junctionEntity))
		{
			return Entity.Null;
		}

		var member = EntityManager.GetComponentData<TrafficGroupMember>(junctionEntity);
		return member.m_GroupEntity;
	}

	public string GetGroupName(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupName>(groupEntity))
		{
			return "";
		}

		var groupName = EntityManager.GetComponentData<TrafficGroupName>(groupEntity);
		return groupName.GetName();
	}

	public void SetGroupName(Entity groupEntity, string name)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupName>(groupEntity))
		{
			return;
		}

		var groupName = new TrafficGroupName(name);
		EntityManager.SetComponentData(groupEntity, groupName);
	}

	private void AssignNewLeader(Entity groupEntity)
	{
		var members = GetGroupMembers(groupEntity);
		if (members.Length > 0)
		{
			var firstMember = members[0];
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(firstMember);
			memberData.m_IsGroupLeader = true;
			memberData.m_LeaderEntity = firstMember;
			EntityManager.SetComponentData(firstMember, memberData);
			
			UpdateAllMembersLeader(groupEntity, firstMember);
		}
		members.Dispose();
	}

	private void ReindexGroupMembers(Entity groupEntity)
	{
		var members = GetGroupMembers(groupEntity);
		for (int i = 0; i < members.Length; i++)
		{
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(members[i]);
			memberData.m_GroupIndex = i;
			EntityManager.SetComponentData(members[i], memberData);
		}
		members.Dispose();
	}

	public Entity GetGroupLeader(Entity groupEntity)
	{
		var members = GetGroupMembers(groupEntity);
		Entity leader = Entity.Null;
		
		foreach (var memberEntity in members)
		{
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			if (memberData.m_IsGroupLeader)
			{
				leader = memberEntity;
				break;
			}
		}
		
		members.Dispose();
		return leader;
	}

	private void UpdateAllMembersLeader(Entity groupEntity, Entity leaderEntity)
	{
		var members = GetGroupMembers(groupEntity);
		
		foreach (var memberEntity in members)
		{
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			memberData.m_LeaderEntity = leaderEntity;
			EntityManager.SetComponentData(memberEntity, memberData);
		}
		
		members.Dispose();
	}

	public void CalculateGreenWaveTiming(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		if (!group.m_GreenWaveEnabled)
		{
			return;
		}

		Entity leaderEntity = GetGroupLeader(groupEntity);
		if (leaderEntity == Entity.Null || !EntityManager.HasComponent<Game.Net.Node>(leaderEntity))
		{
			return;
		}

		var leaderNode = EntityManager.GetComponentData<Game.Net.Node>(leaderEntity);
		float3 leaderPosition = leaderNode.m_Position;

		var members = GetGroupMembers(groupEntity);

		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				continue;
			}

			if (!EntityManager.HasComponent<Game.Net.Node>(memberEntity))
			{
				continue;
			}

			var memberNode = EntityManager.GetComponentData<Game.Net.Node>(memberEntity);
			float3 memberPosition = memberNode.m_Position;

			float distance = math.distance(leaderPosition, memberPosition);

			float travelTimeSeconds = distance / group.m_GreenWaveSpeed;

			int phaseOffset;
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			if (memberData.m_SignalDelay != 0)
			{
				phaseOffset = memberData.m_SignalDelay;
			}
			else
			{
				phaseOffset = (int)math.round(travelTimeSeconds + group.m_GreenWaveOffset);
			}

			memberData.m_DistanceToLeader = distance;
			memberData.m_PhaseOffset = phaseOffset;
			EntityManager.SetComponentData(memberEntity, memberData);

		}

		members.Dispose();
	}

	public void SetGreenWaveEnabled(Entity groupEntity, bool enabled)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		group.m_GreenWaveEnabled = enabled;
		EntityManager.SetComponentData(groupEntity, group);

		if (enabled)
		{
			Entity leaderEntity = GetGroupLeader(groupEntity);
			if (leaderEntity != Entity.Null && EntityManager.HasBuffer<CustomPhaseData>(leaderEntity) && 
			    EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false ,out var phases) && phases.Length > 0)
			{
				CalculateEnhancedGreenWaveTiming(groupEntity);
			}
			else
			{
				CalculateGreenWaveTiming(groupEntity);
			}
			if (group.m_IsCoordinated && leaderEntity != Entity.Null && EntityManager.HasComponent<TrafficLights>(leaderEntity))
			{
				var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
				PropagateLeaderPhaseChange(groupEntity, leaderLights.m_CurrentSignalGroup, leaderLights.m_State);
			}
		}
	}

	public void SetGreenWaveSpeed(Entity groupEntity, float speed)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		group.m_GreenWaveSpeed = math.max(1f, speed);
		EntityManager.SetComponentData(groupEntity, group);

		if (group.m_GreenWaveEnabled)
		{
			Entity leaderEntity = GetGroupLeader(groupEntity);
			if (leaderEntity != Entity.Null && EntityManager.HasBuffer<CustomPhaseData>(leaderEntity) && 
			    EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false ,out var phases) && phases.Length > 0)
			{
				CalculateEnhancedGreenWaveTiming(groupEntity);
			}
			else
			{
				CalculateGreenWaveTiming(groupEntity);
			}
			
			if (group.m_IsCoordinated && leaderEntity != Entity.Null && EntityManager.HasComponent<TrafficLights>(leaderEntity))
			{
				var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
				PropagateLeaderPhaseChange(groupEntity, leaderLights.m_CurrentSignalGroup, leaderLights.m_State);
			}
		}
	}

	public void SetGreenWaveOffset(Entity groupEntity, float offset)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		group.m_GreenWaveOffset = offset;
		EntityManager.SetComponentData(groupEntity, group);

		if (group.m_GreenWaveEnabled)
		{
			Entity leaderEntity = GetGroupLeader(groupEntity);
			if (leaderEntity != Entity.Null && EntityManager.HasBuffer<CustomPhaseData>(leaderEntity) && 
			    EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false ,out var phases) && phases.Length > 0)
			{
				CalculateEnhancedGreenWaveTiming(groupEntity);
			}
			else
			{
				CalculateGreenWaveTiming(groupEntity);
			}
			
			if (group.m_IsCoordinated && leaderEntity != Entity.Null && EntityManager.HasComponent<TrafficLights>(leaderEntity))
			{
				var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
				PropagateLeaderPhaseChange(groupEntity, leaderLights.m_CurrentSignalGroup, leaderLights.m_State);
			}
		}

	}

	public void SetSignalDelay(Entity groupEntity, Entity memberEntity, int signalDelay)
	{
		if (groupEntity == Entity.Null || memberEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroupMember>(memberEntity))
		{
			return;
		}

		var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
		memberData.m_SignalDelay = signalDelay;
		EntityManager.SetComponentData(memberEntity, memberData);

		if (EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
			if (group.m_GreenWaveEnabled)
			{
				Entity leaderEntity = GetGroupLeader(groupEntity);
				if (leaderEntity != Entity.Null && EntityManager.HasBuffer<CustomPhaseData>(leaderEntity) && 
				    EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false ,out var phases) && phases.Length > 0)
				{
					CalculateEnhancedGreenWaveTiming(groupEntity);
				}
				else
				{
					CalculateGreenWaveTiming(groupEntity);
				}
			}
		}
	}

	public void CalculateSignalDelays(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		var members = GetGroupMembers(groupEntity);

		
		Entity leaderEntity = Entity.Null;
		float3 leaderPosition = float3.zero;
		
		foreach (var memberEntity in members)
		{
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			if (memberData.m_IsGroupLeader)
			{
				leaderEntity = memberEntity;
				if (EntityManager.HasComponent<Game.Net.Node>(leaderEntity))
				{
					var leaderNode = EntityManager.GetComponentData<Game.Net.Node>(leaderEntity);
					leaderPosition = leaderNode.m_Position;
				}
				break;
			}
		}

		if (leaderEntity == Entity.Null)
		{
			members.Dispose();
			return;
		}

		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				var leaderMemberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
				leaderMemberData.m_SignalDelay = 0;
				EntityManager.SetComponentData(memberEntity, leaderMemberData);
				continue;
			}

			if (!EntityManager.HasComponent<Game.Net.Node>(memberEntity))
			{
				continue;
			}

			var memberNode = EntityManager.GetComponentData<Game.Net.Node>(memberEntity);
			float3 memberPosition = memberNode.m_Position;

			float distance = math.distance(leaderPosition, memberPosition);
			float travelTimeSeconds = distance / group.m_GreenWaveSpeed;
			int calculatedDelay = (int)math.round(travelTimeSeconds + group.m_GreenWaveOffset);

			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			memberData.m_SignalDelay = calculatedDelay;
			EntityManager.SetComponentData(memberEntity, memberData);

		}

		CalculateGreenWaveTiming(groupEntity);

		members.Dispose();
	}

	public void SetCoordinated(Entity groupEntity, bool coordinated)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		group.m_IsCoordinated = coordinated;
		
		if (coordinated)
		{
			group.m_LastSyncTime = 0f;
			group.m_CycleTimer = 0f;
			
			
			Entity leaderEntity = GetGroupLeader(groupEntity);
			if (leaderEntity != Entity.Null && EntityManager.HasComponent<TrafficLights>(leaderEntity))
			{
				var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
				PropagateLeaderPhaseChange(groupEntity, leaderLights.m_CurrentSignalGroup, leaderLights.m_State);
			}
		}
		
		EntityManager.SetComponentData(groupEntity, group);

	}

	private void ApplyCoordination(Entity groupEntity, TrafficGroup group)
	{
		if (group.m_CycleLength <= 0)
		{
			return;
		}

		Entity leaderEntity = GetGroupLeader(groupEntity);
		if (leaderEntity == Entity.Null || !EntityManager.HasComponent<TrafficLights>(leaderEntity))
		{
			return;
		}

		var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
		var members = GetGroupMembers(groupEntity);
		
		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				continue;
			}

			if (!EntityManager.HasComponent<TrafficLights>(memberEntity))
			{
				continue;
			}

			var trafficLights = EntityManager.GetComponentData<TrafficLights>(memberEntity);
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			
			if (trafficLights.m_SignalGroupCount == 0)
			{
				continue;
			}

			
			int expectedPhase = leaderLights.m_CurrentSignalGroup + memberData.m_PhaseOffset;
			int phaseCount = trafficLights.m_SignalGroupCount;
			if (phaseCount > 0)
			{
				expectedPhase = ((expectedPhase - 1) % phaseCount) + 1;
				if (expectedPhase <= 0) expectedPhase += phaseCount;
			}

			int phaseDiff = math.abs(trafficLights.m_CurrentSignalGroup - expectedPhase);
			if (phaseDiff > 1 && phaseDiff < trafficLights.m_SignalGroupCount - 1)
			{
				trafficLights.m_NextSignalGroup = (byte)expectedPhase;
				if (trafficLights.m_State == TrafficLightState.Ongoing)
				{
					trafficLights.m_State = TrafficLightState.Ending;
				}
				EntityManager.SetComponentData(memberEntity, trafficLights);
			}
		}

		members.Dispose();
	}

	
	public float CalculateCycleLengthFromJunction(Entity junctionEntity)
	{
		if (junctionEntity == Entity.Null)
		{
			return 0f;
		}

		if (!EntityManager.HasBuffer<CustomPhaseData>(junctionEntity))
		{
			return 0f;
		}

		EntityManager.TryGetBuffer<CustomPhaseData>(junctionEntity, false, out var phaseBuffer);
		if (phaseBuffer.Length == 0)
		{
			return 0f;
		}

		float totalCycleLength = 0f;
		for (int i = 0; i < phaseBuffer.Length; i++)
		{
			var phase = phaseBuffer[i];
			totalCycleLength += phase.m_MaximumDuration;
		}

		return totalCycleLength;
	}

	
	private void SyncCycleLengthFromJunction(Entity groupEntity, Entity junctionEntity)
	{
		if (groupEntity == Entity.Null || junctionEntity == Entity.Null)
		{
			return;
		}

		float junctionCycleLength = CalculateCycleLengthFromJunction(junctionEntity);
		if (junctionCycleLength <= 0)
		{
			return; 
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		
		if (EntityManager.HasComponent<TrafficGroupMember>(junctionEntity))
		{
			var member = EntityManager.GetComponentData<TrafficGroupMember>(junctionEntity);
			if (member.m_IsGroupLeader)
			{
				group.m_CycleLength = junctionCycleLength;
				EntityManager.SetComponentData(groupEntity, group);
				return;
			}
		}

		
		float cycleDifference = math.abs(group.m_CycleLength - junctionCycleLength);
		
	}

	
	public void RecalculateGroupCycleLength(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		Entity leaderEntity = GetGroupLeader(groupEntity);
		if (leaderEntity == Entity.Null)
		{
			return;
		}

		float leaderCycleLength = CalculateCycleLengthFromJunction(leaderEntity);
		if (leaderCycleLength <= 0)
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		group.m_CycleLength = leaderCycleLength;
		EntityManager.SetComponentData(groupEntity, group);


		var members = GetGroupMembers(groupEntity);
		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				continue;
			}

			float memberCycleLength = CalculateCycleLengthFromJunction(memberEntity);
			if (memberCycleLength > 0)
			{
				float cycleDifference = math.abs(leaderCycleLength - memberCycleLength);
				
			}
		}
		members.Dispose();
	}
	
	public Dictionary<Entity, (float cycleLength, bool isCompatible)> GetGroupCycleLengthInfo(Entity groupEntity)
	{
		var result = new Dictionary<Entity, (float, bool)>();
		
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return result;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		float targetCycleLength = group.m_CycleLength;

		var members = GetGroupMembers(groupEntity);
		foreach (var memberEntity in members)
		{
			float memberCycleLength = CalculateCycleLengthFromJunction(memberEntity);
			bool isCompatible = memberCycleLength <= 0 || math.abs(targetCycleLength - memberCycleLength) <= 2f;
			result[memberEntity] = (memberCycleLength, isCompatible);
		}
		members.Dispose();

		return result;
	}

	

	
	public void CalculateEnhancedGreenWaveTiming(Entity groupEntity, int mainPhaseIndex = 0)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		Entity leaderEntity = GetGroupLeader(groupEntity);
		
		if (leaderEntity == Entity.Null || !EntityManager.HasComponent<Node>(leaderEntity))
		{
			return;
		}

		var leaderNode = EntityManager.GetComponentData<Node>(leaderEntity);
		float3 leaderPosition = leaderNode.m_Position;

		float leaderCycleLength = CalculateCycleLengthFromJunction(leaderEntity);
		if (leaderCycleLength <= 0)
		{
			CalculateGreenWaveTiming(groupEntity);
			return;
		}

		float mainPhaseStartTime = 0f;
		if (EntityManager.HasBuffer<CustomPhaseData>(leaderEntity))
		{
			EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false, out var leaderPhases);
			for (int i = 0; i < math.min(mainPhaseIndex, leaderPhases.Length); i++)
			{
				mainPhaseStartTime += leaderPhases[i].m_MaximumDuration;
			}
		}

		var members = GetGroupMembers(groupEntity);

		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				
				var leaderMember = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
				leaderMember.m_PhaseOffset = 0;
				leaderMember.m_SignalDelay = 0;
				EntityManager.SetComponentData(memberEntity, leaderMember);
				continue;
			}

			if (!EntityManager.HasComponent<Node>(memberEntity))
			{
				continue;
			}

			var memberNode = EntityManager.GetComponentData<Node>(memberEntity);
			float3 memberPosition = memberNode.m_Position;
			float distance = math.distance(leaderPosition, memberPosition);

			float travelTimeSeconds = distance / group.m_GreenWaveSpeed;
			
			int signalDelay = (int)math.round(travelTimeSeconds + group.m_GreenWaveOffset);
			
			float arrivalTime = mainPhaseStartTime + signalDelay;
			int phaseOffset = (int)(arrivalTime / leaderCycleLength * GetPhaseCount(memberEntity));
			phaseOffset = phaseOffset % math.max(1, GetPhaseCount(memberEntity));

			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			memberData.m_DistanceToLeader = distance;
			memberData.m_PhaseOffset = phaseOffset;
			memberData.m_SignalDelay = signalDelay;
			EntityManager.SetComponentData(memberEntity, memberData);

		}

		members.Dispose();
	}

	private int GetPhaseCount(Entity junctionEntity)
	{
		if (EntityManager.HasBuffer<CustomPhaseData>(junctionEntity))
		{
			return EntityManager.TryGetBuffer<CustomPhaseData>(junctionEntity, false, out var phases) ? phases.Length : 0;
		}
		return 1;
	}
	
	public void PropagateLeaderPhaseChange(Entity groupEntity, byte newPhase, TrafficLightState newState)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		if (!group.m_IsCoordinated)
		{
			return;
		}

		var members = GetGroupMembers(groupEntity);
		Entity leaderEntity = GetGroupLeader(groupEntity);

		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				continue;
			}

			if (!EntityManager.HasComponent<TrafficLights>(memberEntity))
			{
				continue;
			}

			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			var trafficLights = EntityManager.GetComponentData<TrafficLights>(memberEntity);

			int adjustedPhase = newPhase + memberData.m_PhaseOffset;
			int phaseCount = trafficLights.m_SignalGroupCount;
			if (phaseCount > 0)
			{
				adjustedPhase = ((adjustedPhase - 1) % phaseCount) + 1;
			}
			
			if (trafficLights.m_CurrentSignalGroup != adjustedPhase)
			{
				trafficLights.m_NextSignalGroup = (byte)adjustedPhase;
				
				if (trafficLights.m_State == TrafficLightState.Ongoing)
				{
					    trafficLights.m_State = TrafficLightState.Ending;

				}
			}

			EntityManager.SetComponentData(memberEntity, trafficLights);
		}

		members.Dispose();
	}

	
	public void ForceSyncToLeader(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		Entity leaderEntity = GetGroupLeader(groupEntity);
		if (leaderEntity == Entity.Null || !EntityManager.HasComponent<TrafficLights>(leaderEntity))
		{
			return;
		}

		var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
		var members = GetGroupMembers(groupEntity);

		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				continue;
			}

			if (!EntityManager.HasComponent<TrafficLights>(memberEntity))
			{
				continue;
			}

			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			var trafficLights = EntityManager.GetComponentData<TrafficLights>(memberEntity);

			int adjustedPhase = leaderLights.m_CurrentSignalGroup + memberData.m_PhaseOffset;
			int phaseCount = trafficLights.m_SignalGroupCount;
			if (phaseCount > 0)
			{
				adjustedPhase = ((adjustedPhase - 1) % phaseCount) + 1;
			}

			trafficLights.m_CurrentSignalGroup = (byte)adjustedPhase;
			trafficLights.m_State = leaderLights.m_State;
			
			int adjustedTimer = leaderLights.m_Timer - memberData.m_SignalDelay;
			trafficLights.m_Timer = (byte)math.clamp(adjustedTimer, 0, 255);

			EntityManager.SetComponentData(memberEntity, trafficLights);
		}

		members.Dispose();
	}

	

	#region Group Management Extensions

	
	public void JoinGroups(Entity targetGroupEntity, Entity sourceGroupEntity)
	{
		if (targetGroupEntity == Entity.Null || sourceGroupEntity == Entity.Null)
		{
			var messageDialog = new MessageDialog("Cannot join - null entity provided");
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
			return;
		}

		if (!EntityManager.HasComponent<TrafficGroup>(targetGroupEntity) || 
		    !EntityManager.HasComponent<TrafficGroup>(sourceGroupEntity))
		{
			var messageDialog = new MessageDialog(" One or both entities are not valid groups");
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
			return;
		}

		if (targetGroupEntity == sourceGroupEntity)
		{
			var messageDialog = new MessageDialog("Cannot join a group with itself");
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
			return;
		}

		var targetGroup = EntityManager.GetComponentData<TrafficGroup>(targetGroupEntity);
		var sourceGroup = EntityManager.GetComponentData<TrafficGroup>(sourceGroupEntity);

		var targetMembers = GetGroupMembers(targetGroupEntity);
		var sourceMembers = GetGroupMembers(sourceGroupEntity);

		int targetCount = targetMembers.Length;
		int sourceCount = sourceMembers.Length;
		int totalCount = targetCount + sourceCount;

		if (totalCount == 0)
		{
			targetMembers.Dispose();
			sourceMembers.Dispose();
			return;
		}

		float avgCycleLength = (targetGroup.m_CycleLength * targetCount + sourceGroup.m_CycleLength * sourceCount) / totalCount;
		targetGroup.m_CycleLength = avgCycleLength;

		targetGroup.m_GreenWaveSpeed = (targetGroup.m_GreenWaveSpeed * targetCount + sourceGroup.m_GreenWaveSpeed * sourceCount) / totalCount;
		targetGroup.m_GreenWaveOffset = (targetGroup.m_GreenWaveOffset * targetCount + sourceGroup.m_GreenWaveOffset * sourceCount) / totalCount;
		targetGroup.m_GreenWaveEnabled = targetGroup.m_GreenWaveEnabled || sourceGroup.m_GreenWaveEnabled;

		EntityManager.SetComponentData(targetGroupEntity, targetGroup);

		Entity targetLeader = GetGroupLeader(targetGroupEntity);

		int newIndex = targetCount;
		foreach (var memberEntity in sourceMembers)
		{
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			memberData.m_GroupEntity = targetGroupEntity;
			memberData.m_LeaderEntity = targetLeader;
			memberData.m_GroupIndex = newIndex++;
			memberData.m_IsGroupLeader = false; 
			EntityManager.SetComponentData(memberEntity, memberData);
		}

		targetMembers.Dispose();
		sourceMembers.Dispose();

		EntityManager.DestroyEntity(sourceGroupEntity);

		if (targetGroup.m_GreenWaveEnabled)
		{
			Entity leaderEntity = GetGroupLeader(targetGroupEntity);
			if (leaderEntity != Entity.Null && EntityManager.HasBuffer<CustomPhaseData>(leaderEntity) && 
			    EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false, out var phases) && phases.Length > 0)
			{
				CalculateEnhancedGreenWaveTiming(targetGroupEntity);
			}
			else
			{
				CalculateGreenWaveTiming(targetGroupEntity);
			}
		}

		m_Log.Info($"TrafficGroupSystem: Joined groups - {sourceCount} members moved to target group (now {totalCount} members)");
	}

	
	public bool SetGroupLeader(Entity groupEntity, Entity newLeaderEntity)
	{
		if (groupEntity == Entity.Null || newLeaderEntity == Entity.Null)
		{
			return false;
		}

		if (!EntityManager.HasComponent<TrafficGroupMember>(newLeaderEntity))
		{
			m_Log.Warn($"Entity {newLeaderEntity} is not a group member");
			return false;
		}

		var newLeaderMember = EntityManager.GetComponentData<TrafficGroupMember>(newLeaderEntity);
		if (newLeaderMember.m_GroupEntity != groupEntity)
		{
			m_Log.Warn($"Entity {newLeaderEntity} is not in group {groupEntity}");
			return false;
		}

		var members = GetGroupMembers(groupEntity);
		foreach (var memberEntity in members)
		{
			var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
			if (memberData.m_IsGroupLeader)
			{
				memberData.m_IsGroupLeader = false;
				EntityManager.SetComponentData(memberEntity, memberData);
			}
		}

		newLeaderMember.m_IsGroupLeader = true;
		newLeaderMember.m_LeaderEntity = newLeaderEntity;
		newLeaderMember.m_PhaseOffset = 0;
		newLeaderMember.m_SignalDelay = 0;
		newLeaderMember.m_DistanceToLeader = 0f;
		EntityManager.SetComponentData(newLeaderEntity, newLeaderMember);

		UpdateAllMembersLeader(groupEntity, newLeaderEntity);

		members.Dispose();

		RecalculateGroupCycleLength(groupEntity);

		var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
		if (group.m_GreenWaveEnabled)
		{
			if (EntityManager.HasBuffer<CustomPhaseData>(newLeaderEntity) && 
			    EntityManager.TryGetBuffer<CustomPhaseData>(newLeaderEntity, false, out var phases) && phases.Length > 0)
			{
				CalculateEnhancedGreenWaveTiming(groupEntity);
			}
			else
			{
				CalculateGreenWaveTiming(groupEntity);
			}
		}

		return true;
	}

	
	public void SkipStep(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var members = GetGroupMembers(groupEntity);

		foreach (var memberEntity in members)
		{
			if (!EntityManager.HasComponent<TrafficLights>(memberEntity))
			{
				continue;
			}

			var trafficLights = EntityManager.GetComponentData<TrafficLights>(memberEntity);

			int nextPhase = trafficLights.m_CurrentSignalGroup + 1;
			if (nextPhase > trafficLights.m_SignalGroupCount)
			{
				nextPhase = 1;
			}

			trafficLights.m_NextSignalGroup = (byte)nextPhase;
			trafficLights.m_State = TrafficLightState.Ending;
			trafficLights.m_Timer = 0;

			EntityManager.SetComponentData(memberEntity, trafficLights);

			if (EntityManager.HasComponent<CustomTrafficLights>(memberEntity))
			{
				var customLights = EntityManager.GetComponentData<CustomTrafficLights>(memberEntity);
				customLights.m_Timer = 0;
				EntityManager.SetComponentData(memberEntity, customLights);
			}
		}

		members.Dispose();
	}

	private bool ValidatePhaseSyncCompatibility(Entity sourceJunction, Entity targetJunction, out string errorMessage)
	{
		errorMessage = "";

		
		CustomTrafficLights.Patterns sourcePattern = CustomTrafficLights.Patterns.Vanilla;
		bool sourceHasCustomLights = EntityManager.HasComponent<CustomTrafficLights>(sourceJunction);
		if (sourceHasCustomLights)
		{
			var sourceLights = EntityManager.GetComponentData<CustomTrafficLights>(sourceJunction);
			sourcePattern = sourceLights.GetPatternOnly();
		}

		
		CustomTrafficLights.Patterns targetPattern = CustomTrafficLights.Patterns.Vanilla;
		bool targetHasCustomLights = EntityManager.HasComponent<CustomTrafficLights>(targetJunction);
		if (targetHasCustomLights)
		{
			var targetLights = EntityManager.GetComponentData<CustomTrafficLights>(targetJunction);
			targetPattern = targetLights.GetPatternOnly();
		}

		
		if (sourcePattern == CustomTrafficLights.Patterns.CustomPhase)
		{
			if (targetPattern != CustomTrafficLights.Patterns.CustomPhase)
			{
				errorMessage = "Cannot sync phases: Source intersection uses Custom Phases but target intersection does not.\n\n" +
					"Both intersections must be set to Custom Phases to sync phase configurations.";
				return false;
			}

			
			bool sourceHasPhases = EntityManager.HasBuffer<CustomPhaseData>(sourceJunction) && 
				EntityManager.GetBuffer<CustomPhaseData>(sourceJunction).Length > 0;
			bool targetHasPhases = EntityManager.HasBuffer<CustomPhaseData>(targetJunction) && 
				EntityManager.GetBuffer<CustomPhaseData>(targetJunction).Length > 0;

			if (!sourceHasPhases)
			{
				errorMessage = "Cannot sync phases: Source intersection has no custom phase data configured.";
				return false;
			}
		}

		
		if (sourcePattern != CustomTrafficLights.Patterns.CustomPhase && 
			sourcePattern != CustomTrafficLights.Patterns.Vanilla)
		{
			if (targetPattern == CustomTrafficLights.Patterns.Vanilla)
			{
				string sourcePatternName = GetPatternDisplayName(sourcePattern);
				errorMessage = $"Cannot sync phases: Target intersection has no pattern configured.\n\n" +
					$"Source intersection: {sourcePatternName}\n" +
					$"Target intersection: Vanilla (no pattern)\n\n" +
					"Target intersection must have a predefined pattern to sync.";
				return false;
			}
		}

		return true;
	}

	private string GetPatternDisplayName(CustomTrafficLights.Patterns pattern)
	{
		return pattern switch
		{
			CustomTrafficLights.Patterns.Vanilla => "Vanilla",
			CustomTrafficLights.Patterns.SplitPhasing => "Split Phasing",
			CustomTrafficLights.Patterns.ProtectedCentreTurn => "Protected Turns",
			CustomTrafficLights.Patterns.SplitPhasingProtectedLeft => "Split Phasing Protected Left",
			CustomTrafficLights.Patterns.CustomPhase => "Custom Phases",
			CustomTrafficLights.Patterns.FixedTimed => "Fixed Timed",
			_ => pattern.ToString()
		};
	}

	
	public bool CopyPhasesToJunction(Entity sourceJunction, Entity targetJunction)
	{
		if (sourceJunction == Entity.Null || targetJunction == Entity.Null)
		{
			return false;
		}

		
		if (!ValidatePhaseSyncCompatibility(sourceJunction, targetJunction, out string errorMessage))
		{
			var messageDialog = new MessageDialog(
				"Phase Sync Not Allowed",
				errorMessage,
				LocalizedString.Id("Common.OK"));
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
			return false;
		}

		
		if (EntityManager.HasComponent<CustomTrafficLights>(sourceJunction))
		{
			var sourceLights = EntityManager.GetComponentData<CustomTrafficLights>(sourceJunction);
			
			if (!EntityManager.HasComponent<CustomTrafficLights>(targetJunction))
			{
				EntityManager.AddComponentData(targetJunction, new CustomTrafficLights(sourceLights.GetPattern()));
			}
			else
			{
				var targetLights = EntityManager.GetComponentData<CustomTrafficLights>(targetJunction);
				targetLights.SetPattern(sourceLights.GetPattern());
				targetLights.m_Timer = 0;
				EntityManager.SetComponentData(targetJunction, targetLights);
			}
		}

		
		if (EntityManager.HasBuffer<CustomPhaseData>(sourceJunction))
		{
			EntityManager.TryGetBuffer<CustomPhaseData>(sourceJunction, false, out var sourcePhases);
			if (sourcePhases.Length > 0)
			{
				
				if (!EntityManager.HasBuffer<CustomPhaseData>(targetJunction))
				{
					EntityManager.AddBuffer<CustomPhaseData>(targetJunction);
				}

				var targetPhases = EntityManager.GetBuffer<CustomPhaseData>(targetJunction);
				targetPhases.Clear();

				for (int i = 0; i < sourcePhases.Length; i++)
				{
					var sourcePhase = sourcePhases[i];
					
					var newPhase = new CustomPhaseData
					{
						m_MinimumDuration = sourcePhase.m_MinimumDuration,
						m_MaximumDuration = sourcePhase.m_MaximumDuration,
						m_ChangeMetric = sourcePhase.m_ChangeMetric,
						m_WaitFlowBalance = sourcePhase.m_WaitFlowBalance,
						m_LaneOccupiedMultiplier = sourcePhase.m_LaneOccupiedMultiplier,
						m_IntervalExponent = sourcePhase.m_IntervalExponent,
						m_Options = sourcePhase.m_Options & ~CustomPhaseData.Options.EndPhasePrematurely,
						m_TurnsSinceLastRun = 0,
						m_LowFlowTimer = 0,
						m_LowPriorityTimer = 0,
						m_WeightedWaiting = 0f
					};

					targetPhases.Add(newPhase);
				}
			}
		}

		CopyEdgeGroupMaskWithDirectionMatching(sourceJunction, targetJunction);
		CopySubLaneGroupMaskWithLaneTypeMatching(sourceJunction, targetJunction);
		EntityManager.AddComponentData(targetJunction, default(Updated));
		return true;
	}

	
	public void MatchPhaseDurationsToLeader(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		Entity leaderEntity = GetGroupLeader(groupEntity);
		if (leaderEntity == Entity.Null || !EntityManager.HasBuffer<CustomPhaseData>(leaderEntity))
		{
			m_Log.Warn($"Leader has no phases");
			return;
		}

		EntityManager.TryGetBuffer<CustomPhaseData>(leaderEntity, false, out var leaderPhases);
		if (leaderPhases.Length == 0)
		{
			return;
		}

		var members = GetGroupMembers(groupEntity);
		int membersUpdated = 0;

		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				continue;
			}

			if (!EntityManager.HasBuffer<CustomPhaseData>(memberEntity))
			{
				continue;
			}

			EntityManager.TryGetBuffer<CustomPhaseData>(memberEntity, false, out var memberPhases);
			
			int phaseCount = math.min(leaderPhases.Length, memberPhases.Length);
			for (int i = 0; i < phaseCount; i++)
			{
				var memberPhase = memberPhases[i];
				var leaderPhase = leaderPhases[i];
				
				memberPhase.m_MinimumDuration = leaderPhase.m_MinimumDuration;
				memberPhase.m_MaximumDuration = leaderPhase.m_MaximumDuration;
				
				memberPhases[i] = memberPhase;
			}

			membersUpdated++;
		}

		members.Dispose();
	}

	public void PropagatePatternToMembers(Entity groupEntity, CustomTrafficLights.Patterns pattern)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		Entity leaderEntity = GetGroupLeader(groupEntity);
		var members = GetGroupMembers(groupEntity);

		foreach (var memberEntity in members)
		{
			if (memberEntity == leaderEntity)
			{
				continue;
			}

			if (!EntityManager.HasComponent<CustomTrafficLights>(memberEntity))
			{
				EntityManager.AddComponentData(memberEntity, new CustomTrafficLights(pattern));
			}
			else
			{
				var memberLights = EntityManager.GetComponentData<CustomTrafficLights>(memberEntity);
				memberLights.SetPattern(pattern);
				memberLights.m_Timer = 0;
				EntityManager.SetComponentData(memberEntity, memberLights);
			}

			
			CopyEdgeGroupMaskWithDirectionMatching(leaderEntity, memberEntity);
			
			
			CopySubLaneGroupMaskWithLaneTypeMatching(leaderEntity, memberEntity);
		}

		int memberCount = members.Length;
		members.Dispose();
		m_Log.Info($"Propagated pattern {pattern} to {memberCount - 1} group members with full lane matching");
	}

	private void CopyEdgeGroupMaskWithDirectionMatching(Entity sourceJunction, Entity targetJunction)
	{
		if (!EntityManager.HasBuffer<EdgeGroupMask>(sourceJunction) ||
		    !EntityManager.HasBuffer<ConnectedEdge>(sourceJunction) ||
		    !EntityManager.HasBuffer<ConnectedEdge>(targetJunction))
		{
			return;
		}

		EntityManager.TryGetBuffer<EdgeGroupMask>(sourceJunction, false, out var sourceSignals);
		EntityManager.TryGetBuffer<ConnectedEdge>(sourceJunction, false, out var sourceConnectedEdges);
		EntityManager.TryGetBuffer<ConnectedEdge>(targetJunction, false, out var targetConnectedEdges);

		if (sourceSignals.Length == 0)
		{
			return;
		}

		if (!EntityManager.HasBuffer<EdgeGroupMask>(targetJunction))
		{
			EntityManager.AddBuffer<EdgeGroupMask>(targetJunction);
		}

		EntityManager.TryGetBuffer<EdgeGroupMask>(targetJunction, false, out var targetSignals);
		targetSignals.Clear();

		var edgeLookup = GetComponentLookup<Edge>(true);
		var edgeGeometryLookup = GetComponentLookup<EdgeGeometry>(true);
		var connectedEdgeLookup = GetBufferLookup<ConnectedEdge>(true);

		
		var sourceEdgeToSignal = new NativeHashMap<Entity, EdgeGroupMask>(sourceSignals.Length, Allocator.Temp);
		for (int i = 0; i < sourceSignals.Length; i++)
		{
			sourceEdgeToSignal[sourceSignals[i].m_Edge] = sourceSignals[i];
		}

		
		var matchedTargetEdges = new NativeHashSet<Entity>(targetConnectedEdges.Length, Allocator.Temp);

		
		for (int t = 0; t < targetConnectedEdges.Length; t++)
		{
			var targetEdge = targetConnectedEdges[t].m_Edge;
			
			
			Entity matchedSourceEdge = TryFindConnectedSourceEdge(
				targetJunction, targetEdge, sourceJunction, 
				edgeLookup, connectedEdgeLookup, 10); 
			
			if (matchedSourceEdge != Entity.Null && sourceEdgeToSignal.TryGetValue(matchedSourceEdge, out var sourceSignal))
			{
				var targetEdgePos = GetEdgePositionForJunction(targetJunction, targetEdge, edgeLookup, edgeGeometryLookup);
				var newSignal = new EdgeGroupMask(targetEdge, targetEdgePos, sourceSignal);
				targetSignals.Add(newSignal);
				matchedTargetEdges.Add(targetEdge);
			}
		}

		
		float3 sourceCenter = float3.zero;
		for (int i = 0; i < sourceConnectedEdges.Length; i++)
		{
			var edgePos = GetEdgePositionForJunction(sourceJunction, sourceConnectedEdges[i].m_Edge, edgeLookup, edgeGeometryLookup);
			sourceCenter += edgePos;
		}
		sourceCenter /= sourceConnectedEdges.Length;

		float3 targetCenter = float3.zero;
		for (int i = 0; i < targetConnectedEdges.Length; i++)
		{
			var edgePos = GetEdgePositionForJunction(targetJunction, targetConnectedEdges[i].m_Edge, edgeLookup, edgeGeometryLookup);
			targetCenter += edgePos;
		}
		targetCenter /= targetConnectedEdges.Length;

		
		var sourceEdgeData = new NativeList<(Entity edge, float angle, EdgeGroupMask signal)>(sourceConnectedEdges.Length, Allocator.Temp);
		for (int i = 0; i < sourceConnectedEdges.Length; i++)
		{
			var edgeEntity = sourceConnectedEdges[i].m_Edge;
			var edgePos = GetEdgePositionForJunction(sourceJunction, edgeEntity, edgeLookup, edgeGeometryLookup);
			float angle = math.atan2(edgePos.z - sourceCenter.z, edgePos.x - sourceCenter.x);
			if (sourceEdgeToSignal.TryGetValue(edgeEntity, out var signal))
			{
				sourceEdgeData.Add((edgeEntity, angle, signal));
			}
		}

		
		for (int t = 0; t < targetConnectedEdges.Length; t++)
		{
			var targetEdge = targetConnectedEdges[t].m_Edge;
			
			
			if (matchedTargetEdges.Contains(targetEdge))
			{
				continue;
			}

			var targetEdgePos = GetEdgePositionForJunction(targetJunction, targetEdge, edgeLookup, edgeGeometryLookup);
			float targetAngle = math.atan2(targetEdgePos.z - targetCenter.z, targetEdgePos.x - targetCenter.x);

			
			float bestAngleDiff = float.MaxValue;
			int bestSourceIndex = -1;

			for (int s = 0; s < sourceEdgeData.Length; s++)
			{
				float angleDiff = math.abs(AngleDifference(targetAngle, sourceEdgeData[s].angle));
				if (angleDiff < bestAngleDiff)
				{
					bestAngleDiff = angleDiff;
					bestSourceIndex = s;
				}
			}

			if (bestSourceIndex >= 0 && bestAngleDiff < math.PI / 4) 
			{
				var sourceSignal = sourceEdgeData[bestSourceIndex].signal;
				var newSignal = new EdgeGroupMask(targetEdge, targetEdgePos, sourceSignal);
				targetSignals.Add(newSignal);
			}
		}

		sourceEdgeData.Dispose();
		sourceEdgeToSignal.Dispose();
		matchedTargetEdges.Dispose();
	}

	private Entity TryFindConnectedSourceEdge(
		Entity startNode, Entity startEdge, Entity targetNode,
		ComponentLookup<Edge> edgeLookup, BufferLookup<ConnectedEdge> connectedEdgeLookup,
		int maxHops)
	{
		if (!edgeLookup.HasComponent(startEdge))
		{
			return Entity.Null;
		}

		
		var edge = edgeLookup[startEdge];
		Entity currentNode = edge.m_Start == startNode ? edge.m_End : edge.m_Start;
		Entity previousEdge = startEdge;

		
		float3 startDirection = GetEdgeDirection(startNode, startEdge, edgeLookup);

		for (int hop = 0; hop < maxHops; hop++)
		{
			
			if (currentNode == targetNode)
			{
				
				return previousEdge;
			}

			
			if (!connectedEdgeLookup.HasBuffer(currentNode))
			{
				break;
			}

			var connectedEdges = connectedEdgeLookup[currentNode];
			
			
			if (connectedEdges.Length == 2)
			{
				
				Entity nextEdge = Entity.Null;
				for (int i = 0; i < connectedEdges.Length; i++)
				{
					if (connectedEdges[i].m_Edge != previousEdge)
					{
						nextEdge = connectedEdges[i].m_Edge;
						break;
					}
				}

				if (nextEdge == Entity.Null || !edgeLookup.HasComponent(nextEdge))
				{
					break;
				}

				
				var nextEdgeData = edgeLookup[nextEdge];
				Entity nextNode = nextEdgeData.m_Start == currentNode ? nextEdgeData.m_End : nextEdgeData.m_Start;
				
				previousEdge = nextEdge;
				currentNode = nextNode;
			}
			else
			{
				
				Entity bestEdge = Entity.Null;
				float bestAlignment = -2f; 

				float3 incomingDir = GetEdgeDirection(currentNode, previousEdge, edgeLookup);
				
				incomingDir = -incomingDir;

				for (int i = 0; i < connectedEdges.Length; i++)
				{
					var candidateEdge = connectedEdges[i].m_Edge;
					if (candidateEdge == previousEdge)
					{
						continue;
					}

					float3 candidateDir = GetEdgeDirection(currentNode, candidateEdge, edgeLookup);
					float alignment = math.dot(math.normalize(incomingDir.xz), math.normalize(candidateDir.xz));

					
					if (edgeLookup.HasComponent(candidateEdge))
					{
						var candidateEdgeData = edgeLookup[candidateEdge];
						Entity otherEnd = candidateEdgeData.m_Start == currentNode ? candidateEdgeData.m_End : candidateEdgeData.m_Start;
						
						if (otherEnd == targetNode)
						{
							
							return candidateEdge;
						}
					}

					
					if (alignment > bestAlignment && alignment > 0.5f)
					{
						bestAlignment = alignment;
						bestEdge = candidateEdge;
					}
				}

				if (bestEdge == Entity.Null || !edgeLookup.HasComponent(bestEdge))
				{
					break;
				}

				var bestEdgeData = edgeLookup[bestEdge];
				Entity nextNode = bestEdgeData.m_Start == currentNode ? bestEdgeData.m_End : bestEdgeData.m_Start;
				
				previousEdge = bestEdge;
				currentNode = nextNode;
			}
		}

		return Entity.Null;
	}

	private float3 GetEdgeDirection(Entity node, Entity edgeEntity, ComponentLookup<Edge> edgeLookup)
	{
		if (!edgeLookup.HasComponent(edgeEntity))
		{
			return float3.zero;
		}

		var edge = edgeLookup[edgeEntity];
		var edgeGeometryLookup = GetComponentLookup<EdgeGeometry>(true);
		
		if (!edgeGeometryLookup.HasComponent(edgeEntity))
		{
			return float3.zero;
		}

		var geometry = edgeGeometryLookup[edgeEntity];
		
		
		if (edge.m_Start == node)
		{
			
			float3 start = (geometry.m_Start.m_Left.a + geometry.m_Start.m_Right.a) / 2;
			float3 end = (geometry.m_End.m_Left.d + geometry.m_End.m_Right.d) / 2;
			return math.normalize(end - start);
		}
		else
		{
			
			float3 start = (geometry.m_Start.m_Left.a + geometry.m_Start.m_Right.a) / 2;
			float3 end = (geometry.m_End.m_Left.d + geometry.m_End.m_Right.d) / 2;
			return math.normalize(start - end);
		}
	}

	private float AngleDifference(float a, float b)
	{
		float diff = a - b;
		while (diff > math.PI) diff -= 2 * math.PI;
		while (diff < -math.PI) diff += 2 * math.PI;
		return diff;
	}

	private void CopySubLaneGroupMaskWithLaneTypeMatching(Entity sourceJunction, Entity targetJunction)
	{
		if (!EntityManager.HasBuffer<SubLaneGroupMask>(sourceJunction) ||
		    !EntityManager.HasBuffer<ConnectedEdge>(sourceJunction) ||
		    !EntityManager.HasBuffer<ConnectedEdge>(targetJunction) ||
		    !EntityManager.HasBuffer<SubLane>(sourceJunction) ||
		    !EntityManager.HasBuffer<SubLane>(targetJunction))
		{
			return;
		}

		EntityManager.TryGetBuffer<SubLaneGroupMask>(sourceJunction, false, out var sourceSubLaneMasks);
		if (sourceSubLaneMasks.Length == 0)
		{
			return;
		}

		if (!EntityManager.HasBuffer<SubLaneGroupMask>(targetJunction))
		{
			EntityManager.AddBuffer<SubLaneGroupMask>(targetJunction);
		}

		EntityManager.TryGetBuffer<SubLaneGroupMask>(targetJunction, false, out var targetSubLaneMasks);
		EntityManager.TryGetBuffer<SubLane>(sourceJunction, false, out var sourceSubLanes);
		EntityManager.TryGetBuffer<SubLane>(targetJunction, false, out var targetSubLanes);

		var edgeLookup = GetComponentLookup<Edge>(true);
		var edgeGeometryLookup = GetComponentLookup<EdgeGeometry>(true);
		var connectedEdgeLookup = GetBufferLookup<ConnectedEdge>(true);
		var ownerLookup = GetComponentLookup<Owner>(true);
		var carLaneLookup = GetComponentLookup<CarLane>(true);
		var trackLaneLookup = GetComponentLookup<TrackLane>(true);
		var curveLookup = GetComponentLookup<Curve>(true);

		
		var sourceSubLaneMap = new NativeHashMap<Entity, NativeList<SubLaneMatchInfo>>(16, Allocator.Temp);
		
		foreach (var subLaneMask in sourceSubLaneMasks)
		{
			if (!ownerLookup.HasComponent(subLaneMask.m_SubLane))
			{
				continue;
			}

			var owner = ownerLookup[subLaneMask.m_SubLane];
			Entity edgeEntity = owner.m_Owner;
			
			LaneTurnType turnType = GetLaneTurnType(subLaneMask.m_SubLane, carLaneLookup, trackLaneLookup);
			float3 position = GetSubLanePosition(subLaneMask.m_SubLane, curveLookup);

			if (!sourceSubLaneMap.TryGetValue(edgeEntity, out var list))
			{
				list = new NativeList<SubLaneMatchInfo>(8, Allocator.Temp);
				sourceSubLaneMap[edgeEntity] = list;
			}
			list.Add(new SubLaneMatchInfo
			{
				subLane = subLaneMask.m_SubLane,
				turnType = turnType,
				position = position,
				mask = subLaneMask
			});
			sourceSubLaneMap[edgeEntity] = list;
		}

		
		var targetToSourceEdgeMap = BuildEdgeMapping(sourceJunction, targetJunction, edgeLookup, edgeGeometryLookup, connectedEdgeLookup);

		
		for (int t = 0; t < targetSubLaneMasks.Length; t++)
		{
			var targetMask = targetSubLaneMasks[t];
			
			if (!ownerLookup.HasComponent(targetMask.m_SubLane))
			{
				continue;
			}

			var targetOwner = ownerLookup[targetMask.m_SubLane];
			Entity targetEdge = targetOwner.m_Owner;
			
			
			if (!targetToSourceEdgeMap.TryGetValue(targetEdge, out Entity sourceEdge))
			{
				continue;
			}

			
			if (!sourceSubLaneMap.TryGetValue(sourceEdge, out var sourceSubLanes2))
			{
				continue;
			}

			LaneTurnType targetTurnType = GetLaneTurnType(targetMask.m_SubLane, carLaneLookup, trackLaneLookup);
			float3 targetPosition = GetSubLanePosition(targetMask.m_SubLane, curveLookup);

			
			int bestMatchIndex = -1;
			float bestMatchScore = float.MaxValue;

			for (int s = 0; s < sourceSubLanes2.Length; s++)
			{
				var sourceInfo = sourceSubLanes2[s];
				
				
				float turnTypeScore = (sourceInfo.turnType == targetTurnType) ? 0f : 100f;
				
				
				float positionScore = math.distance(sourceInfo.position, targetPosition);
				
				float totalScore = turnTypeScore + positionScore;
				
				if (totalScore < bestMatchScore)
				{
					bestMatchScore = totalScore;
					bestMatchIndex = s;
				}
			}

			if (bestMatchIndex >= 0)
			{
				var sourceInfo = sourceSubLanes2[bestMatchIndex];
				var newMask = new SubLaneGroupMask(targetMask.m_SubLane, targetPosition, sourceInfo.mask);
				targetSubLaneMasks[t] = newMask;
			}
		}

		
		foreach (var kvp in sourceSubLaneMap)
		{
			kvp.Value.Dispose();
		}
		sourceSubLaneMap.Dispose();
		targetToSourceEdgeMap.Dispose();
	}

	private struct SubLaneMatchInfo
	{
		public Entity subLane;
		public LaneTurnType turnType;
		public float3 position;
		public SubLaneGroupMask mask;
	}

	private enum LaneTurnType
	{
		Straight,
		Left,
		Right,
		UTurn,
		Other
	}

	private LaneTurnType GetLaneTurnType(Entity subLane, ComponentLookup<CarLane> carLaneLookup, ComponentLookup<TrackLane> trackLaneLookup)
	{
		if (carLaneLookup.TryGetComponent(subLane, out var carLane))
		{
			if ((carLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
			{
				return LaneTurnType.UTurn;
			}
			if ((carLane.m_Flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
			{
				return LaneTurnType.Left;
			}
			if ((carLane.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) != 0)
			{
				return LaneTurnType.Right;
			}
			return LaneTurnType.Straight;
		}

		if (trackLaneLookup.TryGetComponent(subLane, out var trackLane))
		{
			if ((trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
			{
				return LaneTurnType.Left;
			}
			if ((trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
			{
				return LaneTurnType.Right;
			}
			return LaneTurnType.Straight;
		}

		return LaneTurnType.Other;
	}

	private float3 GetSubLanePosition(Entity subLane, ComponentLookup<Curve> curveLookup)
	{
		if (curveLookup.TryGetComponent(subLane, out var curve))
		{
			return curve.m_Bezier.d;
		}
		return float3.zero;
	}

	private NativeHashMap<Entity, Entity> BuildEdgeMapping(
		Entity sourceJunction, Entity targetJunction,
		ComponentLookup<Edge> edgeLookup, ComponentLookup<EdgeGeometry> edgeGeometryLookup,
		BufferLookup<ConnectedEdge> connectedEdgeLookup)
	{
		var result = new NativeHashMap<Entity, Entity>(8, Allocator.Temp);

		if (!connectedEdgeLookup.HasBuffer(sourceJunction) || !connectedEdgeLookup.HasBuffer(targetJunction))
		{
			return result;
		}

		var sourceEdges = connectedEdgeLookup[sourceJunction];
		var targetEdges = connectedEdgeLookup[targetJunction];

		
		float3 sourceCenter = float3.zero;
		for (int i = 0; i < sourceEdges.Length; i++)
		{
			sourceCenter += GetEdgePositionForJunction(sourceJunction, sourceEdges[i].m_Edge, edgeLookup, edgeGeometryLookup);
		}
		sourceCenter /= sourceEdges.Length;

		float3 targetCenter = float3.zero;
		for (int i = 0; i < targetEdges.Length; i++)
		{
			targetCenter += GetEdgePositionForJunction(targetJunction, targetEdges[i].m_Edge, edgeLookup, edgeGeometryLookup);
		}
		targetCenter /= targetEdges.Length;

		
		var sourceEdgeAngles = new NativeList<(Entity edge, float angle)>(sourceEdges.Length, Allocator.Temp);
		for (int i = 0; i < sourceEdges.Length; i++)
		{
			var pos = GetEdgePositionForJunction(sourceJunction, sourceEdges[i].m_Edge, edgeLookup, edgeGeometryLookup);
			float angle = math.atan2(pos.z - sourceCenter.z, pos.x - sourceCenter.x);
			sourceEdgeAngles.Add((sourceEdges[i].m_Edge, angle));
		}

		
		for (int t = 0; t < targetEdges.Length; t++)
		{
			var targetEdge = targetEdges[t].m_Edge;
			var targetPos = GetEdgePositionForJunction(targetJunction, targetEdge, edgeLookup, edgeGeometryLookup);
			float targetAngle = math.atan2(targetPos.z - targetCenter.z, targetPos.x - targetCenter.x);

			
			Entity matchedSource = TryFindConnectedSourceEdge(targetJunction, targetEdge, sourceJunction, edgeLookup, connectedEdgeLookup, 10);
			
			if (matchedSource == Entity.Null)
			{
				
				float bestAngleDiff = float.MaxValue;
				for (int s = 0; s < sourceEdgeAngles.Length; s++)
				{
					float angleDiff = math.abs(AngleDifference(targetAngle, sourceEdgeAngles[s].angle));
					if (angleDiff < bestAngleDiff && angleDiff < math.PI / 4)
					{
						bestAngleDiff = angleDiff;
						matchedSource = sourceEdgeAngles[s].edge;
					}
				}
			}

			if (matchedSource != Entity.Null)
			{
				result[targetEdge] = matchedSource;
			}
		}

		sourceEdgeAngles.Dispose();
		return result;
	}

	#endregion

	

	

	#region Flow/Wait Look-ahead

	
	public int CalculateBestNextPhase(Entity junctionEntity, int currentPhase)
	{
		if (junctionEntity == Entity.Null || !EntityManager.HasBuffer<CustomPhaseData>(junctionEntity))
		{
			return (currentPhase + 1) % 1; 
		}

		EntityManager.TryGetBuffer<CustomPhaseData>(junctionEntity, false, out var phases);
		if (phases.Length == 0)
		{
			return 0;
		}

		int nextPhase = (currentPhase + 1) % phases.Length;
		float bestMetric = float.MinValue;
		int bestPhase = nextPhase;

		int checkedPhases = 0;
		int checkPhase = nextPhase;

		while (checkedPhases < phases.Length)
		{
			var phase = phases[checkPhase];
			
			float flow = phase.AverageCarFlow();
			float wait = phase.m_WeightedWaiting * phase.m_WaitFlowBalance;
			float metric = CalculatePhaseMetric(phase.m_ChangeMetric, flow, wait);

			if (metric > bestMetric)
			{
				bestMetric = metric;
				bestPhase = checkPhase;
			}

			if (phase.m_MinimumDuration > 0)
			{
				break;
			}

			checkPhase = (checkPhase + 1) % phases.Length;
			checkedPhases++;

			if (checkPhase == currentPhase)
			{
				break;
			}
		}

		return bestPhase;
	}

	
	private float CalculatePhaseMetric(CustomPhaseData.StepChangeMetric metric, float flow, float wait)
	{
		switch (metric)
		{
			case CustomPhaseData.StepChangeMetric.FirstFlow:
				return flow > 0 ? flow : float.MinValue;
			case CustomPhaseData.StepChangeMetric.FirstWait:
				return wait > 0 ? wait : float.MinValue;
			case CustomPhaseData.StepChangeMetric.NoFlow:
				return flow <= 0 ? 1f : float.MinValue;
			case CustomPhaseData.StepChangeMetric.NoWait:
				return wait <= 0 ? 1f : float.MinValue;
			case CustomPhaseData.StepChangeMetric.Default:
			default:
				return flow - wait; 
		}
	}

	
	public void ApplyBestPhaseToGroup(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		Entity leaderEntity = GetGroupLeader(groupEntity);
		if (leaderEntity == Entity.Null || !EntityManager.HasComponent<TrafficLights>(leaderEntity))
		{
			return;
		}

		var leaderLights = EntityManager.GetComponentData<TrafficLights>(leaderEntity);
		int currentPhase = leaderLights.m_CurrentSignalGroup - 1;

		int bestPhase = CalculateBestNextPhase(leaderEntity, currentPhase);

		if (bestPhase != currentPhase)
		{
			
			var members = GetGroupMembers(groupEntity);

			foreach (var memberEntity in members)
			{
				if (!EntityManager.HasComponent<TrafficLights>(memberEntity))
				{
					continue;
				}

				var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
				var trafficLights = EntityManager.GetComponentData<TrafficLights>(memberEntity);

				
				int adjustedPhase = bestPhase + memberData.m_PhaseOffset;
				int phaseCount = GetPhaseCount(memberEntity);
				if (phaseCount > 0)
				{
					adjustedPhase = adjustedPhase % phaseCount;
				}

				trafficLights.m_NextSignalGroup = (byte)(adjustedPhase + 1);
				EntityManager.SetComponentData(memberEntity, trafficLights);
			}

			members.Dispose();
		}
	}

	#endregion


	
	public void OnJunctionGeometryUpdate(Entity junctionEntity)
	{
		if (junctionEntity == Entity.Null)
		{
			return;
		}

		
		if (!EntityManager.HasComponent<TrafficGroupMember>(junctionEntity))
		{
			return;
		}

		var member = EntityManager.GetComponentData<TrafficGroupMember>(junctionEntity);
		Entity groupEntity = member.m_GroupEntity;

		if (groupEntity == Entity.Null)
		{
			return;
		}


		ValidateJunctionPhases(junctionEntity);

		if (member.m_IsGroupLeader)
		{
			RecalculateGroupCycleLength(groupEntity);
			
			var group = EntityManager.GetComponentData<TrafficGroup>(groupEntity);
			if (group.m_GreenWaveEnabled)
			{
				if (EntityManager.HasBuffer<CustomPhaseData>(junctionEntity) && 
				    EntityManager.TryGetBuffer<CustomPhaseData>(junctionEntity, false, out var phases) && phases.Length > 0)
				{
					CalculateEnhancedGreenWaveTiming(groupEntity);
				}
				else
				{
					CalculateGreenWaveTiming(groupEntity);
				}
			}
		}

		UpdateMemberDistanceToLeader(junctionEntity);
	}

	
	private void ValidateJunctionPhases(Entity junctionEntity)
	{
		if (!EntityManager.HasBuffer<CustomPhaseData>(junctionEntity))
		{
			return;
		}

		EntityManager.TryGetBuffer<CustomPhaseData>(junctionEntity, false, out var phases);
		
		for (int i = 0; i < phases.Length; i++)
		{
			var phase = phases[i];
			phase.m_TurnsSinceLastRun = 0;
			phase.m_LowFlowTimer = 0;
			phase.m_LowPriorityTimer = 0;
			phase.m_WeightedWaiting = 0f;
			phase.m_Options &= ~CustomPhaseData.Options.EndPhasePrematurely;
			phases[i] = phase;
		}

		if (EntityManager.HasBuffer<EdgeGroupMask>(junctionEntity))
		{
			EntityManager.TryGetBuffer<EdgeGroupMask>(junctionEntity, false, out var edgeMasks);
			
			if (edgeMasks.Length != phases.Length && phases.Length > 0)
			{
				
				while (edgeMasks.Length > phases.Length)
				{
					edgeMasks.RemoveAt(edgeMasks.Length - 1);
				}
				while (edgeMasks.Length < phases.Length)
				{
					edgeMasks.Add(new EdgeGroupMask());
				}
			}
		}
	}

	
	private void UpdateMemberDistanceToLeader(Entity memberEntity)
	{
		if (!EntityManager.HasComponent<TrafficGroupMember>(memberEntity))
		{
			return;
		}

		var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
		
		if (memberData.m_IsGroupLeader)
		{
			memberData.m_DistanceToLeader = 0f;
			EntityManager.SetComponentData(memberEntity, memberData);
			return;
		}

		Entity leaderEntity = memberData.m_LeaderEntity;
		if (leaderEntity == Entity.Null)
		{
			return;
		}

		if (!EntityManager.HasComponent<Node>(memberEntity) || !EntityManager.HasComponent<Node>(leaderEntity))
		{
			return;
		}

		var memberNode = EntityManager.GetComponentData<Node>(memberEntity);
		var leaderNode = EntityManager.GetComponentData<Node>(leaderEntity);

		float distance = math.distance(memberNode.m_Position, leaderNode.m_Position);
		memberData.m_DistanceToLeader = distance;
		EntityManager.SetComponentData(memberEntity, memberData);
	}

	
	public void HousekeepingAllGroups()
	{
		var groups = m_GroupQuery.ToEntityArray(Allocator.Temp);

		foreach (var groupEntity in groups)
		{
			HousekeepingGroup(groupEntity);
		}

		groups.Dispose();
	}

	
	public void HousekeepingGroup(Entity groupEntity)
	{
		if (groupEntity == Entity.Null || !EntityManager.HasComponent<TrafficGroup>(groupEntity))
		{
			return;
		}

		var members = GetGroupMembers(groupEntity);
		var invalidMembers = new NativeList<Entity>(Allocator.Temp);

		
		foreach (var memberEntity in members)
		{
			if (!EntityManager.Exists(memberEntity) || !EntityManager.HasComponent<TrafficLights>(memberEntity))
			{
				invalidMembers.Add(memberEntity);
			}
		}

		
		foreach (var invalidMember in invalidMembers)
		{
			if (EntityManager.HasComponent<TrafficGroupMember>(invalidMember))
			{
				EntityManager.RemoveComponent<TrafficGroupMember>(invalidMember);
			}
		}

		invalidMembers.Dispose();
		members.Dispose();

		
		int memberCount = GetGroupMemberCount(groupEntity);
		if (memberCount == 0)
		{
			
			EntityManager.DestroyEntity(groupEntity);
			return;
		}

		
		Entity leader = GetGroupLeader(groupEntity);
		if (leader == Entity.Null)
		{
			AssignNewLeader(groupEntity);
		}

		
		ReindexGroupMembers(groupEntity);
	}

	

	#region Edge Position Helpers

	
	private struct AngleComparer : IComparer<(Entity edge, float angle, int originalIndex)>
	{
		public int Compare((Entity edge, float angle, int originalIndex) x, (Entity edge, float angle, int originalIndex) y)
		{
			return x.angle.CompareTo(y.angle);
		}
	}

	
	private float3 GetEdgePositionForJunction(Entity nodeEntity, Entity edgeEntity, ComponentLookup<Edge> edgeLookup, ComponentLookup<EdgeGeometry> edgeGeometryLookup)
	{
		float3 position = float3.zero;
		
		if (!edgeLookup.TryGetComponent(edgeEntity, out Edge edge))
		{
			return position;
		}
		
		if (!edgeGeometryLookup.TryGetComponent(edgeEntity, out EdgeGeometry edgeGeometry))
		{
			return position;
		}
		
		if (edge.m_Start.Equals(nodeEntity))
		{
			position = (edgeGeometry.m_Start.m_Left.a + edgeGeometry.m_Start.m_Right.a) / 2;
		}
		else if (edge.m_End.Equals(nodeEntity))
		{
			position = (edgeGeometry.m_End.m_Left.d + edgeGeometry.m_End.m_Right.d) / 2;
		}
		
		return position;
	}

	#endregion
}
