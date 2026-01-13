import { useCallback, useEffect, useMemo, useState } from 'react'
import styles from "./mainPanel.module.scss"
import { useValue } from 'cs2/api';
import { MainPanelState } from '../../constants';
import Header from './header';
import Content from './content';
import CustomPhaseMainPanel from '../../components/custom-phase-tool/main-panel';
import TrafficGroupsMainPanel from '../traffic-groups/main-panel/IndexComponent';
import{Button, Tooltip} from "cs2/ui"
import { mainPanel, callMainPanelSave, callMainPanelUpdatePosition, callSetMainPanelState, addMemberState } from '../../../bindings';
const traffLightSrc = "Media/Game/Icons/TrafficLights.svg"
const defaultPanel = {
  title: "",
  image: "",
  position: {top: -999999, left: -999999},
  showPanel: false,
  showFloatingButton: false,
  state: 0,
  selectedEntity: { index: 0, version: 0 },
  items: []
};
interface MainPanelType {
  title: string;
  image: string;
  position: { top: number; left: number };
  showPanel: boolean;
  showFloatingButton: boolean;
  state: number;
  selectedEntity: { index: number; version: number };
  items: any[];
}

const useMainPanel = () => {
  const [panel, setPanel] = useState<MainPanelType>(defaultPanel);

  const result = useValue(mainPanel.binding);

  useEffect(() => {
    const newPanel = JSON.parse(result);
    setPanel({
      title: newPanel.title ?? defaultPanel.title,
      image: newPanel.image ?? defaultPanel.image,
      position: newPanel.position ?? defaultPanel.position,
      showPanel: newPanel.showPanel ?? defaultPanel.showPanel,
      showFloatingButton: newPanel.showFloatingButton ?? defaultPanel.showFloatingButton,
      state: newPanel.state ?? defaultPanel.state,
      selectedEntity: newPanel.selectedEntity ?? defaultPanel.selectedEntity,
      items: newPanel.items ?? defaultPanel.items
    });
  }, [result]);

  return panel;
};



export default function MainPanel() {
  const [showFloatingButton, setShowFloatingButton] = useState(false);
  const [showPanel, setShowPanel] = useState(false);

  const [top, setTop] = useState(-999999);
  const [left, setLeft] = useState(-999999);
  const [dragging, setDragging] = useState(false);
  const [recalc, setRecalc] = useState({});

  const [container, setContainer] = useState<Element | null>(null);
  const [toolSideColumn, setToolSideColumn] = useState<Element | null>(null);

  const panel = useMainPanel();
  const addMemberStateRaw = useValue(addMemberState.binding);
  const addMemberData = useMemo(() => {
    try {
      return JSON.parse(addMemberStateRaw);
    } catch {
      return { isAddingMember: false, members: [] };
    }
  }, [addMemberStateRaw]);

  const containerRef = useCallback((el: Element | null) => setContainer(el), []);

  useEffect(() => {
    setShowPanel(panel.showPanel);
    setShowFloatingButton(panel.showFloatingButton);
    if (!dragging) {
      setTop(panel.position.top);
      setLeft(panel.position.left);
    }
  }, [panel.showPanel, panel.showFloatingButton, panel.position.top, panel.position.left, dragging]);

  // Save everything when the panel is closed
  useEffect(() => {
    return () => {
      callMainPanelSave("{}");
    };
  }, []);

  useEffect(() => {
    setToolSideColumn(document.querySelector(".tool-side-column_l9i"));
    if (container && showPanel) {
      const resizeObserver = new ResizeObserver(() => setRecalc({}));
      resizeObserver.observe(container);
      resizeObserver.observe(document.body);
      return () => resizeObserver.disconnect();
    }
  }, [container, showPanel]);

  const floatingButtonClickHandler = useCallback(() => {
    if (panel.showPanel) {
      callSetMainPanelState(JSON.stringify({value: `${MainPanelState.Hidden}`}));
    } else {
      callSetMainPanelState(JSON.stringify({value: `${MainPanelState.Empty}`}));
    }
  }, [panel.showPanel]);

  const mouseDownHandler = useCallback((_event: React.MouseEvent<HTMLElement>) => {
    if (container) {
      const rect = container.getBoundingClientRect();
      setTop(rect.top);
      setLeft(rect.left);
      setDragging(true);
    }
  }, [container]);
  const mouseUpHandler = useCallback((_event: MouseEvent) => {
    if (container) {
      const rect = container.getBoundingClientRect();
      callMainPanelUpdatePosition(JSON.stringify({top: Math.floor(rect.top), left: Math.floor(rect.left)}));
    }
    setDragging(false);
  }, [container]);
  const mouseMoveHandler = useCallback((event: MouseEvent) => {
    setTop((prev) => prev + event.movementY);
    setLeft((prev) => prev + event.movementX);
  }, []);

  useEffect(() => {
    if (dragging) {
      document.body.addEventListener("mouseup", mouseUpHandler);
      document.body.addEventListener("mousemove", mouseMoveHandler);
      return () => {
        document.body.removeEventListener("mouseup", mouseUpHandler);
        document.body.removeEventListener("mousemove", mouseMoveHandler);
      };
    }
  }, [dragging, mouseUpHandler, mouseMoveHandler]);

  const style: React.CSSProperties = useMemo(() => {
    const result: React.CSSProperties = {
      display: showPanel ? "block" : "none"
    };
    if (container && toolSideColumn) {
      const containerRect = container.getBoundingClientRect();
      const toolSideColumnRect = toolSideColumn.getBoundingClientRect();
      result.maxHeight = Math.max(200, toolSideColumnRect.top - containerRect.top);
      if (top > -999999 && left > -999999) {
        result.top = Math.min(top, toolSideColumnRect.top - 200);
        result.left = Math.min(left, document.body.clientWidth - containerRect.width);
        result.top = Math.max(result.top, 0);
        result.left = Math.max(result.left, 0);
      }
    }
    return result;
  }, [showPanel, top, left, container, toolSideColumn, recalc, panel]); // Recalc values on recalc or panel change

  return (
    <>
    <Tooltip tooltip={"Traffic Lights Enhancement"} direction={"down"}>
      <Button
      variant='floating'
      src={traffLightSrc}
      onSelect={floatingButtonClickHandler}
      selected={showPanel}
      tooltipLabel
    />
    </Tooltip>
    
    
      
      <div className={styles.indexContainer}
        ref={containerRef}
        style={style}
      >
        <Header title={panel.title} image={panel.image} onMouseDown={mouseDownHandler} />
        {panel.state != MainPanelState.CustomPhase && panel.state != MainPanelState.TrafficGroups && <Content items={panel.items} addMemberData={addMemberData} />}
        {panel.state == MainPanelState.CustomPhase && <CustomPhaseMainPanel items={panel.items} selectedEntity={panel.selectedEntity} />}
        {panel.state == MainPanelState.TrafficGroups && <TrafficGroupsMainPanel items={panel.items} />}
      </div>
    </>
  );
}