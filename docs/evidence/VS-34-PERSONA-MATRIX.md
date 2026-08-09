# VS-34 Persona Certification Matrix

| Demo identity | Account | Operator | Platform admin |
| --- | --- | --- | --- |
| `customer-account` | Allowed | Denied | Denied |
| `approved-account` | Allowed | Allowed | Denied |
| `platform-administrator` | Allowed | Denied unless separately provisioned | Allowed |
| pending operator member | Allowed | Denied until approval | Denied |

A dedicated integration test also proves the pending operator member becomes allowed through the unchanged operator-access endpoint only after the Platform Administrator records an `Approved` lifecycle transition.

Customer phone OTP delivery is outside VS-34 and remains pending provider configuration.
