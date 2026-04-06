import styled from 'styled-components';

import { useLocalization } from 'cs2/l10n';
import { MainPanelItemTitle } from 'mods/general';

const Container = styled.div`
  display: flex;
  justify-content: space-between;
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
  align-items: center;
`;

const TitleText = styled.div`
  color: var(--textColorDim);
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
`;

const SecondaryText = styled.div`
  color: var(--textColorDim);
  margin-left: 6rem;
`;

export default function TitleDim(props: MainPanelItemTitle) {
  const { translate } = useLocalization();
  return (
    <Container>
      <TitleText>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.title}]`) ?? props.title}</TitleText>
      {props.secondaryText && <SecondaryText>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.secondaryText}]`) ?? props.secondaryText}</SecondaryText>}
    </Container>
  );
}