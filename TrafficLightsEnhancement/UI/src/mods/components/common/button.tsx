import { MouseEventHandler } from 'react';
import styled from 'styled-components';

import { useLocalization } from 'cs2/l10n';
import { Tooltip } from 'cs2/ui';

const ButtonComponent = styled.div<{disabled?: boolean}>`
  padding: 3rem;
  border-radius: 3rem;
  color: var(--accentColorLighter);
  background-color: var(--toolbarFieldColor);
  flex: 1;
  text-align: center;
  ${props => props.disabled ? "filter: brightness(1.0) contrast(0.6);" : ""}
  &:hover {
    ${props => props.disabled ? "" : "filter: brightness(1.2) contrast(1.2);"}
  }
    
`;

export default function Button(props: {label: string, disabled?: boolean, onClick?: MouseEventHandler<HTMLDivElement>, tooltip?: string}) {
  const { translate } = useLocalization();
  return (
    <Tooltip tooltip={props.tooltip} direction="right">
      <ButtonComponent {...props}>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.label}]`) ?? props.label}</ButtonComponent>
    </Tooltip>
  );
}