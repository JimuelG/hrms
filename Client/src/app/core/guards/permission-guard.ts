import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from '../services/permission.service';

export const permissionGuard = (code: string): CanActivateFn => async () => {
  const permissions = inject(PermissionService);
  const router = inject(Router);

  await permissions.ensureLoaded();
  return permissions.can(code) ? true: router.createUrlTree(['/dashboard']);
};
