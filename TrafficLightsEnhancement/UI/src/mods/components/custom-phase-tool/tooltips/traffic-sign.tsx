import { useLocalization } from "cs2/l10n";

import TooltipContainer from "../../common/tooltip-container";
import { CustomPhaseSignalState } from "mods/general";

export default function TrafficSign(props: {state: CustomPhaseSignalState}) {
  const { translate } = useLocalization();
  let text = "";
  if (props.state == "go") {
    text = translate("Tooltip.LABEL[C2VM.TrafficLightsEnhancement.TrafficSignGo]") ?? "Go";
  } else if (props.state == "yield") {
    text = translate("Tooltip.LABEL[C2VM.TrafficLightsEnhancement.TrafficSignYield]") ?? "Yield";
  } else if (props.state == "stop") {
    text = translate("Tooltip.LABEL[C2VM.TrafficLightsEnhancement.TrafficSignStop]") ?? "Stop";
  }
  return (
    <TooltipContainer>
      {text}
    </TooltipContainer>
  );
}