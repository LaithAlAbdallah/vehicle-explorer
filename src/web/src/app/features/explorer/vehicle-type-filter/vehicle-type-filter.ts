import { Component, input, output } from '@angular/core';
import { VehicleTypeModelCount } from '../../../core/vehicle-contracts';

/** "All" plus one chip per vehicle type, each with its model count. undefined = All. */
@Component({
  selector: 'app-vehicle-type-filter',
  template: `
    <button
      type="button"
      class="chip"
      [attr.aria-pressed]="selectedVehicleType() === undefined"
      (click)="selectedVehicleTypeChange.emit(undefined)"
    >
      All <span class="count">{{ totalModelCount() }}</span>
    </button>
    @for (vehicleType of vehicleTypes(); track vehicleType.name) {
      <button
        type="button"
        class="chip"
        [attr.aria-pressed]="selectedVehicleType() === vehicleType.name"
        (click)="selectedVehicleTypeChange.emit(vehicleType.name)"
      >
        {{ vehicleType.name }} <span class="count">{{ vehicleType.modelCount }}</span>
      </button>
    }
  `,
  styles: `
    :host {
      display: flex;
      gap: var(--space-2);
      overflow-x: auto;
      scrollbar-width: none;
    }

    .count {
      font: 500 0.75rem var(--font-mono);
      opacity: 0.7;
    }
  `,
  host: { role: 'group', 'aria-label': 'Vehicle types' },
})
export class VehicleTypeFilter {
  readonly vehicleTypes = input.required<VehicleTypeModelCount[]>();
  readonly totalModelCount = input.required<number>();
  readonly selectedVehicleType = input<string>();
  readonly selectedVehicleTypeChange = output<string | undefined>();
}
