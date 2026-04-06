import { useLocalization } from "cs2/l10n";

import TooltipContainer from "../../common/tooltip-container";

export default function LinkPhase(props: {link: boolean}) {
  const { translate } = useLocalization();
  return (
    <TooltipContainer>
      {props.link && (translate("Tooltip.LABEL[C2VM.TrafficLightsEnhancement.LinkPhase]") ?? "Link Phase")}
      {!props.link && (translate("Tooltip.LABEL[C2VM.TrafficLightsEnhancement.UnlinkPhase]") ?? "Unlink Phase")}
    </TooltipContainer>
  );
}