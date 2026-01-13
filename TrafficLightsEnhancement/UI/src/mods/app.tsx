import { useEffect, useState } from "react";

import engine from "cohtml/cohtml";
import { bindValue, useValue } from "cs2/api";
import mod from "mod.json";

import { CityConfigurationContext, defaultCityConfiguration, LocaleContext } from "./context";
import { defaultLocale } from "./localisations";
import { callKeyPress } from "bindings";

import MainPanel from "./components/main-panel";
import CustomPhaseTool from "./components/custom-phase-tool";

export default function App() {
  const [locale, setLocale] = useState(defaultLocale);

  const localeValue = useValue(bindValue(mod.id, "GetLocale", "{}"));
  const newLocale = JSON.parse(localeValue);
  if (newLocale.locale && newLocale.locale != locale) {
    setLocale(newLocale.locale);
  }

  const cityConfigurationJson = useValue(bindValue(mod.id, "GetCityConfiguration", JSON.stringify(defaultCityConfiguration)));
  const cityConfiguration = JSON.parse(cityConfigurationJson);

  useEffect(() => {
    const keyDownHandler = (event: KeyboardEvent) => {
      if (event.ctrlKey && event.key == "S") {
        callKeyPress(JSON.stringify({ctrlKey: event.ctrlKey, key: event.key}));
      }
    };
    document.addEventListener("keydown", keyDownHandler);
    return () => document.removeEventListener("keydown", keyDownHandler);
  }, []);

  return (
    <CityConfigurationContext.Provider value={cityConfiguration}>
      <LocaleContext.Provider value={locale}>
        <MainPanel />
        <CustomPhaseTool />
      </LocaleContext.Provider>
    </CityConfigurationContext.Provider>
  );
}