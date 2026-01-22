import { CSSProperties, useMemo, useRef, useCallback } from "react";
import { useValue } from "cs2/api";
import { callSetActiveCustomPhaseIndex, callSwapCustomPhase, edgeInfo } from "bindings";
import {
  DndContext,
  closestCenter,
  MouseSensor,
  useSensor,
  useSensors,
  DragEndEvent,
  DragMoveEvent,
} from "@dnd-kit/core";
import { restrictToVerticalAxis, restrictToParentElement } from "@dnd-kit/modifiers";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";

import { MainPanelState } from "../../../constants";
import Button from "../../common/button";
import Scrollable, { ScrollableRef } from "../../common/scrollable";
import Divider from "../../main-panel/items/divider";
import Row from "../../main-panel/items/row";
import Item from "./item";
import ManualControlPanel from "./manual-control-panel";
import SubPanel from "./sub-panel";
import { MainPanelItemButton, MainPanelItem, MainPanelItemCustomPhase, MainPanelItemCustomPhaseHeader } from "mods/general";
import styles from "./modules/index.module.scss"

const ItemContainerStyle: CSSProperties = {
  display: "flex",
  flexDirection: "column",
  flex: 1,
};

const AddButton = () => {
  const data: MainPanelItemButton = {
    itemType: "button",
    type: "button",
    key: "add",
    value: "add",
    label: "Add",
    engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallAddCustomPhase"
  };
  return (
    <Row data={data}><Button {...data} /></Row>
  );
};

const BackButton = () => {
  const data: MainPanelItemButton = {
    itemType: "button",
    type: "button",
    key: "state",
    value: `${MainPanelState.Main}`,
    label: "Back",
    engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetMainPanelState"
  };
  return (
    <Row data={data}><Button {...data} /></Row>
  );
};

const BackToGroupsButton = () => {
  const data: MainPanelItemButton = {
    itemType: "button",
    type: "button",
    key: "state",
    value: `${MainPanelState.TrafficGroups}`,
    label: "Back to Group",
    engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetMainPanelState"
  };
  return (
    <Row data={data}><Button {...data} /></Row>
  );
};

const ManualControlButton = (props: {currentSignalGroup: number}) => {
  const clickHandler = () => {
    const manualSignalGroup = props.currentSignalGroup > 0 ? props.currentSignalGroup : 1;
    callSetActiveCustomPhaseIndex(JSON.stringify({key: "ManualSignalGroup", value: manualSignalGroup}));
  };
  return (
    <Row hoverEffect={true}>
      <Button label="Manual Control" onClick={clickHandler} />
    </Row>
  );
};

