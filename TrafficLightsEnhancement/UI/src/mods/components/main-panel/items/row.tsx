import styled from 'styled-components';
import { CSSProperties } from 'react';
import { trigger } from 'cs2/api';
import { engineCall } from '../../../engine';
import { MainPanelItem } from 'mods/general';

const Container = styled.div<{hoverEffect?: boolean}>`
  padding: 0.25em 0.5em;
  width: 100%;
  display: flex;
  align-items: center;

  &:hover {
    filter: ${props => props.hoverEffect ? "brightness(1.2) contrast(1.2)" : "none"};
  }
  flex-direction: row;
`;

type RowProps = {
  data?: MainPanelItem;
  children: React.ReactNode;
  hoverEffect?: boolean;
  style?: CSSProperties;
  disableEngineCall?: boolean;
  className?: string;
};

export default function Row(props: RowProps) {
  const clickHandler = () => {
    if (props.disableEngineCall) {
      return;
    }
    if (props.data && "engineEventName" in props.data && props.data.engineEventName) {
      const eventName = props.data.engineEventName;
      
      const triggerMatch = eventName.match(/^(.+)\.TRIGGER:(.+)$/);
      if (triggerMatch) {
        const [, modId, triggerName] = triggerMatch;
        trigger(modId, `TRIGGER:${triggerName}`, JSON.stringify(props.data));
      } else {
        engineCall(eventName, JSON.stringify(props.data));
      }
    }
  };

  const handleClick = props.disableEngineCall ? undefined : clickHandler;
  const handlePointerDownCapture = props.disableEngineCall
    ? (event: React.PointerEvent) => {
        console.log("[Row] pointer down capture", {
          hasEngineEvent: !!(props.data && "engineEventName" in props.data),
          targetTag: (event.target as HTMLElement)?.tagName
        });
      }
    : undefined;

  return (
    <Container
      onClick={handleClick}
      onPointerDownCapture={handlePointerDownCapture}
      style={props.style}
      hoverEffect={props.hoverEffect}
      className={props.className}
    >
      {props.children}
    </Container>
  );
}