# NoorPath.Traveller.Infrastructure

Owns the `traveller` PostgreSQL schema for minimum customer-managed Traveller profiles introduced by VS-07. Other modules reference opaque `TravellerId` values and must not read or mutate this schema directly.
