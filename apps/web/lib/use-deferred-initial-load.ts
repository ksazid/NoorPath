"use client";

import { useEffect } from "react";

export function useDeferredInitialLoad(load: () => void | Promise<void>): void {
  useEffect(() => {
    const timeout = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timeout);
  }, [load]);
}
