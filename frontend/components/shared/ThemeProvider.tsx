"use client";

import { createContext, useContext, type ReactNode } from "react";

/**
 * Dark-first theme. The product is dark-only for the MVP (PLAN §3.1); this provider exists so
 * a light/system option can be added later without touching consumers.
 */
type Theme = "dark";

const ThemeContext = createContext<{ theme: Theme }>({ theme: "dark" });

export function ThemeProvider({ children }: { children: ReactNode }) {
  return (
    <ThemeContext.Provider value={{ theme: "dark" }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  return useContext(ThemeContext);
}
