import styled from 'styled-components';

import { useLocalization } from 'cs2/l10n';
import { MainPanelItemMessage } from 'mods/general';

const Container = styled.div`
  margin: 20rem auto;
  flex: 1;
  text-align: center;
`;

export default function Message(props: MainPanelItemMessage) {
  const { translate } = useLocalization();
  return (
    <Container>
      {translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.message}]`) ?? props.message}
    </Container>
  );
}