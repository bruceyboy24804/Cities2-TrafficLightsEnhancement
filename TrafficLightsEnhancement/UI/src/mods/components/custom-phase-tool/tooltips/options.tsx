import { useLocalization } from "cs2/l10n";

import TooltipContainer from "../../common/tooltip-container";

export default function Options() {
  const { translate } = useLocalization();
  return (
    <TooltipContainer>
      {translate("UI.LABEL[C2VM.TrafficLightsEnhancement.test]") ?? "test"}
    </TooltipContainer>
  );
}