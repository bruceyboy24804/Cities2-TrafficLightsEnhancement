import { CSSProperties, useContext } from "react";
import styled from "styled-components";

import { callSetActiveCustomPhaseIndex } from "bindings";

import { LocaleContext } from "../../../context";
import { getString } from "../../../localisations";

import Button from "../../common/button";
import Radio from "../../common/radio";
import Scrollable from "../../common/scrollable";
import Divider from "../../main-panel/items/divider";
import { MainPanelItemCustomPhase, MainPanelItem } from "mods/general";

const Label = styled.div<{dim?: boolean}>`
  color: ${props => props.dim ? "var(--textColorDim)" : "var(--textColor)"};
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
  display: inline;
`;

const Row = styled.div<{hoverEffect?: boolean}>`
  padding: 0.25em 0.5em;
  width: 100%;
  display: flex;
  align-items: center;
  &:hover {
    filter: ${props => props.hoverEffect ? "brightness(1.2) contrast(1.2)" : "none"};
  }
`;

const ItemContainerStyle: CSSProperties = {
  display: "flex",
  flexDirection: "column",
  flex: 1,
};

const BackButton = () => {
  const clickHandler = () => {
    callSetActiveCustomPhaseIndex(JSON.stringify({key: "ManualSignalGroup", value: 0}));
  };
  return (
    <Row hoverEffect={true} onClick={clickHandler}>
      <Button label="Back" />
    </Row>
  );
};

function Item(props: {data: MainPanelItemCustomPhase}) {
  const locale = useContext(LocaleContext);
  const clickHandler = () => {
    callSetActiveCustomPhaseIndex(JSON.stringify({key: "ManualSignalGroup", value: props.data.index + 1}));
  };
  return (
    <Row onClick={clickHandler}>
      <Radio isChecked={props.data.manualSignalGroup == props.data.index + 1} />
      <Label dim={true}>
        {getString(locale, "Phase") + " #" + (props.data.index + 1)}
      </Label>
    </Row>
  );
}

export default function ManualControlPanel(props: {items: MainPanelItem[]}) {
  const locale = useContext(LocaleContext);
  return (
    <>
      <Scrollable style={{flex: 1}} contentStyle={ItemContainerStyle}>
        <Row>
          <Label dim={false}>{getString(locale, "ManualControl")}</Label>
        </Row>
        {props.items.map(item => item.itemType == "customPhase" && <Item data={item} />)}
      </Scrollable>
      <Divider />
      <BackButton />
    </>
  );
}