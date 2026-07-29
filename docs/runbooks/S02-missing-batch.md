# Diagnose a published batch missing from discovery

1. Use the response correlation ID to locate the allowlisted query outcome; never log package content.
2. Confirm the batch is `Published`, its publication time is effective, and its INR price is positive.
3. Confirm the stored operator ID is an explicitly approved non-production test operator.
4. Check the 60-second public cache and retry after expiry.
5. Do not edit audit history or expose the draft. Pausing/revocation is not implemented in S02; use the approved operational control and escalate.
