import { HttpErrorResponse } from '@angular/common/http';
import {
  ApplicationRef,
  Component,
  computed,
  ElementRef,
  inject,
  linkedSignal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { toMakeDisplayName } from '../../core/make-display-name';
import { VehicleApi } from '../../core/vehicle-api';
import { VehicleMake } from '../../core/vehicle-contracts';
import { MakeSearch } from './make-search/make-search';
import { ModelGrid } from './model-grid/model-grid';
import { MaxModelYear, MinModelYear, ModelYearPicker } from './model-year-picker/model-year-picker';
import { VehicleTypeFilter } from './vehicle-type-filter/vehicle-type-filter';

/** Quick picks in the hero; the ids are NHTSA's make ids. */
const PopularMakes: VehicleMake[] = [
  { id: 448, name: 'TOYOTA' },
  { id: 474, name: 'HONDA' },
  { id: 460, name: 'FORD' },
  { id: 452, name: 'BMW' },
  { id: 441, name: 'TESLA' },
  { id: 584, name: 'PORSCHE' },
];

/**
 * The one page: search a make, pick a model year, see its vehicle types and models.
 * The URL holds the state (?makeId=474&modelYear=2015&vehicleType=Motorcycle), so a view can be shared,
 * refreshed, and back/forward works. The page reads the URL and only changes it through navigate().
 */
@Component({
  selector: 'app-explorer-page',
  imports: [MakeSearch, ModelGrid, ModelYearPicker, VehicleTypeFilter],
  templateUrl: './explorer-page.html',
  styleUrl: './explorer-page.scss',
})
export class ExplorerPage {
  private readonly vehicleApi = inject(VehicleApi);
  private readonly router = inject(Router);
  private readonly applicationRef = inject(ApplicationRef);
  private readonly queryParams = toSignal(inject(ActivatedRoute).queryParamMap, {
    requireSync: true,
  });

  protected readonly makeId = computed(() => toPositiveInteger(this.queryParams().get('makeId')));
  protected readonly modelYear = computed(() => {
    const modelYear = toPositiveInteger(this.queryParams().get('modelYear'));
    return modelYear !== undefined && modelYear >= MinModelYear && modelYear <= MaxModelYear
      ? modelYear
      : new Date().getFullYear();
  });
  protected readonly vehicleType = computed(
    () => this.queryParams().get('vehicleType') ?? undefined,
  );

  protected readonly overview = this.vehicleApi.getModelYearOverview(this.makeId, this.modelYear);
  /** The overview, or undefined while loading or failed (reading value() in the error state throws). */
  protected readonly modelYearOverview = computed(() =>
    this.overview.hasValue() ? this.overview.value() : undefined,
  );

  /**
   * The shown make: set at once when a make is picked, then taken from each loaded overview.
   * While another model year of the same make loads, the last known make stays, so the name never flickers.
   */
  private readonly make = linkedSignal<VehicleMake | undefined, VehicleMake | undefined>({
    source: () => this.modelYearOverview()?.make,
    computation: (make, previous) =>
      make ?? (previous && previous.value?.id === this.makeId() ? previous.value : undefined),
  });
  protected readonly makeName = computed(() => {
    const make = this.make();
    return make ? toMakeDisplayName(make.name) : '';
  });

  private readonly makeHeading = viewChild<ElementRef<HTMLElement>>('makeHeading');

  /** Text filter over the model names; cleared when another make is chosen. */
  protected readonly modelFilter = linkedSignal({ source: this.makeId, computation: () => '' });

  protected readonly visibleModels = computed(() => {
    const vehicleType = this.vehicleType();
    const filter = this.modelFilter().trim().toLowerCase();
    return (this.modelYearOverview()?.models ?? [])
      .filter((model) => vehicleType === undefined || model.vehicleTypes.includes(vehicleType))
      .filter((model) => model.name.toLowerCase().includes(filter))
      .sort((first, second) => first.name.localeCompare(second.name));
  });

  protected readonly popularMakes = PopularMakes;
  protected readonly modelYearRange = `${MinModelYear}–${MaxModelYear}`;
  protected readonly toMakeDisplayName = toMakeDisplayName;

  protected selectMake(make: VehicleMake): void {
    this.make.set(make);
    const navigate = async () => {
      await this.navigate({ makeId: make.id, modelYear: this.modelYear(), vehicleType: null });
      // Render now, so the View Transition captures the new layout and not the old one.
      this.applicationRef.tick();
      // The search that had focus is gone (hero) or done (top bar): continue from the make's heading.
      this.makeHeading()?.nativeElement.focus();
    };

    // The signature move: leaving the hero, the search glides up into the top bar (see styles.scss).
    // Browsers without View Transitions, and visitors who ask for reduced motion, simply switch.
    const canGlide =
      document.startViewTransition !== undefined &&
      !matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (this.makeId() === undefined && canGlide) {
      document.startViewTransition(navigate);
    } else {
      void navigate();
    }
  }

  protected selectModelYear(modelYear: number): void {
    void this.navigate({ modelYear });
  }

  protected selectVehicleType(vehicleType: string | undefined): void {
    void this.navigate({ vehicleType: vehicleType ?? null });
  }

  protected goHome(): void {
    void this.router.navigate([], { queryParams: {} });
  }

  /** The API's ProblemDetails detail (a full sentence for the user), else a general message. */
  protected describeError(error: unknown): string {
    const problem = error instanceof HttpErrorResponse ? error.error : undefined;
    return problem?.detail ?? problem?.title ?? 'Something went wrong. Please try again.';
  }

  /** An unknown make id (for example an old shared link): retrying cannot help. */
  protected isNotFound(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 404;
  }

  /** Changes the given query parameters and keeps the others; null removes one. */
  private navigate(queryParams: Params): Promise<boolean> {
    return this.router.navigate([], { queryParams, queryParamsHandling: 'merge' });
  }
}

function toPositiveInteger(value: string | null): number | undefined {
  const number = Number(value);
  return Number.isInteger(number) && number > 0 ? number : undefined;
}
