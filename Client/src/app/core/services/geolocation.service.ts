import { Injectable } from '@angular/core';

export interface GeoFix {
  latitude: number;
  longitude: number;
  accuracyMeters: number;
}

export type GeolocationErrorCode = 'unsupported' | 'denied' | 'unavailable' | 'timeout';

export class GeolocationError extends Error {
  constructor(public readonly code: GeolocationErrorCode, message: string) {
    super(message);
    this.name = 'GeolocationError';
  }
}

@Injectable({
  providedIn: 'root',
})
export class GeolocationService {
  getPosition(): Promise<GeoFix> {
    return new Promise((resolve, reject) => {
      if (!('geolocation' in navigator)) {
        reject(new GeolocationError('unsupported', "This browser doesn't support location services."));
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (p) => resolve({
          latitude: p.coords.latitude,
          longitude: p.coords.longitude,
          accuracyMeters: Math.max(p.coords.accuracy, 0.1)
        }),
        (e) => {
          switch (e.code) {
            case e.PERMISSION_DENIED:
                reject(new GeolocationError('denied',
                  'Location access is blocked. Allow location for this site in your browser settings, then try again.'));
                break;
            case e.TIMEOUT:
                reject(new GeolocationError('timeout',
                  'Getting your location took loo long. Move near a window or outdoors and try again.'));
                break;
            default:
                reject(new GeolocationError('unavailable',
                  "Your location couldn't be determined. Check that location services are on and try again."));
          }
        },
        { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
      )
    })
  }
}
