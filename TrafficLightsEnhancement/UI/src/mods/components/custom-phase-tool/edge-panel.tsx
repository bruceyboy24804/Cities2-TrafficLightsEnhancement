import { useCallback, useContext, useEffect, useRef } from 'react';
import styled from 'styled-components';
import { callUpdateEdgeGroupMaskForJunction } from 'bindings';
import { CityConfigurationContext } from '../../context';
import { EdgeGroupMaskOptions } from '../../constants';
import Lane from './lane';
import LinkVariantOff from '../common/icons/link-variant-off';
import { EdgeInfo, CustomPhaseLaneType, CustomPhaseLane, ScreenPoint, CustomPhaseLaneDirection, CustomPhaseSignalState, EdgeGroupMask } from 'mods/general';
import styles from "./customPhaseTool.module.scss"

function GetCustomPhaseLane(edge: EdgeInfo, index: number, type: CustomPhaseLaneType): CustomPhaseLane {
  const result:  CustomPhaseLane = {
    type: type,
    left: "stop",
    straight: "stop",
    right: "stop",
    uTurn: "stop",
    all: "stop"
  }

  const getDelay = (signal: { m_OpenDelay?: number, m_CloseDelay?: number } | undefined) => {
    if (!signal) return undefined;
    const openDelay = signal.m_OpenDelay ?? 0;
    const closeDelay = signal.m_CloseDelay ?? 0;
    if (openDelay === 0 && closeDelay === 0) return undefined;
    return { openDelay, closeDelay };
  };

  if (type == "carLane") {
    result.left = (edge.m_EdgeGroupMask.m_Car.m_Left.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.left;
    result.straight = (edge.m_EdgeGroupMask.m_Car.m_Straight.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.straight;
    result.right = (edge.m_EdgeGroupMask.m_Car.m_Right.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.right;
    result.uTurn = (edge.m_EdgeGroupMask.m_Car.m_UTurn.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.uTurn;
    result.left = (edge.m_EdgeGroupMask.m_Car.m_Left.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.left;
    result.straight = (edge.m_EdgeGroupMask.m_Car.m_Straight.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.straight;
    result.right = (edge.m_EdgeGroupMask.m_Car.m_Right.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.right;
    result.uTurn = (edge.m_EdgeGroupMask.m_Car.m_UTurn.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.uTurn;
    result.left = edge.m_CarLaneLeftCount <= 0 ? "none" : result.left;
    result.straight = edge.m_CarLaneStraightCount <= 0 ? "none" : result.straight;
    result.right = edge.m_CarLaneRightCount <= 0 ? "none" : result.right;
    result.uTurn = edge.m_CarLaneUTurnCount <= 0 ? "none" : result.uTurn;
    result.leftDelay = getDelay(edge.m_EdgeGroupMask.m_Car.m_Left);
    result.straightDelay = getDelay(edge.m_EdgeGroupMask.m_Car.m_Straight);
    result.rightDelay = getDelay(edge.m_EdgeGroupMask.m_Car.m_Right);
    result.uTurnDelay = getDelay(edge.m_EdgeGroupMask.m_Car.m_UTurn);
  }
  if (type == "publicCarLane") {
    result.left = (edge.m_EdgeGroupMask.m_PublicCar.m_Left.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.left;
    result.straight = (edge.m_EdgeGroupMask.m_PublicCar.m_Straight.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.straight;
    result.right = (edge.m_EdgeGroupMask.m_PublicCar.m_Right.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.right;
    result.uTurn = (edge.m_EdgeGroupMask.m_PublicCar.m_UTurn.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.uTurn;
    result.left = (edge.m_EdgeGroupMask.m_PublicCar.m_Left.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.left;
    result.straight = (edge.m_EdgeGroupMask.m_PublicCar.m_Straight.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.straight;
    result.right = (edge.m_EdgeGroupMask.m_PublicCar.m_Right.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.right;
    result.uTurn = (edge.m_EdgeGroupMask.m_PublicCar.m_UTurn.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.uTurn;
    result.left = edge.m_PublicCarLaneLeftCount <= 0 ? "none" : result.left;
    result.straight = edge.m_PublicCarLaneStraightCount <= 0 ? "none" : result.straight;
    result.right = edge.m_PublicCarLaneRightCount <= 0 ? "none" : result.right;
    result.uTurn = edge.m_PublicCarLaneUTurnCount <= 0 ? "none" : result.uTurn;
    result.leftDelay = getDelay(edge.m_EdgeGroupMask.m_PublicCar.m_Left);
    result.straightDelay = getDelay(edge.m_EdgeGroupMask.m_PublicCar.m_Straight);
    result.rightDelay = getDelay(edge.m_EdgeGroupMask.m_PublicCar.m_Right);
    result.uTurnDelay = getDelay(edge.m_EdgeGroupMask.m_PublicCar.m_UTurn);
  }
  if (type == "trackLane") {
    result.left = (edge.m_EdgeGroupMask.m_Track.m_Left.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.left;
    result.straight = (edge.m_EdgeGroupMask.m_Track.m_Straight.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.straight;
    result.right = (edge.m_EdgeGroupMask.m_Track.m_Right.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.right;
    result.left = (edge.m_EdgeGroupMask.m_Track.m_Left.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.left;
    result.straight = (edge.m_EdgeGroupMask.m_Track.m_Straight.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.straight;
    result.right = (edge.m_EdgeGroupMask.m_Track.m_Right.m_YieldGroupMask & (1 << index)) != 0 ? "yield" : result.right;
    result.left = edge.m_TrackLaneLeftCount <= 0 ? "none" : result.left;
    result.straight = edge.m_TrackLaneStraightCount <= 0 ? "none" : result.straight;
    result.right = edge.m_TrackLaneRightCount <= 0 ? "none" : result.right;
    result.uTurn = "none";
    result.leftDelay = getDelay(edge.m_EdgeGroupMask.m_Track.m_Left);
    result.straightDelay = getDelay(edge.m_EdgeGroupMask.m_Track.m_Straight);
    result.rightDelay = getDelay(edge.m_EdgeGroupMask.m_Track.m_Right);
  }
  if (type == "bicycleLane") {
    if (edge.m_EdgeGroupMask.m_Bicycle) {
      result.all = (edge.m_EdgeGroupMask.m_Bicycle.m_GoGroupMask & (1 << index)) != 0 ? "go" : "stop";
      result.allDelay = getDelay(edge.m_EdgeGroupMask.m_Bicycle);
    }
    result.left = "none";
    result.straight = "none";
    result.right = "none";
    result.uTurn = "none";
  }
  if (type == "pedestrianLaneStopLine" || type == "pedestrianLaneNonStopLine") {
    result.all = (edge.m_EdgeGroupMask.m_Pedestrian.m_GoGroupMask & (1 << index)) != 0 ? "go" : result.all;
    result.allDelay = getDelay(edge.m_EdgeGroupMask.m_Pedestrian);
  }
  return result;
}

function SetBit(input: number, index: number, value: number) {
    return ((input & (~(1 << index))) | (value << index));
}

export default function EdgePanel(props: {data: EdgeInfo, index: number, position: ScreenPoint}) {
  const clickHandler = useCallback((index: number, type: CustomPhaseLaneType, direction: CustomPhaseLaneDirection, currentSignal: CustomPhaseSignalState) => {

    let newSignal = currentSignal == "stop" ? "go" : (currentSignal == "go" ? "yield" : "stop");
    const newGroupMask: EdgeGroupMask = JSON.parse(JSON.stringify(props.data.m_EdgeGroupMask));
    if (type == "carLane") {
      if (direction == "left") {
        newGroupMask.m_Car.m_Left.m_GoGroupMask = SetBit(newGroupMask.m_Car.m_Left.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask.m_Car.m_Left.m_YieldGroupMask = SetBit(newGroupMask.m_Car.m_Left.m_YieldGroupMask, index, newSignal == "yield" ? 1 :  0);
      }
      if (direction == "straight") {
        newGroupMask.m_Car.m_Straight. m_GoGroupMask = SetBit(newGroupMask. m_Car.m_Straight.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask. m_Car.m_Straight.m_YieldGroupMask = SetBit(newGroupMask.m_Car.m_Straight.m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
      if (direction == "right") {
        newGroupMask. m_Car.m_Right. m_GoGroupMask = SetBit(newGroupMask. m_Car.m_Right. m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask.m_Car.m_Right.m_YieldGroupMask = SetBit(newGroupMask. m_Car.m_Right. m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
      if (direction == "uTurn") {
        newGroupMask.m_Car.m_UTurn. m_GoGroupMask = SetBit(newGroupMask. m_Car.m_UTurn.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask. m_Car.m_UTurn.m_YieldGroupMask = SetBit(newGroupMask.m_Car.m_UTurn.m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
    }
    if (type == "publicCarLane") {
      if (direction == "left") {
        newGroupMask. m_PublicCar.m_Left.m_GoGroupMask = SetBit(newGroupMask.m_PublicCar.m_Left.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask.m_PublicCar.m_Left. m_YieldGroupMask = SetBit(newGroupMask.m_PublicCar. m_Left.m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
      if (direction == "straight") {
        newGroupMask.m_PublicCar. m_Straight.m_GoGroupMask = SetBit(newGroupMask.m_PublicCar.m_Straight.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask. m_PublicCar.m_Straight.m_YieldGroupMask = SetBit(newGroupMask.m_PublicCar.m_Straight.m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
      if (direction == "right") {
        newGroupMask.m_PublicCar.m_Right.m_GoGroupMask = SetBit(newGroupMask.m_PublicCar. m_Right.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask.m_PublicCar.m_Right.m_YieldGroupMask = SetBit(newGroupMask.m_PublicCar.m_Right.m_YieldGroupMask, index, newSignal == "yield" ? 1 :  0);
      }
      if (direction == "uTurn") {
        newGroupMask.m_PublicCar. m_UTurn.m_GoGroupMask = SetBit(newGroupMask.m_PublicCar.m_UTurn.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask. m_PublicCar.m_UTurn.m_YieldGroupMask = SetBit(newGroupMask.m_PublicCar.m_UTurn.m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
    }
    if (type == "trackLane") {
      newSignal = currentSignal == "stop" ? "go" : "stop";
      if (direction == "left") {
        newGroupMask.m_Track.m_Left.m_GoGroupMask = SetBit(newGroupMask.m_Track.m_Left.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask.m_Track.m_Left.m_YieldGroupMask = SetBit(newGroupMask.m_Track.m_Left.m_YieldGroupMask, index, newSignal == "yield" ?  1 : 0);
      }
      if (direction == "straight") {
        newGroupMask.m_Track.m_Straight.m_GoGroupMask = SetBit(newGroupMask.m_Track. m_Straight.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask.m_Track. m_Straight.m_YieldGroupMask = SetBit(newGroupMask.m_Track.m_Straight.m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
      if (direction == "right") {
        newGroupMask.m_Track. m_Right.m_GoGroupMask = SetBit(newGroupMask.m_Track. m_Right.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
        newGroupMask.m_Track.m_Right.m_YieldGroupMask = SetBit(newGroupMask.m_Track. m_Right.m_YieldGroupMask, index, newSignal == "yield" ? 1 : 0);
      }
    }
    if (type == "bicycleLane") {
      newSignal = currentSignal == "stop" ? "go" : "stop";
      newGroupMask.m_Bicycle. m_GoGroupMask = SetBit(newGroupMask. m_Bicycle.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
    }
    if (type == "pedestrianLaneStopLine" || type == "pedestrianLaneNonStopLine") {
      newSignal = currentSignal == "stop" ? "go" : "stop";
      newGroupMask.m_Pedestrian.m_GoGroupMask = SetBit(newGroupMask.m_Pedestrian.m_GoGroupMask, index, newSignal != "stop" ? 1 : 0);
    }
    if (props.data.m_Node) {
      callUpdateEdgeGroupMaskForJunction(JSON.stringify({
        junctionIndex: props.data.m_Node.index,
        junctionVersion: props.data.m_Node.version,
        edgeGroupMasks: [newGroupMask]
      }));
    }
  }, [props.data.m_EdgeGroupMask, props.data.m_Node]);

  const unlinkHandler = useCallback(() => {
    const newGroupMask:  EdgeGroupMask = JSON.parse(JSON.stringify(props.data.m_EdgeGroupMask));
    newGroupMask.m_Options |= EdgeGroupMaskOptions. PerLaneSignal;
    if (props.data.m_Node) {
      callUpdateEdgeGroupMaskForJunction(JSON.stringify({
        junctionIndex: props.data.m_Node.index,
        junctionVersion: props.data.m_Node.version,
        edgeGroupMasks: [newGroupMask]
      }));
    }
  }, [props.data.m_EdgeGroupMask, props.data.m_Node]);

  const cityConfiguration = useContext(CityConfigurationContext);
  const carLaneCount = props.data.m_CarLaneLeftCount + props.data.m_CarLaneStraightCount + props.data.m_CarLaneRightCount + props.data.m_CarLaneUTurnCount;
  const publicCarLaneCount = props.data.m_PublicCarLaneLeftCount + props.data.m_PublicCarLaneStraightCount + props.data.m_PublicCarLaneRightCount + props.data.m_PublicCarLaneUTurnCount;
  const trackLaneCount = props.data.m_TrackLaneLeftCount + props.data.m_TrackLaneStraightCount + props.data.m_TrackLaneRightCount;
  
  const bicycleLaneCount = props.data.m_BicycleLaneCount ?? 0;
  
  const containerRef = useRef(null);

  useEffect(() => {
    if (containerRef != null && containerRef.current != null && props.position) {
      // @ts-expect-error:  cohtml specific extension
      containerRef.current.style.leftPX = props.position.left;
      // @ts-expect-error: cohtml specific extension
      containerRef.current.style.topPX = props.position.top;
    }
  }, [containerRef, props.position]);

  if (carLaneCount + publicCarLaneCount + trackLaneCount + bicycleLaneCount + props.data.m_PedestrianLaneStopLineCount + props.data.m_PedestrianLaneNonStopLineCount <= 0) {
    return <></>;
  }

  return (
    <div className={styles.edgePanelContainer} ref={containerRef}>
      <div className={styles.edgePanelContent}>
        <div className={styles.edgePanelLaneContainer}>
          {carLaneCount > 0 && <>
            <div className={styles.edgePanelColumn}>
              <Lane
                data={GetCustomPhaseLane(props.data, props.index, "carLane")}
                index={props.index}
                showIcon={true}
                onClick={clickHandler}
              />
            </div>
            {publicCarLaneCount + trackLaneCount + bicycleLaneCount + props.data.m_PedestrianLaneStopLineCount + props.data.m_PedestrianLaneNonStopLineCount > 0 && <div className={styles.edgePanelDivider}></div>}
          </>}
          {publicCarLaneCount > 0 && <>
            <div className={styles.edgePanelColumn}>
              <Lane
                data={GetCustomPhaseLane(props.data, props.index, "publicCarLane")}
                index={props.index}
                showIcon={true}
                onClick={clickHandler}
              />
            </div>
            {trackLaneCount + bicycleLaneCount + props.data.m_PedestrianLaneStopLineCount + props.data. m_PedestrianLaneNonStopLineCount > 0 && <div className={styles.edgePanelDivider}></div>}
          </>}
          {trackLaneCount > 0 && <>
            <div className={styles.edgePanelColumn}>
              <Lane
                data={GetCustomPhaseLane(props.data, props.index, "trackLane")}
                index={props.index}
                showIcon={true}
                onClick={clickHandler}
              />
            </div>
            {bicycleLaneCount + props.data.m_PedestrianLaneStopLineCount + props.data.m_PedestrianLaneNonStopLineCount > 0 && <div className={styles.edgePanelDivider}></div>}
          </>}
          {bicycleLaneCount > 0 && <>
            <div className={styles.edgePanelColumn}>
              <Lane
                data={GetCustomPhaseLane(props.data, props.index, "bicycleLane")}
                index={props.index}
                showIcon={true}
                onClick={clickHandler}
              />
            </div>
            {props.data.m_PedestrianLaneStopLineCount + props.data.m_PedestrianLaneNonStopLineCount > 0 && <div className={styles.edgePanelDivider}/>}
          </>}
          {(props.data.m_PedestrianLaneStopLineCount > 0 || props.data.m_PedestrianLaneNonStopLineCount > 0) && <>
            <div className={styles.edgePanelColumn}>
              <Lane
                data={GetCustomPhaseLane(props.data, props.index, "pedestrianLaneNonStopLine")}
                index={props.index}
                showIcon={true}
                onClick={clickHandler}
              />
            </div>
          </>}
        </div>
        <div className={styles.edgePanelHorizontalDivider}></div>
        <div className={styles.edgePanelIconContainer}><LinkVariantOff className={styles.edgePanelIconStyle} onClick={unlinkHandler} /></div>
      </div>
    </div>
  );
}