import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import { VehicleApi } from '../../core/vehicle-api';
import { VehicleMake } from '../../core/vehicle-contracts';

const SearchDebounceMilliseconds = 150;
const MinModelYear = 1981;

/** The one page: search a make, pick a model year, see its vehicle types and models. */
@Component({
  selector: 'app-explorer-page',
  templateUrl: './explorer-page.html',
  styleUrl: './explorer-page.scss',
})
export class ExplorerPage {
  private readonly vehicleApi = inject(VehicleApi);

  /** Next year's models are already sold this year, so the newest model year is current year + 1. */
  protected readonly modelYears = Array.from(
    { length: new Date().getFullYear() + 1 - MinModelYear + 1 },
    (_, index) => new Date().getFullYear() + 1 - index,
  );

  protected readonly searchText = signal('');
  protected readonly selectedMake = signal<VehicleMake | undefined>(undefined);
  protected readonly modelYear = signal(new Date().getFullYear());

  /** Waits until typing pauses, so not every keystroke sends a request. */
  private readonly searchTerm = toSignal(
    toObservable(this.searchText).pipe(
      debounceTime(SearchDebounceMilliseconds),
      map((text) => text.trim()),
      distinctUntilChanged(),
    ),
    { initialValue: '' },
  );

  protected readonly makes = this.vehicleApi.searchMakes(this.searchTerm);
  protected readonly overview = this.vehicleApi.getModelYearOverview(
    computed(() => this.selectedMake()?.id),
    this.modelYear,
  );

  protected readonly isMakeListVisible = computed(
    () => this.selectedMake() === undefined && this.searchTerm() !== '',
  );

  protected onSearchInput(text: string): void {
    this.searchText.set(text);
    this.selectedMake.set(undefined);
  }

  protected selectMake(make: VehicleMake): void {
    this.selectedMake.set(make);
    this.searchText.set(make.name);
  }

  /** Shows the API's ProblemDetails title and detail when there is one. */
  protected describeError(error: unknown): string {
    const problem = error instanceof HttpErrorResponse ? error.error : undefined;
    return problem?.title
      ? [problem.title, problem.detail].filter(Boolean).join(' ')
      : 'Something went wrong. Please try again.';
  }
}
