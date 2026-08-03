"use client";

import { useEffect, useRef } from "react";

export function useDeferredInitialLoad(load: () => void | Promise<void>): void {
  const loadRef = useRef(load);
  loadRef.current = load;

  useEffect(() => {
    const timeout = window.setTimeout(() => void loadRef.current(), 0);
    return () => window.clearTimeout(timeout);
  }, []);
}
