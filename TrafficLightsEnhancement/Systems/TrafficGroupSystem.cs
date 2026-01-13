using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Logging;
using Game;
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

		// Find the leader
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
			
			// Immediately sync all members to the leader's current phase
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

			// Calculate expected phase based on leader + offset
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

		// For non-leaders, check compatibility
		float cycleDifference = math.abs(group.m_CycleLength - junctionCycleLength);
		/*if (cycleDifference > 2f) // Allow 2 tick tolerance
		{
			var messageDialog = new MessageDialog($"Junction {junctionEntity} has cycle length {junctionCycleLength} but group expects {group.m_CycleLength}. ");
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
		}*/
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
				/*if (cycleDifference > 2f)
				{
					var messageDialog = new MessageDialog($"TrafficGroupSystem: Member {memberEntity} cycle length ({memberCycleLength}) differs from leader ({leaderCycleLength})");
					GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
				}*/
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
				// Leader has 0 offset
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

		// Get source pattern
		CustomTrafficLights.Patterns sourcePattern = CustomTrafficLights.Patterns.Vanilla;
		bool sourceHasCustomLights = EntityManager.HasComponent<CustomTrafficLights>(sourceJunction);
		if (sourceHasCustomLights)
		{
			var sourceLights = EntityManager.GetComponentData<CustomTrafficLights>(sourceJunction);
			sourcePattern = sourceLights.GetPatternOnly();
		}

		// Get target pattern
		CustomTrafficLights.Patterns targetPattern = CustomTrafficLights.Patterns.Vanilla;
		bool targetHasCustomLights = EntityManager.HasComponent<CustomTrafficLights>(targetJunction);
		if (targetHasCustomLights)
		{
			var targetLights = EntityManager.GetComponentData<CustomTrafficLights>(targetJunction);
			targetPattern = targetLights.GetPatternOnly();
		}

		// For CustomPhase pattern - both must have CustomPhase
		if (sourcePattern == CustomTrafficLights.Patterns.CustomPhase)
		{
			if (targetPattern != CustomTrafficLights.Patterns.CustomPhase)
			{
				errorMessage = "Cannot sync phases: Source intersection uses Custom Phases but target intersection does not.\n\n" +
					"Both intersections must be set to Custom Phases to sync phase configurations.";
				return false;
			}

			// Also verify both have phase data
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

		// For predefined patterns - target must also have a predefined pattern (not Vanilla)
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

		// Validate pattern compatibility before copying
		if (!ValidatePhaseSyncCompatibility(sourceJunction, targetJunction, out string errorMessage))
		{
			var messageDialog = new MessageDialog(
				"Phase Sync Not Allowed",
				errorMessage,
				LocalizedString.Id("Common.OK"));
			GameManager.instance.userInterface.appBindings.ShowMessageDialog(messageDialog, null);
			return false;
		}

		// Always copy the pattern first (works for both predefined and custom patterns)
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

		// Copy CustomPhaseData if source has it (for custom/fixed-timed patterns)
		if (EntityManager.HasBuffer<CustomPhaseData>(sourceJunction))
		{
			EntityManager.TryGetBuffer<CustomPhaseData>(sourceJunction, false, out var sourcePhases);
			if (sourcePhases.Length > 0)
			{
				// Ensure target has a phase buffer
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

		
		if (EntityManager.HasBuffer<EdgeGroupMask>(sourceJunction) && 
		    EntityManager.HasBuffer<ConnectedEdge>(sourceJunction) &&
		    EntityManager.HasBuffer<ConnectedEdge>(targetJunction))
		{
			EntityManager.TryGetBuffer<EdgeGroupMask>(sourceJunction, false, out var sourceSignals);
			EntityManager.TryGetBuffer<ConnectedEdge>(sourceJunction, false, out var sourceConnectedEdges);
			EntityManager.TryGetBuffer<ConnectedEdge>(targetJunction, false, out var targetConnectedEdges);
			
			// Check if edge counts match - required for 1:1 position mapping
			if (sourceConnectedEdges.Length != targetConnectedEdges.Length)
			{
				m_Log.Warn($"TrafficGroupSystem: Edge count mismatch - source has {sourceConnectedEdges.Length}, target has {targetConnectedEdges.Length}. Skipping signal copy.");
			}
			else
			{
				if (!EntityManager.HasBuffer<EdgeGroupMask>(targetJunction))
				{
					EntityManager.AddBuffer<EdgeGroupMask>(targetJunction);
				}
				
				EntityManager.TryGetBuffer<EdgeGroupMask>(targetJunction, false, out var targetSignals);
				targetSignals.Clear();
				
				var edgeLookup = GetComponentLookup<Edge>(true);
				var edgeGeometryLookup = GetComponentLookup<EdgeGeometry>(true);
				
				var sourceEdgePositions = new NativeList<(Entity edge, float angle, int originalIndex)>(sourceConnectedEdges.Length, Allocator.Temp);
				var targetEdgePositions = new NativeList<(Entity edge, float angle, int originalIndex)>(targetConnectedEdges.Length, Allocator.Temp);
				
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
				
				for (int i = 0; i < sourceConnectedEdges.Length; i++)
				{
					var edgePos = GetEdgePositionForJunction(sourceJunction, sourceConnectedEdges[i].m_Edge, edgeLookup, edgeGeometryLookup);
					float angle = math.atan2(edgePos.z - sourceCenter.z, edgePos.x - sourceCenter.x);
					sourceEdgePositions.Add((sourceConnectedEdges[i].m_Edge, angle, i));
				}
				
				for (int i = 0; i < targetConnectedEdges.Length; i++)
				{
					var edgePos = GetEdgePositionForJunction(targetJunction, targetConnectedEdges[i].m_Edge, edgeLookup, edgeGeometryLookup);
					float angle = math.atan2(edgePos.z - targetCenter.z, edgePos.x - targetCenter.x);
					targetEdgePositions.Add((targetConnectedEdges[i].m_Edge, angle, i));
				}
				
				sourceEdgePositions.Sort(new AngleComparer());
				targetEdgePositions.Sort(new AngleComparer());
				
				var sourceEdgeToSignal = new NativeHashMap<Entity, EdgeGroupMask>(sourceSignals.Length, Allocator.Temp);
				for (int i = 0; i < sourceSignals.Length; i++)
				{
					sourceEdgeToSignal[sourceSignals[i].m_Edge] = sourceSignals[i];
				}
				
				int copiedCount = 0;
				
				for (int i = 0; i < sourceEdgePositions.Length && i < targetEdgePositions.Length; i++)
				{
					var sourceEdge = sourceEdgePositions[i].edge;
					var targetEdge = targetEdgePositions[i].edge;
					float sourceAngle = sourceEdgePositions[i].angle;
					float targetAngle = targetEdgePositions[i].angle;
					
					
					if (sourceEdgeToSignal.TryGetValue(sourceEdge, out var sourceSignal))
					{
						var targetEdgePos = GetEdgePositionForJunction(targetJunction, targetEdge, edgeLookup, edgeGeometryLookup);
						var newSignal = new EdgeGroupMask(targetEdge, targetEdgePos, sourceSignal);
						targetSignals.Add(newSignal);
						copiedCount++;
					}
					else
					{
						m_Log.Warn($"TrafficGroupSystem: No signal found for source edge {sourceEdge}");
					}
				}
				sourceEdgePositions.Dispose();
				targetEdgePositions.Dispose();
				sourceEdgeToSignal.Dispose();
			}
		}

		if (EntityManager.HasBuffer<SubLaneGroupMask>(sourceJunction))
		{
			EntityManager.TryGetBuffer<SubLaneGroupMask>(sourceJunction, false, out var sourceSubLaneSignals);
			
			if (!EntityManager.HasBuffer<SubLaneGroupMask>(targetJunction))
			{
				EntityManager.AddBuffer<SubLaneGroupMask>(targetJunction);
			}
			
			EntityManager.TryGetBuffer<SubLaneGroupMask>(targetJunction, false, out var targetSubLaneSignals);
			
			
			if (sourceSubLaneSignals.Length == targetSubLaneSignals.Length)
			{
				for (int i = 0; i < sourceSubLaneSignals.Length; i++)
				{
					var sourceSignal = sourceSubLaneSignals[i];
					var targetSignal = targetSubLaneSignals[i];
					
					var newSignal = new SubLaneGroupMask(targetSignal.m_SubLane, (float3)targetSignal.m_Position, sourceSignal);
					targetSubLaneSignals[i] = newSignal;
				}
			}
			else
			{
				m_Log.Warn($"TrafficGroupSystem: SubLane count mismatch - source has {sourceSubLaneSignals.Length}, target has {targetSubLaneSignals.Length}. Skipping sublane signal copy.");
			}
		}

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
		}

		int memberCount = members.Length;
		members.Dispose();
		m_Log.Info($"Propagated pattern {pattern} to {memberCount - 1} group members");
	}

	#endregion

	

	

	#region Flow/Wait Look-ahead

	
	public int CalculateBestNextPhase(Entity junctionEntity, int currentPhase)
	{
		if (junctionEntity == Entity.Null || !EntityManager.HasBuffer<CustomPhaseData>(junctionEntity))
		{
			return (currentPhase + 1) % 1; // Default to next phase
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
			// Apply to all members with their offsets
			var members = GetGroupMembers(groupEntity);

			foreach (var memberEntity in members)
			{
				if (!EntityManager.HasComponent<TrafficLights>(memberEntity))
				{
					continue;
				}

				var memberData = EntityManager.GetComponentData<TrafficGroupMember>(memberEntity);
				var trafficLights = EntityManager.GetComponentData<TrafficLights>(memberEntity);

				// Calculate adjusted phase with offset
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

		// Check if junction is in a group
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

		// Check for invalid members
		foreach (var memberEntity in members)
		{
			if (!EntityManager.Exists(memberEntity) || !EntityManager.HasComponent<TrafficLights>(memberEntity))
			{
				invalidMembers.Add(memberEntity);
			}
		}

		// Remove invalid members
		foreach (var invalidMember in invalidMembers)
		{
			if (EntityManager.HasComponent<TrafficGroupMember>(invalidMember))
			{
				EntityManager.RemoveComponent<TrafficGroupMember>(invalidMember);
			}
		}

		invalidMembers.Dispose();
		members.Dispose();

		// Check if group still has members
		int memberCount = GetGroupMemberCount(groupEntity);
		if (memberCount == 0)
		{
			// Delete empty group
			EntityManager.DestroyEntity(groupEntity);
			return;
		}

		// Ensure there's a leader
		Entity leader = GetGroupLeader(groupEntity);
		if (leader == Entity.Null)
		{
			AssignNewLeader(groupEntity);
		}

		// Reindex members
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
