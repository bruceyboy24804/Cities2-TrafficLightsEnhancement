import { createContext } from "react";

import { CityConfiguration } from "./general";

const defaultCityConfiguration = {
  leftHandTraffic: false
};

const CityConfigurationContext = createContext<CityConfiguration>(defaultCityConfiguration);

export {
  CityConfigurationContext,
  defaultCityConfiguration
};