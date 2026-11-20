import { httpResource, HttpResourceRef } from '@angular/common/http';
import { Injectable, Signal } from '@angular/core';
import { ModelYearOverview, VehicleMake } from './vehicle-contracts';

const MakeSearchLimit = 10;

/**
 * The backend API as signal-based HTTP resources: each one re-runs its request when its input signals
 * change and exposes value, isLoading and error. Call the methods from a component field
 * (an injection context), so the request is cancelled when the component is destroyed.
 */
@Injectable({ providedIn: 'root' })
export class VehicleApi {
  /** Ranked make search; no request while the search is empty. */
  searchMakes(search: Signal<string>): HttpResourceRef<VehicleMake[]> {
    return httpResource<VehicleMake[]>(
      () => {
        const term = search();
        return term
          ? { url: '/api/makes', params: { search: term, limit: MakeSearchLimit } }
          : undefined;
      },
      { defaultValue: [] },
    );
  }

  /** Vehicle types and models of a make in a model year; no request until a make is chosen. */
  getModelYearOverview(
    makeId: Signal<number | undefined>,
    modelYear: Signal<number>,
  ): HttpResourceRef<ModelYearOverview | undefined> {
    return httpResource<ModelYearOverview>(() => {
      const id = makeId();
      return id === undefined ? undefined : `/api/makes/${id}/model-years/${modelYear()}`;
    });
  }
}
