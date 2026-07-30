"use client";

export default function ErrorState({ reset }: { reset: () => void }) {
  return (
    <main>
      <h1>Operator administration is temporarily unavailable</h1>
      <p>Your account details are safe. Please try again.</p>
      <button onClick={reset} type="button">
        Try again
      </button>
    </main>
  );
}
