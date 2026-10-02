import { HttpErrorResponse } from "@angular/common/http";

export function extractApiError(err: HttpErrorResponse): string {
    if (err.error?.message) return err.error.message;

    if (err.error?.errors) {
        const firstField = Object.values(err.error.errors)[0] as string [] | undefined;
        if (firstField?.length) return firstField[0];
    }

    return 'Something went wrong. Please try again.';
}