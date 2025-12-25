import { useContext } from "react";

import { LocaleContext } from "../../../context";
import { getString } from "../../../localisations";
import { Tooltip } from "cs2/ui";
import TooltipContainer from "../../common/tooltip-container";

export default function LinkPhase(props: {link: boolean}) {
  const locale = useContext(LocaleContext);
  return getString(locale, props.link ? "LinkPhase" : "UnlinkPhase");
}