export default function MainPanel(props: {items: MainPanelItem[], selectedEntity: {index: number, version: number}}) {
  const edgeInfoList = useValue(edgeInfo.binding);
  const phaseItems = useMemo(
    () => props.items.filter(item => item.itemType == "customPhase") as MainPanelItemCustomPhase[],
    [props.items]
  );

  const scrollableRef = useRef<ScrollableRef>(null);
  const scrollSpeed = 8;
  const edgeThreshold = 50;

  const handleDragMove = useCallback((event: DragMoveEvent) => {
    if (!scrollableRef.current) return;
    const rect = scrollableRef.current.getContainerRect();
    if (!rect) return;

    const { activatorEvent } = event;
    if (!activatorEvent || !(activatorEvent instanceof MouseEvent)) return;

    const mouseY = (activatorEvent as MouseEvent).clientY + (event.delta?.y || 0);
    const distanceFromTop = mouseY - rect.top;
    const distanceFromBottom = rect.bottom - mouseY;

    if (distanceFromTop < edgeThreshold) {
      const speed = Math.max(1, scrollSpeed * (1 - distanceFromTop / edgeThreshold));
      scrollableRef.current.scrollBy(-speed);
    } else if (distanceFromBottom < edgeThreshold) {
      const speed = Math.max(1, scrollSpeed * (1 - distanceFromBottom / edgeThreshold));
      scrollableRef.current.scrollBy(speed);
    }
  }, []);

  const sensors = useSensors(
    useSensor(MouseSensor, {
      activationConstraint: {
        distance: 5,
      },
    })
  );

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      const oldIndex = phaseItems.findIndex(item => `phase-${item.index}` === active.id);
      const newIndex = phaseItems.findIndex(item => `phase-${item.index}` === over.id);
      if (oldIndex !== -1 && newIndex !== -1) {
        callSwapCustomPhase(JSON.stringify({ index1: oldIndex, index2: newIndex }));
      }
    }
  };

  
  const filteredEdges = edgeInfoList.filter(edge => 
    edge.m_Node && 
    edge.m_Node.index === props.selectedEntity.index && 
    edge.m_Node.version === props.selectedEntity.version
  );

  let activeIndex = -1;
  let activeViewingIndex = -1;
  let activeItem: MainPanelItemCustomPhase | null = null;
  let currentItem: MainPanelItemCustomPhase | null = null;
  let headerItem: MainPanelItemCustomPhaseHeader | null = null;
  let currentSignalGroup = 0;
  let manualSignalGroup = 0;
  let length = 0;

  
  if (props.items.length > 0 && props.items[0].itemType == "customPhaseHeader") {
    headerItem = props.items[0];
    length = headerItem.phaseCount;
  }

  
  if (props.items.length > 1 && props.items[1].itemType == "customPhase") {
    activeIndex = props.items[1].activeIndex;
    activeViewingIndex = props.items[1].activeViewingIndex;
    currentSignalGroup = props.items[1].currentSignalGroup;
    manualSignalGroup = props.items[1].manualSignalGroup;
  }

  
  if (activeIndex >= 0) {
    const newActiveItem = props.items[activeIndex + 1]; 
    if (newActiveItem && newActiveItem.itemType == "customPhase") {
      activeItem = newActiveItem;
    }
  }
  if (manualSignalGroup > 0) {
    const newCurrentItem = props.items[manualSignalGroup]; 
    if (newCurrentItem && newCurrentItem.itemType == "customPhase") {
      currentItem = newCurrentItem;
    }
  } else if (activeViewingIndex >= 0) {
    const newCurrentItem = props.items[activeViewingIndex + 1]; 
    if (newCurrentItem && newCurrentItem.itemType == "customPhase") {
      currentItem = newCurrentItem;
    }
  } else if (currentSignalGroup > 0 && currentSignalGroup < props.items.length) {
    const newCurrentItem = props.items[currentSignalGroup]; 
    if (newCurrentItem && newCurrentItem.itemType == "customPhase") {
      currentItem = newCurrentItem;
    }
  }

  return (
    <div 
      className={styles.container}
      onPointerDownCapture={(event: React.PointerEvent) => {
        console.log("[MainPanel] container pointer down", {
          targetTag: (event.target as HTMLElement)?.tagName,
          className: (event.target as HTMLElement)?.className
        });
      }}
    >
      <div className={styles.leftPanelContainer}>

        {manualSignalGroup <= 0 && <>
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}
            onDragMove={handleDragMove}
            modifiers={[restrictToVerticalAxis, restrictToParentElement]}
          >
            <Scrollable ref={scrollableRef} style={{flex: 1}} contentStyle={ItemContainerStyle}>
              <SortableContext
                items={phaseItems.map(item => `phase-${item.index}`)}
                strategy={verticalListSortingStrategy}
              >
                {phaseItems.map(item => (
                  <Item data={item} key={`phase-${item.index}`} />
                ))}
              </SortableContext>
            </Scrollable>
          </DndContext>

          {length > 0 && <Divider />}
          {length < 16 && <AddButton />}
          {length > 0 && <ManualControlButton currentSignalGroup={currentSignalGroup} />}
          <BackToGroupsButton />
          <BackButton />
        </>}
        {manualSignalGroup > 0 && <ManualControlPanel items={props.items} />}
      </div>
      <div className={styles.rightPanelContainer}>
        <Scrollable style={{flex: 1}} contentStyle={{flex: 1}} trackStyle={{marginLeft: "0.25em"}}>
          {activeItem && <SubPanel data={activeItem} edges={filteredEdges} phaseIndex={activeItem.index} />}
          {!activeItem && currentSignalGroup > 0 && currentSignalGroup < props.items.length && 
            props.items[currentSignalGroup].itemType === "customPhase" &&
            <SubPanel data={props.items[currentSignalGroup] as MainPanelItemCustomPhase} edges={filteredEdges} phaseIndex={currentSignalGroup - 1} statisticsOnly={true} />}
        </Scrollable>
      </div>
    </div>
  );
}