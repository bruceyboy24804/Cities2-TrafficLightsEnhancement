import { useEffect } from "react";

import { useValue } from "cs2/api";
import { activeEditingCustomPhaseIndex as activeEditingBinding, edgeInfo, screenPoint, callAddWorldPosition, callRemoveWorldPosition } from "bindings";

import { EdgeGroupMaskOptions } from "../../constants";

import EdgePanel from "./edge-panel";
import SubLanePanel from "./sublane-panel";
import { EdgeInfo, ScreenPointMap } from "mods/general";

export default function CustomPhaseTool() {
  const activeEditingCustomPhaseIndex = useValue(activeEditingBinding.binding);
  const edgeInfoList = useValue(edgeInfo.binding);

  useEffect(() => {
    const edgePositionArray = JSON.stringify(edgeInfoList.filter(edge => (edge.m_EdgeGroupMask.m_Options & EdgeGroupMaskOptions.PerLaneSignal) == 0).map(item => item.m_Position));
    const subLanePositionArray = JSON.stringify(edgeInfoList.filter(edge => (edge.m_EdgeGroupMask.m_Options & EdgeGroupMaskOptions.PerLaneSignal) != 0).map(item => item.m_SubLaneInfoList.map(subLane => subLane.m_Position)).flat());
    callAddWorldPosition(edgePositionArray);
    callAddWorldPosition(subLanePositionArray);
    return () => {
      callRemoveWorldPosition(edgePositionArray);
      callRemoveWorldPosition(subLanePositionArray);
    };
  }, [edgeInfoList]);

  const screenPointMap = useValue<ScreenPointMap>(screenPoint.binding);

  return (
    <>
      {activeEditingCustomPhaseIndex >= 0 && edgeInfoList.filter(edge => (edge.m_EdgeGroupMask.m_Options & EdgeGroupMaskOptions.PerLaneSignal) == 0).map(edge => <EdgePanel data={edge} index={activeEditingCustomPhaseIndex} position={screenPointMap[edge.m_Position.key]} />)}
      {activeEditingCustomPhaseIndex >= 0 && edgeInfoList.filter(edge => (edge.m_EdgeGroupMask.m_Options & EdgeGroupMaskOptions.PerLaneSignal) != 0).map(edge => edge.m_SubLaneInfoList.map(subLane => <SubLanePanel edge={edge} subLane={subLane} index={activeEditingCustomPhaseIndex} position={screenPointMap[subLane.m_Position.key]} />).flat())}
    </>
  );
}