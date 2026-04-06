import { useEffect } from "react";

import engine from "cohtml/cohtml";
import { bindValue, useValue } from "cs2/api";
import mod from "mod.json";

import { CityConfigurationContext, defaultCityConfiguration } from "./context";
import { callKeyPress } from "bindings";

import MainPanel from "./components/main-panel";
import CustomPhaseTool from "./components/custom-phase-tool";
import { MigrationIssuesModal } from "./components/migration-issues";

export default function App() {
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
      <MainPanel />
      <CustomPhaseTool />
      <MigrationIssuesModal />
    </CityConfigurationContext.Provider>
  );
}