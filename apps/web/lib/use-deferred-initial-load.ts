"use client";

import { useEffect, useRef } from "react";

export function useDeferredInitialLoad(load: () => void | Promise<void>): void {
  const loadRef = useRef(load);

  // Keep the latest callback without mutating the ref during render.
  useEffect(() => {
    loadRef.current = load;
  }, [load]);

  useEffect(() => {
    const timeout = window.setTimeout(() => void loadRef.current(), 0);
    return () => window.clearTimeout(timeout);
  }, []);
}
