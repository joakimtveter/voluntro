# Global permissions, resolved per request

Identity knows only **global** permissions (`{module}.{resource}.{action}`), bundled into Roles. Resource-level rules such as "leader of *this* group" or "guardian of *this* child" belong to the module that owns the resource. A generic scoped-role system in Identity would have to know every module's resources. Permissions are **not** embedded in the JWT. The token carries the user ID and a permission version stamp, and the API resolves permissions per request through a cached `IPermissionProvider`. Revoking someone's rights therefore takes effect immediately instead of up to one token lifetime later, and tokens don't grow as modules are added.

## Considered options

- Permissions as JWT claims: no lookup and friendlier to microservices, but revocation lags and tokens bloat. It can be adopted later by changing only `IPermissionProvider`.
