import { Component, input, output } from '@angular/core';

/** NHTSA has no models before 1981 (checked live). */
export const MinModelYear = 1981;
/** Next year's models are already sold this year. */
export const MaxModelYear = new Date().getFullYear() + 1;

/** A native select: accessible and comfortable on phones without extra code. */
@Component({
  selector: 'app-model-year-picker',
  template: `
    <label class="label" for="model-year">Model year</label>
    <select #select id="model-year" (change)="modelYearChange.emit(+select.value)">
      @for (year of modelYears; track year) {
        <option [value]="year" [selected]="year === modelYear()">{{ year }}</option>
      }
    </select>
  `,
  styles: `
    :host {
      display: grid;
      gap: var(--space-1);
    }

    select {
      height: var(--control-height);
      padding-inline: var(--space-3);
      font: 500 1rem var(--font-mono);
      color: var(--color-text-1);
      background: var(--color-surface-1);
      border: 1px solid var(--color-hairline-strong);
      border-radius: var(--radius);
      cursor: pointer;
    }
  `,
})
export class ModelYearPicker {
  readonly modelYear = input.required<number>();
  readonly modelYearChange = output<number>();

  protected readonly modelYears = Array.from(
    { length: MaxModelYear - MinModelYear + 1 },
    (_, index) => MaxModelYear - index,
  );
}
